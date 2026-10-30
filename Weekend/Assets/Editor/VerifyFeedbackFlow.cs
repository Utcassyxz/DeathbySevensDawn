using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Player;
using DeathBySeventhsDawn.TimeSystem;

[InitializeOnLoad]
public static class VerifyFeedbackFlow
{
    const string KeyName="VerifyFeedbackFlow";
    static int phase; static double next,deadline; static Keyboard keyboard;static Vector2 origin; static bool originalBackground;
    static VerifyFeedbackFlow(){EditorApplication.update+=Tick;}
    [MenuItem("Prototype/Feedback interaction acceptance")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play before testing.");
        SessionState.SetBool(DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.SuppressionKey,true);
        foreach(string suffix in new[]{"",".bak",".tmp"})
            SessionState.SetString(KeyName+suffix,File.Exists(ProgressionSave.PathName+suffix)?Convert.ToBase64String(File.ReadAllBytes(ProgressionSave.PathName+suffix)):"");
        ProgressionSave.Write(new ProgressionState{dreamSeen=true,journalUnlocked=true,investigationUnlocked=true,tutorialComplete=true});
        EditorSceneManager.OpenScene("Assets/Scenes/Apartment/Bedroom.unity");
        phase=0;next=0;deadline=0;SessionState.SetBool(KeyName,true);EditorApplication.isPlaying=true;
    }
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    static void Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));}
    static void Release(){InputSystem.QueueStateEvent(keyboard,new KeyboardState());}
    static void Capture(string name){ScreenCapture.CaptureScreenshot("../feedback-"+name+".png");}
    static void Tick()
    {
        if(!SessionState.GetBool(KeyName,false)||EditorApplication.isCompiling||!EditorApplication.isPlaying||GameManager.Instance==null)return;
        if(deadline==0)deadline=EditorApplication.timeSinceStartup+60;
        if(EditorApplication.timeSinceStartup>deadline){Finish(false,"Timeout at "+phase);return;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            var clock=TimeManager.Instance;var feedback=UnityEngine.Object.FindAnyObjectByType<GameplayFeedback>();
            var window=UnityEngine.Object.FindAnyObjectByType<WindowLookout2D>();
            var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody2D>();
            switch(phase)
            {
                case 0:
                    originalBackground=Application.runInBackground;Application.runInBackground=true;
                    keyboard=InputSystem.AddDevice<Keyboard>("FeedbackAcceptanceKeyboard"); keyboard.MakeCurrent();
                    Check(JournalUpdates.UnreadCount==0,"Isolated save");
                    clock.SleepUntilNextMorning();Check(clock.Days==2&&feedback.TimeMessage=="第 2 天 · 早晨","Sleep feedback uses final Days");
                    body.position=new Vector2(-.2f,.95f);Physics2D.SyncTransforms();Press(Key.E);break;
                case 1:
                    Check(window.IsOpen&&!player.GetComponent<PlayerController2D>().enabled,"E opens window and locks movement");
                    Check(!KnowledgeManager.Instance.Has(StoryContent.WindowKnowledgeId)&&window.CollectObservation()&&JournalUpdates.UnreadCount==1,"Window requires explicit recording");
                    Release();break;
                case 2:
                    Check(feedback.TimeMessage!=null&&feedback.JournalMessage!=null,"Notifications wait through image panel");
                    Press(Key.Escape);break;
                case 3:Check(!window.IsOpen&&player.GetComponent<PlayerController2D>().enabled,"Esc closes image and restores controller");Release();break;
                case 4:Press(Key.J);break;
                case 5:
                    Check(StoryJournal.WorldBlocked&&JournalUpdates.UnreadCount==0,"J reads displayed discovery");
                    Check(ProgressionSave.Load().journal.Find(e=>e.id==StoryContent.WindowKnowledgeId).isRead,"Displayed read state persisted");Release();break;
                case 6:Capture("window-diary");break;
                case 7:Press(Key.Escape);break;
                case 8:
                    Check(!StoryJournal.WorldBlocked,"Diary closes");Release();
                    clock.AdvanceTime();clock.AdvanceTime();SceneManager.LoadScene("Balcony");break;
                case 9:
                    Check(clock.Days==2&&clock.Period==TimePeriod.Evening,"Days retained on balcony");
                    body.position=Resources.Load<StoryContent>("StoryContent").telescopePosition;player.transform.position=body.position;Physics2D.SyncTransforms();Release();break;
                case 10: Press(Key.E);break;
                case 11:Press(Key.T);break;
                case 12:
                    Check(UnityEngine.Object.FindAnyObjectByType<StoryJournal>().CollectTelescopeEvidence(),"Explicit sketch recording");
                    Check(StoryJournal.WorldBlocked&&KnowledgeManager.Instance.Has(StoryContent.AncientKnowledgeId),"Second-night telescope discovery: blocked="+StoryJournal.WorldBlocked+", learned="+KnowledgeManager.Instance.Has(StoryContent.AncientKnowledgeId)+", scene="+SceneManager.GetActiveScene().name+", player="+player.transform.position+", telescope="+(GameObject.Find("阳台望远镜")!=null?GameObject.Find("阳台望远镜").transform.position.ToString():"missing")+", Days="+clock.Days+", period="+clock.Period+", busy="+FurnitureInteraction.IsBusy+", debug="+ProgressionHUD.DebugConsoleOpen+", window="+(window!=null)+", range="+Resources.Load<StoryContent>("StoryContent").telescopeRange+", journalPlayer="+ReadStoryField("player")+", nearby="+ReadStoryField("nearby")+", page="+ReadStoryField("page"));
                    Check(JournalUpdates.UnreadCount==1,"New telescope entry unread, earlier entry remains read");Release();break;
                case 13:Capture("telescope");break;
                case 14:Press(Key.Escape);break;
                case 15:Check(FirstLoopPrologue.Instance.ChapterTitleOpen,"Prologue card follows ancient god");Release();break;
                case 16:Press(Key.E);break;
                case 17:
                    Check(!StoryJournal.WorldBlocked&&feedback.JournalMessage!=null,"Discovery notification waits until telescope closes");Release();break;
                case 18:Capture("notices");break;
                case 19:Press(Key.J);break;
                case 20:
                    Check(JournalUpdates.UnreadCount==1&&ProgressionSave.Load().journal.Find(e=>e.id=="prologue-entry").isRead,"Read newest prologue entry while ancient observation remains unread");Release();break;
                case 21:Capture("prologue-diary");break;
                case 22:Press(Key.RightArrow);break;
                case 23:Check((int)ReadStoryField("selectedEntry")==1 && JournalUpdates.UnreadCount==0 && ProgressionSave.Load().journal.Find(e=>e.id==StoryContent.AncientKnowledgeId).isRead,"Right arrow reads ancient observation after prologue note");Release();Capture("ancient-diary");break;
                case 24:Press(Key.LeftArrow);break;
                case 25:Check((int)ReadStoryField("selectedEntry")==2,"Left arrow returns to newest journal entry");Release();break;
                case 26:Press(Key.Escape);break;
                case 27:Release();origin=body.position;Press(Key.D);break;
                case 28:
                    Check(Vector2.Distance(origin,body.position)>.5f,"Movement resumes after reading");Release();
                    SceneManager.LoadScene("Corridor");break;
                case 29:
                    Check(GameObject.Find("Room 401 plaque")!=null&&GameObject.Find("Room 402 plaque")!=null&&GameObject.Find("Room 403 plaque")!=null,"Corridor room plaques");
                    Check(GameObject.Find("Room 402 plaque").transform.position.y<0,"402 remains south");Capture("corridor-402");break;
                case 30:
                    Finish(true,"Diary keyboard navigation; 401/402/403 signage; Window E/Esc, 5-second deferred notices, final sleep time, J read/save, second-night telescope, retained Days/read state, and resumed movement.");return;
            }
            phase++;next=EditorApplication.timeSinceStartup+(phase==2?5:.45);
        }
        catch(Exception error){Finish(false,"Phase "+phase+": "+error);}
    }
    static object ReadStoryField(string name) { return typeof(StoryJournal).GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(UnityEngine.Object.FindAnyObjectByType<StoryJournal>()); }
    static void Finish(bool success,string report)
    {
        SessionState.SetBool(KeyName,false);
        if(keyboard!=null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        Application.runInBackground=originalBackground;
        foreach(string suffix in new[]{"",".bak",".tmp"})
        {
            string path=ProgressionSave.PathName+suffix,data=SessionState.GetString(KeyName+suffix,"");
            if(data!="")File.WriteAllBytes(path,Convert.FromBase64String(data));else if(File.Exists(path))File.Delete(path);
        }
        File.WriteAllText("../feedback-validation.txt",(success?"PASS: ":"FAIL: ")+report+" Original save restored.");
        if(success)Debug.Log("FEEDBACK_FLOW_PASS "+report);else Debug.LogError(report);
        if(Application.isBatchMode)EditorApplication.Exit(success?0:1);else EditorApplication.isPlaying=false;
    }
}
