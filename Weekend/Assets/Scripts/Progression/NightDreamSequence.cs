// CORE FLOW: one owner for sleep, daily dreams, Bubble's gift and the seventh-night fish ending.
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Progression
{
    public sealed class NightDreamSequence : MonoBehaviour
    {
        public static NightDreamSequence Instance { get; private set; }
        public static bool WorldBlocked => Instance != null && Instance.active;
        public bool IsActive => active;
        public bool IsBubble => active && kind == Kind.Bubble;
        public bool IsReplacement => active && kind == Kind.Replacement;
        public bool IsShop => IsBubble && market;
        public bool PortraitKnown => GameManager.Instance.State.bubblePortraitUnlocked || GameManager.Instance.HasLoopAbility;

        bool returning, market;
        float portraitRevealedAt = -100;
        Texture2D bubblePortrait;
        public int PageIndex => page;

        enum Kind
        {
            Ordinary,
            Bubble,
            Replacement
        }

        Kind kind;
        bool active;
        int page, openedFrame;
        float entered, lastAdvance;
        string[] pages;
        Texture2D glow;
        readonly DialoguePages subtitles = new DialoguePages();
        void Awake()
        {
            Instance = this;
            bubblePortrait = Resources.Load<Texture2D>("Story/BubbleDialogueInk");
            glow = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f;
                    glow.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - d), 2)));
                }

            glow.Apply(false);
        }

        bool wakePending;
        void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (!wakePending || scene.name != "Bedroom")
                return;
            wakePending = false;
            var bed = GameObject.Find("Bed");
            var player = GameObject.FindWithTag("Player");
            if (bed == null || player == null)
                return;
            var bounds = bed.GetComponent<SpriteRenderer>().bounds;
            var spot = new Vector2(bounds.max.x + .48f, bounds.min.y - .25f);
            player.transform.position = spot;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.position = spot;
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += Loaded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset += Reset;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= Loaded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset -= Reset;
            Reset();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (glow != null)
                Destroy(glow);
        }

        void Reset()
        {
            active = false;
            market = false;
            returning = false;
            pages = null;
            page = 0;
            subtitles.Reset();
        }

        public static bool BeginSleep()
        {
            var game = GameManager.Instance;
            var clock = TimeManager.Instance;
            if (Instance == null || game == null || clock == null || game.HasEnded || MainMenuUI.WorldBlocked || PauseMenuUI.WorldBlocked)
                return false;
            return Instance.Open(clock, game);
        }

        bool Open(TimeManager clock, GameManager game)
        {
            if (active || WorldEndSequence.IsPresenting)
                return true;
            if (!StoryDebugActions.Enabled && ((clock.Days == 5 && !game.State.buildingCrisisResolved) || (clock.Days == 7 && !game.State.daySevenCrisisResolved)))
            { WorldEndSequence.BeginFatalCrisis(clock.Days == 5); return true; }
            var text = StoryText.Current.nightDream;
            bool firstContact = !game.HasLoopAbility && game.HasSeenAncientGod &&
                (game.State.ancientObservationDay == 0 || (game.State.ancientObservationDay == clock.Days && game.State.ancientObservationLoop == game.LoopCount));
            bool returningContact = game.HasLoopAbility && game.State.bubbleDreamUnlocked && game.LoopCount > 0 && game.State.bubbleVisitLoop != game.LoopCount;
            if (!StoryDebugActions.Enabled && (firstContact || returningContact))
            {
                kind = Kind.Bubble;
                returning = game.HasLoopAbility;
                market = false;
                pages = returning ? StoryText.Current.expansion.bubbleReturn : text.yog;
                if (returning)
                    game.State.bubblePortraitUnlocked = game.State.bubbleIntroduced = true;
            }
            else
            {
                clock.CompleteSleep();
                if (!game.HasEnded && !WorldEndSequence.IsPresenting)
                {
                    wakePending = true;
                    StoryTransition.Cover(.7f);
                    SceneManager.LoadScene("Bedroom");
                }

                return true; // Ordinary nights are a quiet transition, never recycled fish dialogue.
            }

            if (pages == null || pages.Length == 0)
                return false;
            active = true;
            page = 0;
            entered = lastAdvance = Time.unscaledTime;
            openedFrame = Time.frameCount;
            subtitles.Reset();
            var player = GameObject.FindWithTag("Player");
            var body = player != null ? player.GetComponent<Rigidbody2D>() : null;
            if (body != null)
                body.linearVelocity = Vector2.zero;
            return true;
        }

        public void DebugSkip()
        {
            if (active)
                FinishSleep();
        }

        void FinishSleep()
        {
            var game = GameManager.Instance;
            var completed = kind;
            Reset();
            if (completed == Kind.Replacement)
            {
                game.TriggerReplacementEnding();
                return;
            }

            if (completed == Kind.Bubble)
            {
                game.GrantLoopAbility();
                game.State.bubbleDreamUnlocked = true;
                game.State.bubbleVisitLoop = game.LoopCount;
                ProgressionSave.Write(game.State);
            }
            TimeManager.Instance.CompleteSleep();
            if (!game.HasEnded && !WorldEndSequence.IsPresenting && Application.CanStreamedLevelBeLoaded("Bedroom"))
            {
                wakePending = true;
                StoryTransition.Cover(.65f);
                SceneManager.LoadScene("Bedroom");
            }
        }

        public void Advance()
        {
            if (!active || Time.frameCount <= openedFrame || Time.unscaledTime - lastAdvance < .16f)
                return;
            lastAdvance = Time.unscaledTime;
            if (market)
            {
                FinishSleep();
                return;
            }

            if (subtitles.Next())
                return;
            subtitles.Reset();
            page++;
            if (kind == Kind.Bubble && !returning && page >= 2 && !GameManager.Instance.State.bubblePortraitUnlocked)
            {
                GameManager.Instance.State.bubblePortraitUnlocked = true;
                portraitRevealedAt = Time.unscaledTime;
                ProgressionSave.Write(GameManager.Instance.State);
            }

            if (page < pages.Length)
                return;
            if (kind == Kind.Bubble && returning)
            {
                market = true;
                page = pages.Length - 1;
                return;
            }

            FinishSleep();
        }

        void Update()
        {
            if (active && !ProgressionHUD.DebugConsoleOpen && !GameSettingsUI.WorldBlocked && StoryControls.ContinuePressed)
                Advance();
        }

        void DrawBubble(float w, float h, float age)
        {
            var pixel = SmallPixelActors.Bubble;
            var main = new Rect(w * .25f, h * .07f, w * .44f, h * .50f);
            GUI.color = PortraitKnown ? new Color(1, 1, 1, Mathf.SmoothStep(0, 1, age / 1.2f)) : new Color(.035f, .04f, .065f, 1);
            GUI.DrawTexture(main, pixel, ScaleMode.ScaleToFit);
            var portrait = new Rect(w * .74f, h * .60f, w * .21f, h * .34f);
            NarrativeUI.Panel(portrait);
            if (PortraitKnown && bubblePortrait != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(portrait.x + 8, portrait.y + 8, portrait.width - 16, portrait.height - 16), bubblePortrait, ScaleMode.ScaleToFit);
            }

            if (!PortraitKnown)
            {
                GUI.color = Color.white;
                var q = new GUIStyle(GUI.skin.label)
                {
                    font = GameTypography.Current,
                    fontSize = 60,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(main, "?", q);
                GUI.Label(portrait, "?", q);
            }
            else
            {
                float progress = Mathf.Clamp01((Time.unscaledTime - portraitRevealedAt) / 1.8f);
                if (progress < 1)
                    foreach (var area in new[]
                    {
                        main,
                        portrait
                    }

                    )
                        for (int y = 0; y < 18; y++)
                            for (int x = 0; x < 18; x++)
                            {
                                float seed = ((x * 73 + y * 37) % 101) / 101f;
                                float life = Mathf.Clamp01((1 - progress) * 2 - seed);
                                if (life <= 0)
                                    continue;
                                GUI.color = new Color(0, 0, 0, life);
                                float drift = progress * progress * (20 + seed * 45);
                                GUI.DrawTexture(new Rect(area.x + x * area.width / 18 + drift * (seed - .5f), area.y + y * area.height / 18 - drift, area.width / 18 + 1, area.height / 18 + 1), Texture2D.whiteTexture);
                            }
            }

            GUI.color = Color.white;
        }

        void DrawMarket(float w, float h, GUIStyle style)
        {
            var panel = new Rect(w * .035f, h * .15f, w * .275f, h * .39f);
            var tradeButton = new GUIStyle(GUI.skin.button)
            {
                font = GameTypography.Current,
                fontSize = 17,
                wordWrap = true
            };
            PrototypeButtons.Apply(tradeButton);
            NarrativeUI.Panel(panel);
            GUI.Label(new Rect(panel.x + 22, panel.y + 15, panel.width - 44, 35), StoryText.Local("献祭理智 · 获取知识", "SANITY FOR KNOWLEDGE"), style);
            var status = new GUIStyle(style)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleRight
            };
            GUI.Label(new Rect(panel.x + 22, panel.y + 43, panel.width - 44, 24), "SAN " + GameManager.Instance.Sanity.ToString("0") + " · " + StoryText.Local("知识 ", "Knowledge ") + GameManager.Instance.Knowledge.ToString("0"), status);
            var offers = BubbleMarket.Offers;
            float y = panel.y + 60;
            bool any = false;
            foreach (var offer in offers)
            {
                if (offer == null || !offer.enabled)
                    continue;
                any = true;
                if (PrototypeButtons.Button(new Rect(panel.x + 22, y, panel.width - 44, 48), (StoryText.English ? offer.titleEn : offer.titleZh) + " · SAN -" + offer.sanityCost, tradeButton))
                    BubbleMarket.Purchase(offer.id);
                y += 54;
            }

            if (!any)
                GUI.Label(new Rect(panel.x + 22, y, panel.width - 44, 80), StoryText.Local("暂无可交换的知识", "No knowledge is offered yet."), style);
        }

        void OnGUI()
        {
            if (!active || ProgressionHUD.DebugConsoleOpen || GameManager.Instance == null)
                return;
            var matrix = GUI.matrix;
            var color = GUI.color;
            int depth = GUI.depth;
            float scale = Mathf.Min(Screen.width / 1200f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUI.depth = -112;
            float w = Screen.width / scale, h = Screen.height / scale, t = Time.unscaledTime - entered;
            try
            {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
                if (kind == Kind.Replacement)
                    EndingIllustration.Draw(new Rect(0, 0, w, h * .76f), true, Mathf.SmoothStep(0, 1, t / 2));
                if (kind == Kind.Bubble)
                {
                    Color[] colors =
                    {
                        new Color(.82f, .12f, .48f),
                        new Color(.10f, .35f, .95f),
                        new Color(.08f, .67f, .43f),
                        new Color(.78f, .08f, .12f)
                    };
                    for (int i = 0; i < 4; i++)
                    {
                        float pulse = .11f + .09f * Mathf.PerlinNoise(i * 7.3f, t * .65f);
                        var c = colors[i];
                        c.a = pulse * Mathf.SmoothStep(0, 1, t / 1.3f);
                        GUI.color = c;
                        GUI.DrawTexture(new Rect(i % 2 == 0 ? -w * .3f : w * .6f, i < 2 ? -h * .3f : h * .2f, w * .7f, h), glow);
                    }

                    DrawBubble(w, h, t);
                }

                GUI.color = Color.white;
                var text = new GUIStyle(GUI.skin.label)
                {
                    font = GameTypography.Current,
                    fontSize = 23,
                    wordWrap = true
                };
                Rect r = kind == Kind.Bubble ? new Rect(w * .06f, h - 190, w * .64f, 180) : NarrativeUI.DialogueRect(w, h);
                subtitles.Set(pages[page], text, r.width - 70, r.height - 88);
                string speaker = kind == Kind.Replacement ? StoryText.Local("雨果", "Hugo") : kind == Kind.Bubble && page > 0 ? StoryText.Current.nightDream.yogSpeaker : StoryText.Current.nightDream.speaker;
                if (kind == Kind.Bubble && StoryText.Current.nightDream.yogSpeakers != null && page < StoryText.Current.nightDream.yogSpeakers.Length)
                    speaker = StoryText.Current.nightDream.yogSpeakers[page];
                if (kind == Kind.Bubble && !PortraitKnown && page > 0)
                    speaker = "???";
                if (market)
                {
                    DrawMarket(w, h, text);
                    NarrativeUI.Dialogue(r, StoryText.Current.nightDream.yogSpeaker, StoryText.Current.expansion.bubbleTradeLine, StoryText.Local("Enter 结束会面", "Enter End meeting"), text);
                }
                else
                    NarrativeUI.Dialogue(r, speaker, subtitles.Current, subtitles.Counter + StoryText.Local("Enter 继续", "Enter Continue"), text);
            }
            finally
            {
                GUI.matrix = matrix;
                GUI.color = color;
                GUI.depth = depth;
            }
        }
    }
}
