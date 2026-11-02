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
                steps = Steps();
                checks = 0;
                error = null;
                next = EditorApplication.timeSinceStartup + 2;
                deadline = next + 150;
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

    [MenuItem("Prototype/Validate revised dream pixels and debug")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new Exception("Exit Play mode before validation.");
        RevisionPixelImport.Apply();
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
        EditorSceneManager.OpenScene("Assets/Scenes/Apartment/LivingRoom.unity");
        EditorApplication.isPlaying = true;
    }

    static void Check(bool condition, string message)
    {
        checks++;
        if (!condition)
            throw new Exception(message);
        Debug.Log("NIGHT_ARC_CHECK " + message);
    }

    static void Capture(string name)
    {
        if (Application.isBatchMode)
            return;
        Directory.CreateDirectory("../docs/validation/revision");
        ScreenCapture.CaptureScreenshot("../docs/validation/revision/" + name + ".png");
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
        for(int period=0;period<3;period++)clock.AdvanceTime();
        SceneManager.LoadScene("Balcony");yield return .7f;
        journal=UnityEngine.Object.FindAnyObjectByType<StoryJournal>();Call(journal,"OpenTelescope");Call(journal,"ObserveTelescope");yield return .35f;
        Check(!journal.ObservationAdvanced&&journal.ObservationZoom==1,"First telescope frame is fixed-scale pixel art");Capture("telescope-first-frame");yield return .2f;
        for(int n=0;n<12&&journal.WatchPhase=="FirstLook";n++){journal.AdvanceObservation();yield return .1f;}
        yield return 3f;
        Check(journal.ObservationAdvanced&&journal.ObservationZoom==1,"An hour later swaps the advancing shadow frame without zooming stars");Capture("telescope-second-frame");yield return .2f;
        Call(journal,"CloseTelescope");
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
        Check(GameObject.Find("Floor lamp") == null && UnityEngine.Object.FindAnyObjectByType<InteriorLighting>().LightCount == 1, "Living room has light effect without the intrusive lamp model");
        var tank = GameObject.Find("Aquarium and cabinet").GetComponent<Collider2D>().bounds;
        Place(new Vector2(tank.center.x, tank.min.y - .3f));
        yield return .3f;
        var fish = AquariumEntity.Instance;
        Capture("pixel-fish-map");yield return .2f;
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
        Capture("pixel-fish-day4");yield return .2f;
        for(int day=0;day<3;day++)clock.CompleteSleep();yield return .4f;
        Capture("pixel-double-day7");yield return .2f;
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
        SceneManager.LoadScene("Balcony");
        yield return 6f;
        Check(UnityEngine.Object.FindAnyObjectByType<BalconyTimeView>().UsesOverheadProjection && GameObject.Find("Balcony floor plan") != null, "Balcony and courtyard share a top-down projection");
        var scopeArt = GameObject.Find("Optical tube and metal tripod");
        Check(scopeArt != null && scopeArt.GetComponent<SpriteRenderer>().bounds.min.y > GameObject.Find("Balcony tiles").GetComponent<SpriteRenderer>().bounds.min.y, "Telescope feet remain inside the balcony railing");
        Check(GameObject.Find("Balcony tiles").GetComponent<SpriteRenderer>().bounds.size.y < 2.6f, "Balcony depth is compressed to apartment scale");
        Capture("balcony-overhead");
        yield return .1f;
        SceneManager.LoadScene("Corridor");
        yield return 1.2f;
        Check(GameObject.Find("Stairwell recess") != null && GameObject.Find("Stair riser shadow") != null && GameObject.Find("Stair handrail") != null, "Stairwell has depth, risers, nosing and paired rails");
        Capture("corridor-stairwell");
        yield return .1f;
        Check(error == null, "No game runtime exceptions in revision paths");
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        if (steps == null)
        {
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
        File.WriteAllText("../revision-validation.txt", failure == null ? "PASS: " + checks + " checks; three distinct stamps and hammer/wake handoff; quiet ordinary nights; second-night Bubble; pre-face foil/late failure; small pixel atlas and dialogue portraits; shared melodic ending variations; deduction reward; save-safe debug previews; overhead balcony; solid stairwell. Original journey, album and settings restored." : "FAIL: " + failure);
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
        foreach (var device in UnityEngine.InputSystem.InputSystem.devices.ToArray())
            if (device.name.StartsWith("SevenDayAcceptanceKeyboard"))
                UnityEngine.InputSystem.InputSystem.RemoveDevice(device);
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
                var scene = SessionState.GetString(Key + "scene", "");
                if (!string.IsNullOrEmpty(scene))
                    EditorSceneManager.OpenScene(scene);
            };
    }
}
