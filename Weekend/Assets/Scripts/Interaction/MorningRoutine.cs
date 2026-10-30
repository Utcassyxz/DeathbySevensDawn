using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Knowledge;
namespace DeathBySeventhsDawn.Interaction
{
    // 1 wake, 2 neighbour, 3 kitchen, 4 bulletin, 5 sofa, 6 desk, 7 telescope, 8 record, 9 connect, 10 free exploration.
    [DefaultExecutionOrder(-90)]
    public sealed class MorningRoutine:MonoBehaviour
    {
        public static MorningRoutine Instance{get;private set;}
        enum Activity{None,Wake,Fridge,Nap,Write}
        Activity activity;
        public static bool WorldBlocked=>Instance!=null&&(Instance.activity!=Activity.None||Time.frameCount<=Instance.blockedFrame);
        public bool IsWaking=>activity==Activity.Wake;
        public bool FridgeAnimating=>activity==Activity.Fridge;
        public Vector2 Bedside{get;private set;}
        bool queued,simulated,rendererEnabled,firstFridge,fridgeVisibleOpen;
        float age;int hit,blockedFrame=-1;
        GameObject player,fridge,sofa,placeholder;Rigidbody2D body;SpriteRenderer original,fridgeRenderer;
        Sprite closedFridge,openedFridge,square;Texture2D pixel;Vector2 bedPoint,standPoint;GUIStyle label,small;
        ProgressionState State=>GameManager.Instance.State;
        public int Stage=>State.morningStage;
        public string Objective{
            get{var t=StoryText.Current.morning;
                if(Stage==2)return t.neighbor;
                if(Stage==3)return SceneManager.GetActiveScene().name=="LivingRoom"?t.kitchen:t.food;
                if(Stage==5)return t.sofa;
                if(Stage==6)return t.desk;
                if(Stage==7)return t.telescope;
                if(Stage==8)return t.investigate;if(Stage==9)return t.connect;
                return null;}
        }
        void Awake(){Instance=this;pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();square=Sprite.Create(pixel,new Rect(0,0,1,1),Vector2.one*.5f,1);openedFridge=Resources.Load<Sprite>("Tutorial/FridgeOpen");}
        void OnEnable(){SceneManager.sceneLoaded+=Loaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=Reset;}
        void OnDisable(){SceneManager.sceneLoaded-=Loaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=Reset;RestorePlayer();}
        void Bind(){player=GameObject.FindWithTag("Player");fridge=GameObject.Find("Refrigerator");sofa=GameObject.Find("Old sofa");fridgeRenderer=fridge!=null?fridge.GetComponent<SpriteRenderer>():null;if(fridgeRenderer!=null&&fridgeRenderer.sprite!=openedFridge)closedFridge=fridgeRenderer.sprite;fridgeVisibleOpen=false;if(State.fridgeDoorOpen)SetFridge(true);}
        void Loaded(Scene scene,LoadSceneMode mode){RestorePlayer();activity=Activity.None;Bind();if(queued&&scene.name=="Bedroom")BeginWake();if(Stage==4&&!State.dayOneSupernovaSeen&&scene.name=="LivingRoom")SetFridge(true);}
        void Start(){Bind();if(Stage==1){queued=true;if(SceneManager.GetActiveScene().name!="Bedroom")SceneManager.LoadScene("Bedroom");}else if(Stage==4&&!State.dayOneSupernovaSeen){if(SceneManager.GetActiveScene().name!="LivingRoom")SceneManager.LoadScene("LivingRoom");}if(Stage>=6&&Stage<10)RestoreEvening();}
        void Reset(){queued=false;RestorePlayer();activity=Activity.None;SetFridge(false);blockedFrame=Time.frameCount;}
        void Save()=>ProgressionSave.Write(State);
        public void QueueWake(){queued=true;State.morningStage=1;Save();}
        void FreezePlayer(){if(player==null)player=GameObject.FindWithTag("Player");body=player.GetComponent<Rigidbody2D>();original=player.GetComponentInChildren<SpriteRenderer>();simulated=body!=null&&body.simulated;rendererEnabled=original!=null&&original.enabled;if(body!=null){body.linearVelocity=Vector2.zero;body.simulated=false;}if(original!=null)original.enabled=false;placeholder=new GameObject("Tutorial posture");var sr=placeholder.AddComponent<SpriteRenderer>();sr.sprite=original!=null?original.sprite:square;sr.color=original!=null?original.color:new Color(.48f,.65f,.58f);sr.sortingOrder=20;placeholder.transform.localScale=original!=null?original.transform.lossyScale:Vector3.one;}
        void MoveBody(Vector2 p){player.transform.position=p;if(body!=null)body.position=p;}
        void RestorePlayer(){if(body!=null)body.simulated=simulated;if(original!=null)original.enabled=rendererEnabled;if(placeholder!=null)Destroy(placeholder);placeholder=null;}
        void BeginWake(){queued=false;var bed=GameObject.Find("Bed");if(bed==null||player==null)return;var r=bed.GetComponent<SpriteRenderer>();var b=r.bounds;bedPoint=b.center;Bedside=new Vector2(b.max.x+.48f,b.min.y-.25f);FreezePlayer();MoveBody(bedPoint);activity=Activity.Wake;age=0;hit=0;}
        public void NeighborTalked(){if(Stage==2){State.morningStage=3;Save();}}
        float Distance(GameObject target){if(target==null||player==null)return float.PositiveInfinity;var c=target.GetComponent<Collider2D>();return Vector2.Distance(player.transform.position,c!=null?c.ClosestPoint(player.transform.position):(Vector2)target.transform.position);}
        public bool CanOpenFridge=>fridge!=null&&Stage!=1&&Stage!=2&&Distance(fridge)<.75f;
        public bool CanNap=>Stage==5&&sofa!=null&&Distance(sofa)<.85f;
        public bool CanWrite=>Stage==6&&SceneManager.GetActiveScene().name=="Bedroom"&&player!=null&&Vector2.Distance(player.transform.position,new Vector2(2.7f,-1.1f))<.9f;
        void SetFridge(bool open){fridgeVisibleOpen=open;if(fridgeRenderer!=null)fridgeRenderer.sprite=open&&openedFridge!=null?openedFridge:closedFridge;}
        public bool OpenFridge(){if(!CanOpenFridge||StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy)return false;
            firstFridge=!State.fridgeOpened&&!State.dayOneNewsStarted&&!State.dayOneSupernovaSeen&&TimeManager.Instance.Days==1;
            State.fridgeOpened=true;if(firstFridge)State.morningStage=4;Save();SetFridge(!fridgeVisibleOpen);State.fridgeDoorOpen=fridgeVisibleOpen;Save();activity=Activity.Fridge;age=0;return true;}
        public void NewsFinished(){if(Stage==4){State.morningStage=5;Save();}age=0;}
        public bool BeginNap(){if(!CanNap||StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy)return false;FreezePlayer();var b=sofa.GetComponent<SpriteRenderer>().bounds;bedPoint=b.center;standPoint=new Vector2(b.center.x,b.min.y-.45f);MoveBody(bedPoint);placeholder.transform.position=bedPoint;placeholder.transform.localRotation=Quaternion.Euler(0,0,75);activity=Activity.Nap;age=0;return true;}
        void RestoreEvening(){var t=TimeManager.Instance;for(int i=0;i<2&&t.Days==1&&t.Period<TimePeriod.Evening&&!GameManager.Instance.HasEnded;i++)t.AdvanceTime();}
        public bool BeginWriting(){if(!CanWrite||StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy)return false;FreezePlayer();standPoint=player.transform.position;placeholder.transform.position=standPoint;activity=Activity.Write;age=0;return true;}
        void Seed(string id,bool evidence){if(State.journal.Exists(e=>e.id==id))return;var def=Investigation.Evidence(id);State.journal.Add(new JournalEntry{id=id,title=def!=null?def.title:StoryText.Current.morning.journalTitle,body=def!=null?def.body:StoryText.Current.morning.journalBody,day=1,loop=GameManager.Instance.LoopCount,isKnowledge=evidence});if(evidence&&!State.knowledgeIds.Contains(id))State.knowledgeIds.Add(id);}
        void FinishWriting(){State.journalUnlocked=true;Seed("first-evening-journal",false);Seed("my-observation-notes",true);if(State.cultDayOneTalked)Seed("401-warning",true);if(State.dayOneSupernovaSeen)Seed("skt079-broadcast",true);foreach(var id in State.initialEvidenceIds)Seed(id,true);State.initialEvidenceIds.Clear();State.morningStage=7;Save();Object.FindAnyObjectByType<StoryJournal>()?.ShowJournal();}
        public void TelescopeObserved(){if(Stage==7){State.investigationUnlocked=true;State.morningStage=8;Save();}}
        public void TelescopeRecorded(){if(Stage==8){State.morningStage=9;Save();}}
        void Update(){if(GameManager.Instance==null||MainMenuUI.IsVisible||GameSettingsUI.WorldBlocked)return;age+=Time.unscaledDeltaTime;
            if(Stage==9&&Investigation.Has("deduction-astronomy")){State.morningStage=10;State.tutorialComplete=true;Save();}
            if(queued&&SceneManager.GetActiveScene().name=="Bedroom")BeginWake();
            if(activity==Activity.Wake){if(age>hit*.85f&&hit<3){CultNeighbor.Instance?.Hammer();hit++;}float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-1.8f)/1.5f));var p=Vector2.Lerp(bedPoint,Bedside,t);MoveBody(p);placeholder.transform.position=p;placeholder.transform.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(80,0,t));if(age>=3.5f){MoveBody(Bedside);RestorePlayer();activity=Activity.None;blockedFrame=Time.frameCount;State.morningStage=2;Save();}return;}
            if(activity==Activity.Fridge){if(age>=.75f){activity=Activity.None;blockedFrame=Time.frameCount;if(firstFridge){firstFridge=false;if(!DayOneSupernovaEvent.Instance.PlayFromFridge())NewsFinished();}}return;}
            if(activity==Activity.Nap||activity==Activity.Write){bool nap=activity==Activity.Nap;if(age>=(nap?3.4f:2.0f)){MoveBody(standPoint);RestorePlayer();activity=Activity.None;blockedFrame=Time.frameCount;if(nap){RestoreEvening();State.morningStage=6;Save();}else FinishWriting();}return;}
            if(Stage==4&&!State.dayOneSupernovaSeen&&SceneManager.GetActiveScene().name=="LivingRoom"&&!DayOneSupernovaEvent.Instance.IsPresenting){if(State.dayOneNewsStarted)DayOneSupernovaEvent.Instance.Resume();else DayOneSupernovaEvent.Instance.PlayFromFridge();}
            if(StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy)return;if(player==null)Bind();var k=Keyboard.current;if(k==null||!k.eKey.wasPressedThisFrame)return;
            if(CanOpenFridge)OpenFridge();else if(CanNap)BeginNap();else if(CanWrite)BeginWriting();
        }
        void DrawDestination(float w,float h){if(Camera.main==null)return;GameObject goal=Stage==3?fridge:Stage==5?sofa:Stage==7?GameObject.Find("阳台望远镜"):null;Vector3 point=Stage==6&&SceneManager.GetActiveScene().name=="Bedroom"?new Vector3(2.7f,-1.1f,0):goal!=null?goal.transform.position:new Vector3(0,0,-999);if(point.z< -900)return;var v=Camera.main.WorldToViewportPoint(point);if(v.z<0)return;GUI.Label(new Rect(v.x*w-95,(1-v.y)*h-80,220,55),StoryText.Local("↓ 到这里","↓ This way"),small);}
        void OnGUI(){if(GameManager.Instance==null||MainMenuUI.WorldBlocked||GameSettingsUI.WorldBlocked)return;if(label==null){label=new GUIStyle(GUI.skin.label){font=GameTypography.Current,fontSize=24,wordWrap=true};small=new GUIStyle(label){fontSize=20};}GameTypography.Use(label,small);
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;float scale=Mathf.Min(Screen.width/1200f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            try{if(activity==Activity.Wake||activity==Activity.Nap||activity==Activity.Write){GUI.depth=-80;float black=activity==Activity.Wake?(age<.75f?1:age<1.12f?1-(age-.75f)/.37f:age<1.38f?(age-1.12f)/.26f:age<1.75f?1-(age-1.38f)/.37f:0):Mathf.SmoothStep(0,.95f,Mathf.Clamp01(age/.8f));GUI.color=new Color(0,0,0,black);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(w*.2f,h*.77f,w*.6f,90),activity==Activity.Wake?StoryText.Current.morning.wake:activity==Activity.Nap?StoryText.Current.morning.nap:StoryText.Current.morning.writing,label);return;}
                if(activity==Activity.Fridge){GUI.Label(new Rect(w*.24f,h-100,w*.52f,65),StoryText.Current.morning.fridgeOpen,label);return;}
                if(StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy)return;
                if(Objective!=null){DrawDestination(w,h);GUI.color=new Color(.025f,.04f,.04f,.88f);GUI.DrawTexture(new Rect(24,24,500,90),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(40,36,470,70),Objective,small);}
                string prompt=CanOpenFridge?StoryText.Local(fridgeVisibleOpen?"E  关上冰箱":"E  打开冰箱",fridgeVisibleOpen?"E  Close refrigerator":"E  Open refrigerator"):CanNap?StoryText.Local("E  在沙发上歇一会儿","E  Rest on the sofa"):CanWrite?StoryText.Local("E  坐到桌前，写下今天的事","E  Sit at the desk and write"):null;
                if(prompt!=null)GUI.Label(new Rect(w/2-300,h-80,600,60),prompt,label);
            }finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}}
        void OnDestroy(){if(Instance==this)Instance=null;RestorePlayer();if(square!=null)Destroy(square);if(pixel!=null)Destroy(pixel);}
    }
}
