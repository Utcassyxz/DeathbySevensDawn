// 职责：编辑器回归检查：InitialState；修改功能后从 Prototype 菜单运行。
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.TimeSystem;
[InitializeOnLoad]
public static class VerifyInitialState
{
    const string KeyName = "VerifyInitialState";
    static string runtimeError; static int stage, scenario; static double next, deadline; static Keyboard keyboard; static bool background;
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static VerifyInitialState()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (message, trace, type) => { if (SessionState.GetBool(KeyName, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) runtimeError = message; };
    }
    [MenuItem("Prototype/Test complete initial-state reset")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play first.");
        SessionState.SetBool(DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.SuppressionKey, true);
        foreach (string suffix in new[] { "", ".bak", ".tmp" })
            SessionState.SetString(KeyName + suffix, File.Exists(ProgressionSave.PathName + suffix) ? Convert.ToBase64String(File.ReadAllBytes(ProgressionSave.PathName + suffix)) : "");
        ProgressionSave.Write(new ProgressionState());
        EditorSceneManager.OpenScene("Assets/Scenes/Apartment/Bedroom.unity");
        runtimeError = null;
        stage = scenario = 0;
        next = deadline = 0;
        SessionState.SetBool(KeyName, true);
        EditorApplication.isPlaying = true;
    }
    static object Read(object item, string name) => item.GetType().GetField(name, Flags).GetValue(item);
    static void Set(object item, string name, object value) => item.GetType().GetField(name, Flags).SetValue(item, value);
    static void Call(object item, string name) => item.GetType().GetMethod(name, Flags).Invoke(item, null);
    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
    static void Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
    }
    static void Release()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
    }
    static void Tick()
    {
        if (!SessionState.GetBool(KeyName, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || GameManager.Instance == null)
            return;
        if (runtimeError != null)
        {
            Finish(false, runtimeError);
            return;
        }
        double now = EditorApplication.timeSinceStartup;
        if (deadline == 0)
            deadline = now + 85;
        if (now > deadline)
        {
            Finish(false, "Timeout " + stage + "/" + scenario);
            return;
        }
        if (now < next)
            return;
        try
        {
            var game = GameManager.Instance;
            var clock = TimeManager.Instance;
            var story = UnityEngine.Object.FindAnyObjectByType<StoryJournal>();
            var hud = UnityEngine.Object.FindAnyObjectByType<ProgressionHUD>();
            var feedback = UnityEngine.Object.FindAnyObjectByType<GameplayFeedback>();
            switch (stage)
            {
                case 0:
                    background = Application.runInBackground;
                    Application.runInBackground = true;
                    keyboard = InputSystem.AddDevice<Keyboard>("InitialStateAcceptanceKeyboard");
                    keyboard.MakeCurrent();
                    Set(story, "dreamPage", 2);
                    KnowledgeManager.Instance.Unlock("reset-old-progress", KnowledgeTier.Cosmic);
                    Call(hud, "RestoreInitialState");
                    break;
                case 1:
                    CheckFresh(game, clock, story, feedback);
                    Call(story, "WakeUp");
                    break;
                case 2:
                    var advanced = (ProgressionState)typeof(GameManager).GetProperty("State", Flags).GetValue(game);
                    advanced.journalUnlocked = advanced.investigationUnlocked = advanced.tutorialComplete = true;
                    Check(!StoryJournal.WorldBlocked && GameObject.FindWithTag("Player").GetComponent<DeathBySeventhsDawn.Player.PlayerController2D>().enabled, "Wake restores controls");
                    if (scenario == 0)
                        Press(Key.J);
                    if (scenario == 1)
                        SceneManager.LoadScene("Corridor");
                    if (scenario == 2)
                    {
                        clock.SleepUntilNextMorning();
                        clock.AdvanceTime();
                        clock.AdvanceTime();
                        SceneManager.LoadScene("Balcony");
                    }
                    if (scenario == 3)
                        SceneManager.LoadScene("LivingRoom");
                    if (scenario == 4)
                    {
                        var window = UnityEngine.Object.FindAnyObjectByType<WindowLookout2D>();
                        var body = GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>();
                        body.position = window.transform.position;
                        body.transform.position = window.transform.position;
                        Physics2D.SyncTransforms();
                        Check(window.TryOpen(), "Window opens");
                    }
                    if (scenario == 5)
                    {
                        var state = (ProgressionState)typeof(GameManager).GetProperty("State", Flags).GetValue(game);
                        state.badEndingOne = true;
                        state.neighborBarricadeSeen = state.neighborFlyersSeen = state.prologueEntered = state.skt079NewsSeen = true;
                        ProgressionSave.Write(state);
                    }
                    break;
                case 3:
                    Release();
                    if (scenario == 0)
                        Check(Read(story, "page").ToString() == "Diary", "Diary open");
                    if (scenario == 1)
                        Check(FirstLoopPrologue.Instance.IsPresenting, "Reset interrupts encounter");
                    if (scenario == 2)
                    {
                        KnowledgeManager.Instance.Unlock(StoryContent.AncientKnowledgeId, KnowledgeTier.Cosmic);
                        Check(FirstLoopPrologue.Instance.BeginPrologue(), "Chapter opens");
                    }
                    if (scenario == 3)
                    {
                        var p = GameObject.FindWithTag("Player");
                        p.transform.position = new Vector3(-3.65f, -1.65f, 0);
                        p.GetComponent<Rigidbody2D>().position = p.transform.position;
                        Press(Key.E);
                    }
                    if (scenario == 4)
                        Check(UnityEngine.Object.FindAnyObjectByType<WindowLookout2D>().IsOpen, "Window remains open before reset");
                    if (scenario == 5)
                    {
                        Call(hud, "ToggleConsole");
                        Set(hud, "unlockedThisSession", true);
                        Set(hud, "enteringCode", false);
                        Set(hud, "debugOpen", true);
                        Check(ProgressionHUD.DebugConsoleOpen, "Debug is accessible in ending");
                        Set(hud, "confirmNewJourney", true);
                        ScreenCapture.CaptureScreenshot("../initial-state-confirmation.png");
                    }
                    break;
                case 4:
                    Release();
                    if (scenario == 3)
                        Check(FurnitureInteraction.IsBusy, "TV menu open before reset");
                    KnowledgeManager.Instance.Unlock("reset-case-" + scenario, KnowledgeTier.Ordinary);
                    Call(hud, "RestoreInitialState");
                    break;
                case 5:
                    CheckFresh(game, clock, story, feedback);
                    if (scenario++ < 5)
                    {
                        stage = 1;
                        next = now + .3;
                        return;
                    }
                    Call(story, "WakeUp");
                    break;
                case 6:
                    Press(Key.D);
                    break;
                case 7:
                    Check(GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>().linearVelocity.x > 0, "Movement resumes after repeated resets");
                    Release();
                    SceneManager.LoadScene("Corridor");
                    break;
                case 8:
                    Check(FirstLoopPrologue.Instance.IsPresenting, "First-loop encounter replays after reset");
                    Finish(true, "Partial dream, diary, corridor cutscene, chapter card, television, window and ending reset; fresh Days/loop/stats/flags/journal; no stale notifications; movement and Day 1 encounter restored. Original save restored.");
                    return;
            }
            stage++;
            next = now + (stage == 2 || stage == 6 ? 4f : .35f);
        }
        catch (Exception error) { Finish(false, "Stage " + stage + " scenario " + scenario + ": " + error); }
    }
    static void CheckFresh(GameManager game, TimeManager clock, StoryJournal story, GameplayFeedback feedback)
    {
        var saved = ProgressionSave.Load();
        Check(SceneManager.GetActiveScene().name == "Bedroom" && clock.Days == 1 && clock.Period == TimePeriod.Morning, "Bedroom Day 1 morning");
        Check(game.LoopCount == 0 && game.Knowledge == 0 && game.Mutation == 0 && !game.HasEnded && game.Vitality == game.MaxVitality && game.Sanity == game.Settings.maximumSanity, "Fresh stats");
        Check(!saved.dreamSeen && !saved.neighborFlyersSeen && !saved.neighborBarricadeSeen && !saved.skt079NewsSeen && !saved.prologueEntered && !saved.badEndingOne && !saved.dayOneSupernovaSeen && saved.journal.Count == 0 && saved.knowledgeIds.Count == 0 && saved.morningStage == 0 && !saved.fridgeOpened && saved.inventory.Count == 0 && !saved.journalUnlocked && !saved.investigationUnlocked && !saved.tutorialComplete && !saved.dayOneNewsStarted && !saved.fridgeDoorOpen, "Fresh persisted story");
        Check(Read(story, "page").ToString() == "Dream" && (int)Read(story, "dreamPage") == 0, "Dream starts from first page");
        Check(!FirstLoopPrologue.Instance.IsPresenting && !FurnitureInteraction.IsBusy && !ProgressionHUD.DebugConsoleOpen && feedback.JournalMessage == null && feedback.TimeMessage == null, "No old modal or notices");
    }
    static void Finish(bool success, string message)
    {
        SessionState.SetBool(KeyName, false);
        if (keyboard != null && keyboard.added)
            InputSystem.RemoveDevice(keyboard);
        Application.runInBackground = background;
        foreach (string suffix in new[] { "", ".bak", ".tmp" })
        {
            string path = ProgressionSave.PathName + suffix, data = SessionState.GetString(KeyName + suffix, "");
            if (data != "")
                File.WriteAllBytes(path, Convert.FromBase64String(data));
            else if (File.Exists(path))
                File.Delete(path);
        }
        File.WriteAllText("../initial-state-validation.txt", (success ? "PASS: " : "FAIL: ") + message);
        if (success)
            Debug.Log("INITIAL_STATE_PASS " + message);
        else
            Debug.LogError(message);
        if (Application.isBatchMode)
            EditorApplication.Exit(success ? 0 : 1);
        else
            EditorApplication.isPlaying = false;
    }
}
