// 职责：夜间梦境。由床铺睡眠触发；见过古神后固定进入“泡泡”的梦，并记录轮回能力。
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DeathBySeventhsDawn.Progression
{
    public sealed class NightDreamSequence : MonoBehaviour
    {
        public static NightDreamSequence Instance { get; private set; }
        public static bool WorldBlocked => Instance != null && Instance.active;

        bool active, isYog;
        int page;
        string[] pages;
        string speaker;
        float entered;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Returns true when the dream owns the sleep transition and will advance time itself.</summary>
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
            if (text == null)
                return false;

            if (game.State.ancientGodSeen && !game.State.yogDreamSeen)
            {
                pages = text.yog;
                speaker = string.IsNullOrEmpty(text.yogSpeaker) ? "泡泡" : text.yogSpeaker;
                isYog = true;
            }
            else
            {
                pages = clock.Days <= 1 ? text.day1 : clock.Days == 2 ? text.day2 : text.day3;
                speaker = string.IsNullOrEmpty(text.speaker) ? "梦" : text.speaker;
                isYog = false;
            }

            if (pages == null || pages.Length == 0)
                return false;
            active = true;
            page = 0;
            entered = Time.unscaledTime;
            return true;
        }

        void Finish()
        {
            var game = GameManager.Instance;
            if (game != null && isYog)
            {
                game.State.ancientGodSeen = true;
                game.State.yogDreamSeen = true;
                ProgressionSave.Write(game.State);
            }
            active = false;
            isYog = false;
            pages = null;
            page = 0;
            TimeManager.Instance?.SleepUntilNextMorning();
        }

        void Update()
        {
            if (!active)
                return;
            var k = Keyboard.current;
            if (k == null || Time.unscaledTime - entered < .25f)
                return;
            if (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)
            {
                page++;
                if (page >= pages.Length)
                    Finish();
            }
        }

        void OnGUI()
        {
            if (!active || GameManager.Instance == null || GameManager.Instance.HasEnded)
                return;
            var old = GUI.color;
            int depth = GUI.depth;
            GUI.depth = -110;
            GUI.color = new Color(.012f, .017f, .03f, .985f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // A quiet, sparse star field; no noise or flashing.
            float t = Time.unscaledTime;
            for (int i = 0; i < 70; i++)
            {
                float x = Mathf.Repeat(i * 97.31f + Mathf.Sin(i * 1.7f) * 19f, 1f) * Screen.width;
                float y = Mathf.Repeat(i * 53.17f + t * .003f, 1f) * Screen.height;
                float a = .10f + .22f * Mathf.PerlinNoise(i * .21f, t * .03f);
                GUI.color = new Color(.72f, .82f, 1f, a);
                GUI.DrawTexture(new Rect(x, y, i % 7 == 0 ? 2 : 1, i % 7 == 0 ? 2 : 1), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label)
            {
                font = GameTypography.Current,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 42f, 18, 30)),
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = new Color(.84f, .82f, .70f);
            string body = pages != null && page < pages.Length ? pages[page] : "";
            GUI.Label(new Rect(Screen.width * .18f, Screen.height * .27f, Screen.width * .64f, Screen.height * .34f), body, style);
            var small = new GUIStyle(style) { fontSize = Mathf.RoundToInt(style.fontSize * .56f) };
            small.normal.textColor = new Color(.54f, .62f, .66f);
            GUI.Label(new Rect(Screen.width * .25f, Screen.height * .72f, Screen.width * .5f, 30), speaker, small);
            GUI.Label(new Rect(Screen.width * .25f, Screen.height * .77f, Screen.width * .5f, 30), StoryText.Local("Enter 继续", "Enter Continue"), small);
            GUI.depth = depth;
            GUI.color = old;
        }
    }
}
