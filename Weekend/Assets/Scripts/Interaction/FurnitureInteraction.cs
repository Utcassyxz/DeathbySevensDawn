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
        void OnEnable() { SceneManager.sceneLoaded+=OnSceneLoaded; Bind(); }
        void OnDisable() { SceneManager.sceneLoaded-=OnSceneLoaded; CloseMenu(); }
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
            if(IsBusy) { if(cancel || interact) CloseMenu(); return; }
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
                watching=false; IsBusy=true;
            }
        }
        void CloseMenu()
        {
            if(restoreMovement && movement!=null) movement.enabled=true;
            restoreMovement=false; movement=null; IsBusy=false; watching=false;
        }
        void PrepareStyles()
        {
            if(label!=null) return;
            var font=Font.CreateDynamicFontFromOSFont(new[] {"Microsoft YaHei","SimHei","Noto Sans CJK SC"},26);
            label=new GUIStyle(GUI.skin.label) {font=font,fontSize=26,wordWrap=true,alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle(GUI.skin.button) {font=font,fontSize=25,wordWrap=true,padding=new RectOffset(16,16,14,14)};
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
                    GUI.Box(new Rect(12,height-95,width-24,80),GUIContent.none,box);
                    GUI.Label(new Rect(20,height-90,width-40,70),target==bed?"E  与床互动 · 上床睡觉":"E  与电视互动 · 看电视",label);
                    return;
                }
                GUI.color=new Color(0,0,0,.88f); GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture); GUI.color=color;
                float panelWidth=Mathf.Min(760,width-24), panelHeight=Mathf.Min(480,height-24);
                GUILayout.BeginArea(new Rect((width-panelWidth)/2,(height-panelHeight)/2,panelWidth,panelHeight),box);
                scroll=GUILayout.BeginScrollView(scroll);
                GUILayout.Label(target==bed?"上床休息":"电视",label); GUILayout.Space(20);
                if(target==bed) {
                    GUILayout.Label("要结束今天，睡到明天早晨吗？",label);
                    if(GUILayout.Button("上床睡觉（跳过今天）",button)) { CloseMenu(); TimeManager.Instance.SleepUntilNextMorning(); }
                } else if(watching) {
                    GUILayout.Label("你看了一会儿电视。新闻与杂音交替闪过，时间悄然流逝。",label);
                    GUILayout.Label("已度过一个时段。",label);
                } else {
                    GUILayout.Label("屏幕亮着。要坐下来看看电视吗？",label);
                    if(GUILayout.Button("看电视（度过一个时段）",button)) { watching=true; TimeManager.Instance.AdvanceTime(); }
                }
                GUILayout.Space(20);
                if(GUILayout.Button(watching?"关掉电视，返回":"离开",button)) CloseMenu();
                GUILayout.Label("E / Esc 返回",label);
                GUILayout.EndScrollView(); GUILayout.EndArea();
            } finally { GUI.matrix=matrix; GUI.color=color; }
        }
    }
}