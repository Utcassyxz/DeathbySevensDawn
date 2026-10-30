using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
namespace DeathBySeventhsDawn.Interaction
{
    [DefaultExecutionOrder(-90)]
    public sealed class MorningRoutine:MonoBehaviour
    {
        public static MorningRoutine Instance{get;private set;}
        public static bool WorldBlocked=>Instance!=null&&(Instance.waking||Instance.openingFridge||Time.frameCount<=Instance.blockedFrame);
        public bool IsWaking=>waking; public bool FridgeAnimating=>openingFridge;
        public Vector2 Bedside{get;private set;}
        bool queued,waking,openingFridge,simulated,rendererEnabled;
        float age;int hit,blockedFrame=-1; GameObject player,fridge,placeholder,fridgeDoor;Rigidbody2D body;SpriteRenderer original;
        Vector2 bedPoint;Sprite square;Texture2D pixel;GUIStyle label,small;
        int Stage=>GameManager.Instance.State.morningStage;
        public string Objective=>Stage==2?StoryText.Current.morning.neighbor:Stage==3?(SceneManager.GetActiveScene().name=="LivingRoom"?StoryText.Current.morning.kitchen:StoryText.Current.morning.food):Stage==5&&age<5?StoryText.Current.morning.afterNews:null;
        void Awake(){Instance=this;pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();square=Sprite.Create(pixel,new Rect(0,0,1,1),Vector2.one*.5f,1);}
        void OnEnable(){SceneManager.sceneLoaded+=Loaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=Reset;}
        void OnDisable(){SceneManager.sceneLoaded-=Loaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=Reset;RestorePlayer();if(fridgeDoor!=null)Destroy(fridgeDoor);}
        void Loaded(Scene s,LoadSceneMode m){player=GameObject.FindWithTag("Player");fridge=s.name=="LivingRoom"?GameObject.Find("Refrigerator"):null;if(queued&&s.name=="Bedroom")BeginWake();}
        void Start(){if(GameManager.Instance.State.morningStage==1){queued=true;if(SceneManager.GetActiveScene().name!="Bedroom"&&Application.CanStreamedLevelBeLoaded("Bedroom"))SceneManager.LoadScene("Bedroom");}}
        void Reset(){queued=waking=openingFridge=false;RestorePlayer();if(fridgeDoor!=null)Destroy(fridgeDoor);blockedFrame=Time.frameCount;}
        public void QueueWake(){queued=true;GameManager.Instance.State.morningStage=1;ProgressionSave.Write(GameManager.Instance.State);}
        void BeginWake(){queued=false;player=GameObject.FindWithTag("Player");var bed=GameObject.Find("Bed");if(player==null||bed==null)return;body=player.GetComponent<Rigidbody2D>();original=player.GetComponentInChildren<SpriteRenderer>();var r=bed.GetComponent<SpriteRenderer>();var bounds=r!=null?r.bounds:new Bounds(bed.transform.position,new Vector3(1,2,0));bedPoint=bounds.center;Bedside=new Vector2(bounds.max.x+.48f,bounds.min.y-.25f);simulated=body!=null&&body.simulated;if(body!=null){body.linearVelocity=Vector2.zero;body.simulated=false;body.position=bedPoint;}player.transform.position=bedPoint;if(original!=null){rendererEnabled=original.enabled;original.enabled=false;}placeholder=new GameObject("Getting out of bed placeholder");var sr=placeholder.AddComponent<SpriteRenderer>();sr.sprite=square;sr.color=new Color(.48f,.65f,.58f);sr.sortingOrder=20;waking=true;age=0;hit=0;}
        void RestorePlayer(){if(body!=null)body.simulated=simulated;if(original!=null)original.enabled=rendererEnabled;if(placeholder!=null)Destroy(placeholder);}
        public void NeighborTalked(){if(Stage==2){GameManager.Instance.State.morningStage=3;ProgressionSave.Write(GameManager.Instance.State);}}
        public bool CanOpenFridge{get{if(fridge==null||player==null||Stage==1||Stage==2)return false;var c=fridge.GetComponent<Collider2D>();return Vector2.Distance(player.transform.position,c!=null?(Vector2)c.ClosestPoint(player.transform.position):(Vector2)fridge.transform.position)<.95f;}}
        public bool OpenFridge(){if(!CanOpenFridge||StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy)return false;openingFridge=true;age=0;GameManager.Instance.State.fridgeOpened=true;ProgressionSave.Write(GameManager.Instance.State);if(fridgeDoor!=null)Destroy(fridgeDoor);fridgeDoor=new GameObject("Fridge open placeholder");var sr=fridgeDoor.AddComponent<SpriteRenderer>();sr.sprite=square;sr.color=new Color(.76f,.81f,.69f);sr.sortingOrder=20;return true;}
        public void NewsFinished(){age=0;if(Stage>0)GameManager.Instance.State.morningStage=5;ProgressionSave.Write(GameManager.Instance.State);if(fridgeDoor!=null)Destroy(fridgeDoor);}
        void Update(){if(GameManager.Instance==null)return;age+=Time.unscaledDeltaTime;
            if(Stage==2&&TimeManager.Instance!=null&&TimeManager.Instance.Days>1){GameManager.Instance.State.morningStage=3;ProgressionSave.Write(GameManager.Instance.State);}
            if(queued&&SceneManager.GetActiveScene().name=="Bedroom")BeginWake();
            if(waking){if(age>hit*.85f&&hit<3){CultNeighbor.Instance?.Hammer();hit++;}float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-1.8f)/1.5f));Vector2 point=Vector2.Lerp(bedPoint,Bedside,t);player.transform.position=point;if(body!=null)body.position=point;placeholder.transform.position=point;placeholder.transform.localScale=Vector3.Lerp(new Vector3(.85f,.28f,1),new Vector3(.44f,.62f,1),t);if(age>=3.5f){waking=false;RestorePlayer();blockedFrame=Time.frameCount;GameManager.Instance.State.morningStage=2;ProgressionSave.Write(GameManager.Instance.State);}return;}
            if(openingFridge){if(fridgeDoor!=null&&fridge!=null){fridgeDoor.transform.position=fridge.transform.position+Vector3.left*Mathf.SmoothStep(0,.65f,Mathf.Clamp01(age/.7f));fridgeDoor.transform.localScale=new Vector3(.5f,1.1f,1);}if(age>=1.1f){openingFridge=false;blockedFrame=Time.frameCount;if(Stage>0)GameManager.Instance.State.morningStage=4;if(DayOneSupernovaEvent.Instance==null||!DayOneSupernovaEvent.Instance.PlayFromFridge())NewsFinished();}return;}
            if(StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy)return;if(player==null)player=GameObject.FindWithTag("Player");if(fridge==null&&SceneManager.GetActiveScene().name=="LivingRoom")fridge=GameObject.Find("Refrigerator");
            if(CanOpenFridge&&Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)OpenFridge();
        }
        void OnGUI(){if(GameManager.Instance==null||MainMenuUI.WorldBlocked)return;if(label==null){var f=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},24);label=new GUIStyle(GUI.skin.label){font=f,fontSize=24,wordWrap=true};small=new GUIStyle(label){fontSize=20};}
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;float scale=Mathf.Min(Screen.width/1200f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            try{if(waking){GUI.depth=-80;float black=age<.75f?1:age<1.12f?1-(age-.75f)/.37f:age<1.38f?(age-1.12f)/.26f:age<1.75f?1-(age-1.38f)/.37f:age<2.05f?.35f*Mathf.Sin((age-1.75f)/.3f*Mathf.PI):0;GUI.color=new Color(0,0,0,Mathf.Clamp01(black));GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(w*.24f,h*.78f,w*.52f,65),StoryText.Current.morning.wake,label);return;}
                if(openingFridge){GUI.Label(new Rect(w*.24f,h-100,w*.52f,65),StoryText.Current.morning.fridgeOpen,label);return;}
                if(StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy)return;
                if(Objective!=null){GUI.color=new Color(.025f,.04f,.04f,.88f);GUI.DrawTexture(new Rect(24,24,440,76),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(40,36,410,60),Objective,small);}
                if(CanOpenFridge)GUI.Label(new Rect(w/2-240,h-80,480,50),StoryText.Current.morning.fridgePrompt,label);
                if(Stage==3&&fridge!=null&&Camera.main!=null){var v=Camera.main.WorldToViewportPoint(fridge.transform.position);GUI.Label(new Rect(v.x*w-65,(1-v.y)*h-70,200,45),StoryText.Local("↓ 厨房","↓ Kitchen"),small);}
            }finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}}
        void OnDestroy(){if(Instance==this)Instance=null;RestorePlayer();if(square!=null)Destroy(square);if(pixel!=null)Destroy(pixel);}
    }
}
