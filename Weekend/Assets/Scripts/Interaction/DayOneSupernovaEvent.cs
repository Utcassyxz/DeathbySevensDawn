using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
namespace DeathBySeventhsDawn.Interaction
{
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
        void Loaded(Scene s,LoadSceneMode m){Close();}
        void ResetEvent(){pending=false;Close();}
        public bool PlayFromFridge(){var state=GameManager.Instance.State;if(presenting||state.dayOneSupernovaSeen||state.dayOneNewsStarted||TimeManager.Instance.Days!=1||SceneManager.GetActiveScene().name!="LivingRoom")return false;state.dayOneNewsStarted=true;state.newsPage=0;ProgressionSave.Write(state);StartPresentation();return true;}
        public bool Resume(){var state=GameManager.Instance.State;if(presenting||state.dayOneSupernovaSeen||!state.dayOneNewsStarted||SceneManager.GetActiveScene().name!="LivingRoom")return false;StartPresentation();return true;}
        void StartPresentation(){if(presenting)return;presenting=true;pending=false;page=Mathf.Clamp(GameManager.Instance.State.newsPage,0,StoryText.Current.dayOneBroadcast.pages.Length-1);openedFrame=Time.frameCount;var tv=GameObject.Find("Television and stand");television=tv!=null?tv.GetComponent<SpriteRenderer>():null;if(television!=null){original=television.color;television.color=new Color(1,.98f,.9f);}}
        public void Advance(){if(!presenting)return;page++;GameManager.Instance.State.newsPage=page;if(page>=StoryText.Current.dayOneBroadcast.pages.Length)Finish();else ProgressionSave.Write(GameManager.Instance.State);}
        void Update(){if(GameSettingsUI.WorldBlocked)return;if(pending&&!StoryJournal.WorldBlocked&&SceneManager.GetActiveScene().name=="LivingRoom")StartPresentation();if(!presenting||Time.frameCount<=openedFrame+1)return;var k=Keyboard.current;if(k!=null&&k.eKey.wasPressedThisFrame)Advance();}
        void Finish(){var state=GameManager.Instance.State;state.dayOneSupernovaSeen=true;state.skt079NewsSeen=true;if(MorningRoutine.Instance!=null&&MorningRoutine.Instance.Stage==4)MorningRoutine.Instance.NewsFinished();else ProgressionSave.Write(state);Close();}
        void Close(){if(presenting)blockedUntilFrame=Time.frameCount;presenting=false;page=0;if(television!=null)television.color=original;television=null;}
        public static void Replay(){if(Instance==null)return;Instance.Close();Instance.pending=true;}
        public static void ResetForNewRun(){Instance?.ResetEvent();}
        void OnGUI(){if(!presenting||GameSettingsUI.WorldBlocked)return;if(body==null){body=new GUIStyle(GUI.skin.label){font=GameTypography.Current,fontSize=24,wordWrap=true};heading=new GUIStyle(body){fontSize=30,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=18};}GameTypography.Use(body,heading,small);
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;float scale=Mathf.Min(Screen.width/1200f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-45;float w=Screen.width/scale;var r=new Rect(w*.16f,Screen.height/scale*.12f,w*.68f,290);
            try{GUI.color=new Color(.025f,.055f,.08f,.97f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=new Color(.62f,.69f,.73f);GUI.DrawTexture(new Rect(r.x,r.y,r.width,3),Texture2D.whiteTexture);GUI.color=Color.white;var t=StoryText.Current.dayOneBroadcast;GUI.Label(new Rect(r.x+26,r.y+20,r.width-52,45),t.title,heading);GUI.Label(new Rect(r.x+26,r.y+86,r.width-52,125),t.pages[page],body);GUI.Label(new Rect(r.x+26,r.yMax-60,r.width-52,50),page==t.pages.Length-1?t.closePrompt:t.continuePrompt,small);}
            finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}}
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
