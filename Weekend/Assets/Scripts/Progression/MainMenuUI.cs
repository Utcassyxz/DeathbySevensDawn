using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Progression
{
    // 启动主界面：开始游戏 / 退出；游戏内“回到主页面”也会回到这里。
    public sealed class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance {get;private set;}
        public static bool WorldBlocked => Instance!=null && (Instance.visible || Time.frameCount<=Instance.blockedUntilFrame);
        public static bool Suppressed => DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.Suppressed;
        bool visible,starting;
        int blockedUntilFrame=-1;
        GUIStyle title,subtitle,tagline,button,small;
        Texture2D gradient,halo;
        Vector2[] stars;
        static readonly Color Ink=new Color(.91f,.89f,.80f),Muted=new Color(.62f,.66f,.60f),Gold=new Color(.78f,.68f,.42f);
        void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(this);return;}
            Instance=this;visible=!Suppressed;
            gradient=new Texture2D(1,64,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            for(int y=0;y<64;y++){float t=y/63f;Color c=Color.Lerp(new Color(.03f,.045f,.065f),new Color(.10f,.075f,.12f),Mathf.Sin(t*Mathf.PI));c=Color.Lerp(c,new Color(.035f,.035f,.045f),t*.55f);gradient.SetPixel(0,y,c);}
            gradient.Apply(false);
            halo=new Texture2D(128,128,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            for(int y=0;y<128;y++)for(int x=0;x<128;x++){float dx=(x-63.5f)/63.5f,dy=(y-63.5f)/63.5f;float a=Mathf.Pow(Mathf.Clamp01(1-Mathf.Sqrt(dx*dx+dy*dy)),2.4f);halo.SetPixel(x,y,new Color(1,1,1,a));}
            halo.Apply(false);
            stars=new Vector2[46];var random=new System.Random(7);for(int i=0;i<stars.Length;i++)stars[i]=new Vector2((float)random.NextDouble(),(float)random.NextDouble());
        }
        void Update()
        {
            if(!visible||starting)return;
            var k=Keyboard.current;
            if(k!=null&&(k.enterKey.wasPressedThisFrame||k.spaceKey.wasPressedThisFrame))StartGame();
        }
        public void StartGame()
        {
            if(!visible||starting)return;starting=true;visible=false;blockedUntilFrame=Time.frameCount;
            DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.ResetForNewRun();
            var game=GameManager.Instance;
            if(game!=null&&game.HasEnded){game.StartNewJourney();starting=false;return;}
            if(game!=null)game.Heal(game.MaxVitality);
            if(TimeManager.Instance!=null)TimeManager.Instance.ResetToDayOne();
            if(Application.CanStreamedLevelBeLoaded("Bedroom"))SceneManager.LoadScene("Bedroom");
            else if(Application.CanStreamedLevelBeLoaded("LivingRoom"))SceneManager.LoadScene("LivingRoom");
            starting=false;
        }
        public static void ReturnToMainMenu()
        {
            if(Instance==null)return;
            Instance.visible=true;Instance.starting=false;Instance.blockedUntilFrame=Time.frameCount;
        }
        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        void Styles()
        {
            if(title!=null)return;
            var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Noto Sans CJK SC"},32);
            title=new GUIStyle(GUI.skin.label){font=font,fontSize=62,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};title.normal.textColor=Ink;
            subtitle=new GUIStyle(title){fontSize=20,fontStyle=FontStyle.Normal};subtitle.normal.textColor=Gold;
            tagline=new GUIStyle(title){fontSize=22,fontStyle=FontStyle.Normal};tagline.normal.textColor=Muted;
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=23,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(18,18,12,12)};
            small=new GUIStyle(GUI.skin.label){font=font,fontSize=16,alignment=TextAnchor.MiddleCenter,wordWrap=true};small.normal.textColor=Muted;
        }
        static void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void OnGUI()
        {
            if(!visible)return;
            Styles();
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Clamp(Screen.height/900f,.8f,2.4f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-100;
            float w=Screen.width/scale,h=Screen.height/scale;
            try
            {
                GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,w,h),gradient);
                var old=GUI.color;GUI.color=new Color(.62f,.72f,.86f,.10f);GUI.DrawTexture(new Rect(w*.5f-h*.55f,h*.18f-h*.5f,h*1.1f,h*1.1f),halo,ScaleMode.ScaleToFit);GUI.color=old;
                for(int i=0;i<stars.Length;i++){float twinkle=.22f+.18f*Mathf.Sin(Time.unscaledTime*.7f+i*1.7f);Fill(new Rect(stars[i].x*w,stars[i].y*h*.78f,2,2),new Color(.82f,.84f,.78f,twinkle));}
                GUI.Label(new Rect(0,h*.16f,w,90),"第七日黎明",title);
                GUI.Label(new Rect(0,h*.16f+86,w,32),"DEATH BY SEVENTH'S DAWN",subtitle);
                GUI.Label(new Rect(0,h*.16f+128,w,34),"时间会重置。知识不会。",tagline);
                float bw=Mathf.Min(360,w-80);
                if(GUI.Button(new Rect((w-bw)/2,h*.52f,bw,64),"开 始 游 戏",button))StartGame();
                if(GUI.Button(new Rect((w-220)/2,h*.52f+82,220,46),"退出游戏",button))QuitGame();
                string hint=GameManager.Instance!=null&&GameManager.Instance.HasEnded?"上次旅程已经结束。开始游戏会清除本轮进度，重新开始。":"开始后从第一天早晨继续；已经获得的知识会保留。";
                GUI.Label(new Rect((w-Mathf.Min(720,w-60))/2,h*.52f+142,Mathf.Min(720,w-60),54),hint,small);
                GUI.Label(new Rect(0,h-38,w,24),"游戏中按 Esc 暂停 · J 日记 · B 线索连线",small);
            }
            finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
        }
        void OnDestroy()
        {
            if(Instance==this)Instance=null;
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool(DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.SuppressionKey,false);
#endif
            if(gradient!=null)Destroy(gradient);if(halo!=null)Destroy(halo);
        }
    }
}
