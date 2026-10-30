using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
namespace DeathBySeventhsDawn.Interaction
{
    // Legacy class / save IDs remain compatible. The event is now an ordinary, fridge-triggered bulletin.
    public sealed class DayOneSupernovaEvent:MonoBehaviour
    {
        public const string SuppressionKey="DayOneSupernovaSuppressed";
        public static DayOneSupernovaEvent Instance{get;private set;}
        public static bool WorldBlocked=>Instance!=null&&(Instance.presenting||Time.frameCount<=Instance.blockedUntilFrame);
#if UNITY_EDITOR
        public static bool Suppressed=>UnityEditor.SessionState.GetBool(SuppressionKey,false);
#else
        public static bool Suppressed=>false;
#endif
        bool presenting,pending;int page,openedFrame,blockedUntilFrame=-1;GUIStyle heading,body,small;SpriteRenderer television;Color original;
        public bool IsPresenting=>presenting;
        public int PageIndex=>page;
        void Awake(){Instance=this;}
        void OnEnable(){if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=ResetEvent;SceneManager.sceneLoaded+=Loaded;}
        void OnDisable(){if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=ResetEvent;SceneManager.sceneLoaded-=Loaded;Close();}
        void Loaded(Scene s,LoadSceneMode m){if(presenting)Close();}
        void ResetEvent(){pending=false;Close();}
        public bool PlayFromFridge(){if(GameManager.Instance==null||GameManager.Instance.State.dayOneSupernovaSeen||TimeManager.Instance.Days!=1||SceneManager.GetActiveScene().name!="LivingRoom")return false;StartPresentation();return true;}
        void StartPresentation(){if(presenting)return;presenting=true;pending=false;page=0;openedFrame=Time.frameCount;var tv=GameObject.Find("Television and stand");television=tv!=null?tv.GetComponent<SpriteRenderer>():null;if(television!=null){original=television.color;television.color=new Color(1,.98f,.9f);}}
        public void Advance(){if(!presenting)return;page++;if(page>=StoryText.Current.dayOneBroadcast.pages.Length)Finish();}
        void Update(){if(pending&&!StoryJournal.WorldBlocked&&SceneManager.GetActiveScene().name=="LivingRoom")StartPresentation();if(!presenting||Time.frameCount<=openedFrame+1)return;var k=Keyboard.current;if(k==null)return;if(k.rKey.wasPressedThisFrame)Investigation.Collect("skt079-broadcast");if(k.eKey.wasPressedThisFrame)Advance();}
        void Finish(){var state=GameManager.Instance.State;state.dayOneSupernovaSeen=true;state.skt079NewsSeen=true;ProgressionSave.Write(state);MorningRoutine.Instance?.NewsFinished();Close();}
        void Close(){if(presenting)blockedUntilFrame=Time.frameCount;presenting=false;page=0;if(television!=null)television.color=original;television=null;}
        public static void Replay(){if(Instance==null)return;Instance.Close();Instance.pending=true;}
        public static void ResetForNewRun(){Instance?.ResetEvent();}
        void OnGUI(){if(!presenting)return;if(body==null){var f=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},24);body=new GUIStyle(GUI.skin.label){font=f,fontSize=24,wordWrap=true};heading=new GUIStyle(body){fontSize=30,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=18};}
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;float scale=Mathf.Min(Screen.width/1200f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-45;float w=Screen.width/scale,h=Screen.height/scale;var r=new Rect(w*.16f,h*.12f,w*.68f,290);
            try{GUI.color=new Color(.025f,.055f,.08f,.97f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=new Color(.62f,.69f,.73f);GUI.DrawTexture(new Rect(r.x,r.y,r.width,3),Texture2D.whiteTexture);GUI.color=Color.white;var t=StoryText.Current.dayOneBroadcast;GUI.Label(new Rect(r.x+26,r.y+20,r.width-52,45),t.title,heading);GUI.Label(new Rect(r.x+26,r.y+86,r.width-52,125),t.pages[Mathf.Clamp(page,0,t.pages.Length-1)],body);GUI.Label(new Rect(r.x+26,r.yMax-60,r.width-52,50),(page==t.pages.Length-1?t.closePrompt:t.continuePrompt)+StoryText.Local("  ·  R 摘录到笔记"," · R Record extract"),small);}
            finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}}
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
