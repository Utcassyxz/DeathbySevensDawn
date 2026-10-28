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

        [field: SerializeField, FormerlySerializedAs("<Day>k__BackingField")] public int Days { get; private set; } = 1;
        [field: SerializeField] public TimePeriod Period { get; private set; } = TimePeriod.Morning;

        public event Action<int, TimePeriod> TimeChanged;
        public event Action SeventhDayEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AdvanceTime()
        {
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.HasEnded) return;
            if (Days >= 7 && Period == TimePeriod.Night)
            {
                SeventhDayEnded?.Invoke();
                return;
            }
            if (Period != TimePeriod.Night) Period++;
            else { Period = TimePeriod.Morning; Days++; }
            Core.GameManager.Instance?.TryTriggerBadEnding(Days);
            TimeChanged?.Invoke(Days, Period);
        }

        // Sleeping uses the same progression path, so it cannot skip Day 7 or ending checks.
        public void SleepUntilNextMorning()
        {
            if(Core.GameManager.Instance != null && Core.GameManager.Instance.HasEnded) return;
            int periods=4-(int)Period;
            for(int i=0;i<periods;i++) {
                AdvanceTime();
                if(Core.GameManager.Instance != null && Core.GameManager.Instance.HasEnded) break;
            }
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void ResetToDayOne()
        {
            Days = 1;
            Period = TimePeriod.Morning;
            Core.GameManager.Instance?.TryTriggerBadEnding(Days);
            TimeChanged?.Invoke(Days, Period);
        }
    }
}
