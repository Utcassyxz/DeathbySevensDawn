using System;
using System.IO;
using System.Linq;
using System.Collections;
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
using DeathBySeventhsDawn.TimeSystem;

[InitializeOnLoad]
public static class VerifyInvestigation
{
    const string KeyName="InvestigationAcceptance";
    static IEnumerator steps;static double next,deadline;static Keyboard keyboard;static bool background;static string error;static int assertions;
    static StoryJournal Story=>UnityEngine.Object.FindAnyObjectByType<StoryJournal>();
    static void Invoke(object obj,string method)=>obj.GetType().GetMethod(method,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(obj,null);
    static ProgressionState State=>(ProgressionState)typeof(GameManager).GetProperty("State",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(GameManager.Instance);
    static VerifyInvestigation()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(m,t,l)=>{if(SessionState.GetBool(KeyName,false)&&(l==LogType.Error||l==LogType.Exception))error=m;};
        EditorApplication.playModeStateChanged+=mode=>{if(mode==PlayModeStateChange.ExitingPlayMode&&SessionState.GetBool(KeyName,false))Finish(false,"User exited test before completion");};
    }
    [MenuItem("Prototype/Test investigation collection and deductions")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before running acceptance.");
        SessionState.SetBool(DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.SuppressionKey,true);
        foreach(string suffix in new[]{"",".bak",".tmp"})SessionState.SetString(KeyName+suffix,File.Exists(ProgressionSave.PathName+suffix)?Convert.ToBase64String(File.ReadAllBytes(ProgressionSave.PathName+suffix)):"");
        ProgressionSave.Write(new ProgressionState{dreamSeen=true,journalUnlocked=true,investigationUnlocked=true,tutorialComplete=true});EditorSceneManager.OpenScene("Assets/Scenes/Apartment/Corridor.unity");
        steps=null;next=deadline=0;error=null;assertions=0;SessionState.SetBool(KeyName,true);EditorApplication.isPlaying=true;
    }
    static void Check(bool ok,string text){assertions++;if(!ok)throw new Exception(text);}
    static void Press(Key key)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
    static void Release()=>InputSystem.QueueStateEvent(keyboard,new KeyboardState());
    static void Place(Vector2 p){var go=GameObject.FindWithTag("Player");go.transform.position=p;go.GetComponent<Rigidbody2D>().position=p;Physics2D.SyncTransforms();}
    static void Capture(string name)=>ScreenCapture.CaptureScreenshot("../investigation-"+name+".png");
    static void Tick()
    {
        if(!SessionState.GetBool(KeyName,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EvidenceInspection.Instance==null)return;
        if(error!=null){Finish(false,error);return;}
        double now=EditorApplication.timeSinceStartup;if(deadline==0)deadline=now+100;if(now>deadline){Finish(false,"Timeout");return;}if(now<next)return;
        try{if(steps==null)steps=Steps();if(!steps.MoveNext()){Finish(true,assertions+" assertions: explicit collection; no auto flyer/news/outline entries; optional sketch; valid and unsupported links; chained deductions; no duplicate rewards; save/load, legacy and loop retention; complete reset. Original save restored.");return;}next=now+(double)(float)steps.Current;}
        catch(Exception ex){Finish(false,ex.ToString());}
    }
    static IEnumerator Steps()
    {
        background=Application.runInBackground;Application.runInBackground=true;keyboard=InputSystem.AddDevice<Keyboard>("InvestigationAcceptanceKeyboard");keyboard.MakeCurrent();
        yield return 5f;
        Check(State.neighborFlyersSeen&&!Investigation.Has("401-warning"),"Encounter does not grant a warning record");
        for(int i=0;i<DeathBySeventhsDawn.Story.StoryText.Current.cult.dayOne.Length;i++)CultNeighbor.Instance.Advance();
        Check(GameObject.Find("401 flyer")==null,"Flyer is reserved for the lobby");
        Check(!Investigation.Has("401-warning"),"Conversation holds the warning for initial diary information");
        Check(Investigation.Collect("401-warning")&&State.journal.Count==1,"Initial warning can populate the journal");
        Check(!Investigation.Collect("401-warning")&&State.journal.Count==1,"Initial warning deduplicates");
        EvidenceInspection.Instance.Close();SceneManager.LoadScene("Bedroom");yield return .4f;
        Place(new Vector2(2.6f,-1.0f));yield return .4f;Press(Key.E);yield return .3f;Release();
        Check(EvidenceInspection.Instance.CurrentId=="my-observation-notes"&&!Investigation.Has("my-observation-notes"),"Desk notes are inspectable");
        Check(EvidenceInspection.Instance.CollectCurrent(),"Collect personal observation notes");EvidenceInspection.Instance.Close();
        State.sanityBurden=65;Place(new Vector2(1.65f,-1.45f));yield return .5f;Press(Key.E);yield return .3f;Release();
        Check(PerceptionClues.IsReading&&!Investigation.Has("filter-bedroom-margins"),"Low SAN reading no longer autocollects");
        Check(PerceptionClues.Instance.CollectCurrent(),"Explicit hidden clue collection");PerceptionClues.Instance.Close();State.sanityBurden=0;
        TimeManager.Instance.SleepUntilNextMorning();SceneManager.LoadScene("LivingRoom");yield return .5f;Place(new Vector2(-3.65f,-1.65f));yield return .4f;Press(Key.E);yield return .4f;Release();
        var furniture=UnityEngine.Object.FindAnyObjectByType<FurnitureInteraction>();Check(furniture.WatchTelevision(),"Watch day 2 news");
        Check(State.skt079NewsSeen&&!Investigation.Has("skt079-broadcast"),"News not automatically excerpted");Check(!furniture.CollectNews()&&Investigation.Collect("skt079-broadcast"),"TV has no record action; initial-information API can populate news");
        Press(Key.Escape);yield return .4f;Release();SceneManager.LoadScene("Corridor");yield return 5f;
        Check(State.neighborBarricadeSeen&&!Investigation.Has("401-barricade"),"Boarding does not automatically record evidence");
        Place(new Vector2(-4.35f,.35f));yield return .4f;Press(Key.E);yield return .4f;Release();
        Check(EvidenceInspection.Instance.CurrentId=="cult-door-note"&&EvidenceInspection.Instance.CollectCurrent(),"Day-two note is examined before the door");EvidenceInspection.Instance.Close();yield return .4f;Press(Key.E);yield return .4f;Release();Check(EvidenceInspection.Instance.CurrentId=="401-barricade"&&EvidenceInspection.Instance.CollectCurrent(),"Inspect and record boarded door");EvidenceInspection.Instance.Close();
        State.sanityBurden=65;Place(new Vector2(-3.45f,1.05f));yield return .4f;Press(Key.E);yield return .4f;Release();
        Check(PerceptionClues.Instance.CollectCurrent(),"Collect hidden underprint deliberately");PerceptionClues.Instance.Close();State.sanityBurden=0;
        Check(Investigation.Connect("401-warning","401-barricade",true),"Link warning to boarded door");
        Check(Investigation.Connect("filter-401-underprint","401-barricade",true),"Cross-check hidden writing with boards");
        SceneManager.LoadScene("Bedroom");yield return .5f;
        var window=UnityEngine.Object.FindAnyObjectByType<WindowLookout2D>();Place(window.transform.position);yield return .4f;
        Check(window.TryOpen()&&!Investigation.Has(StoryContent.WindowKnowledgeId),"Window view does not collect");Check(window.CollectObservation(),"Manually record window");window.CloseView();
        SceneManager.LoadScene("Balcony");yield return .5f;Place(Resources.Load<StoryContent>("StoryContent").telescopePosition);yield return .4f;Press(Key.E);yield return .4f;Release();
        Check(!Story.IsRevelation&&!Investigation.Has("apartment-telescope"),"Telescope opens inspection first");
        Check(Story.CollectTelescopeEvidence(),"Collect telescope");
        Check(!Investigation.Connect("apartment-telescope","apartment-telescope",false),"Self-link rejected");
        Check(!Investigation.Connect("apartment-telescope",StoryContent.AncientKnowledgeId,true),"Uncollected evidence rejected");
        Check(Investigation.Match("apartment-telescope","my-observation-notes")!=null&&!Investigation.Has("deduction-astronomy"),"Preview does not grant conclusion");
        Check(Investigation.Connect("apartment-telescope","my-observation-notes",true),"Deduce astronomy hobby");
        Check(!Investigation.Connect("my-observation-notes","apartment-telescope",true),"Reverse duplicate rejected");
        Check(Investigation.Connect("401-warning","apartment-telescope",false),"Unsupported hypothesis stored as tentative");
        Check(Investigation.FindLink("401-warning","apartment-telescope").conclusion==null,"Tentative is not fact");
        Check(Investigation.RemoveTentative("apartment-telescope","401-warning"),"Tentative can be removed");
        TimeManager.Instance.AdvanceTime();Press(Key.T);yield return .5f;Release();
        Check(Story.IsRevelation&&KnowledgeManager.Instance.Experienced(StoryContent.AncientKnowledgeId)&&!Investigation.Has(StoryContent.AncientKnowledgeId),"Witness has cost but no automatic sketch");
        float knowledge=GameManager.Instance.Knowledge;
        Capture("revelation-uncollected");yield return .4f;
        Check(Story.CollectTelescopeEvidence()&&!Story.CollectTelescopeEvidence()&&GameManager.Instance.Knowledge==knowledge,"Sketch deliberate, deduplicated, no repeated exposure");
        Check(Resources.Load<Texture2D>("Story/AncientOutlineSketch")!=null,"Sketch asset present");
        Check(Investigation.Connect(StoryContent.AncientKnowledgeId,"apartment-telescope",true),"Sketch connected to telescope");
        Check(Investigation.Connect("deduction-optical","deduction-astronomy",true),"Deductions can support later deductions");
        State.neighborFlyersSeen=false;Check(Investigation.Match("401-warning","skt079-broadcast")==null,"Chronology requires witnessing Day 1 warning");State.neighborFlyersSeen=true;
        Check(Investigation.Connect("401-warning","skt079-broadcast",true),"Warning chronology deduction");
        Press(Key.Escape);yield return .4f;Release();Check(FirstLoopPrologue.Instance.ChapterTitleOpen,"Prologue gate uses experience, not collection");
        FirstLoopPrologue.Instance.EnterPrologue();yield return .4f;
        Press(Key.J);yield return .4f;Release();
        Press(Key.B);yield return .3f;Release();
        Check((bool)typeof(StoryJournal).GetField("showBoard",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(Story),"B enters board without mouse");
        var board=(InvestigationBoardUI)typeof(StoryJournal).GetField("board",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(Story);
        board.Select("apartment-telescope");board.Select("skt079-broadcast");
        Check(State.investigationLinks.Count==6,"Selection alone writes no link");
        Press(Key.Enter);yield return .3f;Release();yield return .2f;Check(State.investigationLinks.Count==7,"Keyboard Enter stores tentative link");
        Press(Key.Enter);yield return .3f;Release();yield return .2f;Check(State.investigationLinks.Count==7,"Duplicate tentative link rejected");
        Press(Key.Delete);yield return .3f;Release();yield return .2f;Check(State.investigationLinks.Count==6,"Keyboard removes tentative link");
        board.Reset();board.Select("apartment-telescope");board.Select(StoryContent.AncientKnowledgeId);
        Press(Key.Enter);yield return .3f;Release();Check(State.investigationLinks.Count==6,"Concluded pair is not duplicated");
        typeof(InvestigationBoardUI).GetField("boardScroll",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(board,new Vector2(0,500));
        Capture("board");yield return .6f;
        var loaded=ProgressionSave.Load();Check(loaded.investigationLinks.Count==6&&loaded.journal.Any(e=>e.id=="deduction-deliberate"),"Graph persists on save/load");
        var legacy=JsonUtility.FromJson<ProgressionState>("{\"version\":1,\"journal\":[{\"id\":\"401-warning\",\"title\":\"legacy\"}],\"knowledgeIds\":[]}");Investigation.Normalize(legacy);
        Check(legacy.journal.Count==1&&legacy.investigationLinks!=null&&legacy.observedKnowledgeIds!=null,"Legacy records preserved");
        Press(Key.Escape);yield return .3f;Release();int count=State.journal.Count;LoopManager.Instance.RestartLoop();yield return .6f;
        Check(State.journal.Count==count&&State.investigationLinks.Count==6,"Loop retains collected reasoning");
        GameManager.Instance.StartNewJourney();yield return .6f;
        Check(State.journal.Count==0&&State.investigationLinks.Count==0&&State.observedKnowledgeIds.Count==0&&!EvidenceInspection.Instance.IsOpen,"Full reset clears graph and exposure");
    }
    static void Finish(bool success,string message)
    {
        SessionState.SetBool(KeyName,false);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);Application.runInBackground=background;
        foreach(string suffix in new[]{"",".bak",".tmp"}){string path=ProgressionSave.PathName+suffix,data=SessionState.GetString(KeyName+suffix,"");if(data!="")File.WriteAllBytes(path,Convert.FromBase64String(data));else if(File.Exists(path))File.Delete(path);}
        File.WriteAllText("../investigation-validation.txt",(success?"PASS: ":"FAIL: ")+message);
        if(success)Debug.Log("INVESTIGATION_PASS "+message);else Debug.LogError(message);
        steps=null;if(Application.isBatchMode)EditorApplication.Exit(success?0:1);else EditorApplication.isPlaying=false;
    }
}
