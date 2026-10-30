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
        const float Duration=4f;
        TimeManager clock;
        int previousDay;
        TimePeriod previousPeriod;
        float journalRemaining, timeRemaining;
        string journalMessage, timeMessage;
        GUIStyle style;
        public string JournalMessage => journalRemaining>0 ? journalMessage : null;
        public string TimeMessage => timeRemaining>0 ? timeMessage : null;
        void OnEnable()
        {
            JournalUpdates.Added+=OnJournalAdded;
            if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=ResetJourney;
            clock=TimeManager.Instance;
            if(clock!=null) { previousDay=clock.Days;previousPeriod=clock.Period;clock.TimeChanged+=OnTimeChanged; }
        }
        void OnDisable()
        {
            JournalUpdates.Added-=OnJournalAdded;
            if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=ResetJourney;
            if(clock!=null) clock.TimeChanged-=OnTimeChanged;
        }
        void ResetJourney()
        {
            journalRemaining=timeRemaining=0; journalMessage=timeMessage=null;
            previousDay=clock.Days;previousPeriod=clock.Period;
        }
        void OnJournalAdded(JournalEntry entry)
        {
            journalMessage=DeathBySeventhsDawn.Story.StoryText.Local("日记已更新：","Journal updated: ")+Investigation.LocalTitle(entry.id,entry.title)+DeathBySeventhsDawn.Story.StoryText.Local("  · J 查看"," · J Read");
            journalRemaining=Duration;
        }
        void OnTimeChanged(int day,TimePeriod period)
        {
            if(day==previousDay && period==previousPeriod)return;
            previousDay=day;previousPeriod=period;
            timeMessage=DeathBySeventhsDawn.Story.StoryText.Local("第 ","Day ")+day+DeathBySeventhsDawn.Story.StoryText.Local(" 天 · "," · ")+PeriodName(period);
            timeRemaining=Duration;
        }
        public static string PeriodName(TimePeriod period)
        {
            switch(period) { case TimePeriod.Morning:return DeathBySeventhsDawn.Story.StoryText.Local("早晨","Morning");case TimePeriod.Afternoon:return DeathBySeventhsDawn.Story.StoryText.Local("午后","Afternoon");case TimePeriod.Evening:return DeathBySeventhsDawn.Story.StoryText.Local("傍晚","Evening");default:return DeathBySeventhsDawn.Story.StoryText.Local("深夜","Night"); }
        }
        bool Hidden()
        {
            if(GameManager.Instance==null || GameManager.Instance.HasEnded || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy || ProgressionHUD.DebugConsoleOpen)return true;
            var window=Object.FindAnyObjectByType<WindowLookout2D>();
            return window!=null && window.IsOpen;
        }
        void Update()
        {
            if(Hidden())return;
            journalRemaining=Mathf.Max(0,journalRemaining-Time.unscaledDeltaTime);
            timeRemaining=Mathf.Max(0,timeRemaining-Time.unscaledDeltaTime);
        }
        void OnGUI()
        {
            if(Hidden() || (journalRemaining<=0 && timeRemaining<=0))return;
            if(style==null)
            {
                var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Noto Sans CJK SC"},23);
                style=new GUIStyle(GUI.skin.box){font=font,fontSize=23,alignment=TextAnchor.MiddleCenter,wordWrap=true,padding=new RectOffset(18,18,8,8)};
                style.normal.textColor=new Color(.94f,.91f,.79f);
            }
            var matrix=GUI.matrix;
            float scale=Mathf.Clamp(Screen.height/900f,.8f,2.4f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float width=Screen.width/scale,panel=Mathf.Min(640,width-24),y=72;
            try
            {
                if(timeRemaining>0){GUI.Box(new Rect((width-panel)/2,y,panel,56),timeMessage,style);y+=62;}
                if(journalRemaining>0)GUI.Box(new Rect((width-panel)/2,y,panel,80),journalMessage,style);
            }
            finally {GUI.matrix=matrix;}
        }
    }
}
