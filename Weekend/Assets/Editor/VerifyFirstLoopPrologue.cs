using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Core;
[InitializeOnLoad]
public static class VerifyFirstLoopPrologue
{
    const string KeyName="VerifyFirstLoopPrologue";
    static int phase;static double next,deadline;static Keyboard keyboard;static bool background;static Vector2 origin; static int route; static bool arriving; static Vector2 arrival;
    static readonly string[] routes={"Balcony","LivingRoom","Corridor","LivingRoom","Bedroom","LivingRoom","Bathroom","LivingRoom"};
    static VerifyFirstLoopPrologue(){EditorApplication.update+=Tick;}
    [MenuItem("Prototype/Test 401 first-loop prologue")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first.");
        SessionState.SetBool(DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.SuppressionKey,true);
        foreach(string suffix in new[]{"",".bak",".tmp"})SessionState.SetString(KeyName+suffix,File.Exists(ProgressionSave.PathName+suffix)?Convert.ToBase64String(File.ReadAllBytes(ProgressionSave.PathName+suffix)):"");
        ProgressionSave.Write(new ProgressionState{dreamSeen=true});
        EditorSceneManager.OpenScene("Assets/Scenes/Apartment/Corridor.unity");
        phase=0;route=0;arriving=false;next=deadline=0;SessionState.SetBool(KeyName,true);EditorApplication.isPlaying=true;
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));}
    static void Release(){InputSystem.QueueStateEvent(keyboard,new KeyboardState());}
    static void Capture(string name){ScreenCapture.CaptureScreenshot("../prologue-"+name+".png");}
    static void Place(Vector2 position){var player=GameObject.FindWithTag("Player");player.GetComponent<Rigidbody2D>().position=position;player.transform.position=position;Physics2D.SyncTransforms();}
    static int BoardCount()=>UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("401 barricade plank"));
    static void Tick()
    {
        if(!SessionState.GetBool(KeyName,false)||EditorApplication.isCompiling||!EditorApplication.isPlaying||FirstLoopPrologue.Instance==null)return;
        double now=EditorApplication.timeSinceStartup;if(deadline==0)deadline=now+70;if(now>deadline){Finish(false,"Timeout at phase "+phase);return;}if(now<next)return;
        try
        {
            var story=FirstLoopPrologue.Instance;var clock=TimeManager.Instance;var game=GameManager.Instance;
            var player=GameObject.FindWithTag("Player");var body=player.GetComponent<Rigidbody2D>();float wait=.45f;
            switch(phase)
            {
                case 0:
                    if(!story.IsPresenting)return;
                    background=Application.runInBackground;Application.runInBackground=true;keyboard=InputSystem.AddDevice<Keyboard>("PrologueAcceptanceKeyboard");keyboard.MakeCurrent();origin=body.position;Press(Key.D);Capture("flyers");break;
                case 1:
                    Check(StoryJournal.WorldBlocked&&body.linearVelocity.sqrMagnitude<.001f&&Vector2.Distance(origin,body.position)<.05f,"Encounter blocks movement");Release();wait=4.5f;break;
                case 2:
                    Check(!story.IsPresenting&&ProgressionSave.Load().neighborFlyersSeen,"Day 1 encounter completion saved");
                    Check(GameObject.Find("401 eccentric neighbor")!=null&&clock.Days==1&&clock.Period==TimePeriod.Morning,"Neighbor remains available for day-one conversation; time unchanged");
                    SceneManager.LoadScene("LivingRoom");break;
                case 3:clock.SleepUntilNextMorning();Place(new Vector2(-3.65f,-1.65f));Release();break;
                case 4:Press(Key.E);break;
                case 5:
                    Check(FurnitureInteraction.IsBusy,"TV E interaction");
                    var tv=UnityEngine.Object.FindAnyObjectByType<FurnitureInteraction>();
                    Check(tv.WatchTelevision()&&!tv.WatchTelevision(),"TV watches once per interaction");
                    Check(clock.Days==2&&clock.Period==TimePeriod.Afternoon&&ProgressionSave.Load().skt079NewsSeen,"News saved and advances exactly one period");Release();break;
                case 6:Capture("news");break;
                case 7:Press(Key.Escape);break;
                case 8:Check(!FurnitureInteraction.IsBusy,"TV closes");Release();SceneManager.LoadScene("Corridor");wait=1.35f;break;
                case 9:Check(!story.IsPresenting,"Day 2 retains day-one boarding without replay");Capture("boarding");wait=3.5f;break;
                case 10:
                    Check(!story.IsPresenting&&ProgressionSave.Load().neighborBarricadeSeen&&BoardCount()==4,"Boarded door saved and visible");
                    Check(GameObject.Find("401 first-loop story props").GetComponentsInChildren<Collider2D>(true).Length==0,"Story props preserve walking clearance");
                    SceneManager.LoadScene("Bedroom");break;
                case 11:SceneManager.LoadScene("Corridor");break;
                case 12:
                    Check(!story.IsPresenting&&GameObject.Find("401 eccentric neighbor")==null&&BoardCount()==4,"Re-entry retains boards without replay");
                    clock.AdvanceTime();SceneManager.LoadScene("Balcony");break;
                case 13:Place(Resources.Load<StoryContent>("StoryContent").telescopePosition);Release();break;
                case 14:Press(Key.E);break;
                case 15:Check(StoryJournal.WorldBlocked&&!KnowledgeManager.Instance.Has(StoryContent.AncientKnowledgeId),"Telescope inspection does not collect");Press(Key.T);break;
                case 16:Check(KnowledgeManager.Instance.Experienced(StoryContent.AncientKnowledgeId),"Ancient experience without automatic sketch");Release();Capture("ancient-god");break;
                case 17:Press(Key.Escape);break;
                case 18:Check(story.ChapterTitleOpen&&!ProgressionSave.Load().prologueEntered,"Chapter card waits for confirmation");Release();Capture("chapter-card");break;
                case 19:Press(Key.E);break;
                case 20:
                    var state=ProgressionSave.Load();
                    Check(!story.ChapterTitleOpen&&state.prologueEntered&&state.neighborFlyersSeen&&state.skt079NewsSeen&&state.neighborBarricadeSeen,"Full first-loop flags saved");
                    Check(clock.Days==2&&clock.Period==TimePeriod.Evening&&game.LoopCount==0&&state.journal.Count==1,"Chapter entry preserves Days, period, loop and deduplicated journal");Release();break;
                case 21:LoopManager.Instance.RestartLoop();break;
                case 22:SceneManager.LoadScene("Corridor");break;
                case 23:
                    Check(game.LoopCount==1&&clock.Days==1&&!story.IsPresenting&&GameObject.Find("401 first-loop story props")==null,"401 intro does not repeat in second loop");
                    Check(game.Knowledge==30&&ProgressionSave.Load().journal.Count==1,"Knowledge and journal survive loop");
                    clock.SleepUntilNextMorning();Check(!story.WatchNews().Contains("SKT-079")&&!story.BeginPrologue(),"First-loop news and title do not replay");
                    SceneManager.LoadScene("LivingRoom");break;
                case 24:
                    if(route<routes.Length)
                    {
                        if(arriving)
                        {
                            if(SceneManager.GetActiveScene().name!=routes[route])return;
                            Check(Vector2.Distance(body.position,arrival)<.08f&&clock.Days==2,"Door arrival and Days preservation: "+routes[route]);
                            route++;arriving=false;next=now+.8;return;
                        }
                        var door=UnityEngine.Object.FindObjectsByType<DeathBySeventhsDawn.World.RoomDoor2D>(FindObjectsSortMode.None).Single(d=>d.destinationScene.EndsWith("/"+routes[route]+".unity"));
                        arrival=door.arrivalPosition;Place(door.transform.position);arriving=true;next=now+.3;return;
                    }
                    Finish(true,"Eight physical door routes; Day 1 boarding + movement lock; Day 2 TV SKT-079 and one-period cost; boarding/re-entry; evening telescope -> confirmed prologue; only the chapter memoir automatically recorded; evidence requires explicit collection; retained Days; second-loop suppression. Original save restored.");return;
            }
            phase++;next=now+wait;
        }
        catch(Exception error){Finish(false,"Phase "+phase+": "+error);}
    }
    static void Finish(bool success,string report)
    {
        SessionState.SetBool(KeyName,false);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);Application.runInBackground=background;
        foreach(string suffix in new[]{"",".bak",".tmp"}){string path=ProgressionSave.PathName+suffix,data=SessionState.GetString(KeyName+suffix,"");if(data!="")File.WriteAllBytes(path,Convert.FromBase64String(data));else if(File.Exists(path))File.Delete(path);}
        File.WriteAllText("../first-loop-prologue-validation.txt",(success?"PASS: ":"FAIL: ")+report);
        if(success)Debug.Log("FIRST_LOOP_PROLOGUE_PASS "+report);else Debug.LogError(report);
        if(Application.isBatchMode)EditorApplication.Exit(success?0:1);else EditorApplication.isPlaying=false;
    }
}
