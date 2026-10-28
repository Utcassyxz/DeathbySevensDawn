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
            if (Period != TimePeriod.Night)
            {
                Period++;
            }
            else
            {
                Period = TimePeriod.Morning;
                Days++;
            }

            TimeChanged?.Invoke(Days, Period);

            if (Days > 7)
            {
                SeventhDayEnded?.Invoke();
            }
        }

        public void ResetToDayOne()
        {
            Days = 1;
            Period = TimePeriod.Morning;
            TimeChanged?.Invoke(Days, Period);
        }
    }
}
