// 职责：回归验证教程限制、锡纸封缸、天台变量与第七天终局；备份并恢复玩家存档。
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
public static class VerifyStoryRevision
{
    const string Key = "StoryRevisionRegression";
    static IEnumerator steps;
    static double next, deadline;
    static string error;
    static int checks;
    static ProgressionState State => (ProgressionState)typeof(GameManager).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameManager.Instance);
    static VerifyStoryRevision()
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
                deadline = next + 80;
            }
            if (s == PlayModeStateChange.EnteredEditMode)
                Restore();
        };
        Application.logMessageReceived += (m, s, t) => { if (steps != null && (t == LogType.Error || t == LogType.Exception) && !s.Contains("UnityEditor.Search.SearchDatabase")) error = m; };
    }
    [MenuItem("Prototype/Validate story revision")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new Exception("Exit Play mode before validation.");
        foreach (var suffix in new[] { "", ".bak", ".tmp" })
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
        ProgressionSave.Write(new ProgressionState { dreamSeen = true, morningStage = 2 });
        EditorSceneManager.OpenScene("Assets/Scenes/Apartment/LivingRoom.unity");
        EditorApplication.isPlaying = true;
    }
    static void Check(bool condition, string message)
    {
        checks++;
        if (!condition)
            throw new Exception(message);
        Debug.Log("REVISION_CHECK " + message);
    }
    static void Capture(string name)
    {
        if (Application.isBatchMode)
            return;
        Directory.CreateDirectory("../docs/validation/story-revision");
        ScreenCapture.CaptureScreenshot("../docs/validation/story-revision/" + name + ".png");
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
        Check(!game.HasEnded && !game.HasSeenAncientGod && !game.AquariumSealed, "Clean save has no new route flags");
        foreach (var action in new[] { "window", "bed", "television", "fridge", "telescope", "aquarium", "evidence" })
            Check(TutorialGate.IsBlocked(action), "Neighbor stage blocks " + action);
        Check(!TutorialGate.IsBlocked("neighbor") && !string.IsNullOrEmpty(TutorialGate.Reminder), "Neighbor stage gives narrative reminder");
        var tvObject = GameObject.Find("Television and stand");
        var tv = tvObject.GetComponent<Collider2D>().bounds;
        var playerBody = GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>();
        playerBody.simulated = false;
        Place(new Vector2(tv.center.x, tv.min.y - .35f));
        yield return .4f;
        Check(TutorialGate.NearBlockedProp(playerBody.position), "Blocked prop has an actual proximity reminder");
        Capture("tutorial-reminder");
        yield return .3f;
        playerBody.simulated = true;
        State.morningStage = 3;
        Check(!TutorialGate.IsBlocked("fridge") && TutorialGate.IsBlocked("bed"), "Kitchen stage opens only lesson action");
        State.morningStage = 5;
        Check(!TutorialGate.IsBlocked("sofa") && TutorialGate.IsBlocked("television"), "Sofa stage does not expose TV time advance");
        State.morningStage = 6;
        Check(!TutorialGate.IsBlocked("desk") && TutorialGate.IsBlocked("telescope"), "Desk stage cannot skip to telescope");
        State.morningStage = 7;
        Check(!TutorialGate.IsBlocked("telescope") && TutorialGate.IsBlocked("evidence"), "Observation lesson opens before generic evidence");
        State.tutorialComplete = State.journalUnlocked = State.investigationUnlocked = true;
        State.morningStage = 10;
        Check(!TutorialGate.IsBlocked("window") && !TutorialGate.IsBlocked("aquarium"), "Completed tutorial restores optional interactions");
        clock.SleepUntilNextMorning();
        var tank = GameObject.Find("Aquarium and cabinet");
        Place(tank.transform.position + Vector3.down * .5f);
        yield return .3f;
        Check(!AquariumEntity.Instance.ThrowFoil(), "Cannot seal tank without token");
        State.inventory.Add(CultNeighbor.Token);
        Place(new Vector2(-6, -3));
        Check(!AquariumEntity.Instance.ThrowFoil(), "Token cannot be used remotely");
        Place(tank.transform.position + Vector3.down * .5f);
        yield return .2f;
        Call(AquariumEntity.Instance, "OpenOffer");
        yield return .3f;
        Capture("foil-offer");
        yield return .3f;
        Check(AquariumEntity.Instance.ThrowFoil() && game.AquariumSealed && !State.inventory.Contains(CultNeighbor.Token), "Day two token consumed and seal committed together");
        Check(!AquariumEntity.Instance.ThrowFoil(), "Repeated use cannot consume a second token");
        Call(AquariumEntity.Instance, "CloseDialogue");
        for (int day = 3; day <= 6; day++)
        {
            clock.SleepUntilNextMorning();
            yield return .2f;
            Check(clock.Days == day && !AquariumEntity.Instance.FormVisible, "Sealed tank stays empty on day " + day);
        }
        SceneManager.LoadScene("Bedroom");
        yield return .4f;
        SceneManager.LoadScene("LivingRoom");
        yield return .4f;
        Check(!AquariumEntity.Instance.FormVisible && ProgressionSave.Load().aquariumSealed, "Seal survives room reload and disk round-trip");
        State.knowledgeIds.Add(StoryContent.AncientKnowledgeId);
        State.observedKnowledgeIds.Add(StoryContent.AncientKnowledgeId);
        Check(!game.HasSeenAncientGod, "Legacy balcony evidence cannot count as rooftop encounter");
        int loops = game.LoopCount;
        clock.SleepUntilNextMorning();
        yield return .2f;
        Check(clock.Days==7&&!game.HasEnded,"Day seven remains playable");
        clock.SleepUntilNextMorning();
        Check(clock.Days==7&&game.WorldEndEnding&&game.HasEnded,"Seventh night ends healthy unawakened journey");
        Check(WorldEndSequence.IsPresenting, "Fire and mutation transition starts automatically");
        yield return 2f;
        Capture("world-end-fire");
        yield return .2f;
        clock.AdvanceTime();
        clock.SleepUntilNextMorning();
        LoopManager.Instance.RestartLoop();
        Check(clock.Days == 7 && game.LoopCount == loops, "World ending blocks time and automatic loop reset");
        Check(ProgressionSave.Load().worldEndEnding, "World ending saved independently from aging ending");
        yield return WorldEndSequence.Duration + .2f;
        Check(!WorldEndSequence.IsPresenting, "Transition finishes automatically into ending page");
        Capture("world-end-result");
        yield return .3f;
        game.StartNewJourney();
        yield return .4f;
        Check(!game.HasEnded && !game.AquariumSealed && !game.HasSeenAncientGod, "New journey resets all new route flags");
        State.dreamSeen = State.tutorialComplete = State.journalUnlocked = State.investigationUnlocked = true;
        State.morningStage = 10;
        var journal = UnityEngine.Object.FindAnyObjectByType<StoryJournal>();
        var pageType = typeof(StoryJournal).GetNestedType("Page", BindingFlags.NonPublic);
        typeof(StoryJournal).GetMethod("SetPage", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(journal, new[] { Enum.Parse(pageType, "None") });
        SceneManager.LoadScene("LivingRoom");
        yield return .5f;
        clock.SleepUntilNextMorning();
        clock.SleepUntilNextMorning();
        yield return .3f;
        Check(AquariumEntity.Instance.FormVisible, "Untreated tank still develops day three organism");
        Capture("untreated-day-three");
        yield return .3f;
        State.inventory.Add(CultNeighbor.Token);
        tank = GameObject.Find("Aquarium and cabinet");
        Place(tank.transform.position + Vector3.down * .5f);
        yield return .2f;
        Check(AquariumEntity.Instance.ThrowFoil() && !AquariumEntity.Instance.FormVisible, "Late token use removes existing organism immediately");
        Call(AquariumEntity.Instance, "CloseDialogue");
        Check(game.RecordAncientGodEncounter() && !game.RecordAncientGodEncounter(), "Explicit future rooftop hook is idempotent");
        Check(ProgressionSave.Load().ancientGodSeen, "Rooftop encounter flag persists");
        clock.ResetToDayOne();
        for (int d = 0; d < 6; d++)
            clock.SleepUntilNextMorning();
        Check(clock.Days == 7 && !game.HasEnded, "Actual encounter preserves existing loop route");
        clock.SleepUntilNextMorning();
        Check(clock.Days == 1 && game.LoopCount == 1, "Awakened seven-day boundary still loops");
        Check(game.AquariumSealed, "Seal remains across a loop in this journey");
        StoryText.ToggleLanguage();
        Check(!string.IsNullOrEmpty(StoryText.Current.aquarium.throwFoil) && StoryText.Current.worldEnd.lines.Length == 4, "English catalog contains new interaction and ending");
        StoryText.ToggleLanguage();
        Check(error == null, "No runtime errors");
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        if (steps == null)
        {
            if (!EditorApplication.isPlaying && File.Exists("../story-revision-request.txt"))
            {
                File.Delete("../story-revision-request.txt");
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
        catch (Exception e) { Finish(e.ToString()); }
    }
    static void Finish(string failure)
    {
        steps = null;
        File.WriteAllText("../story-revision-validation.txt", failure == null ? "PASS: " + checks + " checks; tutorial policy, token proximity/consumption, early and late sealing, daily forms, room reload, save round-trip, legacy evidence, healthy day-seven ending, automatic transition, reset and awakened loop. Original player save restored." : "FAIL: " + failure);
        EditorApplication.isPlaying = false;
        Restore();
        if (Application.isBatchMode)
            EditorApplication.Exit(failure == null ? 0 : 1);
    }
    static void Restore()
    {
        if (!SessionState.GetBool(Key, false))
            return;
        SessionState.SetBool(Key, false);
        foreach (var suffix in new[] { "", ".bak", ".tmp" })
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
            EditorApplication.delayCall += () => { var scene = SessionState.GetString(Key + "scene", ""); if (!string.IsNullOrEmpty(scene)) EditorSceneManager.OpenScene(scene); };
    }
}
