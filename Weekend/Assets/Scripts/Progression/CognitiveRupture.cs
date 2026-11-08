using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;

namespace DeathBySeventhsDawn.Progression
{
    // The ordinary city is 2D. Only a real hostile encounter tears the perceptual filter.
    public sealed class CognitiveRupture : MonoBehaviour
    {
        static CognitiveRupture instance;
        public static bool IsActive => instance!=null&&instance.active;
        bool active, entering, loaded,tear=true,inPlace;
        float elapsed;
        Texture2D membrane;
        void Awake()
        {
            instance=this;
            membrane=new Texture2D(192,128,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear};
            for(int y=0;y<128;y++)for(int x=0;x<192;x++)
            {
                float fiber=Mathf.PerlinNoise(x*.12f,y*.04f),vein=Mathf.Abs(Mathf.Sin(x*.13f+Mathf.Sin(y*.10f)*3));
                var color=Color.Lerp(new Color(.045f,.006f,.012f),new Color(.43f,.065f,.08f),fiber);
                if(vein<.12f)color=new Color(.11f,.008f,.021f);
                membrane.SetPixel(x,y,color);
            }
            membrane.Apply();
        }
        void OnEnable()
        {
            if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=Cancel;
            if(TimeSystem.LoopManager.Instance!=null)TimeSystem.LoopManager.Instance.LoopRestarted+=Cancel;
        }
        void OnDisable()
        {
            if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=Cancel;
            if(TimeSystem.LoopManager.Instance!=null)TimeSystem.LoopManager.Instance.LoopRestarted-=Cancel;
        }
        void Cancel(){active=false;}
        public static void DebugSkip(){if(StoryDebugActions.Enabled&&instance!=null)instance.Cancel();}
        public static bool BeginEncounter(string id)
        {
            if(!City.CityWorld.IsCity(SceneManager.GetActiveScene().name)||SceneManager.GetActiveScene().name=="CityEncounter3D")return false;
            return BeginSpecialEncounter(id,"CityEncounter3D",true);
        }
        public static bool BeginSpecialEncounter(string id,string destination,bool sanityCost,bool tear=true)
        {
            if(instance==null||IsActive||SceneManager.GetActiveScene().name==destination||GameManager.Instance.HasEnded||!Application.CanStreamedLevelBeLoaded(destination))return false;
            var actor=GameObject.FindWithTag("Player");if(actor==null)return false;
            var state=GameManager.Instance.State;
            state.encounterReturnScene=SceneManager.GetActiveScene().name;state.encounterId=id;state.encounterDestination=destination;
            state.encounterReturnX=actor.transform.position.x;state.encounterReturnY=actor.transform.position.y;
            var body=actor.GetComponent<Rigidbody2D>();if(body!=null)body.linearVelocity=Vector2.zero;
            instance.inPlace=false;instance.Begin(true,tear);if(sanityCost)GameManager.Instance.LoseSanity(2);ProgressionSave.Write(state);return true;
        }
        public static bool ReturnFromSpecial()
        {
            if(instance==null||IsActive||GameManager.Instance.HasEnded||SceneManager.GetActiveScene().name!=GameManager.Instance.State.encounterDestination)return false;
            instance.inPlace=false;instance.Begin(false,instance.tear);ProgressionSave.Write(GameManager.Instance.State);return true;
        }
        public static bool Escape()
        {
            if(instance==null||IsActive||SceneManager.GetActiveScene().name!="CityEncounter3D"||GameManager.Instance.HasEnded)return false;
            var state=GameManager.Instance.State;
            if(!state.cityActions.Contains("escaped-"+state.encounterId))state.cityActions.Add("escaped-"+state.encounterId);
            return ReturnFromSpecial();
        }
        public static bool RevealInPlace()
        {if(instance==null||IsActive)return false;instance.inPlace=true;instance.Begin(true,true);return true;}
        void Begin(bool to3D,bool flesh=true)
        {
            entering=to3D;tear=flesh;active=true;loaded=false;elapsed=0;
            if(to3D&&tear)ApartmentAudio.Instance?.PlayEndingScream();
        }
        public static void RestoreExploration(GameObject actor)
        {
            if(!IsActive||instance.entering)return;
            var state=GameManager.Instance.State;if(SceneManager.GetActiveScene().name!=state.encounterReturnScene)return;
            var position=new Vector3(state.encounterReturnX,state.encounterReturnY,0);
            actor.transform.position=position;var body=actor.GetComponent<Rigidbody2D>();if(body!=null)body.position=position;
        }
        void Update()
        {
            if(!active||MainMenuUI.WorldBlocked||PauseMenuUI.WorldBlocked)return;
            elapsed+=Time.unscaledDeltaTime;
            float duration=entering?3.2f:1.4f;
            if(!loaded&&elapsed>=duration*.5f)
            {
                loaded=true;
                if(inPlace)return;
                string destination=entering?GameManager.Instance.State.encounterDestination:GameManager.Instance.State.encounterReturnScene;
                if(!Application.CanStreamedLevelBeLoaded(destination)){Debug.LogError("Encounter destination missing: "+destination);active=false;return;}
                SceneManager.LoadScene(destination);
            }
            if(elapsed>=duration){active=false;ProgressionSave.Write(GameManager.Instance.State);}
        }
        void OnGUI()
        {
            if(!active)return;
            var old=GUI.color;int depth=GUI.depth;GUI.depth=-180;
            float progress=Mathf.Clamp01(elapsed/(entering?3.2f:1.4f)),w=Screen.width,h=Screen.height;
            if(!tear){GUI.color=new Color(.01f,.015f,.02f,Mathf.Sin(progress*Mathf.PI));GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=old;GUI.depth=depth;return;}
            // Wet, asymmetric membranes split open. No strobing or alternating white frames.
            float opening=entering?Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.45f)/.55f)):1-Mathf.SmoothStep(0,1,progress);
            for(int strip=0;strip<48;strip++)
            {
                float y=h*strip/48f,edge=w*(.46f+.045f*Mathf.Sin(strip*.7f)),gap=w*.65f*opening;
                float alpha=entering?Mathf.Clamp01(progress*5):1-progress;
                GUI.color=new Color(1,1,1,alpha);
                GUI.DrawTextureWithTexCoords(new Rect(-gap,y,edge,h/48+2),membrane,new Rect(0,strip/48f,.5f,1/48f));
                GUI.DrawTextureWithTexCoords(new Rect(edge+gap,y,w-edge,h/48+2),membrane,new Rect(.5f,strip/48f,.5f,1/48f));
                GUI.color=new Color(.45f,.015f,.035f,alpha*.8f);
                GUI.DrawTexture(new Rect(edge-gap-3,y,6,h/48+3),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(edge+gap-3,y,6,h/48+3),Texture2D.whiteTexture);
            }
            GUI.color=new Color(.12f,0,.015f,Mathf.Sin(progress*Mathf.PI)*.3f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);
            var style=new GUIStyle(GUI.skin.label){font=GameTypography.Current,fontSize=Mathf.Max(20,Mathf.RoundToInt(h/30)),alignment=TextAnchor.MiddleCenter,wordWrap=true};
            GUI.color=new Color(.9f,.67f,.63f,1);
            var text=Story.StoryText.Current.city.Find(entering?"filter-tear":"filter-return");
            GUI.Label(new Rect(w*.15f,h*.74f,w*.7f,h*.18f),text.body,style);
            GUI.color=old;GUI.depth=depth;
        }
        void OnDestroy(){if(instance==this)instance=null;if(membrane!=null)Destroy(membrane);}
    }
}
