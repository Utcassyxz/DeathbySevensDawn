// 职责：统一冰箱触发和主动看电视的新闻；每条新闻一页配图，播完不再重播或推进时间。
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
namespace DeathBySeventhsDawn.Interaction
{
    public sealed class DayOneSupernovaEvent : MonoBehaviour
    {
        public const string SuppressionKey = "DayOneSupernovaSuppressed";
        public static DayOneSupernovaEvent Instance
        {
            get; private set;
        }
#if UNITY_EDITOR
        public static bool Suppressed=>UnityEditor.SessionState.GetBool(SuppressionKey,false);
#else
        public static bool Suppressed => false;
#endif
        public static bool WorldBlocked => Instance != null && (Instance.presenting || Time.frameCount <= Instance.blockedUntilFrame);
        public bool IsPresenting => presenting;
        public bool IsBoring => presenting && boring;
        public int PageIndex => page;
        public Texture2D CurrentIllustration => !boring && illustrations != null && page < illustrations.Length ? illustrations[page] : null;
        public bool BulletinConsumed => GameManager.Instance.State.dayOneSupernovaSeen || GameManager.Instance.State.skt079NewsSeen;
        bool presenting, boring, intro, replay;
        int page, openedFrame, blockedUntilFrame = -1;
        Texture2D[] illustrations;
        GUIStyle heading, body, small;
        readonly DialoguePages subtitles = new DialoguePages();
        void Awake()
        {
            Instance = this;
        }
        void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset += ResetEvent;
            SceneManager.sceneLoaded += Loaded;
        }
        void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset -= ResetEvent;
            SceneManager.sceneLoaded -= Loaded;
            Close();
        }
        void Loaded(Scene scene, LoadSceneMode mode) => Close();
        void ResetEvent()
        {
            Close();
            replay = false;
        }
        public bool PlayFromFridge()
        {
            var state = GameManager.Instance.State;
            if (presenting || GameManager.Instance.HasEnded || BulletinConsumed || state.dayOneNewsStarted || TimeManager.Instance.Days != 1 || SceneManager.GetActiveScene().name != "LivingRoom")
                return false;
            state.dayOneNewsStarted = true;
            state.newsPage = 0;
            ProgressionSave.Write(state);
            StartPresentation(true, false, false);
            return true;
        }
        public bool Resume()
        {
            var state = GameManager.Instance.State;
            if (presenting || GameManager.Instance.HasEnded || BulletinConsumed || !state.dayOneNewsStarted || TimeManager.Instance.Days != 1 || state.morningStage != 4 || SceneManager.GetActiveScene().name != "LivingRoom")
                return false;
            StartPresentation(true, false, false);
            return true;
        }
        public bool OpenTelevision()
        {
            if (presenting || GameManager.Instance.HasEnded || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy)
                return false;
            if (SceneManager.GetActiveScene().name != "LivingRoom")
                return false;
            StartPresentation(false, BulletinConsumed, false);
            return true;
        }
        void StartPresentation(bool isIntro, bool noNews, bool isReplay)
        {
            subtitles.Reset();
            intro = isIntro;
            boring = noNews;
            replay = isReplay;
            presenting = true;
            page = isIntro ? Mathf.Clamp(GameManager.Instance.State.newsPage, 0, StoryText.Current.dayOneBroadcast.pages.Length - 1) : 0;
            openedFrame = Time.frameCount;
            var paths = StoryText.Current.dayOneBroadcast.images;
            illustrations = new Texture2D[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                illustrations[i] = Resources.Load<Texture2D>(paths[i]);
        }
        public void Advance()
        {
            if (!presenting)
                return;
            if (boring)
            {
                Close();
                return;
            }
            if (subtitles.Next())
                return;
            subtitles.Reset();
            page++;
            if (!replay)
            {
                GameManager.Instance.State.newsPage = page;
                ProgressionSave.Write(GameManager.Instance.State);
            }
            if (page >= StoryText.Current.dayOneBroadcast.pages.Length)
                Finish();
        }
        void Finish()
        {
            bool finishMeal = intro;
            if (!replay)
            {
                var state = GameManager.Instance.State;
                state.dayOneSupernovaSeen = state.skt079NewsSeen = true;
                ProgressionSave.Write(state);
            }
            Close();
            if (finishMeal)
                MorningRoutine.Instance?.NewsFinished();
        }
        public void Close()
        {
            if (presenting)
                blockedUntilFrame = Time.frameCount;
            subtitles.Reset();
            presenting = false;
            page = 0;
            intro = false;
            boring = false;
        }
        void Update()
        {
            if (!presenting || GameSettingsUI.WorldBlocked || Time.frameCount <= openedFrame + 1)
                return;
            if (GameManager.Instance.HasEnded)
            {
                Close();
                return;
            }
            var k = Keyboard.current;
            if (k != null && k.eKey.wasPressedThisFrame)
                Advance();
            else if (k != null && k.escapeKey.wasPressedThisFrame && !intro)
                Close();
        }
        public static void Replay()
        {
            if (Instance != null)
            {
                Instance.Close();
                Instance.StartPresentation(false, false, true);
            }
        }
        public static void ResetForNewRun() => Instance?.ResetEvent();
        void OnGUI()
        {
            if (!presenting || GameSettingsUI.WorldBlocked)
                return;
            if (body == null)
            {
                body = new GUIStyle(GUI.skin.label) { fontSize = 25, wordWrap = true };
                heading = new GUIStyle(body) { fontSize = 34, fontStyle = FontStyle.Bold };
                small = new GUIStyle(body) { fontSize = 18 };
            }
            GameTypography.Use(body, heading, small);
            var matrix = GUI.matrix;
            var color = GUI.color;
            int depth = GUI.depth;
            float scale = Mathf.Min(Screen.width / 1200f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUI.depth = -45;
            float w = Screen.width / scale, h = Screen.height / scale;
            var text = StoryText.Current.dayOneBroadcast;
            try
            {
                GUI.color = new Color(.015f, .025f, .025f, .985f);
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
                GUI.color = Color.white;
                float panel = Mathf.Min(1000, w - 64), left = (w - panel) / 2;
                GUI.Label(new Rect(left, 22, panel, 48), boring ? StoryText.Local("电视", "Television") : text.title, heading);
                if (!boring)
                {
                    if (CurrentIllustration != null)
                        GUI.DrawTexture(new Rect(left, 84, panel, h * .49f), CurrentIllustration, ScaleMode.ScaleToFit);
                    subtitles.Set(text.pages[page], body, panel, 112);
                    GUI.Label(new Rect(left, h * .65f, panel, 112), subtitles.Current, body);
                    GUI.Label(new Rect(left, h - 76, panel, 42), $"{page + 1} / {text.pages.Length}   ·   " + subtitles.Counter + (page == text.pages.Length - 1 ? text.closePrompt : text.continuePrompt), small);
                }
                else
                {
                    GUI.Label(new Rect(left, h * .32f, panel, 40), text.narrator, small);
                    GUI.Label(new Rect(left, h * .4f, panel, 150), text.boring, body);
                    GUI.Label(new Rect(left, h - 76, panel, 42), StoryText.Local("E / Esc  离开", "E / Esc  Leave"), small);
                }
            }
            finally { GUI.matrix = matrix; GUI.color = color; GUI.depth = depth; }
        }
        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
