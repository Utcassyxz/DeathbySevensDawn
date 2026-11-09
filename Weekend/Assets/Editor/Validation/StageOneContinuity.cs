using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Story;

// A continuity driver, not a manual playthrough: only navigation and waiting are accelerated.
// No debug story skips, date assignments, clue injection, gifts, or story-state writes.
public static class StageOneContinuity
{
    static GameManager G=>GameManager.Instance;
    static TimeManager C=>TimeManager.Instance;
    static ApartmentEventDirector D=>ApartmentEventDirector.Instance;
    static MorningRoutine M=>MorningRoutine.Instance;
    static void Check(bool ok,string label)=>VerifyRevision.Check(ok,"CONTINUITY "+(StoryText.English?"EN ":"ZH ")+label);
    static void Place(Vector3 position)
    {
        var actor=GameObject.FindWithTag("Player");var controller=actor.GetComponent<CharacterController>();if(controller)controller.enabled=false;actor.transform.position=position;
        var body=actor.GetComponent<Rigidbody2D>();if(body){body.position=position;body.linearVelocity=Vector2.zero;}if(controller)controller.enabled=true;Physics.SyncTransforms();Physics2D.SyncTransforms();
    }
    static void Near(string name)
    {var target=GameObject.Find(name);var collider=target.GetComponent<Collider2D>();var b=collider?collider.bounds:target.GetComponent<SpriteRenderer>().bounds;Place(new Vector3(b.center.x,b.min.y-.30f,0));}
    static IEnumerator Key(Key key)
    {InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return .12f;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return .15f;}
    public static IEnumerator Run()
    {
        foreach(bool english in new[]{false,true})
        {
            if(StoryText.English!=english)StoryText.ToggleLanguage();CrisisRules.LegacyValidation=false;TimeManager.AutoTimeSuppressed=true;
            UnityEditor.SessionState.SetBool(DayOneSupernovaEvent.SuppressionKey,false);
            MainMenuUI.ReturnToMainMenu();MainMenuUI.Instance.StartGame();yield return 12.5f;
            for(int i=0;i<50&&M.Stage<2;i++){var press=Key(UnityEngine.InputSystem.Key.Enter);while(press.MoveNext())yield return press.Current;yield return .25f;}
            Check(!StoryDebugActions.Enabled&&M.Stage==2,"real opening wakes without debug flags (stage "+M.Stage+")");
            SceneManager.LoadScene("Corridor");yield return 1.2f;Place(new Vector3(-4.7f,.42f,0));yield return .3f;
            if(!CultNeighbor.WorldBlocked)CultNeighbor.Instance.TalkDayOne();
            for(int i=0;i<40;i++)CultNeighbor.Instance.Advance();yield return .5f;M.AdvanceStory();yield return .2f;
            Check(M.Stage==3,"first neighbor meeting leads home");
            SceneManager.LoadScene("LivingRoom");yield return 1.3f;Near("Refrigerator");yield return .2f;Check(M.OpenFridge(),"open actual refrigerator");yield return 1f;M.AdvanceStory();yield return .2f;
            for(int i=0;i<45&&DayOneSupernovaEvent.Instance.IsPresenting;i++){DayOneSupernovaEvent.Instance.Advance();yield return .06f;}
            yield return .8f;M.AdvanceStory();yield return .2f;Check(M.Stage==5,"original four bulletins lead to meal");
            Near("Old sofa");yield return .2f;Check(M.BeginNap(),"sofa rest starts");yield return 5.5f;M.AdvanceStory();yield return .2f;Check(M.Stage==6,"rest reaches desk beat");
            SceneManager.LoadScene("Bedroom");yield return 1.3f;Place(new Vector3(2.7f,-1.1f,0));yield return .2f;Check(M.BeginWriting(),"writing starts at actual desk");yield return 2.2f;M.AdvanceStory();yield return .3f;
            var close=Key(UnityEngine.InputSystem.Key.Escape);while(close.MoveNext())yield return close.Current;
            SceneManager.LoadScene("Balcony");yield return 1.3f;Near("阳台望远镜");yield return .2f;var journal=Object.FindAnyObjectByType<StoryJournal>();Check(journal.OpenTelescopeFromWorld(),"first telescope opens naturally");
            var observe=Key(UnityEngine.InputSystem.Key.Enter);while(observe.MoveNext())yield return observe.Current;
            for(int i=0;i<20;i++){journal.AdvanceObservation();yield return .3f;}
            Check(journal.CollectTelescopeEvidence(),"observations recorded after real hour/recheck sequence");
            close=Key(UnityEngine.InputSystem.Key.Escape);while(close.MoveNext())yield return close.Current;
            Check(Investigation.Connect("first-night-silhouette","first-night-recheck",true),"confirm the first deduction without adding clues");yield return .3f;Check(M.Stage==10,"tutorial completes through its owners");
            C.SleepUntilNextMorning();yield return 1.5f;Check(C.Days==2&&!G.HasLoopAbility,"ordinary first night reaches Day2 without invented gift");
            if(NeighborSlice.WorldBlocked){var read=Key(UnityEngine.InputSystem.Key.Enter);while(read.MoveNext())yield return read.Current;}
            TimeManager.AutoTimeSuppressed=false;yield return 1f;Check(!C.AutoPaused,"exploration clock resumes after sleeping (modal="+StoryJournal.WorldBlocked+", morning="+MorningRoutine.WorldBlocked+", pause="+PauseMenuUI.WorldBlocked+")");C.SpendGameMinutes(975);yield return 1f;Check(D.CurrentCard=="night-call","normal clock reaches compulsory late-night visit (minute="+C.GameMinute+", card="+D.CurrentCard+")");
            D.Choose(0);yield return 1.3f;Check(D.CurrentCard=="night-intro","human 2D arrival");D.Choose(0);yield return .2f;D.Choose(0);yield return .2f;D.Choose(0);yield return 5.9f;if(D.CurrentCard=="night-controls")D.Choose(0);
            Check(D.ChaseActive,"normal continuity tears into the first-person chase");Place(new Vector3(-5,0,0));var escape=Key(UnityEngine.InputSystem.Key.E);while(escape.MoveNext())yield return escape.Current;yield return 1.5f;
            Check(SceneManager.GetActiveScene().name=="NeighborRoom2D","escape returns to the same 2D room");Place(new Vector3(-5,0,0));Object.FindAnyObjectByType<NeighborRoom>().UseNearest();yield return 1.3f;
            Check(SceneManager.GetActiveScene().name=="Corridor","leave through 401");
            for(int day=3;day<=7;day++)
            {
                for(int step=0;step<25&&C.Days<day;step++)
                {
                    if(NeighborSlice.WorldBlocked||FirstLoopPrologue.Instance.ChapterTitleOpen){var read=Key(UnityEngine.InputSystem.Key.Enter);while(read.MoveNext())yield return read.Current;}
                    else if(D.CurrentCard!=null)D.Choose(0);
                    else C.SpendGameMinutes(240);
                    yield return .2f;
                }
                Check(C.Days==day&&!G.HasEnded,"continuous calendar reaches Day"+day+" alive (actual="+C.Days+", modal="+StoryJournal.WorldBlocked+")");
            }
            for(int step=0;step<8&&!G.HasEnded&&!WorldEndSequence.IsPresenting;step++)
            {
                if(D.CurrentCard!=null)D.Choose(D.CurrentCard=="day7-call"?1:0);
                C.SpendGameMinutes(180);yield return .2f;
            }
            yield return 6.3f;Check(C.Days==7&&G.HasEnded&&!G.HasLoopAbility,"seventh-night purification ends the unawakened route");Check(!StoryDebugActions.Enabled,"no debug story skips throughout the route");
            TimeManager.AutoTimeSuppressed=true;
        }
    }
}
