// 职责：验证梦境节奏、单提示、日夜配色、两晚观测和七日分支；恢复玩家原存档。
using System;
using System.IO;
using System.Collections;
using System.Reflection;
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
public static class VerifyPresentation
{
    const string Key = "PresentationRegression";
    static IEnumerator steps;
    static double next, deadline;
    static string error;
    static int checks;
    static ProgressionState State => (ProgressionState)typeof(GameManager).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameManager.Instance);

    static VerifyPresentation()
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

    [MenuItem("Prototype/Validate presentation and two-night story")]
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
        Debug.Log("PRESENTATION_CHECK " + message);
    }

    static void Capture(string name)
    {
        if (Application.isBatchMode)
            return;
        Directory.CreateDirectory("../docs/validation/presentation");
        ScreenCapture.CaptureScreenshot("../docs/validation/presentation/" + name + ".png");
    }

    static void Place(Vector2 p)
    {
        var g = GameObject.FindWithTag("Player");
        g.transform.position = p;
        g.GetComponent<Rigidbody2D>().position = p;
        Physics2D.SyncTransforms();
    }

    static void Call(object o, string method) => o.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, null);
    static IEnumerator Steps()
    {
        while (GameManager.Instance == null)
            yield return .1f;
        Application.runInBackground = true;
        var game = GameManager.Instance;
        var clock = TimeManager.Instance;
        var routine = MorningRoutine.Instance;
        if (!Application.isBatchMode)
        {
            var dream = UnityEngine.Object.FindAnyObjectByType<StoryJournal>();
            State.dreamSeen = false;
            Call(dream, "ResetJourney");
            yield return 1.7f;
            Capture("dream-arrival");
            yield return 5.5f;
            Capture("dream-memory");
            yield return .2f;
            Call(dream, "WakeUp");
            yield return 4f;
            Capture("wake-single-goal");
            yield return .2f;
            State.morningStage = 3;
            SceneManager.LoadScene("LivingRoom");
            yield return .5f;
        }

        Check(UnityEngine.Object.FindAnyObjectByType<WorldPromptUI>() != null, "Single prompt presenter installed");
        Check(UnityEngine.Object.FindAnyObjectByType<TimeAtmosphere>() != null, "World atmosphere installed");
        for (int chapter = 0; chapter < 4; chapter++)
        {
            float previous = 0;
            bool smooth = true;
            for (int i = 0; i < 500; i++)
            {
                float opacity = DreamMotion.Opacity(i * .02f, chapter);
                smooth &= opacity >= 0 && opacity <= 1 && Mathf.Abs(opacity - previous) < .05f;
                previous = opacity;
            }

            Check(smooth, "Dream chapter " + chapter + " has gradual bounded fades");
        }

        Check(DreamMotion.WakeOpenness(0) == 0 && DreamMotion.WakeOpenness(3.2f) > .99f, "Organic eyelids open fully before standing");
        Check(TimeAtmosphere.Palette(TimePeriod.Morning).b > TimeAtmosphere.Palette(TimePeriod.Morning).r, "Dawn is blue white");
        Check(TimeAtmosphere.Palette(TimePeriod.Afternoon) == Color.white, "Noon is neutral white");
        Check(TimeAtmosphere.Palette(TimePeriod.Evening).r > TimeAtmosphere.Palette(TimePeriod.Evening).g, "Dusk is warm rose");
        Check(TimeAtmosphere.Palette(TimePeriod.Night).maxColorComponent < .5f, "Night is dark");
        State.morningStage = 2;
        WorldPromptUI.Offer("window", "E window", 99);
        WorldPromptUI.Offer("evidence", "E evidence", 99);
        Check(WorldPromptUI.Pick() == TutorialGate.Reminder && !WorldPromptUI.Pick().Contains("window"), "Neighbor goal wins over every optional prompt");
        State.morningStage = 3;
        var fridge = GameObject.Find("Refrigerator");
        var bounds = fridge.GetComponent<SpriteRenderer>().bounds;
        Place(new Vector2(fridge.transform.position.x, bounds.min.y - .3f));
        yield return .3f;
        Check(routine.OpenFridge(), "Open refrigerator starts intro");
        yield return 1.1f;
        Check(routine.FridgeAnimating && !DayOneSupernovaEvent.Instance.IsPresenting, "TV turns itself on caption has reading time");
        Capture("tv-turns-on");
        yield return 2.2f;
        var news = DayOneSupernovaEvent.Instance;
        Check(news.IsPresenting, "Bulletin follows the TV cue");
        for (int i = 0; i < 30 && news.IsPresenting; i++)
            news.Advance();
        yield return .2f;
        Check(routine.MealPresenting && clock.Period == TimePeriod.Afternoon, "Breakfast lands in noon");
        Capture("sleepy-hugo");
        yield return 5.8f;
        Check(State.journal.Count == 0 && !State.journalUnlocked, "No premature diary after breakfast");
        var sofa = GameObject.Find("Old sofa").GetComponent<SpriteRenderer>().bounds;
        Place(new Vector2(sofa.center.x, sofa.min.y - .3f));
        yield return .2f;
        Check(routine.BeginNap(), "Begin sofa nap");
        yield return 2.8f;
        var tint = UnityEngine.Object.FindAnyObjectByType<TimeAtmosphere>().CurrentTint;
        Check(tint.r > tint.g, "Nap crosses through dusk colour");
        Capture("rose-dusk");
        yield return 2.8f;
        Check(routine.Stage == 6 && clock.Period == TimePeriod.Evening && !State.journalUnlocked, "Nap wakes in evening before diary");
        SceneManager.LoadScene("Bedroom");
        yield return .5f;
        Place(new Vector2(2.7f, -1.1f));
        yield return .2f;
        Check(routine.BeginWriting(), "Desk writing starts");
        yield return 2.3f;
        Check(State.journalUnlocked && State.journal.Count == 4 && clock.Period==TimePeriod.Night, "Writing reaches first night and adds initial information once");
        var journal = UnityEngine.Object.FindAnyObjectByType<StoryJournal>();
        Call(journal, "CloseTelescope");
        yield return .1f;
        SceneManager.LoadScene("Balcony");
        yield return .5f;
        Place(Resources.Load<StoryContent>("StoryContent").telescopePosition);
        Call(journal, "OpenTelescope");
        Call(journal, "ObserveTelescope");
        yield return .3f;
        Check(State.firstNightObjectSeen && !State.ancientGodSeen && State.investigationUnlocked, "First night shows approaching body, no awakening");
        Check(Resources.Load<Texture2D>("Story/ApproachingBody") != null, "Original approaching body illustration loads");
        Capture("first-night-object");
        yield return .2f;
        Check(journal.CollectTelescopeEvidence(), "Explicit recording starts connection lesson");
        Call(journal, "CloseTelescope");
        yield return .1f;
        Check(Investigation.Connect("my-observation-notes", "apartment-telescope", true), "Astronomy inference confirms");
        yield return .2f;
        Check(State.tutorialComplete, "First night completes tutorial");
        WorldPromptUI.Offer("window", "Window", 55);
        WorldPromptUI.Offer("evidence", "Evidence", 70);
        Check(WorldPromptUI.CanActivate("evidence") && !WorldPromptUI.CanActivate("window"), "Visible world prompt owns the matching E action");
        int loops = game.LoopCount;
        LoopManager.Instance.RestartLoop();
        Check(clock.Days == 1 && game.LoopCount == loops, "Unawakened Hugo cannot manually loop");
        clock.SleepUntilNextMorning();
        SceneManager.LoadScene("Corridor");
        yield return 5f;
        Check(clock.Days == 2 && Investigation.Collect("cult-door-note"), "Second-day note can be collected");
        var cult = CultNeighbor.Instance;
        Check(cult.KnockDoor(), "First-day conversation and note permit day-two meeting");
        for (int i = 0; i < 30 && !State.cultTokenOffered; i++)
            cult.Advance();
        yield return 1.1f;
        Check(cult.TakeToken() && cult.HasToken && !State.ancientGodSeen, "Foil pickup does not awaken or reset time");
        for (int i = 0; i < 6 && clock.Days < 7; i++)
            clock.SleepUntilNextMorning();
        Check(clock.Days == 7 && !game.HasEnded, "Seventh morning remains playable");
        clock.AdvanceTime();
        clock.AdvanceTime();
        clock.AdvanceTime();
        Check(clock.Period == TimePeriod.Night && !game.HasEnded, "Seventh night remains playable until its boundary");
        clock.AdvanceTime();
        Check(game.WorldEndEnding && game.HasEnded && game.LoopCount == loops && clock.Days == 7, "Seventh night ends in world destruction, no loop or day eight");
        Check(StoryText.Current.worldEnd.title == "碌碌无为的雨果", "First ending is named as requested");
        yield return 2f;
        Capture("ordinary-hugo-ending");
        yield return 4f;
        game.StartNewJourney();
        yield return .4f;
        State.dreamSeen = State.tutorialComplete = State.journalUnlocked = State.investigationUnlocked = true;
        State.morningStage = 10;
        State.telescopeOnRoof = true;
        journal = UnityEngine.Object.FindAnyObjectByType<StoryJournal>();
        Call(journal, "CloseTelescope");
        clock.SleepUntilNextMorning();
        SceneManager.LoadScene("Rooftop");
        yield return .6f;
        Place(RooftopAccess.TelescopeSpot);
        var roof = RooftopAccess.Instance;
        Check(roof.Observe() && !game.HasSeenAncientGod, "Daylight never triggers the cosmic encounter");
        roof.Close();
        yield return .1f;
        if (!Application.isBatchMode)
        {
            yield return 2.2f;
            Capture("rooftop-dawn");
            yield return .2f;
        }

        clock.AdvanceTime();
        if (!Application.isBatchMode)
        {
            yield return 2.2f;
            Capture("rooftop-noon");
            yield return .2f;
        }

        clock.AdvanceTime();
        if (!Application.isBatchMode)
        {
            yield return 2.2f;
            Capture("rooftop-dusk");
            yield return .2f;
        }

        clock.AdvanceTime();
        yield return 2.2f;
        Check(GameObject.Find("Time sky and Milky Way") != null, "Rooftop has a real time-dependent pixel sky");
        Capture("rooftop-milky-way");
        yield return .2f;
        Check(roof.Observe() && game.HasSeenAncientGod, "Choosing second-night rooftop observation awakens Hugo");
        roof.Close();
        yield return .1f;
        Check(ProgressionSave.Load().ancientGodSeen, "Awakening persists independently from evidence");
        clock.ResetToDayOne();
        for (int i = 0; i < 6; i++)
            clock.SleepUntilNextMorning();
        Check(!game.HasEnded && clock.Days == 7, "Awakened route survives seventh morning");
        clock.SleepUntilNextMorning();
        Check(clock.Days == 1 && game.LoopCount == 1 && !game.HasEnded, "Awakened route alone loops at seven-day boundary");
        StoryText.ToggleLanguage();
        Check(StoryText.Current.telescope.firstNight.Contains("tomorrow") && StoryText.Current.worldEnd.title == "Hugo, Who Did Nothing", "Both languages describe the same two-night branch");
        StoryText.ToggleLanguage();
        var style = new GUIStyle
        {
            font = GameTypography.Current,
            fontSize = 23,
            wordWrap = true
        };
        var pages = new DialoguePages();
        pages.Set(StoryText.Current.telescope.firstNight, style, 900, 108);
        string rebuilt = "";
        do
        {
            Check(style.CalcHeight(new GUIContent(pages.Current), 900) <= 106, "Unified subtitle pages fit");
            rebuilt += pages.Current;
        }
        while (pages.Next());
        Check(rebuilt == StoryText.Current.telescope.firstNight, "Pagination retains all dialogue");
        Check(error == null, "No game runtime errors");
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        if (steps == null)
        {
            if (!EditorApplication.isPlaying && File.Exists("../presentation-request.txt"))
            {
                File.Delete("../presentation-request.txt");
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
        File.WriteAllText("../presentation-validation.txt", failure == null ? "PASS: " + checks + " checks; gradual dream and eyelid envelopes; single prompt priority; dawn/noon/dusk/night; breakfast and nap; diary; first-night approaching body; second-day foil; unawakened seventh-night world ending; voluntary rooftop awakening and loop; bilingual text and subtitle bounds. Original save restored." : "FAIL: " + failure);
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
