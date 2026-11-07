using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;

namespace DeathBySeventhsDawn.Progression
{
    public sealed class JourneyCheckpoint : MonoBehaviour
    {
        static bool restoring;
        static string resumeScene;
        static Vector3 resumePosition;
        void OnEnable(){SceneManager.sceneLoaded+=Loaded;}
        void OnDisable(){SceneManager.sceneLoaded-=Loaded;}
        public static bool Resume()
        {
            var state=GameManager.Instance.State;
            if(!state.hasCheckpoint||string.IsNullOrEmpty(state.savedScene)||!Application.CanStreamedLevelBeLoaded(state.savedScene))return false;
            DeathBySeventhsDawn.Interaction.RooftopAccess.Instance.RestoreFloor(state.savedFloor);
            restoring=true;resumeScene=state.savedScene;resumePosition=new Vector3(state.savedX,state.savedY,state.savedZ);
            MainMenuUI.Instance.DebugSkip();SceneManager.LoadScene(resumeScene);return true;
        }
        public static void RestoreActor(GameObject actor)
        {
            if(!restoring||SceneManager.GetActiveScene().name!=resumeScene)return;
            var cc=actor.GetComponent<CharacterController>();if(cc!=null)cc.enabled=false;
            actor.transform.position=resumePosition;
            var rb=actor.GetComponent<Rigidbody2D>();if(rb!=null)rb.position=resumePosition;
            if(cc!=null)cc.enabled=true;restoring=false;
        }
        void Loaded(Scene scene,LoadSceneMode mode){StartCoroutine(AfterLoad());}
        IEnumerator AfterLoad()
        {
            yield return null;yield return null;
            var actor=GameObject.FindWithTag("Player");if(actor!=null)RestoreActor(actor);
            if(!MainMenuUI.WorldBlocked)ProgressionSave.Write(GameManager.Instance.State);
        }
        void OnApplicationPause(bool paused){if(paused&&!MainMenuUI.WorldBlocked)ProgressionSave.Write(GameManager.Instance.State);}
        void OnApplicationQuit(){if(!Application.isBatchMode&&!MainMenuUI.WorldBlocked)ProgressionSave.Write(GameManager.Instance.State);}
    }
}
