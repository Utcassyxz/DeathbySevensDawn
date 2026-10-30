using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DeathBySeventhsDawn.Interaction
{
    // 第一天下午（正午之后）强制播放的超新星紧急广播。只强制一次，之后写入永久记录。
    public sealed class DayOneSupernovaEvent : MonoBehaviour
    {
        public static DayOneSupernovaEvent Instance {get;private set;}
        public static bool WorldBlocked => Instance!=null && (Instance.presenting || Time.frameCount<=Instance.blockedUntilFrame);
        public bool IsPresenting => presenting;
        // 自动验收会推进时间；测试入口在进入 Play Mode 前用 SessionState 写入此键，避免强制事件打断旧测试。
        public const string SuppressionKey="DayOneSupernovaSuppressed";
#if UNITY_EDITOR
        public static bool Suppressed => UnityEditor.SessionState.GetBool(SuppressionKey,false);
#else
        public static bool Suppressed => false;
#endif
        static readonly string[] fallbackPages={"广播文本缺失：请检查 Resources/StoryText.json。"};
        bool presenting,pending,late,tvColorSaved;
        int page,openedFrame,blockedUntilFrame=-1;
        float flash,elapsed;
        Color tvOriginalColor;
        GameObject tvGlow;
        SpriteRenderer tvRenderer,glowRenderer;
        Transform tvTransform;
        Sprite glowSprite;
        Texture2D glowTexture;
        GUIStyle title,body,small;
        void Awake(){if(Instance!=null&&Instance!=this){Destroy(this);return;}Instance=this;}
        void OnEnable(){SceneManager.sceneLoaded+=OnLoaded;if(TimeManager.Instance!=null)TimeManager.Instance.TimeChanged+=OnTimeChanged;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=ResetEvent;if(LoopManager.Instance!=null)LoopManager.Instance.LoopRestarted+=ResetEvent;}
        void OnDisable(){SceneManager.sceneLoaded-=OnLoaded;if(TimeManager.Instance!=null)TimeManager.Instance.TimeChanged-=OnTimeChanged;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=ResetEvent;if(LoopManager.Instance!=null)LoopManager.Instance.LoopRestarted-=ResetEvent;Close();}
        void Start(){if(TimeManager.Instance!=null)OnTimeChanged(TimeManager.Instance.Days,TimeManager.Instance.Period);}
        void OnTimeChanged(int day,TimePeriod period)
        {
            if(GameManager.Instance==null||GameManager.Instance.HasEnded)return;
            if(GameManager.Instance.State.dayOneSupernovaSeen)return;
            // 从早晨直接睡到第二天时，时间会连续跳过下午；只要经过第一天下午就排队，之后补播。
            if(day==1&&period>=TimePeriod.Afternoon)pending=true;
        }
        void OnLoaded(Scene scene,LoadSceneMode mode){if(presenting){DestroyGlow();BuildGlow();}}
        void ResetEvent(){pending=false;Close();}
        void Update()
        {
            var game=GameManager.Instance;var clock=TimeManager.Instance;
            if(game==null||clock==null)return;
            if(game.HasEnded){Close();return;}
            if(presenting){if(!ProgressionHUD.DebugConsoleOpen)TickPresentation();return;}
            if(!pending||game.State.dayOneSupernovaSeen||Suppressed)return;
            if(ProgressionHUD.DebugConsoleOpen||StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy)return;
            var window=Object.FindAnyObjectByType<WindowLookout2D>();if(window!=null&&window.IsOpen)return;
            if(SceneManager.GetActiveScene().name!="LivingRoom")
            {
                // 广播是强制的：不在客厅时把玩家带回客厅；加载完成后下一帧再开始。
                if(Application.CanStreamedLevelBeLoaded("LivingRoom"))SceneManager.LoadScene("LivingRoom");
                return;
            }
            StartPresentation();
        }
        void StartPresentation()
        {
            presenting=true;pending=false;page=0;flash=1;elapsed=0;openedFrame=Time.frameCount;
            late=TimeManager.Instance.Days!=1||TimeManager.Instance.Period!=TimePeriod.Afternoon;
            BuildGlow();
        }
        void TickPresentation()
        {
            float dt=Time.unscaledDeltaTime;elapsed+=dt;flash=Mathf.MoveTowards(flash,0,dt*1.1f);
            if(tvRenderer!=null)
            {
                float pulse=.85f+Mathf.Sin(Time.unscaledTime*3.4f)*.15f;
                if(tvGlow!=null)tvGlow.transform.localScale=Vector3.one*(Mathf.Max(tvRenderer.bounds.size.x,tvRenderer.bounds.size.y)*4f*pulse);
                tvRenderer.color=Color.Lerp(tvOriginalColor,new Color(1f,.98f,.90f),.62f+.18f*Mathf.Sin(Time.unscaledTime*3.4f));
            }
            if(glowRenderer!=null)glowRenderer.color=new Color(1f,.94f,.76f,.22f+.10f*Mathf.Sin(Time.unscaledTime*2.1f));
            bool advance=false;
#if ENABLE_INPUT_SYSTEM
            var keyboard=Keyboard.current;
            advance=keyboard!=null&&(keyboard.eKey.wasPressedThisFrame||keyboard.spaceKey.wasPressedThisFrame||keyboard.enterKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            advance=Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return);
#endif
            if(Time.frameCount<=openedFrame+2||elapsed<.25f)advance=false;
            if(!advance)return;
            var pages=StoryText.Current.dayOneBroadcast?.pages;
            if(pages==null||pages.Length==0){Finish();return;}
            page++;
            if(page>=pages.Length)Finish();
        }
        void Finish()
        {
            var state=GameManager.Instance.State;
            state.dayOneSupernovaSeen=true;
            if(state.journal==null)state.journal=new List<JournalEntry>();
            var entry=state.journal.Find(e=>e!=null&&e.id=="day-one-supernova");
            bool added=entry==null;
            if(added)
            {
                var text=StoryText.Current.dayOneBroadcast;
                entry=new JournalEntry{id="day-one-supernova",title=text?.journalTitle??"第一天的紧急广播",body=text?.journalBody??"",isKnowledge=false,day=1,loop=GameManager.Instance.LoopCount};
                state.journal.Add(entry);
            }
            ProgressionSave.Write(state);if(added)JournalUpdates.NotifyAdded(entry);
            presenting=false;page=0;blockedUntilFrame=Time.frameCount;DestroyGlow();
        }
        void Close()
        {
            if(presenting)blockedUntilFrame=Time.frameCount;
            presenting=false;page=0;DestroyGlow();
        }
        // 调试面板用：清除“已看过”标记并重新排队，方便反复验收广播和氛围。
        public static void Replay()
        {
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool(SuppressionKey,false);
#endif
            var instance=Instance;if(instance==null||GameManager.Instance==null)return;
            GameManager.Instance.State.dayOneSupernovaSeen=false;
            ProgressionSave.Write(GameManager.Instance.State);
            instance.Close();instance.pending=true;
        }
        public static void ResetForNewRun()
        {
            var instance=Instance;if(instance==null)return;
            instance.pending=false;instance.Close();
        }
        void BuildGlow()
        {
            var tv=GameObject.Find("Television and stand");if(tv==null)return;
            tvTransform=tv.transform;tvRenderer=tv.GetComponent<SpriteRenderer>();
            if(tvRenderer!=null){tvOriginalColor=tvRenderer.color;tvColorSaved=true;}
            if(glowTexture==null)
            {
                glowTexture=new Texture2D(128,128,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
                for(int y=0;y<128;y++)for(int x=0;x<128;x++)
                {
                    float dx=(x-63.5f)/63.5f,dy=(y-63.5f)/63.5f;
                    float a=Mathf.Pow(Mathf.Clamp01(1f-Mathf.Sqrt(dx*dx+dy*dy)),2.2f);
                    glowTexture.SetPixel(x,y,new Color(1,1,1,a));
                }
                glowTexture.Apply(false);
                glowSprite=Sprite.Create(glowTexture,new Rect(0,0,128,128),new Vector2(.5f,.5f),128f);
            }
            tvGlow=new GameObject("Supernova TV glow");
            tvGlow.transform.position=tvTransform.position;
            glowRenderer=tvGlow.AddComponent<SpriteRenderer>();glowRenderer.sprite=glowSprite;glowRenderer.color=new Color(1f,.94f,.76f,.25f);
            if(tvRenderer!=null){glowRenderer.sortingLayerID=tvRenderer.sortingLayerID;glowRenderer.sortingOrder=tvRenderer.sortingOrder+1;}else glowRenderer.sortingOrder=9;
            if(tvRenderer!=null){if(tvRenderer.sharedMaterial!=null)glowRenderer.sharedMaterial=tvRenderer.sharedMaterial;tvGlow.transform.localScale=Vector3.one*(Mathf.Max(tvRenderer.bounds.size.x,tvRenderer.bounds.size.y)*4f);}
            else tvGlow.transform.localScale=Vector3.one*4f;
        }
        void DestroyGlow()
        {
            if(tvRenderer!=null&&tvColorSaved)tvRenderer.color=tvOriginalColor;
            tvColorSaved=false;tvRenderer=null;glowRenderer=null;tvTransform=null;
            if(tvGlow!=null)Destroy(tvGlow);tvGlow=null;
        }
        void PrepareStyles()
        {
            if(body!=null)return;
            var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Noto Sans CJK SC"},26);
            small=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true};small.normal.textColor=new Color(.78f,.79f,.70f);
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=24,wordWrap=true};body.normal.textColor=new Color(.94f,.92f,.84f);
            title=new GUIStyle(body){fontSize=38,fontStyle=FontStyle.Bold};
        }
        void OnGUI()
        {
            if(!presenting||ProgressionHUD.DebugConsoleOpen)return;
            PrepareStyles();
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Clamp(Screen.height/900f,.8f,2.4f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-45;
            float w=Screen.width/scale,h=Screen.height/scale;
            try
            {
                GUI.color=new Color(.015f,.02f,.028f,.72f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);
                DrawTelevisionGlow(w,h);
                GUI.color=new Color(1f,.97f,.88f,flash*.55f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);
                GUI.color=color;
                float panel=Mathf.Min(960,w-48);var area=new Rect((w-panel)/2,h*.10f,panel,h*.36f);
                GUI.color=new Color(.025f,.03f,.038f,.82f);GUI.DrawTexture(area,Texture2D.whiteTexture);
                GUI.color=new Color(.86f,.74f,.45f,.9f);GUI.DrawTexture(new Rect(area.x,area.y,3,area.height),Texture2D.whiteTexture);GUI.color=color;
                GUILayout.BeginArea(new Rect(area.x+26,area.y+16,area.width-52,area.height-32));
                var broadcast=StoryText.Current.dayOneBroadcast;
                var pages=StoryText.Pages(broadcast?.pages,fallbackPages);
                GUILayout.Label(late?(broadcast?.kickerLate??"第一天 · 稍后"):(broadcast?.kicker??"第一天 · 正午"),small);GUILayout.Label(broadcast?.title??"紧 急 广 播",title);GUILayout.Space(8);
                GUILayout.Label(late&&page==0?(broadcast?.lateFirstPage??""):pages[Mathf.Clamp(page,0,pages.Length-1)],body);
                GUILayout.FlexibleSpace();GUILayout.Label(page>=pages.Length-1?(broadcast?.closePrompt??"关掉电视 · E / Space"):(broadcast?.continuePrompt??"继续 · E / Space"),small);
                GUILayout.EndArea();
                GUI.Label(new Rect(28,20,150,26),broadcast?.liveLabel??"●  LIVE",small);
            }
            finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
        }
        void DrawTelevisionGlow(float w,float h)
        {
            if(glowTexture==null)return;
            var camera=Camera.main;
            Vector2 center=new Vector2(w*.5f,h*.45f);float size=Mathf.Min(w,h)*1.15f;
            if(tvTransform!=null&&camera!=null)
            {
                var vp=camera.WorldToViewportPoint(tvTransform.position);
                if(vp.z>0&&vp.x>=-.05f&&vp.x<=1.05f&&vp.y>=-.05f&&vp.y<=1.05f){center=new Vector2(vp.x*w,(1-vp.y)*h);size=Mathf.Min(w,h)*(1.15f+.08f*Mathf.Sin(Time.unscaledTime*3.4f));}
            }
            var old=GUI.color;GUI.color=new Color(1f,.94f,.78f,.16f+.06f*Mathf.Sin(Time.unscaledTime*2.1f));
            GUI.DrawTexture(new Rect(center.x-size/2,center.y-size/2,size,size),glowTexture,ScaleMode.ScaleToFit);
            GUI.color=old;
        }
        void OnDestroy()
        {
            if(Instance==this)
            {
                Instance=null;
#if UNITY_EDITOR
                UnityEditor.SessionState.SetBool(SuppressionKey,false);
#endif
            }
            DestroyGlow();if(glowSprite!=null)Destroy(glowSprite);if(glowTexture!=null)Destroy(glowTexture);
        }
    }
}
