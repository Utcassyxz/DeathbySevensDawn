// CORE CONTRACT: 文案任务不要改动控制流程；工程变更先读 docs/CORE_CODE_MAP.md 并同步验收。
// 职责：只响应死亡的轮回；存活跨日不重置，终局状态禁止再次重置。
using System;
using DeathBySeventhsDawn.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeathBySeventhsDawn.TimeSystem
{
    public class LoopManager : MonoBehaviour
    {
        public static LoopManager Instance { get; private set; }

        [SerializeField]
        private string dayOneSceneName = "LivingRoom";
        bool restarting;
        public event Action LoopRestarted;
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Died += RestartLoop;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Died -= RestartLoop;
        }

        public void RestartLoop()
        {
            if (restarting || GameManager.Instance.Vitality > 0 || GameManager.Instance.HasEnded || GameManager.Instance.TryTriggerBadEnding(TimeManager.Instance.Days))
                return;
            if (!GameManager.Instance.HasLoopAbility)
                return;
            restarting = true;
            int returnDay=!Progression.CrisisRules.LegacyValidation?GameManager.Instance.State.stageReturnDay:0;
            if(returnDay==2||returnDay==7){GameManager.Instance.RestoreStageLoop(returnDay);TimeManager.Instance.SetNarrativeTime(returnDay,TimePeriod.Morning,420);Progression.ProgressionSave.Write(GameManager.Instance.State,false);}
            else {GameManager.Instance.RegisterNewLoop();TimeManager.Instance.ResetToDayOne();}
            LoopRestarted?.Invoke();
            if (Application.CanStreamedLevelBeLoaded(dayOneSceneName))
                SceneManager.LoadScene(dayOneSceneName);
            else
                Debug.LogWarning("Day 1 reset completed, but starting scene is absent from Build Profiles: " + dayOneSceneName);
            restarting = false;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
