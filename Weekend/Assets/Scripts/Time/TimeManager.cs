// CORE CONTRACT: 文案任务不要改动控制流程；工程变更先读 docs/CORE_CODE_MAP.md 并同步验收。
// CORE: Days唯一时钟；原地等待不算睡眠，致死危机先演出，存活允许超过七天。
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace DeathBySeventhsDawn.TimeSystem
{
    public enum TimePeriod
    {
        Morning,
        Afternoon,
        Evening,
        Night
    }

    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [field: SerializeField, FormerlySerializedAs("<Day>k__BackingField")]
        public int Days { get; private set; } = 1;

        [field: SerializeField]
        public TimePeriod Period { get; private set; } = TimePeriod.Morning;

        public event Action<int, TimePeriod> TimeChanged;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            var saved = Core.GameManager.Instance?.State;
            if(saved != null && saved.hasCheckpoint) { Days = Mathf.Max(1, saved.savedDay); Period = (TimePeriod)Mathf.Clamp(saved.savedPeriod,0,3); }
        }

        bool completingSleep;
        public void AdvanceTime()
        {
            if (Progression.WorldEndSequence.IsPresenting || (Core.GameManager.Instance != null && Core.GameManager.Instance.HasEnded))
                return;
            if (Period == TimePeriod.Night)
            {
                var game = Core.GameManager.Instance;
                if (!Progression.StoryDebugActions.Enabled &&
                    Progression.CrisisRules.Due(Days, game.State) != Progression.CrisisKind.None)
                {
                    Progression.WorldEndSequence.BeginFatalCrisis(Progression.CrisisRules.Due(Days, game.State));
                    return;
                }
                if (!completingSleep)
                {
                    game.State.sleeplessDays++;
                    if (game.State.sleeplessDays > 2) game.LoseSanity(5);
                }
            }
            if (Period != TimePeriod.Night)
                Period++;
            else
            {
                Period = TimePeriod.Morning;
                Days++;
            }

            Progression.ProgressionSave.Write(Core.GameManager.Instance.State);
            TimeChanged?.Invoke(Days, Period);
        }

        // Sleeping uses the same progression path, so it cannot skip Day 7 or ending checks.
        // CORE FLOW: all player sleep passes through the same dream owner.
        public void SleepUntilNextMorning()
        {
            if (Progression.NightDreamSequence.BeginSleep())
                return;
            CompleteSleep();
        }

        // Explicit clock-only transition for the dream runner and scenario setup.
        public void CompleteSleep()
        {
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.HasEnded)
                return;
            Core.GameManager.Instance.State.sleeplessDays = 0;
            completingSleep = true;
            try
            {
                int periods = 4 - (int)Period;
                for (int i = 0; i < periods; i++)
                {
                    AdvanceTime();
                    if (Core.GameManager.Instance != null && Core.GameManager.Instance.HasEnded)
                        break;
                }
            }
            finally
            {
                completingSleep = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void DebugSetTime(int day, TimePeriod period)
        {
            if(!Progression.StoryDebugActions.Enabled)return;
            Days=Mathf.Max(1,day);Period=period;TimeChanged?.Invoke(Days,Period);
        }
        public void ResetToDayOne()
        {
            Days = 1;
            Period = TimePeriod.Morning;
            TimeChanged?.Invoke(Days, Period);
        }
    }
}
