using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.TimeSystem;
namespace DeathBySeventhsDawn.Progression
{
    // 全屏梦核氛围：按天数、时段、房间给画面叠加柔和的粉彩光、窗边光晕、暗角和轻微颗粒。
    // 只做屏幕滤镜，不修改场景光照、材质或相机，避免破坏现有像素画面与碰撞。
    public sealed class DreamcoreAmbience : MonoBehaviour
    {
        Texture2D gradient,vignette,halo,grain;
        Color top,bottom,targetTop,targetBottom,haloColor,targetHalo;
        float vignetteStrength=.16f,targetVignette=.16f;
        bool dirty=true;
        Transform anchor;
        float anchorScale=1f,grainPhase;
        void Awake()
        {
            gradient=new Texture2D(1,64,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            vignette=Radial(128,false);halo=Radial(128,true);grain=Grain(64);
            top=bottom=haloColor=new Color(0,0,0,0);
        }
        void OnEnable(){SceneManager.sceneLoaded+=OnLoaded;if(TimeManager.Instance!=null)TimeManager.Instance.TimeChanged+=OnTimeChanged;if(GameManager.Instance!=null){GameManager.Instance.JourneyReset+=Refresh;GameManager.Instance.Changed+=Refresh;}}
        void OnDisable(){SceneManager.sceneLoaded-=OnLoaded;if(TimeManager.Instance!=null)TimeManager.Instance.TimeChanged-=OnTimeChanged;if(GameManager.Instance!=null){GameManager.Instance.JourneyReset-=Refresh;GameManager.Instance.Changed-=Refresh;}}
        void Start(){Refresh();}
        void OnLoaded(Scene scene,LoadSceneMode mode){Refresh();}
        void OnTimeChanged(int day,TimePeriod period){Refresh();}
        void Refresh()
        {
            var game=GameManager.Instance;var clock=TimeManager.Instance;if(game==null||clock==null)return;
            Color topRgb,bottomRgb,light;float alpha,vig;
            switch(clock.Period)
            {
                case TimePeriod.Morning:topRgb=new Color(.82f,.92f,1f);bottomRgb=new Color(1f,.90f,.80f);light=new Color(1f,.94f,.80f);alpha=.14f;vig=.14f;break;
                case TimePeriod.Afternoon:topRgb=new Color(1f,.96f,.78f);bottomRgb=new Color(1f,.84f,.70f);light=new Color(1f,.90f,.65f);alpha=.12f;vig=.15f;break;
                case TimePeriod.Evening:topRgb=new Color(.80f,.72f,.98f);bottomRgb=new Color(1f,.74f,.64f);light=new Color(1f,.72f,.55f);alpha=.18f;vig=.21f;break;
                default:topRgb=new Color(.18f,.28f,.52f);bottomRgb=new Color(.36f,.22f,.46f);light=new Color(.55f,.75f,1f);alpha=.22f;vig=.27f;break;
            }
            float day=Mathf.InverseLerp(1,7,clock.Days);
            topRgb=Color.Lerp(topRgb,new Color(.82f,.80f,.70f),day*.26f);
            bottomRgb=Color.Lerp(bottomRgb,new Color(.90f,.84f,.72f),day*.18f);
            Color room=RoomColor(SceneManager.GetActiveScene().name);
            topRgb=Color.Lerp(topRgb,room,.30f);bottomRgb=Color.Lerp(bottomRgb,room,.22f);
            float unease=1-Mathf.Clamp01(game.Sanity/100f);
            topRgb=Color.Lerp(topRgb,new Color(.86f,.58f,.78f),unease*.16f);
            bottomRgb=Color.Lerp(bottomRgb,new Color(.58f,.70f,.78f),unease*.12f);
            targetTop=new Color(topRgb.r,topRgb.g,topRgb.b,alpha);
            targetBottom=new Color(bottomRgb.r,bottomRgb.g,bottomRgb.b,alpha*.82f);
            targetHalo=new Color(light.r,light.g,light.b,.17f);
            targetVignette=vig+unease*.10f;
            FindAnchor();
        }
        static Color RoomColor(string scene)
        {
            switch(scene)
            {
                case "Bedroom":return new Color(.98f,.78f,.82f);
                case "LivingRoom":return new Color(.78f,1f,.90f);
                case "Bathroom":return new Color(.80f,.96f,1f);
                case "Balcony":return new Color(1f,.86f,.70f);
                case "Corridor":return new Color(.96f,.94f,.70f);
                default:return Color.white;
            }
        }
        void FindAnchor()
        {
            anchor=null;anchorScale=1f;string scene=SceneManager.GetActiveScene().name;string name=null;
            switch(scene){case "Bedroom":name="Large story window";anchorScale=1.1f;break;case "LivingRoom":name="Window";anchorScale=1f;break;case "Corridor":name="Wall lamp";anchorScale=.5f;break;case "Balcony":name="Distant window";anchorScale=1.2f;break;}
            if(!string.IsNullOrEmpty(name)){var go=GameObject.Find(name);if(go!=null)anchor=go.transform;}
        }
        void Update()
        {
            if(GameManager.Instance==null||TimeManager.Instance==null)return;
            float step=1-Mathf.Exp(-Time.unscaledDeltaTime*1.35f);
            top=Color.Lerp(top,targetTop,step);bottom=Color.Lerp(bottom,targetBottom,step);haloColor=Color.Lerp(haloColor,targetHalo,step);vignetteStrength=Mathf.Lerp(vignetteStrength,targetVignette,step);
            if(Delta(top,targetTop)+Delta(bottom,targetBottom)>0.002f)dirty=true;
            grainPhase+=Time.unscaledDeltaTime;
        }
        static float Delta(Color a,Color b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)+Mathf.Abs(a.a-b.a);
        void OnGUI()
        {
            if(Event.current.type!=EventType.Repaint||MainMenuUI.IsVisible||GameSettingsUI.IsOpen)return;
            var game=GameManager.Instance;var clock=TimeManager.Instance;if(game==null||clock==null)return;
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.matrix=Matrix4x4.identity;GUI.depth=90;
            float w=Screen.width,h=Screen.height;
            try
            {
                if(dirty){BuildGradient();dirty=false;}
                float breathe=Mathf.Clamp01(1f+.04f*Mathf.Sin(Time.unscaledTime*.35f));
                GUI.color=new Color(1,1,1,breathe);GUI.DrawTexture(new Rect(0,0,w,h),gradient);
                DrawHalo(w,h);
                GUI.color=new Color(.07f,.05f,.11f,vignetteStrength);GUI.DrawTexture(new Rect(0,0,w,h),vignette);
                float unease=1-Mathf.Clamp01(game.Sanity/100f);
                GUI.color=new Color(1,1,1,.018f+unease*.012f);
                GUI.DrawTextureWithTexCoords(new Rect(0,0,w,h),grain,new Rect(grainPhase*.013f,grainPhase*.007f,w/64f,h/64f));
            }
            finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
        }
        void DrawHalo(float w,float h)
        {
            Vector2 center;float size;
            if(anchor!=null&&Camera.main!=null)
            {
                var vp=Camera.main.WorldToViewportPoint(anchor.position);
                if(vp.z<=0)return;
                center=new Vector2(vp.x*w,(1-vp.y)*h);size=Mathf.Min(w,h)*anchorScale;
            }
            else {center=new Vector2(w*.5f,h*.34f);size=Mathf.Min(w,h)*.8f;}
            var c=haloColor;c.a*=.9f+.1f*Mathf.Sin(Time.unscaledTime*.8f);
            GUI.color=c;GUI.DrawTexture(new Rect(center.x-size/2,center.y-size/2,size,size),halo,ScaleMode.ScaleToFit);
        }
        void BuildGradient()
        {
            for(int y=0;y<gradient.height;y++)gradient.SetPixel(0,y,Color.Lerp(bottom,top,(float)y/(gradient.height-1)));
            gradient.Apply(false);
        }
        static Texture2D Radial(int size,bool soft)
        {
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=(x-(size-1)*.5f)/(size*.5f),dy=(y-(size-1)*.5f)/(size*.5f);
                float d=Mathf.Clamp01(Mathf.Sqrt(dx*dx+dy*dy));
                float a=soft?Mathf.Pow(1-d,2.2f):Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1f,d));
                texture.SetPixel(x,y,new Color(1,1,1,a));
            }
            texture.Apply(false);return texture;
        }
        static Texture2D Grain(int size)
        {
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Point};
            var random=new System.Random(1997);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){float v=(float)random.NextDouble();texture.SetPixel(x,y,new Color(1,1,1,v));}
            texture.Apply(false);return texture;
        }
        void OnDestroy(){if(gradient!=null)Destroy(gradient);if(vignette!=null)Destroy(vignette);if(halo!=null)Destroy(halo);if(grain!=null)Destroy(grain);}
    }
}
