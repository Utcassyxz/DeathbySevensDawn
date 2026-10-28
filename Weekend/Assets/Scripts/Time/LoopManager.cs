using DeathBySeventhsDawn.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeathBySeventhsDawn.TimeSystem
{
    public class LoopManager : MonoBehaviour
    {
        [SerializeField] private string dayOneSceneName = "LivingRoom";

        public void RestartLoop()
        {
            GameManager.Instance?.RegisterNewLoop();
            TimeManager.Instance?.ResetToDayOne();

            // KnowledgeManager intentionally does not reset.
            SceneManager.LoadScene(dayOneSceneName);
        }
    }
}
