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
            if(saved != null && saved.hasCheckpoint) { Days = Mathf.Max(1, saved.savedDay); Period = (TimePeriod)Mathf.Clamp(saved.savedPeriod,0,3); gameMinute=saved.savedMinute>=Starts[(int)Period]&&saved.savedMinute<Ends[(int)Period]?saved.savedMinute:Starts[(int)Period]; }
        }

        public static bool AutoTimeSuppressed;
        public bool AutoAdvance=true;
        public float periodLengthMinutes=6;
        static readonly float[] Starts={420,720,1020,1260};
        static readonly float[] Ends={720,1020,1260,1560};
        float gameMinute=420,saveElapsed,loadGrace;
        public float GameMinute=>gameMinute;
        public float PeriodProgress=>Mathf.InverseLerp(Starts[(int)Period],Ends[(int)Period],gameMinute);
        public string ClockText=>((int)gameMinute/60%24).ToString("00")+":"+((int)gameMinute%60).ToString("00");
        public bool AutoPaused=>AutoTimeSuppressed||!AutoAdvance||(!Application.isBatchMode&&!Application.isFocused)||Core.GameManager.Instance==null||Core.GameManager.Instance.HasEnded||Progression.StoryDebugActions.Enabled||Interaction.StoryJournal.WorldBlocked||Interaction.FurnitureInteraction.IsBusy||Time.timeScale==0||loadGrace>0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAutoTime(){AutoTimeSuppressed=false;}
        void OnEnable(){UnityEngine.SceneManagement.SceneManager.sceneLoaded+=Loaded;}
        void OnDisable(){UnityEngine.SceneManagement.SceneManager.sceneLoaded-=Loaded;}
        void Loaded(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode){loadGrace=1;}
        void Update()
        {
            loadGrace=Mathf.Max(0,loadGrace-Time.unscaledDeltaTime);
            if(!AutoPaused)AdvanceExplorationSeconds(Time.unscaledDeltaTime);
        }
        public void AdvanceExplorationSeconds(float seconds)
        {
            if(AutoPaused||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            float span=Ends[(int)Period]-Starts[(int)Period];
            gameMinute+=Mathf.Min(seconds,1)*span/(Mathf.Max(1,periodLengthMinutes)*60);
            if(gameMinute>=Ends[(int)Period]){gameMinute=Ends[(int)Period]-.001f;AdvanceTime();}
            Core.GameManager.Instance.State.savedMinute=gameMinute;
            saveElapsed+=seconds;if(saveElapsed>=30){saveElapsed=0;Progression.ProgressionSave.Write(Core.GameManager.Instance.State);}
        }
        public void RestoreMinute(float minute){gameMinute=minute>=Starts[(int)Period]&&minute<Ends[(int)Period]?minute:Starts[(int)Period];}
        void SnapMinute(){gameMinute=Starts[(int)Period];if(Core.GameManager.Instance!=null)Core.GameManager.Instance.State.savedMinute=gameMinute;}
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

            SnapMinute();
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
            Days=Mathf.Max(1,day);Period=period;SnapMinute();TimeChanged?.Invoke(Days,Period);
        }
        public void ResetToDayOne()
        {
            Days = 1;
            Period = TimePeriod.Morning;
            SnapMinute();
            TimeChanged?.Invoke(Days, Period);
        }
    }
}
