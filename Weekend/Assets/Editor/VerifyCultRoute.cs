using System;
using System.IO;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.TimeSystem;
[InitializeOnLoad]public static class VerifyCultRoute{
static IEnumerator steps;static double next;static bool errors;static int checks;
const string Key="CultRouteTest";
static VerifyCultRoute(){EditorApplication.update+=Tick;Application.logMessageReceived+=(m,t,l)=>{if(steps!=null&&(l==LogType.Error||l==LogType.Exception)&&!t.Contains("UnityEditor.Search.SearchDatabase"))errors=true;};EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){steps=Steps();next=EditorApplication.timeSinceStartup+3;checks=0;errors=false;}if(s==PlayModeStateChange.ExitingPlayMode&&SessionState.GetBool(Key,false))Restore();};}
static void Check(bool b,string s){checks++;if(!b)throw new Exception(s);}
static ProgressionState State=>(ProgressionState)typeof(GameManager).GetProperty("State",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(GameManager.Instance);
static void Call(object o,string name)=>o.GetType().GetMethod(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(o,null);
[MenuItem("Prototype/Test cult route and inventory")]
public static void Run(){if(EditorApplication.isPlaying)return;if(SessionState.GetBool(Key,false))Restore();foreach(var suffix in new[]{"",".bak",".tmp"}){var p=ProgressionSave.PathName+suffix;SessionState.SetString(Key+suffix,File.Exists(p)?Convert.ToBase64String(File.ReadAllBytes(p)):"");}SessionState.SetBool(Key+"background",Application.runInBackground);Application.runInBackground=true;SessionState.SetBool(Key,true);SessionState.SetInt(Key+"lang",PlayerPrefs.GetInt("Story.Language",0));PlayerPrefs.SetInt("Story.Language",0);SessionState.SetBool(DayOneSupernovaEvent.SuppressionKey,true);ProgressionSave.Write(new ProgressionState{dreamSeen=true,journalUnlocked=true,investigationUnlocked=true,tutorialComplete=true});EditorSceneManager.OpenScene("Assets/Scenes/Apartment/Bedroom.unity");EditorApplication.isPlaying=true;}
static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(steps==null){if(!EditorApplication.isPlaying&&File.Exists("../cult-request.txt")){File.Delete("../cult-request.txt");Run();}return;}if(EditorApplication.timeSinceStartup<next)return;try{if(steps.MoveNext())next=EditorApplication.timeSinceStartup+(float)steps.Current;else Finish(null);}catch(Exception e){Finish(e.ToString());}}
static void Place(Vector2 p){var g=GameObject.FindWithTag("Player");g.transform.position=p;g.GetComponent<Rigidbody2D>().position=p;Physics2D.SyncTransforms();}
static IEnumerator Steps(){while(GameManager.Instance==null)yield return .2f;
var cult=CultNeighbor.Instance;Check(cult!=null,"Cult system bootstrapped");
Check(Resources.Load<AudioClip>("Audio/WoodHammer").samples>1000&&Resources.Load<AudioClip>("Audio/DoorKnock")!=null,"Real audio clips imported");
yield return 5f;Check(cult.GetComponent<AudioSource>()!=null&&cult.ImpactCount>0,"Wood impacts actually triggered in room");
State.sanityBurden=50;var ink=(Color)typeof(JournalNotebookUI).GetProperty("SelectionInk",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).GetValue(null);Check(ink.r<.1f,"SAN 50 selection is black");State.sanityBurden=51;ink=(Color)typeof(JournalNotebookUI).GetProperty("SelectionInk",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).GetValue(null);Check(ink.r>.5f&&ink.g<.2f,"SAN below 50 selection is red");State.sanityBurden=0;
SceneManager.LoadScene("Corridor");yield return 5f;
Check(State.neighborBarricadeSeen,"Day one hammering encounter completes");Check(!State.cultDayOneTalked,"Witnessing does not automatically talk");
Place(new Vector2(-4.7f,1));Check(cult.TalkDayOne(),"Day one conversation available");for(int i=0;i<StoryText.Current.cult.dayOne.Length;i++)cult.Advance();Check(State.cultDayOneTalked,"Completing dialogue records first gate");
Check(!cult.TalkDayOne(),"Day one conversation deduplicates");TimeManager.Instance.SleepUntilNextMorning();yield return .3f;
Check(!cult.KnockDoor()&&!cult.DoorOpen,"No note means no handover");Call(cult,"Close");
Check(Investigation.Collect("cult-door-note"),"Note deliberately collected");State.cultDayOneTalked=false;Check(!cult.KnockDoor(),"Missing day one cannot enter route");Call(cult,"Close");State.cultDayOneTalked=true;
Check(cult.KnockDoor()&&cult.DoorOpen,"Both prerequisites open eye closeup");yield return 1.4f;ScreenCapture.CaptureScreenshot("../cult-eye.png");yield return .3f;
for(int i=0;i<StoryText.Current.cult.door.Length;i++)cult.Advance();Check(State.cultTokenOffered&&!cult.HasToken,"Gift offered, not auto collected");yield return 1.2f;ScreenCapture.CaptureScreenshot("../cult-token.png");yield return .3f;
Check(cult.TakeToken()&&cult.HasToken&&cult.RouteEligible,"Explicit pickup stores key item and route eligibility");Check(!cult.TakeToken()&&State.inventory.Count==1,"Pickup deduplication");Check(ProgressionSave.Load().inventory.Contains(CultNeighbor.Token),"Inventory persists");
StoryText.ToggleLanguage();Check(StoryText.Current.cult.itemTitle=="Folded foil triangle","Item bilingual");StoryText.ToggleLanguage();
SceneManager.LoadScene("LivingRoom");yield return .8f;var tv=GameObject.Find("Television and stand").GetComponent<BoxCollider2D>();Check(tv.size.y<1&&tv.offset.y>0,"TV collider uses shallow footprint");
Place(new Vector2(-4.5f,-3.15f));var body=GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>();body.GetComponent<DeathBySeventhsDawn.Player.PlayerController2D>().enabled=false;body.linearVelocity=new Vector2(2,0);yield return 1f;Check(body.position.x>-2.8f,"TV bottom passable in physics");body.linearVelocity=Vector2.zero;
using(var sky=new OpeningSky()){var t=sky.Render(7,.7f);File.WriteAllBytes("../opening-mass.png",t.EncodeToPNG());Check(t.width==480&&t.filterMode==FilterMode.Point,"Giant remains on pixel grid");}
if(!Application.isBatchMode){State.sanityBurden=51;Investigation.Collect("my-observation-notes");var keyboard=InputSystem.AddDevice<Keyboard>("CultVisualKeyboard");keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.J));yield return .5f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());var journal=UnityEngine.Object.FindAnyObjectByType<StoryJournal>();var notebook=typeof(StoryJournal).GetField("notebook",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(journal);var style=(GUIStyle)typeof(JournalNotebookUI).GetField("rowTitle",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(notebook);if(!Application.isBatchMode)Check(style.hover.textColor.r<.4f&&style.active.textColor.r<.4f&&style.focused.textColor.r<.4f,"Notebook hover, active and focus remain dark ink");ScreenCapture.CaptureScreenshot("../notebook-selection-red.png");yield return .4f;InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Escape));yield return .3f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .3f;InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.I));yield return .5f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());Check(cult.BagOpen,"I opens inventory");ScreenCapture.CaptureScreenshot("../cult-bag.png");yield return .4f;InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Escape));yield return .3f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.RemoveDevice(keyboard);Check(!cult.BagOpen,"Esc closes inventory");State.sanityBurden=0;}var state=State;GameManager.Instance.StartNewJourney();yield return .5f;Check(!State.cultDayOneTalked&&!State.cultTokenOffered&&State.inventory.Count==0,"Full reset clears route and bag");Check(!errors,"No runtime errors");}
static void Finish(string error){steps=null;File.WriteAllText("../cult-validation.txt",error==null?"PASS: "+checks+" checks. Day-one gate; note gate; missed-route refusal; eye closeup; animated handover; manual pickup; inventory dedup/save; bilingual item; TV footprint; giant pixel render; full reset; no runtime errors.":"FAIL: "+error);EditorApplication.isPlaying=false;Restore();if(Application.isBatchMode)EditorApplication.Exit(error==null?0:1);}
static void Restore(){if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);Application.runInBackground=SessionState.GetBool(Key+"background",false);foreach(var suffix in new[]{"",".bak",".tmp"}){var p=ProgressionSave.PathName+suffix;var b=SessionState.GetString(Key+suffix,"");if(b.Length>0)File.WriteAllBytes(p,Convert.FromBase64String(b));else if(File.Exists(p))File.Delete(p);}PlayerPrefs.SetInt("Story.Language",SessionState.GetInt(Key+"lang",0));PlayerPrefs.Save();SessionState.SetBool(DayOneSupernovaEvent.SuppressionKey,false);steps=null;}
}
