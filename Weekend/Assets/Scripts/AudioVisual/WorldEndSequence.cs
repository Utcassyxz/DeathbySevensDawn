// 职责：第七天未见古神的单次火焰/异变演出，结束后交给结局页面。
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Interaction;

namespace DeathBySeventhsDawn.Progression
{
    /// <summary>One unskippable Day 7 transition: smoke, rising fire and deformed shadows, then the saved ending.</summary>
    public sealed class WorldEndSequence : MonoBehaviour
    {
        public const float Duration = 5.5f;
        public static WorldEndSequence Instance { get; private set; }
        public static bool IsPresenting => Instance != null && Instance.playing;

        public static bool FirstCycleActive => Instance != null && Instance.playing && Instance.firstCycle;
        public static bool InReunion => FirstCycleActive && Instance.reunion;
        public int ReunionPage => reunionPage;
        bool playing, firstCycle, reunion;
        int reunionPage;
        float lastAdvance;
        Texture2D earth, portrait;
        readonly DialoguePages reunionText = new DialoguePages();
        public static bool TryBeginFirstApocalypse()
        {
            var game=GameManager.Instance;var clock=TimeManager.Instance;
            if(Instance==null||game==null||clock==null) return false;
            if(IsPresenting) return true;
            if(game.HasEnded||!game.HasLoopAbility||game.LoopCount!=0||game.State.firstApocalypseExplained||clock.Days!=7||clock.Period!=TimePeriod.Night) return false;
            game.State.firstApocalypsePending=true;
            ProgressionSave.Write(game.State);
            Instance.BeginFirstCycle();
            return true;
        }
        void Start()
        {
            if(GameManager.Instance.State.firstApocalypsePending && GameManager.Instance.HasLoopAbility && !GameManager.Instance.HasEnded) BeginFirstCycle();
        }
        void BeginFirstCycle()
        {
            playing=firstCycle=true;reunion=false;reunionPage=0;elapsed=0;
            reunionText.Reset();
            ApartmentAudio.Instance?.PlayEndingScream();
            var body=GameObject.FindWithTag("Player")?.GetComponent<Rigidbody2D>();
            if(body!=null)body.linearVelocity=Vector2.zero;
        }
        public void AdvanceReunion()
        {
            if(!InReunion || Time.unscaledTime-lastAdvance<.16f)return;
            lastAdvance=Time.unscaledTime;
            if(reunionText.Next())return;
            reunionText.Reset();reunionPage++;
            if(reunionPage >= StoryText.Current.expansion.apocalypseReunion.Length) CompleteReunion();
        }
        void CompleteReunion()
        {
            var state=GameManager.Instance.State;
            state.firstApocalypseExplained=true;state.firstApocalypsePending=false;
            ProgressionSave.Write(state);
            ResetSequence();
            StoryTransition.Cover(.8f);
            LoopManager.Instance.RestartLoop();
        }
        float elapsed;
        GUIStyle caption;
        Texture2D flame, round;
        void Awake()
        {
            Instance = this;
            portrait=Resources.Load<Texture2D>("Story/BubbleDialogueInk");
            earth=new Texture2D(72,72,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
            for(int y=0;y<72;y++) for(int x=0;x<72;x++)
            {
                float dx=(x-35.5f)/34,dy=(y-35.5f)/34;
                Color c=Color.clear;
                if(dx*dx+dy*dy<1)
                {
                    c=Mathf.PerlinNoise(x*.075f+2,y*.08f)>.51f?new Color(.24f,.43f,.37f):new Color(.10f,.24f,.40f);
                    c*=Mathf.Clamp01(.80f-dx*.38f+dy*.12f);c.a=1;
                    if(Mathf.PerlinNoise(x*.13f,y*.11f)>.66f)c=new Color(.55f,.63f,.63f);
                }
                earth.SetPixel(x,y,c);
            }
            earth.Apply();
            // Reusable pixel silhouettes, generated once; no per-frame textures or scene mutations.
            flame = new Texture2D(64, 128, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            round = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 64; x++)
                {
                    float v = y / 127f, center = 31.5f + Mathf.Sin(v * 8) * 8 * v;
                    float edge = (1 - v) * 27 + Mathf.Sin(v * 17) * 3;
                    float alpha = Mathf.Clamp01((edge - Mathf.Abs(x - center)) * 1.5f);
                    float hot = Mathf.Clamp01(1 - v * 1.3f - Mathf.Abs(x - center) / 36);
                    flame.SetPixel(x, y, new Color(Mathf.Lerp(.38f, .96f, hot), Mathf.Lerp(.055f, .48f, hot), Mathf.Lerp(.025f, .10f, hot), alpha));
                }

            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    round.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 15.5f) * 12)));
            flame.Apply();
            round.Apply();
        }

        void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndingTriggered += OnEnding;
                GameManager.Instance.JourneyReset += ResetSequence;
            }
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndingTriggered -= OnEnding;
                GameManager.Instance.JourneyReset -= ResetSequence;
            }

            ResetSequence();
        }

        void OnEnding(string id)
        {
            if (id != "world-end")
                return;
            elapsed = 0;
            firstCycle=reunion=false;
            playing = true;
        }

        public void DebugSkip() { if(FirstCycleActive) CompleteReunion(); else ResetSequence(); }
        void ResetSequence()
        {
            playing = firstCycle = reunion = false;
            reunionPage=0;reunionText.Reset();
            elapsed = 0;
        }

        void Update()
        {
            if (ProgressionHUD.DebugConsoleOpen)
                return;
            if (playing)
            {
                if(reunion)
                {
                    if(StoryControls.ContinuePressed) AdvanceReunion();
                    return;
                }
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= Duration)
                {
                    if(firstCycle){reunion=true;lastAdvance=Time.unscaledTime;reunionText.Reset();}
                    else playing=false;
                }
            }
        }

        static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        void OnGUI()
        {
            if (!playing)
                return;
            var matrix = GUI.matrix;
            var color = GUI.color;
            int depth = GUI.depth;
            GUI.depth = -200;
            GUI.matrix = Matrix4x4.identity;
            float w = Screen.width, h = Screen.height, t = Mathf.Clamp01(elapsed / Duration);
            try
            {
                if(firstCycle)
                {
                    DrawFirstCycle(w,h,t);
                    return;
                }
                // Steady palette and slow motion avoid a flashing full-screen effect.
                Fill(new Rect(0, 0, w, h), new Color(.065f, .025f, .025f, Mathf.Min(1, t * 3)));
                // Uneven flames and drifting embers; silhouettes remain joined as limbs lengthen.
                for (int i = 0; i < 24; i++)
                {
                    float x = i * w / 23f, rise = (.24f + .16f * Mathf.PerlinNoise(i * .7f, elapsed * .4f)) * h * Mathf.Clamp01(t * 2);
                    GUI.color = new Color(.8f, .6f, .48f, .12f);
                    GUI.DrawTexture(new Rect(x - w * .08f, h - rise * 1.7f, w * .16f, rise * 1.6f), round);
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(x - w * .05f, h - rise, w * .105f, rise * 1.15f), flame);
                }

                var ink = new Color(.035f, .028f, .025f);
                for (int i = 0; i < 7; i++)
                {
                    float x = w * (i + 1) / 8f, y = h * .74f, stretch = 1 + t * (.35f + i * .04f), unit = h / 900f;
                    Fill(new Rect(x - 12 * unit, y - 112 * unit * stretch, 24 * unit, 140 * unit * stretch), ink);
                    GUI.color = ink;
                    GUI.DrawTexture(new Rect(x - 20 * unit, y - 147 * unit * stretch, 40 * unit, 48 * unit), round);
                    Limb(new Vector2(x - 10 * unit, y - 85 * unit * stretch), new Vector2(x - (40 + 28 * t) * unit, y - 45 * unit), 10 * unit, ink);
                    Limb(new Vector2(x + 10 * unit, y - 85 * unit * stretch), new Vector2(x + (45 + 75 * t) * unit, y - (80 + 18 * i) * unit), 10 * unit, ink);
                    Limb(new Vector2(x - 6 * unit, y), new Vector2(x - (14 + 20 * t) * unit, y + 105 * unit), 12 * unit, ink);
                    Limb(new Vector2(x + 6 * unit, y), new Vector2(x + (20 + 30 * t) * unit, y + 110 * unit), 12 * unit, ink);
                }

                for (int i = 0; i < 40; i++)
                {
                    float x = (i * .371f % 1) * w + Mathf.Sin(elapsed * .7f + i) * 12;
                    float y = h - (elapsed * 36 + i * 41) % (h * .6f);
                    Fill(new Rect(x, y, 2 + i % 3, 4 + i % 5), new Color(.75f, .27f, .08f, .32f));
                }

                Fill(new Rect(0, 0, w, h), new Color(0, 0, 0, Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .72f) / .28f))));
                if (caption == null)
                    caption = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 26,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true
                    };
                GameTypography.Use(caption);
                GUI.color = Color.white;
                var lines = StoryText.Current.worldEnd.lines;
                GUI.Label(new Rect(w * .12f, h * .16f, w * .76f, h * .2f), lines[Mathf.Min(lines.Length - 1, (int)(t * lines.Length))], caption);
            }
            finally
            {
                GUI.matrix = matrix;
                GUI.color = color;
                GUI.depth = depth;
            }
        }

        void DrawFirstCycle(float w,float h,float t)
        {
            Fill(new Rect(0,0,w,h),new Color(.006f,.009f,.018f));
            for(int i=0;i<100;i++) Fill(new Rect((i*97%311)/311f*w,(i*61%193)/193f*h,2,2),new Color(.18f,.22f,.30f));
            if(!reunion)
            {
                var planet=new Rect(w*.5f-h*.26f,h*.16f,h*.52f,h*.52f);
                GUI.color=Color.white;
                for(int band=0;band<12;band++)
                {
                    float tear=Mathf.Pow(Mathf.Clamp01((t-.42f)/.58f),2);
                    float shift=Mathf.Sin(band*13.7f)*planet.width*.28f*tear;
                    GUI.DrawTextureWithTexCoords(new Rect(planet.x+shift,planet.y+band*planet.height/12,planet.width,planet.height/12+1),earth,new Rect(0,1-(band+1)/12f,1,1/12f));
                }
                for(int i=0;i<34;i++)
                {
                    float x=planet.x+planet.width*(i*37%97)/97f;
                    float y=planet.y+planet.height*(i*19%89)/89f;
                    Vector2 p=new Vector2((x-planet.center.x)/(planet.width*.48f),(y-planet.center.y)/(planet.height*.48f));
                    if(p.sqrMagnitude>.85f || t<i/48f)continue;
                    Fill(new Rect(x,y,4+8*t,3+20*t),new Color(.88f,.24f,.10f));
                    if(t>.35f)Fill(new Rect(x-8*t,y,12*t,4),new Color(.9f,.51f,.21f));
                }
                Fill(new Rect(0,0,w,h),new Color(0,0,0,Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.72f)/.28f))));
                var style=new GUIStyle(GUI.skin.label){font=GameTypography.Current,fontSize=Mathf.RoundToInt(h/35),alignment=TextAnchor.MiddleCenter,wordWrap=true};
                GUI.color=NarrativeUI.Ink;
                var captions=StoryText.Current.expansion.apocalypseCaptions;
                GUI.Label(new Rect(w*.15f,h*.76f,w*.70f,h*.15f),captions[Mathf.Min(captions.Length-1,(int)(t*captions.Length))],style);
                return;
            }
            float scale=Mathf.Min(w/1200,h/800);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);w/=scale;h/=scale;
            GUI.color=Color.white;GUI.DrawTexture(new Rect(w*.25f,h*.05f,w*.42f,h*.48f),SmallPixelActors.Bubble,ScaleMode.ScaleToFit);
            var art=new Rect(w*.75f,h*.60f,w*.20f,h*.33f);NarrativeUI.Panel(art);
            if(portrait!=null)GUI.DrawTexture(new Rect(art.x+8,art.y+8,art.width-16,art.height-16),portrait,ScaleMode.ScaleToFit);
            var text=new GUIStyle(GUI.skin.label){font=GameTypography.Current,fontSize=23,wordWrap=true};text.normal.textColor=NarrativeUI.Ink;
            var box=new Rect(w*.06f,h*.76f,w*.64f,h*.225f);
            var lines=StoryText.Current.expansion.apocalypseReunion;
            reunionText.Set(lines[Mathf.Clamp(reunionPage,0,lines.Length-1)],text,box.width-50,box.height-86);
            NarrativeUI.Dialogue(box,StoryText.Current.expansion.apocalypseSpeakers[Mathf.Clamp(reunionPage,0,lines.Length-1)],reunionText.Current,reunionText.Counter+StoryText.Local("Enter 继续","Enter Continue"),text);
        }

        static void Limb(Vector2 start, Vector2 end, float width, Color color)
        {
            var previous = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg, start);
            Fill(new Rect(start.x, start.y - width * .5f, Vector2.Distance(start, end), width), color);
            GUI.matrix = previous;
        }

        void OnDestroy()
        {
            if(earth!=null)Destroy(earth);
            if (flame != null)
                Destroy(flame);
            if (round != null)
                Destroy(round);
            if (Instance == this)
                Instance = null;
        }
    }
}
