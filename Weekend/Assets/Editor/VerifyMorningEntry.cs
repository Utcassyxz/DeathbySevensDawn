using System;
using System.IO;
using System.Linq;
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
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.TimeSystem;
[InitializeOnLoad]public static class VerifyMorningEntry
{
    const string TestKey="MorningEntryAcceptance";static IEnumerator steps;static double next,deadline;static int count;static string error;static Keyboard keyboard;static bool pendingInput;static Key pendingKey;
    static ProgressionState State=>(ProgressionState)typeof(GameManager).GetProperty("State",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(GameManager.Instance);
    static object Field(object o,string n)=>o.GetType().GetField(n,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(o);
    static void Call(object o,string n)=>o.GetType().GetMethod(n,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(o,null);
    static StoryJournal Journal=>UnityEngine.Object.FindAnyObjectByType<StoryJournal>();
    static void Check(bool ok,string reason){count++;if(!ok)throw new Exception(reason);}
    static VerifyMorningEntry(){InputSystem.onBeforeUpdate+=ApplyPendingInput;EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(!SessionState.GetBool(TestKey,false))return;if(s==PlayModeStateChange.EnteredPlayMode){steps=Steps();next=EditorApplication.timeSinceStartup+3;deadline=next+150;count=0;error=null;}if(s==PlayModeStateChange.ExitingPlayMode)Restore();};Application.logMessageReceived+=(m,t,l)=>{if(steps!=null&&(l==LogType.Error||l==LogType.Exception)&&!t.Contains("UnityEditor.Search.SearchDatabase"))error=m;};}
    [MenuItem("Prototype/Test full first-day onboarding and settings")]
    public static void Run(){if(EditorApplication.isPlaying)return;if(SessionState.GetBool(TestKey,false))Restore();foreach(var suffix in new[]{"",".bak",".tmp"}){var p=ProgressionSave.PathName+suffix;SessionState.SetString(TestKey+suffix,File.Exists(p)?Convert.ToBase64String(File.ReadAllBytes(p)):"");}SessionState.SetInt(TestKey+"Focus",(int)InputSystem.settings.backgroundBehavior);SessionState.SetInt(TestKey+"Lang",PlayerPrefs.GetInt("Story.Language",0));SessionState.SetFloat(TestKey+"Volume",GameSettingsUI.MasterVolume);PlayerPrefs.SetInt("Story.Language",0);SessionState.SetBool(TestKey+"Bg",PlayerSettings.runInBackground);Application.runInBackground=true;SessionState.SetBool(TestKey,true);SessionState.SetBool(DayOneSupernovaEvent.SuppressionKey,true);ProgressionSave.Write(new ProgressionState());EditorSceneManager.OpenScene("Assets/Scenes/Apartment/Bedroom.unity");EditorApplication.isPlaying=true;}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(steps==null){if(!EditorApplication.isPlaying&&File.Exists("../morning-entry-request.txt")){File.Delete("../morning-entry-request.txt");Run();}return;}if(EditorApplication.timeSinceStartup<next)return;try{if(error!=null)throw new Exception(error);if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Acceptance timeout");if(steps.MoveNext())next=EditorApplication.timeSinceStartup+(float)steps.Current;else Finish(null);}catch(Exception e){Finish(e.ToString());}}
    static void ApplyPendingInput(){if(!pendingInput||InputState.currentUpdateType!=InputUpdateType.Dynamic||keyboard==null)return;keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,pendingKey==Key.None?new KeyboardState():new KeyboardState(pendingKey));pendingInput=false;}
    static void Press(Key key){if(Application.isBatchMode){pendingKey=key;pendingInput=true;}else{keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));}}static void Release(){if(Application.isBatchMode){pendingKey=Key.None;pendingInput=true;}else InputSystem.QueueStateEvent(keyboard,new KeyboardState());}
    static void Place(Vector2 p){var g=GameObject.FindWithTag("Player");g.transform.position=p;g.GetComponent<Rigidbody2D>().position=p;Physics2D.SyncTransforms();}
    static void Capture(string name){if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot("../tutorial-"+name+".png");}
    static IEnumerator Steps(){
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;if(StoryText.English)StoryText.ToggleLanguage();while(GameManager.Instance==null)yield return .2f;
        keyboard=InputSystem.AddDevice<Keyboard>("FirstDayAcceptanceKeyboard");keyboard.MakeCurrent();
        Check(State.version==2&&!State.journalUnlocked&&!State.investigationUnlocked&&!State.tutorialComplete,"Fresh journey hides journal and investigation");
        Check(Resources.Load<Sprite>("Tutorial/FridgeOpen")!=null,"Actual opened-fridge sprite loads");
        Check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Story/LobbyNoticeFlyer.prefab")!=null,"Flyer design archived for lobby");
        MainMenuUI.ReturnToMainMenu();yield return 5f;
        Check(StoryText.Current.mainMenu.title=="七日"&&StoryText.Current.mainMenu.subtitle=="SEVEN DAY","Chinese title has uppercase English subtitle");Capture("title-chinese");yield return .3f;
        var audio=ApartmentAudio.Instance;var title=(AudioSource)Field(audio,"title");var home=(AudioSource)Field(audio,"home");
        Check(audio.Ready&&title.isPlaying&&title.volume>home.volume,"Independent title score plays");Check(Math.Abs(title.timeSamples-home.timeSamples)<1024,"Music remains phase aligned");
        GameSettingsUI.Show();GameSettingsUI.SetVolume(.37f);StoryText.ToggleLanguage();yield return .3f;
        Check(GameSettingsUI.IsOpen&&Mathf.Abs(AudioListener.volume-.37f)<.001f&&Mathf.Abs(GameSettingsUI.MasterVolume-.37f)<.001f,"Volume applies and saves");
        Check(StoryText.English&&StoryText.Current.mainMenu.title=="SEVEN DAY"&&GameTypography.Current==GameTypography.English,"English uses formal serif typography and uppercase title");
        Capture("settings-english");yield return .3f;Press(Key.Enter);yield return .25f;Release();Check(!MainMenuUI.IsOpening,"Enter inside settings cannot start the game");
        Press(Key.Escape);yield return .3f;Release();Check(!GameSettingsUI.IsOpen&&MainMenuUI.IsVisible,"Esc closes settings back to title");Capture("title-english");yield return .3f;StoryText.ToggleLanguage();GameSettingsUI.SetVolume(SessionState.GetFloat(TestKey+"Volume",1));
        MainMenuUI.Instance.StartGame();MainMenuUI.Instance.StartGame();yield return 6.5f;Check(audio.ScreamPlaying&&MainMenuUI.IsOpening,"Opening sound plays and duplicate start is ignored");
        Capture("eyes");yield return .4f;
        if(!Application.isBatchMode){var screen=new Texture2D(2,2);screen.LoadImage(File.ReadAllBytes("../tutorial-eyes.png"));Check(screen.GetPixel((int)(screen.width*460f/480),(int)(screen.height*140f/270)).maxColorComponent<.025f,"Final opening stays black");UnityEngine.Object.DestroyImmediate(screen);}
        yield return 5f;Check(!MainMenuUI.IsOpening&&Field(Journal,"page").ToString()=="Dream","Opening leads into dream");
        Check(title.volume>0&&home.volume>0,"Music crossfades into the dream");Press(Key.E);yield return .25f;Release();Check((int)Field(Journal,"dreamPage")==0,"Dream ignores E");
        for(int i=0;i<4;i++){Press(Key.Enter);yield return .3f;Release();yield return .2f;}Check(State.dreamSeen&&MorningRoutine.Instance.IsWaking,"Enter wakes beside bed");yield return 4f;
        var routine=MorningRoutine.Instance;var p=GameObject.FindWithTag("Player");Check(Vector2.Distance(p.transform.position,routine.Bedside)<.2f&&p.GetComponent<Rigidbody2D>().simulated,"Wake restores player body");
        Check(CultNeighbor.Instance.ImpactCount>0&&State.morningStage==2,"Hammer cues neighbour objective");Check(State.journal.Count==0,"Dream and wake add no journal entries");
        Press(Key.J);yield return .25f;Release();Press(Key.B);yield return .25f;Release();Check(Field(Journal,"page").ToString()=="None"&&!Investigation.Collect("tank-label"),"Journal, board and collection stay locked before writing");Capture("wake-no-journal");yield return .3f;
        var window=UnityEngine.Object.FindAnyObjectByType<WindowLookout2D>();var before=p.transform.position;Place((Vector2)window.transform.position+Vector2.down*1.2f);yield return .25f;Check(!window.CanInteract&&!window.TryOpen(),"Distant window does not trigger");Place((Vector2)window.transform.position+Vector2.down*.25f);yield return .25f;Check(window.TryOpen(),"Window still works close by");window.CloseView();Place(before);
        SceneManager.LoadScene("Corridor");yield return 5f;Check(CultNeighbor.WorldBlocked&&Field(CultNeighbor.Instance,"view").ToString()=="DayOne","Neighbour initiates warning");Check(GameObject.Find("401 flyer")==null&&!Investigation.Has("401-flyers"),"No flyer or collected flyer on fourth floor");Capture("neighbour");yield return .3f;
        for(int i=0;i<StoryText.Current.cult.dayOne.Length;i++)CultNeighbor.Instance.Advance();Check(State.cultDayOneTalked&&routine.Stage==3,"Complete warning leads home");Check(State.journal.Count==0,"Warning is held for initial diary, not recorded early");
        SceneManager.LoadScene("LivingRoom");yield return .7f;var fridge=GameObject.Find("Refrigerator");var fridgeRenderer=fridge.GetComponent<SpriteRenderer>();var closed=fridgeRenderer.sprite;var fb=fridgeRenderer.bounds;Place(new Vector2(fb.center.x,fb.min.y-.3f));yield return .3f;
        Check(routine.OpenFridge(),"Kitchen fridge is interactive");Check(fridgeRenderer.sprite==Resources.Load<Sprite>("Tutorial/FridgeOpen")&&fridgeRenderer.sprite!=closed,"Opening replaces the sprite instead of drawing a rectangle");yield return 1f;
        var news=DayOneSupernovaEvent.Instance;Check(news.IsPresenting&&State.dayOneNewsStarted&&routine.Stage==4,"First fridge opening starts bulletin once");Check(!routine.OpenFridge()&&!news.PlayFromFridge(),"Fridge and bulletin reject concurrent repeats");
        Press(Key.R);yield return .25f;Release();Check(State.journal.Count==0&&!State.journalUnlocked,"News has no R journal collection");Capture("fridge-news");yield return .3f;
        news.Advance();int page=news.PageIndex;Check(page==1&&ProgressionSave.Load().newsPage==1,"News progress saves per page");SceneManager.LoadScene("Bedroom");yield return .4f;SceneManager.LoadScene("LivingRoom");yield return .7f;Check(news.IsPresenting&&news.PageIndex==page,"Interrupted bulletin resumes at saved page");
        for(int i=page;i<StoryText.Current.dayOneBroadcast.pages.Length;i++)news.Advance();yield return .4f;Check(State.dayOneSupernovaSeen&&routine.Stage==5&&State.journal.Count==0,"Finishing news leads to sofa without a diary");
        fridge=GameObject.Find("Refrigerator");fb=fridge.GetComponent<SpriteRenderer>().bounds;Place(new Vector2(fridge.transform.position.x,fb.min.y-.3f));yield return .3f;
        for(int i=0;i<3;i++){Check(routine.OpenFridge(),"Fridge can toggle after news "+i);yield return 1f;Check(!news.IsPresenting&&routine.Stage==5&&State.newsPage==4,"Repeat fridge never rewinds tutorial or news "+i);}
        Check(TimeManager.Instance.Days==1&&TimeManager.Instance.Period==TimePeriod.Morning,"Fridge toggles consume no time");
        var sofa=GameObject.Find("Old sofa").GetComponent<SpriteRenderer>().bounds;Place(new Vector2(sofa.center.x,sofa.min.y-.3f));yield return .3f;Check(routine.CanNap&&routine.BeginNap(),"Sofa begins guided nap");Capture("sofa-nap");yield return 3.8f;
        Check(TimeManager.Instance.Days==1&&TimeManager.Instance.Period==TimePeriod.Evening&&routine.Stage==6,"Sofa nap reaches same-day evening exactly");Check(!State.journalUnlocked&&State.journal.Count==0,"Waking on sofa still does not grant journal");
        SceneManager.LoadScene("Bedroom");yield return .6f;Place(new Vector2(2.7f,-1.1f));yield return .3f;Check(routine.CanWrite&&routine.BeginWriting(),"Player reaches desk to write");yield return 2.3f;
        Check(State.journalUnlocked&&!State.investigationUnlocked&&routine.Stage==7,"Writing unlocks journal before investigation");Check(Field(Journal,"page").ToString()=="Diary","First journal opens after writing");
        Check(State.journal.Count==4&&State.journal.Select(e=>e.id).Distinct().Count()==4&&Investigation.Has("401-warning")&&Investigation.Has("skt079-broadcast")&&Investigation.Has("my-observation-notes"),"Day-one initial information is inserted once as a batch");Check(!Investigation.Has("401-flyers")&&!Investigation.Collect("apartment-telescope"),"Reserved flyer stays absent and active investigation stays locked");Capture("first-journal");yield return .3f;
        Press(Key.Escape);yield return .3f;Release();Check(!routine.BeginWriting(),"Writing cannot be repeated for duplicate initial information");
        SceneManager.LoadScene("Balcony");yield return .6f;Place(Resources.Load<StoryContent>("StoryContent").telescopePosition);yield return .3f;Press(Key.E);yield return .3f;Release();Check(Field(Journal,"page").ToString()=="Telescope","Evening telescope inspection opens");Check(!Journal.CollectTelescopeEvidence(),"R cannot collect telescope before first observation");
        Press(Key.T);yield return .35f;Release();Check(State.investigationUnlocked&&routine.Stage==8&&!Journal.IsRevelation,"First evening teaches investigation without ancient revelation");Check(State.journal.Count==4&&!Investigation.Has("apartment-telescope"),"Observation itself does not auto-collect");Capture("investigation-lesson");yield return .3f;
        Check(Journal.CollectTelescopeEvidence()&&!Journal.CollectTelescopeEvidence()&&routine.Stage==9&&!State.tutorialComplete,"R records telescope once and leads to connection lesson");
        Press(Key.Escape);yield return .3f;Release();Press(Key.B);yield return .3f;Release();var board=(InvestigationBoardUI)Field(Journal,"board");board.Select("my-observation-notes");board.Select("apartment-telescope");
        Press(Key.Enter);yield return .3f;Release();Check(!State.tutorialComplete,"Tentative link is not a completed deduction");yield return .2f;Press(Key.Enter);yield return .3f;Release();yield return .3f;
        Check(Investigation.Has("deduction-astronomy")&&State.tutorialComplete&&routine.Stage==10,"Confirmed astronomy deduction completes onboarding");Capture("first-deduction");yield return .3f;
        var saved=ProgressionSave.Load();Check(saved.version==2&&saved.journalUnlocked&&saved.investigationUnlocked&&saved.tutorialComplete&&saved.dayOneSupernovaSeen,"Tutorial completion persists");
        var legacy=JsonUtility.FromJson<ProgressionState>("{\"version\":1,\"journal\":[{\"id\":\"skt079-broadcast\"}],\"knowledgeIds\":[]}");Investigation.Normalize(legacy);Check(legacy.version==2&&legacy.journal.Count==1&&legacy.journalUnlocked&&legacy.investigationUnlocked,"Existing collected saves migrate without losing access");
        Press(Key.Escape);yield return .3f;Release();GameManager.Instance.StartNewJourney();yield return .6f;Check(!State.journalUnlocked&&!State.investigationUnlocked&&!State.tutorialComplete&&!State.dayOneNewsStarted&&!State.fridgeOpened&&!State.fridgeDoorOpen&&State.newsPage==0&&State.journal.Count==0&&routine.Stage==0,"Full new-journey reset clears all tutorial and news state");Check(Field(Journal,"page").ToString()=="Dream"&&TimeManager.Instance.Period==TimePeriod.Morning,"Full reset returns to dream and morning");Check(error==null,"No game runtime errors");
    }
    static void Finish(string failure){steps=null;File.WriteAllText("../morning-entry-validation.txt",failure==null?"PASS: "+count+" checks; settings and serif fonts; uppercase title; hidden journal; archived lobby flyer; real fridge sprite; once-only news and interruption recovery; sofa to evening; desk writing and deduplicated initial information; telescope investigation and confirmed connection; persistence, migration and complete reset.":"FAIL: "+failure);EditorApplication.isPlaying=false;Restore();if(Application.isBatchMode)EditorApplication.Exit(failure==null?0:1);}
    static void Restore(){if(!SessionState.GetBool(TestKey,false))return;SessionState.SetBool(TestKey,false);InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)SessionState.GetInt(TestKey+"Focus",0);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);keyboard=null;foreach(var suffix in new[]{"",".bak",".tmp"}){var p=ProgressionSave.PathName+suffix;var b=SessionState.GetString(TestKey+suffix,"");if(b.Length>0)File.WriteAllBytes(p,Convert.FromBase64String(b));else if(File.Exists(p))File.Delete(p);}PlayerPrefs.SetInt("Story.Language",SessionState.GetInt(TestKey+"Lang",0));GameSettingsUI.SetVolume(SessionState.GetFloat(TestKey+"Volume",1));PlayerPrefs.Save();Application.runInBackground=SessionState.GetBool(TestKey+"Bg",false);PlayerSettings.runInBackground=SessionState.GetBool(TestKey+"Bg",false);SessionState.SetBool(DayOneSupernovaEvent.SuppressionKey,false);steps=null;}
}
