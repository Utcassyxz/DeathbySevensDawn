using System;
using UnityEngine;

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

        [field: SerializeField] public int Day { get; private set; } = 1;
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
                Day++;
            }

            TimeChanged?.Invoke(Day, Period);

            if (Day > 7)
            {
                SeventhDayEnded?.Invoke();
            }
        }

        public void ResetToDayOne()
        {
            Day = 1;
            Period = TimePeriod.Morning;
            TimeChanged?.Invoke(Day, Period);
        }
    }
}
