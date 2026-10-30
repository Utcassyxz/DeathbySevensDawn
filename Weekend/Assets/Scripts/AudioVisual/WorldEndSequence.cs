// 职责：第七天未见古神的单次火焰/异变演出，结束后交给结局页面。
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Story;

namespace DeathBySeventhsDawn.Progression
{
    /// <summary>One unskippable Day 7 transition: smoke, rising fire and deformed shadows, then the saved ending.</summary>
    public sealed class WorldEndSequence : MonoBehaviour
    {
        public const float Duration = 5.5f;
        public static WorldEndSequence Instance
        {
            get; private set;
        }
        public static bool IsPresenting => Instance != null && Instance.playing;
        bool playing;
        float elapsed;
        GUIStyle caption;
        Texture2D flame, round;
        void Awake()
        {
            Instance = this;
            // Reusable pixel silhouettes, generated once; no per-frame textures or scene mutations.
            flame = new Texture2D(64, 128, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            round = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
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
            GameManager.Instance.EndingTriggered += OnEnding;
            GameManager.Instance.JourneyReset += ResetSequence;
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
            playing = true;
        }
        void ResetSequence()
        {
            playing = false;
            elapsed = 0;
        }
        void Update()
        {
            if (playing)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= Duration)
                    playing = false;
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
                    caption = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                GameTypography.Use(caption);
                GUI.color = Color.white;
                var lines = StoryText.Current.worldEnd.lines;
                GUI.Label(new Rect(w * .12f, h * .16f, w * .76f, h * .2f), lines[Mathf.Min(lines.Length - 1, (int)(t * lines.Length))], caption);
            }
            finally { GUI.matrix = matrix; GUI.color = color; GUI.depth = depth; }
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
            if (flame != null)
                Destroy(flame);
            if (round != null)
                Destroy(round);
            if (Instance == this)
                Instance = null;
        }
    }
}
