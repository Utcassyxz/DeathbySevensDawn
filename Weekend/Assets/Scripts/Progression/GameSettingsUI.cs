using UnityEngine;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Story;
namespace DeathBySeventhsDawn.Progression
{
    [DefaultExecutionOrder(-120)]
    public sealed class GameSettingsUI:MonoBehaviour
    {
        public static GameSettingsUI Instance{get;private set;}
        public static bool IsOpen=>Instance!=null&&Instance.open;
        public static bool WorldBlocked=>Instance!=null&&(Instance.open||Time.frameCount<=Instance.blockedFrame);
        public static float MasterVolume=>PlayerPrefs.GetFloat("SevenDay.MasterVolume",1);
        bool open;int blockedFrame=-1;GUIStyle title,label,button;
        void Awake(){Instance=this;AudioListener.volume=MasterVolume;}
        public static void SetVolume(float value){value=Mathf.Clamp01(value);AudioListener.volume=value;PlayerPrefs.SetFloat("SevenDay.MasterVolume",value);}
        public static void Show(){if(Instance==null)return;Instance.open=true;Instance.blockedFrame=Time.frameCount;}
        public static void Hide(){if(Instance==null)return;Instance.open=false;Instance.blockedFrame=Time.frameCount;PlayerPrefs.Save();}
        void OnEnable(){if(DeathBySeventhsDawn.Core.GameManager.Instance!=null)DeathBySeventhsDawn.Core.GameManager.Instance.JourneyReset+=Reset;}
        void OnDisable(){if(DeathBySeventhsDawn.Core.GameManager.Instance!=null)DeathBySeventhsDawn.Core.GameManager.Instance.JourneyReset-=Reset;Reset();}
        void Reset(){if(open)Hide();}
        void Update(){if(open&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Hide();}
        void OnGUI(){if(!open)return;
            if(label==null){label=new GUIStyle(GUI.skin.label){font=GameTypography.Current,fontSize=24,wordWrap=true};title=new GUIStyle(label){fontSize=38,fontStyle=FontStyle.Bold};button=new GUIStyle(GUI.skin.button){font=GameTypography.Current,fontSize=23,padding=new RectOffset(15,15,12,12)};}
            GameTypography.Use(title,label,button);PrototypeButtons.Apply(button);
            var matrix=GUI.matrix;var color=GUI.color;var depth=GUI.depth;float scale=Mathf.Min(Screen.width/1100f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-200;
            float w=Screen.width/scale,h=Screen.height/scale,pw=Mathf.Min(590,w-50);var panel=new Rect((w-pw)/2,(h-460)/2,pw,460);
            try{GUI.color=new Color(0,0,0,.95f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=new Color(.065f,.085f,.08f);GUI.DrawTexture(panel,Texture2D.whiteTexture);GUI.color=new Color(.65f,.55f,.34f);GUI.DrawTexture(new Rect(panel.x,panel.y,pw,3),Texture2D.whiteTexture);GUI.color=Color.white;
                GUILayout.BeginArea(new Rect(panel.x+30,panel.y+25,pw-60,410));GUILayout.Label(StoryText.Local("设置","Settings"),title);GUILayout.Space(22);
                GUILayout.Label(StoryText.Local("总音量","Master volume")+"   "+Mathf.RoundToInt(MasterVolume*100)+"%",label);
                float volume=GUILayout.HorizontalSlider(MasterVolume,0,1,GUILayout.Height(26));if(Mathf.Abs(volume-MasterVolume)>.001f)SetVolume(volume);
                GUILayout.Space(26);GUILayout.Label(StoryText.Local("语言","Language"),label);GUILayout.BeginHorizontal();
                button.font=GameTypography.Chinese;if(GUILayout.Button((StoryText.English?"":"• ")+"中文",button,GUILayout.MinHeight(50))&&StoryText.English)StoryText.ToggleLanguage();
                button.font=GameTypography.English;if(GUILayout.Button((StoryText.English?"• ":"")+"English",button,GUILayout.MinHeight(50))&&!StoryText.English)StoryText.ToggleLanguage();GUILayout.EndHorizontal();button.font=GameTypography.Current;
                GUILayout.Space(38);if(GUILayout.Button(StoryText.Local("返回  ·  Esc","Back  ·  Esc"),button,GUILayout.MinHeight(50)))Hide();GUILayout.EndArea();
            }finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}}
        void OnApplicationQuit(){PlayerPrefs.Save();}
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
