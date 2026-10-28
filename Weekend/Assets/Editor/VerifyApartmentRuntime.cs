using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.World;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class VerifyApartmentRuntime
{
 const string Key="ApartmentRuntimeVerification";
 static readonly string[] Targets={"Balcony","LivingRoom","Corridor","LivingRoom","Bedroom","LivingRoom","Bathroom","LivingRoom"};
 static int movementStage; static double movementUntil; static Vector2 movementOrigin; static float movedDistance;
 static int step; static bool awaiting; static double readyAt,deadline; static Vector2 arrival;
 static VerifyApartmentRuntime(){EditorApplication.update+=Tick;}
 [MenuItem("Prototype/Verify current layout")]
 public static void Start(){step=0;movementStage=0;awaiting=false;readyAt=0;deadline=0;movementUntil=0;movedDistance=0;SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/Apartment/LivingRoom.unity");EditorApplication.isPlaying=true;}
 static void Tick()
 {
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
  try{
   double now=EditorApplication.timeSinceStartup;
   if(deadline==0){deadline=now+40;readyAt=now+1;}
   if(now>deadline)throw new Exception("Timeout at route "+step);
   if(movementStage==0){movementOrigin=GameObject.FindWithTag("Player").transform.position;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.D));movementUntil=now+.6;movementStage=1;return;}
   if(movementStage==1){if(now<movementUntil)return;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());movedDistance=((Vector2)GameObject.FindWithTag("Player").transform.position-movementOrigin).magnitude;if(movedDistance<.5f)throw new Exception("Player did not respond to held D input: distance="+movedDistance);Debug.Log("MOVEMENT_OK distance="+movedDistance);movementStage=2;readyAt=now+.2;return;}
   if(awaiting){
    if(SceneManager.GetActiveScene().name!=Targets[step])return;
    var p=GameObject.FindWithTag("Player");if(p==null||Camera.main==null)throw new Exception("Missing player/camera");
    if(Vector2.Distance(p.transform.position,arrival)>.08f)throw new Exception("Wrong arrival on "+Targets[step]+": "+p.transform.position+" expected "+arrival);
    Debug.Log("RUNTIME_ROUTE_OK "+step+" -> "+Targets[step]);step++;awaiting=false;readyAt=now+.8;
    if(step==Targets.Length){SessionState.SetBool(Key,false);File.WriteAllText("../runtime-validation.txt","PASS: player moved "+movedDistance+" units under held D input. 8 physical door triggers switched scenes and placed player at expected arrivals.\n");Debug.Log("RUNTIME_VERIFICATION_OK");if(Application.isBatchMode)EditorApplication.Exit(0);else EditorApplication.isPlaying=false;return;}
   }
   if(now<readyAt)return;
   var door=UnityEngine.Object.FindObjectsByType<RoomDoor2D>(FindObjectsSortMode.None).Single(d=>d.destinationScene.EndsWith("/"+Targets[step]+".unity"));
   arrival=door.arrivalPosition;awaiting=true;
   var rb=GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>();rb.position=door.transform.position;rb.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();
  }catch(Exception ex){SessionState.SetBool(Key,false);Debug.LogException(ex);if(Application.isBatchMode)EditorApplication.Exit(1);else EditorApplication.isPlaying=false;}
 }
}
