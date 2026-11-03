using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.TimeSystem;

public static class SpatialApocalypseAcceptance
{
    static ProgressionState State => (ProgressionState)typeof(GameManager).GetProperty("State",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(GameManager.Instance);
    static void Check(bool yes,string message)=>VerifyRevision.Check(yes,message);
    static void Place(Vector2 p)
    {
        var g=GameObject.FindWithTag("Player"); g.transform.position=p;
        var rb=g.GetComponent<Rigidbody2D>();rb.position=p;rb.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();
    }
    static void Ready()
    {
        StoryDebugActions.SkipForcedStory();State.tutorialComplete=State.journalUnlocked=State.investigationUnlocked=true;
        State.morningStage=10;State.dreamSeen=true;State.neighborBarricadeSeen=true;
    }
    static IEnumerator Key(Keyboard keyboard,UnityEngine.InputSystem.Key key)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return .15f;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .15f;
    }
    public static IEnumerator Run()
    {
        var game=GameManager.Instance;var clock=TimeManager.Instance;
        game.StartNewJourney();yield return .8f;Ready();clock.CompleteSleep();
        State.cultDayOneTalked=true;
        SceneManager.LoadScene("Corridor");yield return .7f;Ready();Place(new Vector2(-4.55f,.20f));yield return .3f;
        var keyboard=InputSystem.AddDevice<Keyboard>("SevenDayAcceptanceKeyboardSpatial");
        var press=Key(keyboard,UnityEngine.InputSystem.Key.E);while(press.MoveNext())yield return press.Current;
        var inspection=UnityEngine.Object.FindAnyObjectByType<EvidenceInspection>();
        Check(inspection.IsOpen,"Day-two note examinable; day="+clock.Days+" prompt="+WorldPromptUI.VisiblePrompt+" blocked="+StoryJournal.WorldBlocked);
        Check(!RooftopAccess.Instance.WorldLabelsVisible,"EXIT overlay labels are suppressed while evidence is open");
        VerifyRevision.Capture("evidence-no-floating-exit");yield return .2f;
        Check(inspection.CollectCurrent()&&Investigation.Has("cult-door-note"),"The door note is recorded through the real collection action");
        inspection.Close();yield return .3f;
        press=Key(keyboard,UnityEngine.InputSystem.Key.E);while(press.MoveNext())yield return press.Current;
        Check(CultNeighbor.Instance.DoorOpen,"Real E at the walkable wall approach opens the neighbour's eye scene");
        VerifyRevision.Capture("401-knock-restored");yield return .2f;
        press=Key(keyboard,UnityEngine.InputSystem.Key.Escape);while(press.MoveNext())yield return press.Current;
        State.inventory.Add(CultNeighbor.Token);yield return .2f;
        press=Key(keyboard,UnityEngine.InputSystem.Key.E);while(press.MoveNext())yield return press.Current;
        Check(CultNeighbor.WorldBlocked&&!CultNeighbor.Instance.DoorOpen,"Holding the triangle still allows a closed-door knock, without another gift");
        press=Key(keyboard,UnityEngine.InputSystem.Key.Escape);while(press.MoveNext())yield return press.Current;
        Place(RooftopAccess.StairsFour);yield return .3f;
        Check(RooftopAccess.Instance.UseStairs(),"North corridor portal enters its corresponding stair core");yield return .6f;
        var at=GameObject.FindWithTag("Player").transform.position;
        Check(at.y<-.7f&&Mathf.Abs(at.x)<.3f&&GameObject.Find("Fire door").transform.position.y<0,"Stairwell entry and actual door both lie on the south side");
        VerifyRevision.Capture("south-stairwell-entry");yield return .2f;
        Check(RooftopAccess.Instance.UseStairs(),"South-side door returns to the correct corridor portal");yield return .6f;
        var journal=UnityEngine.Object.FindAnyObjectByType<StoryJournal>();journal.ShowJournal();yield return .2f;
        Check(!RooftopAccess.Instance.WorldLabelsVisible,"Journal and clue pages cannot bring back the EXIT label");
        journal.DebugSkip();yield return .3f;
        InputSystem.RemoveDevice(keyboard);
        // First encounter and midnight transition use the real story owners. Clock-only calls prepare days.
        game.StartNewJourney();yield return .8f;Ready();clock.CompleteSleep();
        while(clock.Period!=TimePeriod.Night)clock.AdvanceTime();
        game.RecordAncientGodEncounter();clock.SleepUntilNextMorning();yield return .4f;
        for(int n=0;n<60&&NightDreamSequence.WorldBlocked;n++){NightDreamSequence.Instance.Advance();yield return .2f;}
        Check(game.HasLoopAbility&&game.LoopCount==0,"Midnight fixture earns the original gift through a complete first Bubble meeting");
        Ready();State.fishAlly=true; // isolate the surviving-to-midnight branch from the separate fish murder ending.
        while(clock.Days<7)clock.CompleteSleep();
        while(clock.Period!=TimePeriod.Night)clock.AdvanceTime();
        Check(!WorldEndSequence.IsPresenting,"Day-seven night remains playable before the midnight boundary");
        int album=EndingCollection.Count;clock.AdvanceTime();yield return .3f;
        Check(WorldEndSequence.FirstCycleActive&&clock.Days==7&&game.LoopCount==0&&!game.HasEnded,"First awakened midnight starts destruction without prematurely resetting or recording an ending");
        var saved=ProgressionSave.Load();Check(saved.firstApocalypsePending&&!saved.firstApocalypseExplained,"Interrupted apocalypse has a persistent continuation checkpoint");
        clock.AdvanceTime();Check(clock.Days==7&&game.LoopCount==0,"Time and automatic loop progression are locked through the cutscene");
        yield return 1.8f;VerifyRevision.Capture("first-midnight-earth");yield return WorldEndSequence.Duration;
        Check(WorldEndSequence.InReunion&&game.LoopCount==0,"Destruction hands off to Bubble rather than directly to morning");
        VerifyRevision.Capture("bubble-first-midnight-reunion");yield return .2f;
        for(int n=0;n<90&&WorldEndSequence.InReunion;n++){WorldEndSequence.Instance.AdvanceReunion();yield return .2f;}
        Check(State.firstApocalypseExplained&&!State.firstApocalypsePending&&game.LoopCount==1&&clock.Days==1&&!game.HasEnded,"Finishing the gift explanation returns exactly once to the first day of the next loop");
        Check(EndingCollection.Count==album,"The first-cycle story transition does not unlock a terminal ending");
        saved=ProgressionSave.Load();Check(saved.firstApocalypseExplained&&!saved.firstApocalypsePending,"Completed first apocalypse survives saving and reloading");
        Ready();while(clock.Days<7)clock.CompleteSleep();while(clock.Period!=TimePeriod.Night)clock.AdvanceTime();clock.AdvanceTime();yield return .5f;
        Check(!WorldEndSequence.FirstCycleActive&&game.LoopCount==2,"Later loops do not repeat the first-gift explanation");
        game.StartNewJourney();yield return .8f;Ready();while(clock.Days<7)clock.CompleteSleep();while(clock.Period!=TimePeriod.Night)clock.AdvanceTime();clock.AdvanceTime();yield return .3f;
        Check(game.WorldEndEnding&&!WorldEndSequence.FirstCycleActive&&!game.HasLoopAbility,"Never meeting Bubble preserves the unawakened world-destruction ending");
        WorldEndSequence.Instance.DebugSkip();
    }
}
