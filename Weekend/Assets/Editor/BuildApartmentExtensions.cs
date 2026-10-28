using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.World;

public static class BuildApartmentExtensions
{
 static Material material;
 static Sprite square;
 static GameObject playerTemplate, cameraTemplate;
 const string Scenes = "Assets/Scenes/Apartment/";
 const string Art = "Assets/Art/Extensions/";
 // Legacy generator retained for reference; current authored scenes use the south balcony layout.
 public static void Build()
 {
  Directory.CreateDirectory(Art); Directory.CreateDirectory("Assets/Prefabs/Extensions");
  AssetDatabase.Refresh();
  material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Prototype/PrototypeUnlit.mat");
  square=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Prototype/Square.png").OfType<Sprite>().First();
  EditorSceneManager.OpenScene(Scenes+"LivingRoom.unity");
  playerTemplate=UnityEngine.Object.Instantiate(GameObject.FindWithTag("Player"));
  cameraTemplate=UnityEngine.Object.Instantiate(Camera.main.gameObject);
  var playerSource=playerTemplate; var cameraSource=cameraTemplate;
  playerTemplate=PrefabUtility.SaveAsPrefabAsset(playerSource,"Assets/Prefabs/Extensions/ExplorationPlayer.prefab");
  cameraTemplate=PrefabUtility.SaveAsPrefabAsset(cameraSource,"Assets/Prefabs/Extensions/ExplorationCamera.prefab");
  UnityEngine.Object.DestroyImmediate(playerSource); UnityEngine.Object.DestroyImmediate(cameraSource);
  
  // Connect through the existing entrance and the open area at the left of the window.
  RemoveNamed("Door to Corridor"); RemoveNamed("Door to Balcony"); RemoveNamed("Balcony doorway");
  var north=GameObject.Find("Wall North").GetComponent<BoxCollider2D>();north.size=Vector2.one;north.offset=Vector2.zero;
  Door("Door to Corridor",new Vector2(.37f,3.32f),new Vector2(1.15f,.45f),"Corridor",new Vector2(0,1.05f));
  Door("Door to Balcony",new Vector2(-3.1f,3.30f),new Vector2(1.15f,.45f),"Balcony",new Vector2(0,-1.05f));
  Panel("Balcony doorway",-3.1f,3.3f,1.2f,.5f,new Color(.32f,.52f,.55f),4,false);
  EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  MakeRoom("Balcony",new Vector2(0,-1.05f),new Color(.10f,.15f,.22f));
  Texture("BalconyTiles",128,64,(x,y)=> x%16==0||y%16==0 ? new Color(.23f,.29f,.3f):new Color(.43f+(x+y)%3*.015f,.46f,.43f));
  Picture("Balcony tiles","BalconyTiles",0,0,10,4,-5);
  Panel("North railing",0,2,10.4f,.22f,new Color(.17f,.23f,.25f),4,true);
  Panel("West railing",-5,0,.22f,4,new Color(.17f,.23f,.25f),4,true);
  Panel("East railing",5,0,.22f,4,new Color(.17f,.23f,.25f),4,true);
  for(int i=-5;i<=5;i++) Panel("Railing post",i*.9f,1.85f,.08f,.5f,new Color(.38f,.44f,.43f),5,false);
  Panel("South wall left",-2.85f,-2,4.3f,.25f,new Color(.29f,.3f,.31f),4,true);
  Panel("South wall right",2.85f,-2,4.3f,.25f,new Color(.29f,.3f,.31f),4,true);
  Door("Return to Apartment",new Vector2(0,-1.75f),new Vector2(1.25f,.5f),"LivingRoom",new Vector2(-3.1f,2.35f));
  Panel("Door sill",0,-1.8f,1.4f,.25f,new Color(.65f,.58f,.43f),1,false);
  var planter=new GameObject("Balcony planter");
  Panel("Terracotta planter",-3.8f,.85f,1.1f,.7f,new Color(.47f,.28f,.20f),2,true).transform.SetParent(planter.transform);
  Panel("Leaves",-3.8f,1.22f,1.2f,.5f,new Color(.26f,.37f,.24f),3,false).transform.SetParent(planter.transform);
  PrefabUtility.SaveAsPrefabAssetAndConnect(planter,"Assets/Prefabs/Extensions/BalconyPlanter.prefab",InteractionMode.AutomatedAction);
  // Skyline is scenery behind the railing, outside the walkable surface.
  for(int i=0;i<12;i++)Panel("Distant building",-7+i*1.25f,3.7f,1,1.2f+(i%3)*.45f,new Color(.16f,.21f,.29f),-8,false);
  Save("Balcony");
  MakeRoom("Corridor",new Vector2(0,1.05f),new Color(.08f,.09f,.12f));
  Texture("CorridorTiles",192,64,(x,y)=>x%24==0||y%16==0?new Color(.22f,.21f,.21f):new Color(.39f+(x/24+y/16)%2*.035f,.36f,.31f));
  Picture("Corridor tiles","CorridorTiles",0,0,14,4,-5);
  Panel("North wall",0,2,14.4f,.3f,new Color(.34f,.31f,.28f),3,true);
  Panel("South wall",0,-2,14.4f,.3f,new Color(.25f,.24f,.23f),3,true);
  Panel("West end",-7,0,.3f,4,new Color(.25f,.24f,.23f),3,true);
  Panel("East end",7,0,.3f,4,new Color(.25f,.24f,.23f),3,true);
  for(int i=-1;i<=1;i++)
  {
   float x=i*4.7f;
   Panel(i==0?"Apartment entrance":"Closed neighbor door",x,1.65f,1.25f,.55f,new Color(.32f,.22f,.17f),4,false);
   Panel("Door handle",x+.4f,1.55f,.12f,.12f,new Color(.73f,.62f,.35f),5,false);
   Panel("Door mat",x,.95f,1.65f,.5f,new Color(.26f,.24f,.21f),0,false);
   Panel("Wall lamp",x,2.3f,.55f,.18f,new Color(.85f,.73f,.44f),4,false);
  }
  Door("Return to Apartment",new Vector2(0,1.65f),new Vector2(1.2f,.5f),"LivingRoom",new Vector2(.37f,2.3f));
  var panel=Panel("Utility cabinet",5.9f,-1.25f,1,.65f,new Color(.38f,.42f,.43f),2,true);
  PrefabUtility.SaveAsPrefabAssetAndConnect(panel,"Assets/Prefabs/Extensions/CorridorCabinet.prefab",InteractionMode.AutomatedAction);
  Save("Corridor");
  var list=EditorBuildSettings.scenes.ToList();
  foreach(string name in new[]{"Balcony","Corridor"}) if(!list.Any(s=>s.path==Scenes+name+".unity"))list.Add(new EditorBuildSettingsScene(Scenes+name+".unity",true));
  EditorBuildSettings.scenes=list.ToArray();
  
  AssetDatabase.SaveAssets();
  Validate();
  EditorSceneManager.OpenScene(Scenes+"LivingRoom.unity");
  Debug.Log("EXTENSIONS_BUILD_OK");
 }
 static void RemoveNamed(string name){var g=GameObject.Find(name);if(g!=null)UnityEngine.Object.DestroyImmediate(g);}
 static void MakeRoom(string name,Vector2 spawn,Color background)
 {
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var p=UnityEngine.Object.Instantiate(playerTemplate);p.name="Player";p.transform.position=spawn;p.SetActive(true);
  var c=UnityEngine.Object.Instantiate(cameraTemplate);c.name="Main Camera";c.SetActive(true);
  c.GetComponent<Camera>().backgroundColor=background;c.GetComponent<Camera>().orthographicSize=5;
 }
 static GameObject Panel(string name,float x,float y,float w,float h,Color color,int order,bool solid)
 {
  var g=new GameObject(name);g.transform.position=new Vector3(x,y,0);
  var s=g.AddComponent<SpriteRenderer>();s.sprite=square;s.sharedMaterial=material;s.color=color;s.sortingOrder=order;
  g.transform.localScale=new Vector3(w/square.bounds.size.x,h/square.bounds.size.y,1);
  if(solid)g.AddComponent<BoxCollider2D>();return g;
 }
 static void Door(string name,Vector2 p,Vector2 size,string scene,Vector2 arrival)
 {
  var g=new GameObject(name);g.transform.position=p;
  var col=g.AddComponent<BoxCollider2D>();col.isTrigger=true;col.size=size;
  var door=g.AddComponent<RoomDoor2D>();door.destinationScene=Scenes+scene+".unity";door.arrivalPosition=arrival;
 }
 static void Texture(string name,int w,int h,Func<int,int,Color> pixel)
 {
  var t=new Texture2D(w,h);for(int y=0;y<h;y++)for(int x=0;x<w;x++)t.SetPixel(x,y,pixel(x,y));t.Apply();
  string path=Art+name+".png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
  AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);
  imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=16;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.SaveAndReimport();
 }
 static void Picture(string name,string asset,float x,float y,float w,float h,int order)
 {
  var g=Panel(name,x,y,w,h,Color.white,order,false);var s=g.GetComponent<SpriteRenderer>();s.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+asset+".png");g.transform.localScale=new Vector3(w/s.sprite.bounds.size.x,h/s.sprite.bounds.size.y,1);
 }
 static void Save(string name){EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Scenes+name+".unity");}
 public static void Validate()
 {
  int doors=0;
  foreach(string name in new[]{"LivingRoom","Bedroom","Bathroom","Balcony","Corridor"})
  {
   EditorSceneManager.OpenScene(Scenes+name+".unity");
   if(GameObject.FindWithTag("Player")==null||Camera.main==null)throw new Exception(name+": missing player or camera");
   foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)>0)throw new Exception("Missing script");
   foreach(var d in UnityEngine.Object.FindObjectsByType<RoomDoor2D>(FindObjectsSortMode.None))
   {
    if(!EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==d.destinationScene))throw new Exception("Unregistered door "+d.name);doors++;
   }
  }
  File.WriteAllText("../Validation.txt","All five scenes have player/camera and no missing scripts. "+doors+" door destinations registered.\n");
  Debug.Log("EXTENSIONS_VALIDATION_OK doors="+doors);
 }
}

