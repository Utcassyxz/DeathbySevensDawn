using UnityEngine;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;

namespace DeathBySeventhsDawn.Progression
{
    // 游戏内暂停：继续 / 回到主页面 / 退出游戏。Esc 打开，Esc/E 关闭。
    public sealed class PauseMenuUI : MonoBehaviour
    {
        public static PauseMenuUI Instance {get;private set;}
        public static bool WorldBlocked => Instance!=null && (Instance.open || Time.frameCount<=Instance.blockedUntilFrame);
        bool open;
        int blockedUntilFrame=-1;
        GUIStyle title,body,small,button,box;
        static readonly Color Ink=new Color(.90f,.88f,.79f),Muted=new Color(.62f,.64f,.58f);
        void Awake(){if(Instance!=null&&Instance!=this){Destroy(this);return;}Instance=this;}
        void OnDisable(){Close();}
        void Update()
        {
            if(open)
            {
                var key=Keyboard.current;
                if(key!=null&&(key.escapeKey.wasPressedThisFrame||key.eKey.wasPressedThisFrame))Close();
                return;
            }
            if(MainMenuUI.WorldBlocked||MainMenuUI.Suppressed)return;
            if(GameManager.Instance==null||GameManager.Instance.HasEnded)return;
            if(ProgressionHUD.DebugConsoleOpen||StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy)return;
            var window=Object.FindAnyObjectByType<WindowLookout2D>();if(window!=null&&window.IsOpen)return;
            var keyboard=Keyboard.current;
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)Open();
        }
        void Open(){open=true;blockedUntilFrame=Time.frameCount;}
        void Close(){if(open)blockedUntilFrame=Time.frameCount;open=false;}
        void ReturnToMain(){Close();MainMenuUI.ReturnToMainMenu();}
        void Styles()
        {
            if(body!=null)return;
            var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Noto Sans CJK SC"},24);
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=19,wordWrap=true,alignment=TextAnchor.MiddleCenter};body.normal.textColor=Ink;
            title=new GUIStyle(body){fontSize=36,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=14};small.normal.textColor=Muted;
            box=new GUIStyle(GUI.skin.box){padding=new RectOffset(24,24,20,20)};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=20,padding=new RectOffset(12,12,10,10)};
        }
        void OnGUI()
        {
            if(!open)return;
            Styles();
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Clamp(Screen.height/900f,.8f,2.4f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-90;
            float w=Screen.width/scale,h=Screen.height/scale;
            try
            {
                GUI.color=new Color(.015f,.025f,.03f,.76f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=color;
                float pw=Mathf.Min(430,w-48);var panel=new Rect((w-pw)/2,(h-380)/2,pw,380);
                GUI.color=new Color(.06f,.09f,.09f,.98f);GUI.DrawTexture(panel,Texture2D.whiteTexture);GUI.color=color;
                GUILayout.BeginArea(new Rect(panel.x+24,panel.y+22,panel.width-48,panel.height-44),box);
                GUILayout.Label("暂  停",title);GUILayout.Space(8);GUILayout.Label("时间仍然停在这一刻。",small);GUILayout.Space(18);
                if(GUILayout.Button("继续游戏",button,GUILayout.MinHeight(48)))Close();
                if(GUILayout.Button("回到主页面",button,GUILayout.MinHeight(48)))ReturnToMain();
                if(GUILayout.Button("退出游戏",button,GUILayout.MinHeight(48)))MainMenuUI.QuitGame();
                GUILayout.Space(10);GUILayout.Label("Esc / E 继续",small);
                GUILayout.EndArea();
            }
            finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
