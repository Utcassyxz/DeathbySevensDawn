// 职责：标题页与开场动画；开始游戏时重置当前旅程。
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Progression
{
    // 启动主界面：开始游戏 / 退出；游戏内“回到主页面”也会回到这里。
    public sealed class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }
        public static bool WorldBlocked => Instance != null && (Instance.visible || Time.frameCount <= Instance.blockedUntilFrame);
        public static bool IsVisible => Instance != null && Instance.visible;
        public static float OpeningProgress => Instance != null && Instance.starting ? Mathf.Clamp01((Time.unscaledTime - Instance.openingStarted) / 11f) : 0;
        public static bool IsOpening => Instance != null && Instance.starting;
        public static bool Suppressed => DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.Suppressed;

        bool visible, starting;
        float openingStarted;
        OpeningSky sky = new OpeningSky();
        int blockedUntilFrame = -1;
        GUIStyle title, subtitle, button, small;
        Texture2D plateNormal, plateHover, platePressed;
        Texture2D gradient, halo;
        Vector2[] stars;
        static readonly Color Ink = new Color(.91f, .89f, .80f), Muted = new Color(.62f, .66f, .60f), Gold = new Color(.78f, .68f, .42f);
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            visible = !Suppressed;
            gradient = new Texture2D(1, 64, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < 64; y++)
            {
                float t = y / 63f;
                Color c = Color.Lerp(new Color(.03f, .045f, .065f), new Color(.10f, .075f, .12f), Mathf.Sin(t * Mathf.PI));
                c = Color.Lerp(c, new Color(.035f, .035f, .045f), t * .55f);
                gradient.SetPixel(0, y, c);
            }

            gradient.Apply(false);
            halo = new Texture2D(128, 128, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float dx = (x - 63.5f) / 63.5f, dy = (y - 63.5f) / 63.5f;
                    float a = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy)), 2.4f);
                    halo.SetPixel(x, y, new Color(1, 1, 1, a));
                }

            halo.Apply(false);
            stars = new Vector2[46];
            var random = new System.Random(7);
            for (int i = 0; i < stars.Length; i++)
                stars[i] = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
        }

        void Update()
        {
            if (!visible || GameSettingsUI.WorldBlocked)
                return;
            if (starting)
            {
                if (Time.unscaledTime - openingStarted >= 11f)
                    FinishOpening();
                return;
            }

            if (EndingCollectionUI.GalleryOpen)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    EndingCollectionUI.CloseGallery();
                return;
            }

            var k = Keyboard.current;
            if (k != null && k.enterKey.wasPressedThisFrame)
                StartGame();
        }

        public void StartGame()
        {
            if (!visible || starting || GameSettingsUI.WorldBlocked)
                return;
            starting = true;
            openingStarted = Time.unscaledTime;
            ApartmentAudio.Instance?.BeginOpening();
        }

        void FinishOpening()
        {
            visible = false;
            blockedUntilFrame = Time.frameCount;
            ApartmentAudio.Instance?.EndOpening();
            DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.ResetForNewRun();
            var game = GameManager.Instance;
            // 新的一轮游戏：清空旧存档、知识、线索、剧情标记和轮回进度。
            if (game != null)
            {
                game.StartNewJourney();
                starting = false;
                return;
            }

            if (TimeManager.Instance != null)
                TimeManager.Instance.ResetToDayOne();
            if (Application.CanStreamedLevelBeLoaded("Bedroom"))
                SceneManager.LoadScene("Bedroom");
            else if (Application.CanStreamedLevelBeLoaded("LivingRoom"))
                SceneManager.LoadScene("LivingRoom");
            starting = false;
        }

        public static void ReturnToMainMenu()
        {
            if (Instance == null)
                return;
            Instance.visible = true;
            Instance.starting = false;
            Instance.blockedUntilFrame = Time.frameCount;
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
            if (title != null)
                return;
            var font = DeathBySeventhsDawn.Progression.GameTypography.Current;
            title = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 62,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            title.normal.textColor = Ink;
            subtitle = new GUIStyle(title)
            {
                fontSize = 20,
                fontStyle = FontStyle.Normal
            };
            subtitle.normal.textColor = Gold;
            button = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 23,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(18, 18, 12, 12)
            };
            plateNormal = MakePlate(false, false);
            plateHover = MakePlate(true, false);
            platePressed = MakePlate(true, true);
            button.border = new RectOffset(8, 8, 8, 8);
            button.normal.background = plateNormal;
            button.hover.background = plateHover;
            button.focused.background = plateHover;
            button.active.background = platePressed;
            button.normal.textColor = Ink;
            button.hover.textColor = new Color(1, .96f, .81f);
            button.focused.textColor = button.hover.textColor;
            button.active.textColor = Gold;
            PrototypeButtons.Apply(button);
            small = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            small.normal.textColor = Muted;
        }

        // Small nine-sliced metal plates: a raised rim, recessed face and deterministic grain.
        static Texture2D MakePlate(bool hover, bool pressed)
        {
            const int width = 128, height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    bool top = y > height / 2;
                    float grain = (((x * 73 + y * 137 + x * y * 3) % 29) / 28f - .5f) * .026f;
                    Color c = Color.Lerp(new Color(.075f, .105f, .105f), new Color(.19f, .23f, .22f), y / (float)(height - 1));
                    if (hover)
                        c += new Color(.035f, .032f, .012f, 0);
                    c += new Color(grain, grain, grain, 0);
                    if (edge == 0)
                        c = new Color(.035f, .035f, .03f);
                    else if (edge == 1)
                        c = new Color(.36f, .31f, .20f);
                    else if (edge <= 3)
                        c = top ? new Color(.65f, .57f, .37f) : new Color(.22f, .19f, .12f);
                    else if (edge == 4)
                        c = top ? new Color(.10f, .12f, .11f) : new Color(.34f, .34f, .26f);
                    else if (edge == 5)
                        c = new Color(.045f, .06f, .06f);
                    if (pressed)
                    {
                        c = Color.Lerp(c, Color.black, .21f);
                        if (edge >= 2 && edge <= 4)
                            c = top ? new Color(.14f, .13f, .10f) : new Color(.46f, .40f, .27f);
                    }

                    if (hover && edge == 2)
                        c = Color.Lerp(c, new Color(.84f, .74f, .49f), .28f);
                    if ((x < 2 || x >= width - 2) && (y < 2 || y >= height - 2))
                        c = Color.clear;
                    texture.SetPixel(x, y, c);
                }

            texture.Apply(false);
            return texture;
        }

        bool PlateButton(Rect rect, string text)
        {
            bool pressed = rect.Contains(Event.current.mousePosition) && Mouse.current != null && Mouse.current.leftButton.isPressed;
            Fill(new Rect(rect.x + 3, rect.y + 6, rect.width, rect.height), new Color(0, 0, 0, .55f));
            Fill(new Rect(rect.x + 2, rect.y + rect.height - 2, rect.width - 4, 6), new Color(.10f, .085f, .055f));
            button.contentOffset = pressed ? new Vector2(0, 2) : Vector2.zero;
            bool clicked = GUI.Button(rect, text, button);
            foreach (float x in new[]
            {
                rect.x + 13,
                rect.xMax - 16
            }

            )
            {
                var bolt = new Rect(x, rect.center.y - 2 + (pressed ? 2 : 0), 3, 3);
                Fill(new Rect(bolt.x, bolt.y + 1, 3, 3), new Color(.025f, .035f, .035f));
                Fill(bolt, new Color(.44f, .40f, .28f));
                Fill(new Rect(bolt.x, bolt.y, 2, 1), new Color(.68f, .60f, .41f));
            }

            return clicked;
        }

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void OnGUI()
        {
            DeathBySeventhsDawn.Progression.GameTypography.Use(title, subtitle, button, small);
            if (!visible || GameSettingsUI.IsOpen)
                return;
            Styles();
            title.font = StoryText.English ? GameTypography.English : GameTypography.Chinese;
            subtitle.font = StoryText.English ? GameTypography.Chinese : GameTypography.English;
            var matrix = GUI.matrix;
            var color = GUI.color;
            int depth = GUI.depth;
            float scale = Mathf.Min(Screen.height / 900f, Screen.width / 1100f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUI.depth = -100;
            float w = Screen.width / scale, h = Screen.height / scale;
            try
            {
                GUI.color = Color.white;
                float progress = starting ? Mathf.Clamp01((Time.unscaledTime - openingStarted) / 11f) : 0;
                GUI.DrawTexture(new Rect(0, 0, w, h), sky.Render(Time.unscaledTime, progress), ScaleMode.ScaleAndCrop);
                if (starting)
                {
                    GUI.Label(new Rect(0, h - 45, w, 30), StoryText.Local("跳过 · Esc", "Skip · Esc"), small);
                    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                        FinishOpening();
                    return;
                }

                if (EndingCollectionUI.GalleryOpen)
                {
                    EndingCollectionUI.DrawGallery(w, h);
                    return;
                }

                GUI.Label(new Rect(0, h * .10f, w, 90), StoryText.Current.mainMenu?.title ?? "七日", title);
                GUI.Label(new Rect(0, h * .10f + 86, w, 32), StoryText.Current.mainMenu?.subtitle ?? "SEVEN DAY", subtitle);
                float bw = Mathf.Min(300, w - 80);
                if (PlateButton(new Rect((w - bw) / 2, h * .61f, bw, 58), StoryText.Current.mainMenu?.start ?? "开 始 新 游 戏"))
                    StartGame();
                if (PlateButton(new Rect((w - bw) / 2, h * .61f + 78, bw, 52), StoryText.Local("设置", "Settings")))
                    GameSettingsUI.Show();
                if (PlateButton(new Rect((w - bw) / 2, h * .61f + 148, bw, 52), StoryText.Local("结局收录", "Ending collection")))
                    EndingCollectionUI.OpenGallery();
                if (PlateButton(new Rect((w - bw) / 2, h * .61f + 216, bw, 52), StoryText.Current.mainMenu?.quit ?? "退出游戏"))
                    QuitGame();
            }
            finally
            {
                GUI.matrix = matrix;
                GUI.color = color;
                GUI.depth = depth;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool(DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.SuppressionKey,false);
#endif
            if (plateNormal != null)
                Destroy(plateNormal);
            if (plateHover != null)
                Destroy(plateHover);
            if (platePressed != null)
                Destroy(platePressed);
            sky.Dispose();
            if (gradient != null)
                Destroy(gradient);
            if (halo != null)
                Destroy(halo);
        }
    }
}
