// 职责：非模态的日记与时段提示；弹窗期间暂存显示时长。
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.TimeSystem;
namespace DeathBySeventhsDawn.Progression
{
    // 两条轻量提示，不夺取输入。弹窗期间暂存，回到房间再显示完整时长。
    public sealed class GameplayFeedback : MonoBehaviour
    {
        const float Duration = 4f;
        TimeManager clock;
        int previousDay;
        TimePeriod previousPeriod;
        float journalRemaining, timeRemaining;
        string journalMessage, timeMessage;
        GUIStyle style;
        public string JournalMessage => journalRemaining > 0 ? journalMessage : null;
        public string TimeMessage => timeRemaining > 0 ? timeMessage : null;
        void OnEnable()
        {
            JournalUpdates.Added += OnJournalAdded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset += ResetJourney;
            clock = TimeManager.Instance;
            if (clock != null)
            {
                previousDay = clock.Days;
                previousPeriod = clock.Period;
                clock.TimeChanged += OnTimeChanged;
            }
        }
        void OnDisable()
        {
            JournalUpdates.Added -= OnJournalAdded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset -= ResetJourney;
            if (clock != null)
                clock.TimeChanged -= OnTimeChanged;
        }
        void ResetJourney()
        {
            journalRemaining = timeRemaining = 0;
            journalMessage = timeMessage = null;
            previousDay = clock.Days;
            previousPeriod = clock.Period;
        }
        void OnJournalAdded(JournalEntry entry)
        {
            journalMessage = DeathBySeventhsDawn.Story.StoryText.UI("copy.GameplayFeedback.172") + Investigation.LocalTitle(entry.id, entry.title) + DeathBySeventhsDawn.Story.StoryText.UI("copy.GameplayFeedback.173");
            journalRemaining = Duration;
        }
        void OnTimeChanged(int day, TimePeriod period)
        {
            if (day == previousDay && period == previousPeriod)
                return;
            previousDay = day;
            previousPeriod = period;
            timeMessage = DeathBySeventhsDawn.Story.StoryText.UI("copy.GameplayFeedback.174") + day + DeathBySeventhsDawn.Story.StoryText.UI("copy.GameplayFeedback.175") + PeriodName(period);
            timeRemaining = Duration;
        }
        public static string PeriodName(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.Morning:
                    return DeathBySeventhsDawn.Story.StoryText.UI("copy.GameplayFeedback.176");
                case TimePeriod.Afternoon:
                    return DeathBySeventhsDawn.Story.StoryText.UI("copy.GameplayFeedback.177");
                case TimePeriod.Evening:
                    return DeathBySeventhsDawn.Story.StoryText.UI("copy.GameplayFeedback.178");
                default:
                    return DeathBySeventhsDawn.Story.StoryText.UI("copy.GameplayFeedback.179");
            }
        }
        bool Hidden()
        {
            if (GameManager.Instance == null || GameManager.Instance.HasEnded || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy || ProgressionHUD.DebugConsoleOpen)
                return true;
            var window = Object.FindAnyObjectByType<WindowLookout2D>();
            return window != null && window.IsOpen;
        }
        void Update()
        {
            if (Hidden())
                return;
            journalRemaining = Mathf.Max(0, journalRemaining - Time.unscaledDeltaTime);
            timeRemaining = Mathf.Max(0, timeRemaining - Time.unscaledDeltaTime);
        }
        void OnGUI()
        {
            DeathBySeventhsDawn.Progression.GameTypography.Use(style);
            if (Hidden() || (journalRemaining <= 0 && timeRemaining <= 0))
                return;
            if (style == null)
            {
                var font = DeathBySeventhsDawn.Progression.GameTypography.Current;
                style = new GUIStyle(GUI.skin.box) { font = font, fontSize = 23, alignment = TextAnchor.MiddleCenter, wordWrap = true, padding = new RectOffset(18, 18, 8, 8) };
                style.normal.textColor = new Color(.94f, .91f, .79f);
            }
            var matrix = GUI.matrix;
            float scale = Mathf.Clamp(Screen.height / 900f, .8f, 2.4f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width=Screen.width/scale,height=Screen.height/scale,panel=Mathf.Min(640,width-48);
            try
            {
                if(journalRemaining>0 && string.IsNullOrEmpty(WorldPromptUI.VisiblePrompt))
                {var r=new Rect((width-panel)/2,height-230,panel,70);NarrativeUI.Panel(r);GUI.Label(r,journalMessage,style);}
            }
            finally { GUI.matrix = matrix; }
        }
    }
}
