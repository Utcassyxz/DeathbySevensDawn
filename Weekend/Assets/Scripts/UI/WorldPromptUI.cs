// One world prompt slot; clock and small key legend occupy separate corners.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Progression
{
    [DefaultExecutionOrder(2000)]
    public sealed class WorldPromptUI : MonoBehaviour
    {
        struct Candidate
        {
            public string text;
            public int priority, frame;
        }

        static readonly Dictionary<string, Candidate> candidates = new Dictionary<string, Candidate>();
        public static string VisiblePrompt { get; private set; }

        static string selectedSource;

        public static bool CanActivate(string source)
        {
            Pick();
            return selectedSource == null || selectedSource == source;
        }

        public static void Offer(string source, string text, int priority = 20)
        {
            if (!string.IsNullOrEmpty(text))
                candidates[source] = new Candidate
                {
                    text = text,
                    priority = priority,
                    frame = Time.frameCount
                };
        }

        public static string Pick()
        {
            var game = GameManager.Instance;
            string best = null, bestSource = null;
            int priority = int.MinValue;
            foreach (var pair in candidates)
                if (pair.Value.frame >= Time.frameCount - 1 && (pair.Value.priority > priority || (pair.Value.priority == priority && string.CompareOrdinal(pair.Key, bestSource) < 0)))
                {
                    best = pair.Value.text;
                    priority = pair.Value.priority;
                    bestSource = pair.Key;
                }

            if (game != null && !game.State.tutorialComplete && game.State.morningStage >= 2 && game.State.morningStage <= 9)
            {
                int stage = game.State.morningStage;
                var routine = MorningRoutine.Instance;
                if (stage >= 7)
                {
                    // The tutorial reminder used to replace the actual telescope prompt.
                    // Keep a real E prompt when one exists, and only fall back to the hint.
                    if (best != null)
                    {
                        selectedSource = bestSource;
                        return best;
                    }
                    selectedSource = "telescope";
                    return TutorialGate.Reminder;
                }
                selectedSource = stage == 2 ? "neighbor" : "morning";
                return TutorialGate.Reminder + (routine != null && !string.IsNullOrEmpty(routine.ActionPrompt) ? "\n" + routine.ActionPrompt : "");
            }

            selectedSource = bestSource;
            if (game != null && game.State.tutorialComplete && game.LoopCount == 0 && game.State.firstNightWatchComplete && TimeManager.Instance.Days == 1 && TimeManager.Instance.Period == TimePeriod.Night)
                return best ?? StoryText.Local("该睡了。回卧室休息。", "Time for bed. Head back to the bedroom.");
            return best;
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += Loaded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset += Clear;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= Loaded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset -= Clear;
            Clear();
        }

        void Loaded(Scene s, LoadSceneMode m) => Clear();
        void Clear()
        {
            candidates.Clear();
            VisiblePrompt = null;
            selectedSource = null;
        }

        public static bool Hidden => GameManager.Instance == null || GameManager.Instance.HasEnded || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy || ProgressionHUD.DebugConsoleOpen || Object.FindAnyObjectByType<WindowLookout2D>()?.IsOpen == true;

        void LateUpdate()
        {
            VisiblePrompt = Hidden ? null : Pick();
        }

        void OnGUI()
        {
            var game = GameManager.Instance;
            if (game == null || game.HasEnded || MainMenuUI.WorldBlocked || GameSettingsUI.WorldBlocked || PauseMenuUI.WorldBlocked || ProgressionHUD.DebugConsoleOpen)
                return;
            var old = GUI.matrix;
            int depth = GUI.depth;
            var color = GUI.color;
            float scale = Mathf.Min(Screen.width / 1200f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUI.depth = -72;
            float w = Screen.width / scale, h = Screen.height / scale;
            var label = new GUIStyle(GUI.skin.label)
            {
                font = GameTypography.Current,
                fontSize = 18,
                wordWrap = true,
                alignment = TextAnchor.UpperRight
            };
            label.normal.textColor = NarrativeUI.Ink;
            try
            {
                // The clock never shares a rect with journal hints or destination instructions.
                if (!StoryJournal.WorldBlocked && !FurnitureInteraction.IsBusy)
                {
                    var clock = TimeManager.Instance;
                    var r = new Rect(w - 225, 16, 200, 46);
                    NarrativeUI.Panel(r);
                    GUI.Label(new Rect(r.x + 12, r.y + 9, r.width - 24, 32), StoryText.Local($"第 {clock.Days} 天 · ", $"DAY {clock.Days} · ") + GameplayFeedback.PeriodName(clock.Period), label);
                    if (game.State.journalUnlocked)
                    {
                        label.fontSize = 14;
                        label.alignment = TextAnchor.UpperRight;
                        GUI.Label(new Rect(w - 225, 70, 200, 36), StoryText.Local("J 日记", "J Journal") + (game.State.investigationUnlocked ? StoryText.Local("  ·  B 连线", "  ·  B Board") : "") + StoryText.Local("  ·  I 背包", "  ·  I Inventory"), label);
                    }
                }

                if (Hidden)
                    return;
                VisiblePrompt = Pick();
                if (!string.IsNullOrEmpty(VisiblePrompt))
                {
                    // One compact top-left task chip. No full-width bottom banner.
                    label.fontSize = 15;
                    label.alignment = TextAnchor.MiddleLeft;
                    float textHeight = label.CalcHeight(new GUIContent(VisiblePrompt), 248);
                    var p = new Rect(28, 106, 280, Mathf.Clamp(textHeight + 18, 44, 84));
                    NarrativeUI.Panel(p);
                    GUI.Label(new Rect(p.x + 14, p.y + 8, p.width - 28, p.height - 16), VisiblePrompt, label);
                }
            }
            finally
            {
                GUI.matrix = old;
                GUI.depth = depth;
                GUI.color = color;
            }
        }
    }
}
