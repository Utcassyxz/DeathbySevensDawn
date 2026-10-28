using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeathBySeventhsDawn.World
{
 [RequireComponent(typeof(BoxCollider2D))]
 public class RoomDoor2D:MonoBehaviour
 {
  public string destinationScene;
  public Vector2 arrivalPosition;
  static string pendingScene;
  static Vector2 pendingPosition;
  static float blockedUntil;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Initialize(){pendingScene=null;blockedUntil=0;SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;}
  static void OnLoaded(Scene scene,LoadSceneMode mode)
  {
   if(scene.path!=pendingScene)return;
   var player=GameObject.FindWithTag("Player");
   if(player!=null){player.transform.position=new Vector3(pendingPosition.x,pendingPosition.y,0);var rb=player.GetComponent<Rigidbody2D>();if(rb!=null){rb.position=pendingPosition;rb.linearVelocity=Vector2.zero;}Physics2D.SyncTransforms();}
   pendingScene=null;blockedUntil=Time.unscaledTime+.6f;
  }
  void OnTriggerEnter2D(Collider2D other)
  {
   if(!other.CompareTag("Player")||Time.unscaledTime<blockedUntil||pendingScene!=null)return;
   if(!Application.CanStreamedLevelBeLoaded(destinationScene)){Debug.LogError("Door destination missing from scene list: "+destinationScene);return;}
   pendingScene=destinationScene;pendingPosition=arrivalPosition;SceneManager.LoadScene(destinationScene);
  }
 }
}
