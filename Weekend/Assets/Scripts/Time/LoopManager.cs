using System;
using DeathBySeventhsDawn.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeathBySeventhsDawn.TimeSystem
{
    public class LoopManager : MonoBehaviour
    {
        public static LoopManager Instance { get; private set; }
        [SerializeField] private string dayOneSceneName="LivingRoom";
        bool restarting;
        public event Action LoopRestarted;
        void Awake()
        {
            if(Instance!=null && Instance!=this) { Destroy(gameObject); return; }
            Instance=this; DontDestroyOnLoad(gameObject);
        }
        void OnEnable() { if(TimeManager.Instance!=null) TimeManager.Instance.SeventhDayEnded+=RestartLoop; }
        void OnDisable() { if(TimeManager.Instance!=null) TimeManager.Instance.SeventhDayEnded-=RestartLoop; }
        public void RestartLoop()
        {
            if(restarting || GameManager.Instance.HasEnded || GameManager.Instance.TryTriggerBadEnding(TimeManager.Instance.Days)) return;
            restarting=true;
            GameManager.Instance.RegisterNewLoop();
            TimeManager.Instance.ResetToDayOne();
            LoopRestarted?.Invoke();
            if(Application.CanStreamedLevelBeLoaded(dayOneSceneName)) SceneManager.LoadScene(dayOneSceneName);
            else Debug.LogWarning("Day 1 reset completed, but starting scene is absent from Build Profiles: "+dayOneSceneName);
            restarting=false;
        }
        void OnDestroy() { if(Instance==this) Instance=null; }
    }
}