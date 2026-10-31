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
        Texture2D contact, replacement, glow;
        readonly DialoguePages subtitles = new DialoguePages();
        void Awake()
        {
            Instance = this;
            contact = Resources.Load<Texture2D>("Story/BubbleContact");
            replacement = Resources.Load<Texture2D>("Story/EndingReplacement");
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
            if (active)
                return true;
            var text = StoryText.Current.nightDream;
            if (clock.Days == 7 && !game.AquariumSealed)
            {
                kind = Kind.Replacement;
                pages = text.replacement;
                while (clock.Period != TimePeriod.Night && !game.HasEnded)
                    clock.AdvanceTime();
                if (game.HasEnded)
                    return true;
            }
            else if (game.HasSeenAncientGod && !game.HasLoopAbility)
            {
                kind = Kind.Bubble;
                pages = text.yog;
            }
            else
            {
                kind = Kind.Ordinary;
                pages = clock.Days == 1 ? text.day1 : clock.Days == 2 ? text.day2 : game.AquariumSealed ? text.protectedDream : text.day3;
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

        public void Advance()
        {
            if (!active || Time.frameCount <= openedFrame || Time.unscaledTime - lastAdvance < .16f)
                return;
            lastAdvance = Time.unscaledTime;
            if (subtitles.Next())
                return;
            subtitles.Reset();
            page++;
            if (page < pages.Length)
                return;
            var game = GameManager.Instance;
            var completed = kind;
            Reset();
            if (completed == Kind.Replacement)
            {
                game.TriggerReplacementEnding();
                return;
            }

            if (completed == Kind.Bubble)
                game.GrantLoopAbility();
            TimeManager.Instance.CompleteSleep();
            if (!game.HasEnded && Application.CanStreamedLevelBeLoaded("Bedroom"))
            {
                wakePending = true;
                StoryTransition.Cover(.65f);
                SceneManager.LoadScene("Bedroom");
            }
        }

        void Update()
        {
            if (active && !GameSettingsUI.WorldBlocked && StoryControls.ContinuePressed)
                Advance();
        }

        void OnGUI()
        {
            if (!active || GameManager.Instance == null)
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
                Texture2D art = kind == Kind.Replacement ? replacement : kind == Kind.Bubble ? contact : null;
                if (art != null)
                {
                    GUI.color = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, t / 2));
                    GUI.DrawTexture(new Rect(0, 0, w, h * .76f), art, ScaleMode.ScaleAndCrop);
                }

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
                }

                GUI.color = Color.white;
                var text = new GUIStyle(GUI.skin.label)
                {
                    font = GameTypography.Current,
                    fontSize = 23,
                    wordWrap = true
                };
                Rect r = NarrativeUI.DialogueRect(w, h);
                subtitles.Set(pages[page], text, r.width - 70, r.height - 88);
                string speaker = kind == Kind.Replacement ? StoryText.Local("雨果", "Hugo") : kind == Kind.Bubble && page > 0 ? StoryText.Current.nightDream.yogSpeaker : StoryText.Current.nightDream.speaker;
                if (kind == Kind.Bubble && StoryText.Current.nightDream.yogSpeakers != null && page < StoryText.Current.nightDream.yogSpeakers.Length)
                    speaker = StoryText.Current.nightDream.yogSpeakers[page];
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
