using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Player;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Progression;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DeathBySeventhsDawn.Interaction
{
    // Bind existing furniture at runtime; scene assets and their colliders stay unchanged.
    public sealed class FurnitureInteraction : MonoBehaviour
    {
        public static bool IsBusy { get; private set; }
        [SerializeField] float interactionDistance=.85f;
        GameObject bed, television, player, target;
        PlayerController2D movement;
        bool restoreMovement, watching;
        GUIStyle label, button, box;
        Vector2 scroll;
        string watchingReport;
        void OnEnable() { SceneManager.sceneLoaded+=OnSceneLoaded; if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=CloseMenu; Bind(); }
        void OnDisable() { SceneManager.sceneLoaded-=OnSceneLoaded; if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=CloseMenu; CloseMenu(); }
        void OnSceneLoaded(Scene scene,LoadSceneMode mode) { CloseMenu(); Bind(); }
        void Bind() { bed=GameObject.Find("Bed"); television=GameObject.Find("Television and stand"); player=GameObject.FindWithTag("Player"); }
        float Distance(GameObject item)
        {
            if(item==null || player==null) return float.PositiveInfinity;
            Vector2 position=player.transform.position;
            var collider=item.GetComponent<Collider2D>();
            if(collider!=null) return Vector2.Distance(position,collider.ClosestPoint(position));
            var renderer=item.GetComponent<Renderer>();
            return renderer!=null ? Vector2.Distance(position,(Vector2)renderer.bounds.ClosestPoint(position)) : Vector2.Distance(position,item.transform.position);
        }
        void Update()
        {
            if(GameManager.Instance==null || GameManager.Instance.HasEnded) { CloseMenu(); target=null; return; }
            if(ProgressionHUD.DebugConsoleOpen || StoryJournal.WorldBlocked) return;
            bool interact=false,cancel=false;
#if ENABLE_INPUT_SYSTEM
            var keyboard=Keyboard.current;
            interact=keyboard!=null && keyboard.eKey.wasPressedThisFrame;
            cancel=keyboard!=null && keyboard.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            interact=Input.GetKeyDown(KeyCode.E); cancel=Input.GetKeyDown(KeyCode.Escape);
#endif
            if(IsBusy) {
#if ENABLE_INPUT_SYSTEM
                if(keyboard!=null&&keyboard.rKey.wasPressedThisFrame)CollectNews();
#endif
                if(cancel || interact) CloseMenu(); return; }
            if(player==null) Bind();
            // Give the existing window interaction priority if interaction areas overlap.
            var window=Object.FindAnyObjectByType<WindowLookout2D>();
            if(window!=null && (window.IsOpen || window.CanInteract)) { target=null; return; }
            float bedDistance=Distance(bed), tvDistance=Distance(television);
            target=bedDistance<=tvDistance?bed:television;
            if(Mathf.Min(bedDistance,tvDistance)>interactionDistance) target=null;
            if(target!=null && interact) {
                movement=player.GetComponent<PlayerController2D>();
                restoreMovement=movement!=null && movement.enabled;
                if(restoreMovement) movement.enabled=false;
                var rb=player.GetComponent<Rigidbody2D>(); if(rb!=null) rb.linearVelocity=Vector2.zero;
                watching=false; watchingReport=null; IsBusy=true;
            }
        }
        public bool WatchTelevision()
        {
            if(!IsBusy || target!=television || watching || StoryJournal.WorldBlocked) return false;
            watchingReport=FirstLoopPrologue.Instance!=null?FirstLoopPrologue.Instance.WatchNews():"新闻与杂音交替闪过。";
            watching=true; TimeManager.Instance.AdvanceTime(); return true;
        }
        public bool CollectNews()=>watching&&GameManager.Instance.State.skt079NewsSeen&&Knowledge.Investigation.Collect("skt079-broadcast");
        void CloseMenu()
        {
            if(restoreMovement && movement!=null) movement.enabled=true;
            restoreMovement=false; movement=null; IsBusy=false; watching=false; watchingReport=null; scroll=Vector2.zero;
        }
        void PrepareStyles()
        {
            if(label!=null) return;
            var font=Font.CreateDynamicFontFromOSFont(new[] {"Microsoft YaHei","SimHei","Noto Sans CJK SC"},26);
            label=new GUIStyle(GUI.skin.label) {font=font,fontSize=26,wordWrap=true,alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle(GUIStyle.none) {font=font,fontSize=25,wordWrap=true,padding=new RectOffset(16,16,14,14),alignment=TextAnchor.MiddleCenter};
            button.normal.textColor=new Color(.74f,.78f,.66f);button.hover.textColor=new Color(.96f,.94f,.82f);button.active.textColor=Color.white;
            box=new GUIStyle(GUI.skin.box) {padding=new RectOffset(24,24,24,24)};
        }
        void OnGUI()
        {
            if(GameManager.Instance==null || GameManager.Instance.HasEnded || ProgressionHUD.DebugConsoleOpen || StoryJournal.WorldBlocked) return;
            if(target==null && !IsBusy) return;
            PrepareStyles();
            var matrix=GUI.matrix; var color=GUI.color;
            float scale=Mathf.Clamp(Screen.height/900f,.8f,2.4f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float width=Screen.width/scale,height=Screen.height/scale;
            try {
                if(!IsBusy) {
                    float hint=Mathf.Min(440,width-32);
                    GUI.color=new Color(.035f,.055f,.05f,.92f);GUI.DrawTexture(new Rect((width-hint)/2,height-82,hint,54),Texture2D.whiteTexture);GUI.color=color;
                    GUI.Label(new Rect((width-hint)/2,height-82,hint,54),target==bed?"E  休息":"E  看电视",label);
                    return;
                }
                GUI.color=new Color(0,0,0,.88f); GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture); GUI.color=color;
                float panelWidth=Mathf.Min(760,width-24), panelHeight=Mathf.Min(target==bed?480:620,height-24);
                GUILayout.BeginArea(new Rect((width-panelWidth)/2,(height-panelHeight)/2,panelWidth,panelHeight),box);
                scroll=GUILayout.BeginScrollView(scroll);
                GUILayout.Label(target==bed?"上床休息":"电视",label); GUILayout.Space(20);
                if(target==bed) {
                    GUILayout.Label("要结束今天，睡到明天早晨吗？",label);
                    if(GUILayout.Button("上床睡觉（跳过今天）",button)) { CloseMenu(); TimeManager.Instance.SleepUntilNextMorning(); }
                } else if(watching) {
                    GUILayout.Label(watchingReport,label);
                    GUILayout.Label("已度过一个时段。",label);
                    if(GameManager.Instance.State.skt079NewsSeen&&!Knowledge.Investigation.Has("skt079-broadcast")&&GUILayout.Button("摘录新闻到笔记 · R",button))CollectNews();
                } else {
                    GUILayout.Label("屏幕亮着。要坐下来看看电视吗？",label);
                    if(GUILayout.Button("看电视（度过一个时段）",button)) WatchTelevision();
                }
                GUILayout.Space(20);
                GUILayout.EndScrollView();
                if(GUILayout.Button(watching?"关掉电视，返回":"离开",button)) CloseMenu();
                GUILayout.Label("E / Esc 返回",label);
                GUILayout.EndArea();
            } finally { GUI.matrix=matrix; GUI.color=color; }
        }
    }
}