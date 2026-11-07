// 职责：验证梦境节奏、单提示、日夜配色、两晚观测和七日分支；恢复玩家原存档。
using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

[InitializeOnLoad]
public static class VerifyRevision
{
    const string Key = "RevisionRegression";
    static IEnumerator steps;
    static double next, deadline;
    static string error;
    static int checks;
    static ProgressionState State => (ProgressionState)typeof(GameManager).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameManager.Instance);

    static VerifyRevision()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += s =>
        {
            if (!SessionState.GetBool(Key, false))
                return;
            if (s == PlayModeStateChange.EnteredPlayMode)
            {
                // Only isolate Unity devices. The user's keyboard in other applications is unaffected.
                var keyboards=UnityEngine.InputSystem.InputSystem.devices.OfType<UnityEngine.InputSystem.Keyboard>().Where(k=>k.enabled).ToArray();
                SessionState.SetString(Key+"PhysicalKeyboards",string.Join("|",keyboards.Select(k=>k.name)));
                foreach(var keyboard in keyboards)UnityEngine.InputSystem.InputSystem.DisableDevice(keyboard);
                UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("SevenDayAcceptanceKeyboardWorld").MakeCurrent();
                steps = SessionState.GetBool(Key + "OnlyCompanions", false) ? Extensions() : DeathCycleAcceptance.Run();
                if(SessionState.GetBool(Key+"OnlySpatial",false)) steps=SpatialApocalypseAcceptance.Run();
                if(SessionState.GetBool(Key+"OnlyDeathCycle",false)) steps=DeathCycleAcceptance.Run();
                if(SessionState.GetBool(Key+"OnlyCity",false)) steps=CityAcceptance.Run();
                if(SessionState.GetBool(Key+"OnlyCity",false))
                {
                    var inputSettings=UnityEngine.InputSystem.InputSystem.settings;
                    SessionState.SetInt(Key+"CityInputFocus",(int)inputSettings.editorInputBehaviorInPlayMode);
                    SessionState.SetInt(Key+"CityInputBackground",(int)inputSettings.backgroundBehavior);
                    inputSettings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    inputSettings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
                    Application.runInBackground=true;
                }
                checks = 0;
                error = null;
                next = EditorApplication.timeSinceStartup + 2;
                deadline = next + 300;
            }

            if (s == PlayModeStateChange.EnteredEditMode)
                Restore();
        };
        Application.logMessageReceived += (m, s, t) =>
        {
            if (steps != null && (t == LogType.Error || t == LogType.Exception) && !s.Contains("UnityEditor.Search.SearchDatabase"))
                error = m;
        };
    }

    [MenuItem("Prototype/Validate death cycle and free debug")]
    public static void RunDeathCycle()
    { SessionState.SetBool(Key+"OnlyDeathCycle",true); Run(); }

    [MenuItem("Prototype/Legacy contracts/Spatial layout before death-cycle revision")]
    public static void RunSpatial()
    {
        SessionState.SetBool(Key+"OnlySpatial",true); Run();
    }

    [MenuItem("Prototype/Legacy contracts/Permanent companion before death-cycle revision")]
    public static void RunCompanions()
    {
        SessionState.SetBool(Key + "OnlyCompanions", true);
        Run();
    }

    [MenuItem("Prototype/Validate seven-day city")]
    public static void RunCity(){ SessionState.SetBool(Key+"OnlyCity",true); Run(); }

    [MenuItem("Prototype/Validate current prototype")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new Exception("Exit Play mode before validation.");
        // A compiler rejection may prevent EnteredPlayMode/EnteredEditMode; restore that backup before taking a new one.
        bool deathMode=SessionState.GetBool(Key+"OnlyDeathCycle",false);
        if(SessionState.GetBool(Key,false)) Restore();
        SessionState.SetBool(Key+"OnlyDeathCycle",deathMode);
        RevisionPixelImport.Apply();
        CompanionArtImport.Apply();
        foreach (var suffix in new[]
        {
            "",
            ".bak",
            ".tmp"
        }

        )
        {
            var p = ProgressionSave.PathName + suffix;
            SessionState.SetString(Key + suffix, File.Exists(p) ? Convert.ToBase64String(File.ReadAllBytes(p)) : "");
        }

        foreach (var suffix in new[]
        {
            "",
            ".bak",
            ".tmp"
        }

        )
        {
            string albumPath = EndingCollection.PathName + suffix;
            SessionState.SetString(Key + "album" + suffix, File.Exists(albumPath) ? Convert.ToBase64String(File.ReadAllBytes(albumPath)) : "");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(EndingCollection.PathName));
        File.WriteAllText(EndingCollection.PathName, "{\"ids\":[]}");
        EndingCollection.Reload();
        SessionState.SetInt(Key + "lang", PlayerPrefs.GetInt("Story.Language", 0));
        SessionState.SetBool(Key + "suppressed", SessionState.GetBool(DayOneSupernovaEvent.SuppressionKey, false));
        SessionState.SetBool(Key + "background", PlayerSettings.runInBackground);
        SessionState.SetString(Key + "scene", SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key, true);
        PlayerPrefs.SetInt("Story.Language", 0);
        SessionState.SetBool(DayOneSupernovaEvent.SuppressionKey, true);
        ProgressionSave.Write(new ProgressionState { dreamSeen = true, morningStage = 3, cultDayOneTalked = true, neighborBarricadeSeen = true });
        EditorSceneManager.OpenScene("Assets/Apartment/LivingRoom.unity");
        EditorApplication.isPlaying = true;
    }

    public static void Check(bool condition, string message)
    {
        checks++;
        if (!condition)
            throw new Exception(message);
        Debug.Log("NIGHT_ARC_CHECK " + message);
    }

    public static void Capture(string name)
    {
        if (Application.isBatchMode)
            return;
        Directory.CreateDirectory("../docs/validation/death-cycle-2026-11-02");
        ScreenCapture.CaptureScreenshot("../docs/validation/death-cycle-2026-11-02/" + name + ".png");
    }

    static void Place(Vector2 p)
    {
        var g = GameObject.FindWithTag("Player");
        g.transform.position = p;
        g.GetComponent<Rigidbody2D>().position = p;
        Physics2D.SyncTransforms();
    }

    static void Call(object o, string method) => o.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, null);
    static void Ready()
    {
        State.dreamSeen = State.tutorialComplete = State.journalUnlocked = State.investigationUnlocked = true;
        State.morningStage = 10;
        State.cultDayOneTalked = State.neighborBarricadeSeen = true;
        Call(UnityEngine.Object.FindAnyObjectByType<StoryJournal>(), "CloseTelescope");
    }

    static IEnumerator EndDream()
    {
        for (int i = 0; i < 40 && NightDreamSequence.WorldBlocked; i++)
        {
            NightDreamSequence.Instance.Advance();
            yield return .22f;
        }
    }

    static IEnumerator Steps()
    {
        yield return .7f;
        Ready();
        var game = GameManager.Instance;
        var clock = TimeManager.Instance;
        var audio = ApartmentAudio.Instance;
        StoryDebugActions.SkipForcedStory();
        yield return .2f;
        State.dreamSeen = false;
        State.morningStage = 0;
        var journal = UnityEngine.Object.FindAnyObjectByType<StoryJournal>();
        yield return .4f;
        Check(journal.OpeningDreamVisible, "Opening dream is available before waking");
        journal.AdvanceOpeningDream();
        yield return .4f;
        journal.AdvanceOpeningDream();
        yield return .4f;
        int impact = audio.DreamImpactCount;
        for (int beat = 1; beat <= 3; beat++)
        {
            journal.AdvanceOpeningDream();
            yield return .38f;
            Check(journal.OpeningKickCount == beat && audio.DreamImpactCount == impact + beat, "One Enter produces exactly one impact " + beat);
            Check(Mathf.Approximately(journal.OpeningDarkness, beat / 3f), "Each stamp darkens the image " + beat);
            Capture("opening-stamp-" + beat);
            yield return .1f;
        }

        Check(audio.LastDreamImpact == 3 && audio.HammerLayerReady, "Third impact is the dry hammer handoff");
        yield return .65f;
        Check(!journal.OpeningDreamVisible && MorningRoutine.Instance.IsWaking, "Third stamp automatically hands off to blinking wake");
        Capture("wake-handoff");
        yield return .1f;
        StoryDebugActions.SkipForcedStory();
        yield return .2f;
        Check(State.tutorialComplete && State.morningStage == 10 && !MorningRoutine.WorldBlocked, "Debug skip restores free exploration");
        clock.ResetToDayOne();
        for (int period = 0; period < 3; period++)
            clock.AdvanceTime();
        SceneManager.LoadScene("Balcony");
        yield return .7f;
        journal = UnityEngine.Object.FindAnyObjectByType<StoryJournal>();
        Call(journal, "OpenTelescope");
        Call(journal, "ObserveTelescope");
        yield return .35f;
        Check(!journal.ObservationAdvanced && journal.ObservationZoom == 1, "First telescope frame is fixed-scale pixel art");
        Capture("telescope-first-frame");
        yield return .2f;
        for (int n = 0; n < 12 && journal.WatchPhase == "FirstLook"; n++)
        {
            journal.AdvanceObservation();
            yield return .1f;
        }

        yield return 3f;
        Check(journal.ObservationAdvanced && journal.ObservationZoom == 1, "An hour later swaps the advancing shadow frame without zooming stars");
        Capture("telescope-second-frame");
        yield return .2f;
        Call(journal, "CloseTelescope");
        clock.SleepUntilNextMorning();
        yield return .8f;
        Check(clock.Days == 2 && !NightDreamSequence.WorldBlocked && !game.HasLoopAbility, "First ordinary night has no repeat dream");
        SceneManager.LoadScene("LivingRoom");
        yield return .4f;
        var earlyTank = GameObject.Find("Aquarium and cabinet").GetComponent<Collider2D>().bounds;
        Place(new Vector2(earlyTank.center.x, earlyTank.min.y - .3f));
        State.inventory.Add(CultNeighbor.Token);
        yield return .3f;
        Check(AquariumEntity.Instance.ThrowFoil() && game.AquariumSealed, "Day-two foil prevents the first human face");
        Call(AquariumEntity.Instance, "CloseDialogue");
        State.aquariumSealed = false;
        for (int i = 0; i < 3; i++)
            clock.AdvanceTime();
        game.RecordAncientGodEncounter();
        clock.SleepUntilNextMorning();
        yield return 2.6f;
        Check(NightDreamSequence.Instance.IsBubble && clock.Days == 2, "Only the second night opens Bubble after the sighting");
        Capture("bubble-grand-entry");
        yield return .1f;
        var dream = EndDream();
        while (dream.MoveNext())
            yield return dream.Current;
        yield return .7f;
        Check(clock.Days == 3 && game.HasLoopAbility, "Completing Bubble grants return once");
        SceneManager.LoadScene("LivingRoom");
        yield return .6f;
        Check(RadiationRules.CanSeal(2) && !RadiationRules.CanSeal(3) && !RadiationRules.CanSeal(4), "Foil works only before the first human face");
        Check(Resources.Load<Texture2D>("Story/FishMapPixels").width <= 128, "Map fish use a genuinely small pixel atlas");
        Check(GameObject.Find("Floor lamp") == null && UnityEngine.Object.FindAnyObjectByType<InteriorLighting>().LightCount == 0, "Living room contains neither lamp model nor orphan light pool");
        var tank = GameObject.Find("Aquarium and cabinet").GetComponent<Collider2D>().bounds;
        Place(new Vector2(tank.center.x, tank.min.y - .3f));
        yield return .3f;
        var fish = AquariumEntity.Instance;
        Capture("pixel-fish-map");
        yield return .2f;
        Call(fish, "OpenDialogue");
        yield return .4f;
        Capture("pixel-fish-detailed-dialogue");
        yield return .1f;
        Call(fish, "CloseDialogue");
        State.inventory.Add(CultNeighbor.Token);
        Check(fish.ThrowFoil() && !game.AquariumSealed && !State.inventory.Contains(CultNeighbor.Token), "Day-three foil is too late and cannot reverse the face");
        Call(fish, "CloseDialogue");
        State.aquariumSealed = false;
        clock.SleepUntilNextMorning();
        yield return .8f;
        Check(clock.Days == 4 && !NightDreamSequence.WorldBlocked, "Third night has no fish dream");
        SceneManager.LoadScene("LivingRoom");
        yield return .4f;
        tank = GameObject.Find("Aquarium and cabinet").GetComponent<Collider2D>().bounds;
        Place(new Vector2(tank.center.x, tank.min.y - .3f));
        State.inventory.Add(CultNeighbor.Token);
        yield return .3f;
        Check(AquariumEntity.Instance.ThrowFoil() && !game.AquariumSealed && !State.inventory.Contains(CultNeighbor.Token), "Late foil is consumed but cannot reverse the transformation");
        Call(AquariumEntity.Instance, "CloseDialogue");
        Capture("pixel-fish-day4");
        yield return .2f;
        for (int day = 0; day < 3; day++)
            clock.CompleteSleep();
        yield return .4f;
        Capture("pixel-double-day7");
        yield return .2f;
        string before = JsonUtility.ToJson(State);
        int album = EndingCollection.Count;
        for (int ending = 1; ending <= 3; ending++)
        {
            StoryDebugActions.PreviewEnding(ending);
            yield return 1.2f;
            Check(EndingCollectionUI.PreviewOpen && JsonUtility.ToJson(State) == before && EndingCollection.Count == album, "Ending preview never changes save or album " + ending);
            Capture("ending-preview-" + ending);
            yield return .1f;
            EndingCollectionUI.ClosePreview();
        }

        var theme = Resources.Load<TextAsset>("Audio/EndingTheme");
        Check(theme != null && theme.text.Contains("112") && theme.text.Contains("64") && theme.text.Contains("84"), "All three endings share the same authored melody with three tempos");
        Check(Resources.Load<AudioClip>("Audio/EndingGood").length < Resources.Load<AudioClip>("Audio/EndingBad").length, "Good variation is faster than bad with the same phrase count");
        Check(Resources.Load<AudioClip>("Audio/UiPress").length < .06f, "Button click is a short crisp transient");
        Check(Resources.Load<AudioClip>("Audio/DeductionSuccess") != null && Resources.Load<AudioClip>("Audio/EndingCurtain") != null, "Deduction and theatrical ending cues exist");
        int chimes = audio.DeductionCount;
        State.investigationUnlocked = true;
        var rule = Investigation.Catalog.deductions.First(d => Investigation.Evidence(d.left) != null && Investigation.Evidence(d.right) != null && !Investigation.Has(d.id));
        Investigation.Collect(rule.left);
        Investigation.Collect(rule.right);
        Check(Investigation.Connect(rule.left, rule.right, true) && audio.DeductionCount == chimes + 1, "A new valid deduction plays exactly one cheerful cue");
        Investigation.Connect(rule.left, rule.right, true);
        Check(audio.DeductionCount == chimes + 1, "An existing conclusion cannot replay the reward cue");
        clock.ResetToDayOne();
        clock.AdvanceTime();
        SceneManager.LoadScene("Balcony");
        yield return 6f;
        Check(UnityEngine.Object.FindAnyObjectByType<BalconyTimeView>().UsesOverheadProjection && GameObject.Find("Balcony floor plan") != null, "Balcony uses a consistent overhead pixel courtyard");
        var scopeArt = GameObject.Find("Optical tube and metal tripod");
        Check(scopeArt != null && scopeArt.GetComponent<SpriteRenderer>().bounds.min.y > GameObject.Find("Balcony tiles").GetComponent<SpriteRenderer>().bounds.min.y, "Telescope feet remain inside the balcony railing");
        Check(GameObject.Find("Balcony tiles").GetComponent<SpriteRenderer>().bounds.size.y < 2.6f, "Balcony depth is compressed to apartment scale");
        Capture("balcony-overhead");
        yield return .1f;
        SceneManager.LoadScene("Corridor");
        yield return 1.2f;
        Check(GameObject.Find("EXIT stairwell door") != null && GameObject.Find("Stairwell recess") == null, "Corridor leads through EXIT instead of a staircase drawn in the hall");
        var roof = RooftopAccess.Instance;
        Place(RooftopAccess.StairsFour + Vector2.right * .65f);
        yield return .2f;
        Capture("corridor-exit");
        yield return .2f;
        Check(roof.UseStairs(), "EXIT enters the separate stairwell");
        yield return .8f;
        Check(SceneManager.GetActiveScene().name == "Stairwell" && roof.CurrentFloor == 4, "Stairwell opens at floor four");
        Check(GameObject.Find("Up tread").transform.root.localScale.x < .7f && Camera.main.orthographicSize < 3, "Compact stairwell is authored at the smaller intended scale");
        Check(GameObject.Find("Up tread") != null && GameObject.Find("Down tread") != null && GameObject.Find("Up riser") != null && GameObject.Find("Down riser") != null, "Two opposed modeled flights have separate risers and nosing");
        var doorLeaf = GameObject.Find("Door leaf collision").GetComponent<BoxCollider2D>();
        Check(RooftopAccess.LandingDoor.y > doorLeaf.bounds.max.y && GameObject.Find("Fire door").transform.position.y < -1.6f && Mathf.Abs(GameObject.Find("Fire door").transform.position.x-RooftopAccess.LandingDoor.x) < .05f, "Fire door is embedded at the south approach, clear of both flights");
        var actor = GameObject.FindWithTag("Player");
        bool clear = true;
        foreach (var point in new[]
        {
            RooftopAccess.LandingDoor + Vector2.up * .50f,
            RooftopAccess.StairUp,
            RooftopAccess.StairDown,
            new Vector2(0, 0)
        }

        )
            foreach (var hit in Physics2D.OverlapCircleAll(point, .32f))
                if (!hit.isTrigger && hit.gameObject != actor)
                    clear = false;
        Check(clear, "Door arrival and both stair approaches have player-width clearance");
        Capture("stairwell-four");
        yield return .2f;
        Place(RooftopAccess.StairUp);
        Check(roof.UseStairs() && roof.CurrentFloor == 5, "Upper flight reaches floor five");
        yield return .8f;
        Check(!roof.ChangeFloor(1), "Top floor does not exceed the roof");
        Place(RooftopAccess.LandingDoor);
        Check(roof.UseStairs(), "Fifth-floor fire door opens to roof");
        yield return .8f;
        Check(SceneManager.GetActiveScene().name == "Rooftop", "Roof uses a real separate destination");
        Place(RooftopAccess.StairsFive);
        Check(roof.UseStairs(), "Roof returns through its stairwell door");
        yield return .8f;
        for (int floor = 4; floor >= 1; floor--)
        {
            Place(RooftopAccess.StairDown);
            Check(roof.UseStairs() && roof.CurrentFloor == floor, "Lower flight reaches floor " + floor);
            yield return .8f;
        }

        Check(!roof.ChangeFloor(-1), "Ground floor cannot descend out of prototype");
        Place(RooftopAccess.LandingDoor);
        Check(roof.UseStairs() && roof.IsOpen, "Unbuilt lower apartment wings clearly remain locked");
        roof.Close();
        yield return .2f;
        for (int floor = 2; floor <= 4; floor++)
        {
            Check(roof.ChangeFloor(1), "Return to floor " + floor);
            yield return .8f;
        }

        Place(RooftopAccess.LandingDoor);
        Check(roof.UseStairs(), "Fourth-floor fire door returns to corridor");
        yield return .8f;
        Check(SceneManager.GetActiveScene().name == "Corridor", "Travel loop preserves the 402 corridor");
        // Second-night scope: no balcony inspection path; single carry action then roof placement.
        clock.ResetToDayOne();
        clock.CompleteSleep();
        clock.AdvanceTime();
        clock.AdvanceTime();
        clock.AdvanceTime();
        State.telescopeCarried = State.telescopeOnRoof = false;
        SceneManager.LoadScene("Balcony");
        yield return .8f;
        var scope = GameObject.Find("阳台望远镜");
        Place((Vector2)scope.transform.position + Vector2.down * .45f);
        yield return .3f;
        Check(!journal.OpenTelescopeFromWorld(), "Second night never offers balcony observation");
        Check(roof.TakeTelescope() && State.telescopeCarried, "Second-night telescope is taken for the roof");
        roof.Close();
        yield return .2f;
        Check(GameObject.Find("阳台望远镜") == null, "Carried telescope disappears from balcony");
        SceneManager.LoadScene("Rooftop");
        yield return .8f;
        Place(RooftopAccess.TelescopeSpot);
        Check(roof.PlaceTelescope() && State.telescopeOnRoof && !State.telescopeCarried, "Same carried telescope installs on rooftop");
        roof.Close();
        Check(Resources.Load<AudioClip>("Audio/EndingGood").length > 100 && Resources.Load<AudioClip>("Audio/EndingBad").length > 180 && Resources.Load<AudioClip>("Audio/EndingTrue").length > 135, "Endings contain four developing statements and a full cadence");
        Check(theme.text.Contains("sharedHarmony") && theme.text.Contains("brass melody") && theme.text.Contains("march snare"), "Shared harmony is reorchestrated with strings and marching-band layers");
        Capture("rooftop-after-placement");
        yield return .1f;
        var extension = Extensions();
        while (extension.MoveNext())
            yield return extension.Current;
        Check(error == null, "No game runtime exceptions in revision paths");
    }

    static IEnumerator Extensions()
    {
        var extension = DoorMorningAcceptance.Corridor();
        while (extension.MoveNext())
            yield return extension.Current;
        extension = CompanionAcceptance.Run();
        while (extension.MoveNext())
            yield return extension.Current;
        extension = ContinuityAcceptance.Run();
        while (extension.MoveNext())
            yield return extension.Current;
        extension = SpatialApocalypseAcceptance.Run();
        while(extension.MoveNext()) yield return extension.Current;
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        if (steps == null)
        {
            if(!EditorApplication.isPlaying && File.Exists("../death-cycle-request.txt"))
            { File.Delete("../death-cycle-request.txt"); RunDeathCycle(); return; }
            if(!EditorApplication.isPlaying && File.Exists("../spatial-request.txt"))
            { File.Delete("../spatial-request.txt"); RunSpatial(); return; }
            if (!EditorApplication.isPlaying && File.Exists("../companions-request.txt"))
            {
                File.Delete("../companions-request.txt");
                RunCompanions();
                return;
            }
            if (!EditorApplication.isPlaying && File.Exists("../revision-request.txt"))
            {
                File.Delete("../revision-request.txt");
                Run();
            }

            return;
        }

        if (EditorApplication.timeSinceStartup < next)
            return;
        try
        {
            if (error != null)
                throw new Exception(error);
            if (EditorApplication.timeSinceStartup > deadline)
                throw new Exception("Timed out");
            if (steps.MoveNext())
                next = EditorApplication.timeSinceStartup + (float)steps.Current;
            else
                Finish(null);
        }
        catch (Exception e)
        {
            Finish(e.ToString());
        }
    }

    static void Finish(string failure)
    {
        steps = null;
        string scope = SessionState.GetBool(Key + "OnlyCompanions", false) ? "focused companion and world checks; seated writing, curtain alignment, seven-day pixels, fish understanding, Bubble reveal and shop, SAN exchange, v1/v2/v3/v4 save compatibility; walked doors, terminal stair floors, permanent seated fish and repeat chats, automatic Bubble, stove textures. Original journey, album and settings restored." : "three distinct stamps and hammer/wake handoff; quiet ordinary nights; observation-night Bubble and later-loop market; pre-face foil/late failure; small pixel atlas and dialogue portraits; shared melodic ending variations; deduction reward; save-safe debug previews; coarse balcony with seven-day exterior; dedicated two-flight stairwell and floor travel; second-night carry-only scope; extended shared-harmony orchestrations; seated writing; night curtains; seven radiation views; fish understanding and investigation; unknown/revealed Bubble; SAN exchange and v4 save migration; physically walked bedroom and bathroom doors; terminal-floor flight removal; persistent seated fish; repeat chat shuffle bag; automatic later-loop Bubble; stove on/off texture swap. Original journey, album and settings restored.";
        if(SessionState.GetBool(Key+"OnlySpatial",false)) scope="focused spatial and first-apocalypse checks; real note collection and knocking, modal EXIT suppression, south stairwell entrance, destruction/reunion/once-only rewind, saved checkpoints and unawakened ending preserved. Original journey, album and settings restored.";
        else scope += " South entrance, wall-side stool and aquarium push, neighbour knocks and first awakened midnight reunion verified.";
        if(!SessionState.GetBool(Key+"OnlyCompanions",false)&&!SessionState.GetBool(Key+"OnlySpatial",false)) scope="death-only returns, sleep debt and in-place waiting, accelerated per-life fish, right EXIT, consolidated scenes and free debug; original journey/album/settings restored.";
        if(SessionState.GetBool(Key+"OnlyCity",false)) scope="seven-day city integration: evidence, deductions, interventions, fish reachability, AI, defense, checkpoint and legacy migration; journey/album/settings restored.";
        File.WriteAllText(SessionState.GetBool(Key+"OnlyCity",false)?"../city-validation.txt":"../revision-validation.txt", failure == null ? "PASS: " + checks + " checks; " + scope : "FAIL: " + failure);
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode)
        {
            Restore();
            EditorApplication.Exit(failure == null ? 0 : 1);
        }
    }

    static void Restore()
    {
        if (!SessionState.GetBool(Key, false))
            return;
        SessionState.SetBool(Key, false);
        SessionState.SetBool(Key + "OnlyCompanions", false);
        SessionState.SetBool(Key+"OnlySpatial",false);
        SessionState.SetBool(Key+"OnlyDeathCycle",false);
        if(SessionState.GetBool(Key+"OnlyCity",false))
        {
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Key+"CityInputFocus",0);
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=(UnityEngine.InputSystem.InputSettings.BackgroundBehavior)SessionState.GetInt(Key+"CityInputBackground",0);
        }
        SessionState.SetBool(Key+"OnlyCity",false);
        foreach (var device in UnityEngine.InputSystem.InputSystem.devices.ToArray())
            if (device.name.StartsWith("SevenDayAcceptanceKeyboard"))
                UnityEngine.InputSystem.InputSystem.RemoveDevice(device);
        var keyboardNames=SessionState.GetString(Key+"PhysicalKeyboards","").Split('|');
        foreach(var keyboard in UnityEngine.InputSystem.InputSystem.devices.OfType<UnityEngine.InputSystem.Keyboard>())
            if(keyboardNames.Contains(keyboard.name)){UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();}
        foreach (var suffix in new[]
        {
            "",
            ".bak",
            ".tmp"
        }

        )
        {
            var path = ProgressionSave.PathName + suffix;
            var bytes = SessionState.GetString(Key + suffix, "");
            if (bytes.Length > 0)
                File.WriteAllBytes(path, Convert.FromBase64String(bytes));
            else if (File.Exists(path))
                File.Delete(path);
        }

        foreach (var suffix in new[]
        {
            "",
            ".bak",
            ".tmp"
        }

        )
        {
            string albumPath = EndingCollection.PathName + suffix;
            string saved = SessionState.GetString(Key + "album" + suffix, "");
            if (saved.Length > 0)
                File.WriteAllBytes(albumPath, Convert.FromBase64String(saved));
            else if (File.Exists(albumPath))
                File.Delete(albumPath);
        }

        EndingCollection.Reload();
        PlayerPrefs.SetInt("Story.Language", SessionState.GetInt(Key + "lang", 0));
        PlayerPrefs.Save();
        SessionState.SetBool(DayOneSupernovaEvent.SuppressionKey, SessionState.GetBool(Key + "suppressed", false));
        Application.runInBackground = SessionState.GetBool(Key + "background", false);
        PlayerSettings.runInBackground = SessionState.GetBool(Key + "background", false);
        if (!Application.isBatchMode)
            EditorApplication.delayCall += () =>
            {
                var scene = SessionState.GetString(Key + "scene", "").Replace("Assets/Scenes/Apartment/","Assets/Apartment/");
                if (!string.IsNullOrEmpty(scene))
                    EditorSceneManager.OpenScene(scene);
            };
    }
}
