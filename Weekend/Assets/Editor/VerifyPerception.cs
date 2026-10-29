using System;
using System.IO;
using System.Linq;
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
public static class VerifyPerception
{
    const string KeyName="VerifyPerception";static int phase;static double next,deadline;static Keyboard keyboard;static bool background;static string error;
    static SpriteRenderer[] original;static Color[] colors;static int colliders;
    static VerifyPerception(){EditorApplication.update+=Tick;Application.logMessageReceived+=(m,t,l)=>{if(SessionState.GetBool(KeyName,false)&&(l==LogType.Error||l==LogType.Exception))error=m;};}
    [MenuItem("Prototype/Test perception clues and music")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first.");
        foreach(string suffix in new[]{"",".bak",".tmp"})SessionState.SetString(KeyName+suffix,File.Exists(ProgressionSave.PathName+suffix)?Convert.ToBase64String(File.ReadAllBytes(ProgressionSave.PathName+suffix)):"");
        ProgressionSave.Write(new ProgressionState{dreamSeen=true});EditorSceneManager.OpenScene("Assets/Scenes/Apartment/Bedroom.unity");
        phase=0;next=deadline=0;error=null;SessionState.SetBool(KeyName,true);EditorApplication.isPlaying=true;
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));}
    static void Release(){InputSystem.QueueStateEvent(keyboard,new KeyboardState());}
    static ProgressionState State=>(ProgressionState)typeof(GameManager).GetProperty("State",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(GameManager.Instance);
    static void Place(Vector2 p){var player=GameObject.FindWithTag("Player");player.transform.position=p;player.GetComponent<Rigidbody2D>().position=p;Physics2D.SyncTransforms();}
    static void Tick()
    {
        if(!SessionState.GetBool(KeyName,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||PerceptionClues.Instance==null)return;
        if(error!=null){Finish(false,error);return;}
        double now=EditorApplication.timeSinceStartup;if(deadline==0)deadline=now+70;if(now>deadline){Finish(false,"Timeout "+phase);return;}if(now<next)return;
        try
        {
            var clue=PerceptionClues.Instance;var music=ApartmentAudio.Instance;var game=GameManager.Instance;float wait=.4f;
            switch(phase)
            {
                case 0:
                    background=Application.runInBackground;Application.runInBackground=true;keyboard=InputSystem.AddDevice<Keyboard>("PerceptionAcceptanceKeyboard");keyboard.MakeCurrent();
                    Check(music.Ready&&!clue.FilterOpen&&!clue.ClueVisible,"Initial music and hidden clues");
                    original=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);colors=original.Select(r=>r.color).ToArray();colliders=UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Length;
                    State.sanityBurden=64;ScreenCapture.CaptureScreenshot("../perception-normal.png");break;
                case 1:Check(!clue.FilterOpen&&!clue.ClueVisible,"SAN 36 preserves filter");State.sanityBurden=65;break;
                case 2:
                    Check(clue.FilterOpen&&clue.ClueVisible,"SAN 35 exposes clue");for(int i=0;i<original.Length;i++)Check(original[i].color==colors[i],"Original sprite colors unchanged");
                    Check(UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Length==colliders,"No collision change");Place(clue.CluePosition);ScreenCapture.CaptureScreenshot("../perception-hidden.png");wait=2;break;
                case 3:Check(music.HiddenBlend>.9f,"Music responds to low SAN");var loops=music.GetComponents<AudioSource>().Where(s=>s.loop).ToArray();Check(loops.All(s=>s.isPlaying)&&Math.Abs(loops[0].timeSamples-loops[1].timeSamples)<1024,"Music stems play in sync");Press(Key.E);break;
                case 4:Check(PerceptionClues.IsReading&&KnowledgeManager.Instance.Has(clue.CurrentId),"E reads and records clue");Release();ScreenCapture.CaptureScreenshot("../perception-reading.png");break;
                case 5:Press(Key.Escape);break;
                case 6:Release();break;
                case 7:Press(Key.E);break;
                case 8:Check(State.journal.Count==1,"Repeated clue is deduplicated");Release();clue.Close();State.sanityBurden=0;break;
                case 9:Check(!clue.ClueVisible&&!clue.FilterOpen&&State.journal.Count==1,"Recovered SAN hides marker, remembered clue retained");SceneManager.LoadScene("Corridor");State.neighborFlyersSeen=true;State.sanityBurden=65;break;
                case 10:Check(clue.ClueVisible,"Corridor clue present");SceneManager.LoadScene("Balcony");TimeManager.Instance.SleepUntilNextMorning();TimeManager.Instance.AdvanceTime();TimeManager.Instance.AdvanceTime();State.sanityBurden=0;break;
                case 11:Place(Resources.Load<StoryContent>("StoryContent").telescopePosition);break;
                case 12:Press(Key.E);break;
                case 13:
                    var story=UnityEngine.Object.FindAnyObjectByType<StoryJournal>();Check(story.IsRevelation&&music.RevelationCount==1,"Ancient god invokes realistic cut-in and sound");Check(Resources.Load<Texture2D>("Story/AncientGodRealistic")!=null,"Realistic image loaded");Release();ScreenCapture.CaptureScreenshot("../perception-revelation.png");wait=1;break;
                case 14:Press(Key.Escape);break;
                case 15:Release();Check(FirstLoopPrologue.Instance.ChapterTitleOpen,"Existing prologue transition preserved");game.StartNewJourney();break;
                case 16:
                    Check(!clue.FilterOpen&&!clue.ClueVisible&&State.journal.Count==0&&music.HiddenBlend<.05f&&music.RevelationCount==0,"Reset clears clues, reading and audio state");
                    Check(UnityEngine.Object.FindObjectsByType<ApartmentAudio>(FindObjectsSortMode.None).Length==1&&music.GetComponents<AudioSource>().Length==3,"Scene changes preserve one bounded music system");
                    Check(music.GetComponents<AudioSource>().All(s=>s.outputAudioMixerGroup!=null),"All music sources route through mixer");
                    Finish(true,"SAN 36/35 boundary; original sprite colors/colliders unchanged; hidden clue E reading and deduplication; recovery retains journal; three rooms; synchronized music layers and mixer routing; realistic ancient cut-in and one cue; full reset. Original save restored.");return;
            }
            phase++;next=now+wait;
        }
        catch(Exception ex){Finish(false,"Phase "+phase+": "+ex);}
    }
    static void Finish(bool success,string message)
    {
        SessionState.SetBool(KeyName,false);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);Application.runInBackground=background;
        foreach(string suffix in new[]{"",".bak",".tmp"}){string path=ProgressionSave.PathName+suffix,data=SessionState.GetString(KeyName+suffix,"");if(data!="")File.WriteAllBytes(path,Convert.FromBase64String(data));else if(File.Exists(path))File.Delete(path);}
        File.WriteAllText("../perception-validation.txt",(success?"PASS: ":"FAIL: ")+message);
        if(success)Debug.Log("PERCEPTION_PASS "+message);else Debug.LogError(message);
        if(Application.isBatchMode)EditorApplication.Exit(success?0:1);else EditorApplication.isPlaying=false;
    }
}
