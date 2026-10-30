// 职责：编辑器回归检查：ExpansionRunner；修改功能后从 Prototype 菜单运行。
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Interaction;
[InitializeOnLoad]
public static class ExpansionRunner
{
    static IEnumerator routine; static double next; static byte[][] backup; static int oldLang; static bool errors;
    static ExpansionRunner()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (a, b, c) => { if (routine != null && (c == LogType.Exception || c == LogType.Error)) errors = true; };
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling)
            return;
        if (routine == null)
        {
            if (!EditorApplication.isPlaying && File.Exists("../regression-request.txt"))
            {
                var command = File.ReadAllText("../regression-request.txt");
                File.Delete("../regression-request.txt");
                PlayerPrefs.SetInt("Story.Language", 0);
                if (command == "investigation")
                    VerifyInvestigation.Run();
                else if (command == "seven")
                    VerifySevenDayProgression.Run();
                return;
            }
            if (!EditorApplication.isPlaying && File.Exists("../expansion-runtime-request.txt"))
            {
                File.Delete("../expansion-runtime-request.txt");
                Run();
            }
            return;
        }
        if (EditorApplication.timeSinceStartup < next)
            return;
        try
        {
            if (routine.MoveNext())
                next = EditorApplication.timeSinceStartup + (float)routine.Current;
            else
                Finish(null);
        }
        catch (Exception e) { Finish(e.ToString()); }
    }
    static void Require(bool a, string b)
    {
        if (!a)
            throw new Exception(b);
    }
    static void Call(object o, string m) => o.GetType().GetMethod(m, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(o, null);
    static void Place(Vector2 p)
    {
        var go = GameObject.FindWithTag("Player");
        go.transform.position = p;
        go.GetComponent<Rigidbody2D>().position = p;
    }
    [MenuItem("Prototype/Test expanded investigation runtime")]
    static void Run()
    {
        backup = new byte[3][];
        int i = 0;
        foreach (var ext in new[] { "", ".bak", ".tmp" })
        {
            var p = ProgressionSave.PathName + ext;
            backup[i++] = File.Exists(p) ? File.ReadAllBytes(p) : null;
        }
        oldLang = PlayerPrefs.GetInt("Story.Language", 0);
        PlayerPrefs.SetInt("Story.Language", 0);
        SessionState.SetBool(DayOneSupernovaEvent.SuppressionKey, true);
        ProgressionSave.Write(new ProgressionState { dreamSeen = true, journalUnlocked = true, investigationUnlocked = true, tutorialComplete = true });
        EditorSceneManager.OpenScene("Assets/Scenes/Apartment/Bedroom.unity");
        errors = false;
        routine = Steps();
        EditorApplication.isPlaying = true;
        next = EditorApplication.timeSinceStartup + 3;
    }
    static IEnumerator Steps()
    {
        while (GameManager.Instance == null)
            yield return .2f;
        var clock = TimeManager.Instance;
        Require(Investigation.Collect("my-observation-notes"), "Collect first notes");
        clock.SleepUntilNextMorning();
        Place(new Vector2(2.7f, -1.1f));
        yield return .7f;
        Require(EvidenceInspection.Instance.TryOpen("notes-gap"), "Revisit desk unlocks missing pages");
        Require(EvidenceInspection.Instance.CollectCurrent(), "Collect missing pages");
        EvidenceInspection.Instance.Close();
        UnityEngine.SceneManagement.SceneManager.LoadScene("LivingRoom");
        yield return .8f;
        var tank = GameObject.Find("Aquarium and cabinet");
        Require(tank != null, "Tank exists");
        Place(tank.transform.position + new Vector3(0, -1, 0));
        yield return .5f;
        Require(EvidenceInspection.Instance.TryOpen("tank-label"), "Tank label reachable before mutation");
        Require(EvidenceInspection.Instance.CollectCurrent(), "Collect label");
        EvidenceInspection.Instance.Close();
        clock.SleepUntilNextMorning();
        yield return .5f;
        Call(AquariumEntity.Instance, "OpenDialogue");
        Require(AquariumEntity.Instance.IsTalking, "Day three tank dialogue");
        Require(!Investigation.Has("tank-eye"), "Dialogue does not auto collect");
        Require(Investigation.Collect("tank-eye"), "Record observation");
        Call(AquariumEntity.Instance, "CloseDialogue");
        Require(Investigation.Connect("tank-label", "tank-eye", false) && Investigation.Connect("tank-label", "tank-eye", true), "New deduction supports tentative then conclusion");
        clock.SleepUntilNextMorning();
        clock.SleepUntilNextMorning();
        yield return .5f;
        Require(Investigation.Collect("tank-overflow"), "Day five observation");
        clock.SleepUntilNextMorning();
        yield return .5f;
        Require(Investigation.Collect("001-name"), "Day six testimony");
        Require(Investigation.Connect("notes-gap", "001-name", true), "Memory gap deduction");
        Require(Investigation.Connect("tank-overflow", "001-name", true), "Waterline deduction");
        DeathBySeventhsDawn.Story.StoryText.ToggleLanguage();
        Require(Investigation.Title("notes-gap") == "Missing pages", "Collected title changes language");
        Require(Investigation.Has("deduction-book-reader"), "Language retains deductions");
        var state = ProgressionSave.Load();
        Require(state.journal.Exists(x => x.id == "deduction-waterline"), "New graph persists");
        Require(!errors, "No runtime errors");
        MainMenuUI.ReturnToMainMenu();
        yield return .4f;
        ScreenCapture.CaptureScreenshot("../menu-english.png");
        yield return .4f;
        MainMenuUI.Instance.StartGame();
        MainMenuUI.Instance.StartGame();
        Require(MainMenuUI.WorldBlocked, "Opening blocks world and repeat clicks");
        yield return 7f;
        ScreenCapture.CaptureScreenshot("../opening-runtime.png");
        yield return 5f;
        Require(!MainMenuUI.WorldBlocked && clock.Days == 1 && ProgressionSave.Load().journal.Count <= 1, "Opening starts one fresh journey");
        Require(StoryJournal.WorldBlocked, "Opening enters dream");
        Require(!errors, "Opening has no runtime errors");
        yield return .1f;
    }
    static void Finish(string failure)
    {
        routine = null;
        File.WriteAllText("../expansion-runtime-validation.txt", failure == null ? "PASS: desk revisit; reachable tank label; day-three dialogue and manual collection; day-five/six records; three new deductions; live localized journal; persistence; full opening -> fresh journey / dream; repeated start guard; no runtime errors." : "FAIL: " + failure);
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => { int i = 0; foreach (var ext in new[] { "", ".bak", ".tmp" }) { var p = ProgressionSave.PathName + ext; if (backup[i] != null) File.WriteAllBytes(p, backup[i]); else if (File.Exists(p)) File.Delete(p); i++; } PlayerPrefs.SetInt("Story.Language", oldLang); PlayerPrefs.Save(); SessionState.SetBool(DayOneSupernovaEvent.SuppressionKey, false); };
    }
}
