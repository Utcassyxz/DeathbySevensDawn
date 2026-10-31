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
public static class VerifyNightArc
{
    const string Key = "NightArcRegression";
    static IEnumerator steps;
    static double next, deadline;
    static string error;
    static int checks;
    static ProgressionState State => (ProgressionState)typeof(GameManager).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameManager.Instance);

    static VerifyNightArc()
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

    [MenuItem("Prototype/Validate fish dreams and scenery")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new Exception("Exit Play mode before validation.");
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
        Directory.CreateDirectory("../docs/validation/night-arc");
        ScreenCapture.CaptureScreenshot("../docs/validation/night-arc/" + name + ".png");
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
        while (GameManager.Instance == null)
            yield return .1f;
        Application.runInBackground = true;
        Ready();
        var game = GameManager.Instance;
        var clock = TimeManager.Instance;
        var audio = ApartmentAudio.Instance;
        Check(audio.Ready && audio.SpecialMusicReady, "Four original scores and original apartment music load");
        audio.PlayDreamKick(1, 3);
        float firstKick = audio.DreamKickGain;
        audio.PlayDreamKick(3, 3);
        Check(audio.DreamKickGain > firstKick && audio.HammerLayerReady, "Mud and crack kicks grow while an audible hammer layer joins");
        int presses = audio.UiPressCount;
        audio.PlayUiPress();
        Check(audio.UiPressCount == presses + 1, "Shared UI press feedback reaches an audio source");
        Check(Resources.Load<Texture2D>("Story/CosmicOccultation") != null && Resources.Load<Texture2D>("Story/BubbleContact") != null, "New telescope and dream illustrations load");
        Check(RadiationRules.Fish(2, false) == FishStage.Ordinary && RadiationRules.Fish(3, false) == FishStage.HumanFace && RadiationRules.Fish(4, false) == FishStage.Speaking, "Fish face and voice have distinct days");
        Check(RadiationRules.Fish(7, false) == FishStage.Replacement && RadiationRules.Fish(7, true) == FishStage.Ordinary, "Foil suppresses human transformation");
        for (int i = 0; i < 3; i++)
            clock.AdvanceTime();
        SceneManager.LoadScene("Balcony");
        yield return .5f;
        var journal = UnityEngine.Object.FindAnyObjectByType<StoryJournal>();
        Call(journal, "OpenTelescope");
        Call(journal, "ObserveTelescope");
        yield return .3f;
        Check(journal.ObservationZoom == 1, "First observation uses the full star field");
        Capture("first-observation");
        yield return .2f;
        for (int i = 0; i < 12 && journal.WatchPhase == "FirstLook"; i++)
        {
            journal.AdvanceObservation();
            yield return .1f;
        }

        yield return .9f;
        Capture("one-hour-later");
        yield return 2f;
        Check(journal.WatchPhase == "Recheck" && journal.ObservationZoom > 1.3f, "After one hour the same cosmic silhouette is visibly larger");
        Capture("larger-silhouette");
        yield return .2f;
        for (int i = 0; i < 12 && journal.WatchPhase == "Recheck"; i++)
        {
            journal.AdvanceObservation();
            yield return .1f;
        }

        Call(journal, "CloseTelescope");
        yield return .2f;
        clock.SleepUntilNextMorning();
        yield return .4f;
        Check(NightDreamSequence.WorldBlocked && !NightDreamSequence.Instance.IsBubble && clock.Days == 1, "First sleep waits inside an ordinary dream");
        Capture("first-night-dream");
        yield return .2f;
        var dream = EndDream();
        while (dream.MoveNext())
            yield return dream.Current;
        yield return .5f;
        Check(clock.Days == 2 && !game.HasLoopAbility, "Ordinary dream reaches day two without return power");
        var bed = GameObject.Find("Bed").GetComponent<SpriteRenderer>().bounds;
        Check(GameObject.FindWithTag("Player").transform.position.x > bed.max.x, "Morning wakes beside the bed");
        for (int i = 0; i < 3; i++)
            clock.AdvanceTime();
        Check(game.RecordAncientGodEncounter() && !game.HasLoopAbility, "Sighting alone cannot grant return");
        int loop = game.LoopCount;
        LoopManager.Instance.RestartLoop();
        Check(game.LoopCount == loop && clock.Days == 2, "No early rollback before Bubble");
        clock.AdvanceTime();
        yield return .5f;
        Check(NightDreamSequence.Instance.IsBubble && !game.HasLoopAbility, "Body sighting selects the fixed Bubble dream");
        Capture("bubble-fragments");
        yield return .3f;
        NightDreamSequence.Instance.Advance();
        yield return .3f;
        Capture("bubble-meeting");
        yield return .2f;
        dream = EndDream();
        while (dream.MoveNext())
            yield return dream.Current;
        yield return .4f;
        Check(game.HasLoopAbility && clock.Days == 3, "Only completed meeting grants return and reaches day three");
        SceneManager.LoadScene("LivingRoom");
        yield return .5f;
        var fish = AquariumEntity.Instance;
        Check(fish.FormVisible, "Day three has a face in the fish");
        Capture("fish-day3");
        yield return .2f;
        clock.SleepUntilNextMorning();
        yield return .3f;
        Check(NightDreamSequence.WorldBlocked && !NightDreamSequence.Instance.IsBubble, "Third night has its own ordinary dream");
        dream = EndDream();
        while (dream.MoveNext())
            yield return dream.Current;
        yield return .4f;
        SceneManager.LoadScene("LivingRoom");
        yield return .4f;
        Call(fish, "OpenDialogue");
        yield return .3f;
        Check(fish.IsTalking, "Day four fish can speak");
        Capture("fish-day4");
        yield return .2f;
        Call(fish, "CloseDialogue");
        for (int i = 0; i < 3; i++)
            clock.CompleteSleep();
        yield return .3f;
        Check(clock.Days == 7 && RadiationRules.Fish(clock.Days, false) == FishStage.Replacement, "Unprotected fish reaches the double stage");
        Capture("fish-day7");
        yield return .2f;
        clock.SleepUntilNextMorning();
        yield return .3f;
        Check(NightDreamSequence.Instance.IsReplacement && !game.HasEnded, "Sleeping beside the double starts the replacement scene");
        Capture("replacement-scene");
        yield return .2f;
        dream = EndDream();
        while (dream.MoveNext())
            yield return dream.Current;
        yield return .6f;
        Check(game.FishReplacementEnding && EndingCollection.Has("fish-replacement") && clock.Days == 7, "Fish replaces Hugo and records ending 03 without day eight");
        Capture("replacement-ending");
        yield return .2f;
        game.StartNewJourney();
        yield return .5f;
        Ready();
        Check(EndingCollection.Has("fish-replacement"), "Beginning a new journey preserves collected endings");
        SceneManager.LoadScene("LivingRoom");
        yield return .4f;
        State.inventory.Add(CultNeighbor.Token);
        var tank = GameObject.Find("Aquarium and cabinet").GetComponent<Collider2D>().bounds;
        Place(new Vector2(tank.center.x, tank.min.y - .30f));
        yield return .3f;
        Check(AquariumEntity.Instance.ThrowFoil() && !State.inventory.Contains(CultNeighbor.Token), "Foil is a manual nearby single-use item");
        Call(AquariumEntity.Instance, "CloseDialogue");
        for (int i = 0; i < 6; i++)
            clock.CompleteSleep();
        yield return .3f;
        Check(game.AquariumSealed && !AquariumEntity.Instance.FormVisible, "Protection persists to day seven");
        clock.CompleteSleep();
        yield return 6f;
        Check(game.WorldEndEnding && !game.FishReplacementEnding, "Unawakened protected Hugo reaches world ending instead of replacement");
        yield return 1f;
        Capture("world-ending");
        yield return .2f;
        game.StartNewJourney();
        yield return .4f;
        Ready();
        SceneManager.LoadScene("Balcony");
        yield return 1f;
        var vista = UnityEngine.Object.FindAnyObjectByType<BalconyTimeView>();
        Check(vista != null && vista.SkyColor.maxColorComponent > .5f, "Balcony dawn has a visible sky independent of camera clear");
        yield return 3f;
        Capture("balcony-dawn");
        yield return .2f;
        clock.AdvanceTime();
        yield return .4f;
        Check(vista.SkyColor.b > .5f, "Balcony noon stays blue and bright");
        yield return 3f;
        Capture("balcony-noon");
        yield return .2f;
        clock.AdvanceTime();
        yield return .3f;
        yield return 3f;
        Capture("balcony-dusk");
        yield return .2f;
        clock.AdvanceTime();
        yield return .4f;
        Check(vista.SkyColor.maxColorComponent < .2f, "Balcony night is distinctly dark");
        yield return 3f;
        Capture("balcony-night");
        yield return .2f;
        SceneManager.LoadScene("Corridor");
        yield return .7f;
        Check(GameObject.Find("neighbor wood door") != null && GameObject.Find("402 welcome mat") != null, "Corridor has consistent door architecture and entrance mat");
        Check(GameObject.Find("Return to Apartment").transform.position.y < 0, "402 exit remains below Hugo");
        Capture("corridor-night");
        yield return .2f;
        SceneManager.LoadScene("LivingRoom");
        yield return 1f;
        Check(UnityEngine.Object.FindAnyObjectByType<InteriorLighting>().LightCount == 1, "Only the visible room lamp emits light; aquarium glow removed");
        Capture("home-lamp-night");
        yield return .2f;
        StoryText.ToggleLanguage();
        Check(StoryText.Current.fishEnding.title == "A Life in Your Place" && StoryText.Current.nightDream.yog.Length == 7, "English covers the revised ending and Bubble exchange");
        StoryText.ToggleLanguage();
        Check(error == null, "No runtime exceptions in the new arc");
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        if (steps == null)
        {
            if (!EditorApplication.isPlaying && File.Exists("../night-arc-request.txt"))
            {
                File.Delete("../night-arc-request.txt");
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
        File.WriteAllText("../night-arc-validation.txt", failure == null ? "PASS: " + checks + " checks; daily dreams; delayed Bubble return gift; third-day face/fourth-day voice/seventh-day replacement; foil protection and ending precedence; permanent ending collection; dawn/noon/dusk/night balcony; corridor architecture; owned night lamps; original music and button feedback; bilingual text. Original journey, collection and settings restored." : "FAIL: " + failure);
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
