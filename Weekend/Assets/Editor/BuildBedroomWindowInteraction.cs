using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.World;

public static class BuildBedroomWindowInteraction
{
    const string Rooms = "Assets/Scenes/Apartment/";
    [MenuItem("Prototype/Build bedroom window interaction")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play first.");
        EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory("Assets/Art/WindowViews");
        Directory.CreateDirectory("Assets/ScriptableObjects/WindowViews");
        for (int day = 1; day <= 7; day++)
        {
            string path = "Assets/Art/WindowViews/Day" + day.ToString("00") + ".png";
            if (File.Exists(path)) continue;
            var texture = new Texture2D(320, 180, TextureFormat.RGB24, false);
            float decay = (day - 1) / 6f;
            Color sky = Color.Lerp(new Color(.55f,.74f,.77f), new Color(.19f,.21f,.19f), decay);
            Color building = Color.Lerp(new Color(.40f,.47f,.47f), new Color(.12f,.15f,.14f), decay);
            for (int y = 0; y < 180; y++) for (int x = 0; x < 320; x++)
            {
                Color color = Color.Lerp(sky * .75f, sky, y / 180f);
                int block = x / 40;
                int roof = 42 + (block * 37 % 61);
                if (y < roof)
                {
                    color = building * (.75f + block % 3 * .1f);
                    if (x % 12 >= 4 && x % 12 <= 7 && y % 15 >= 5 && y % 15 <= 9)
                        color = (block + y / 15 + x / 12) % 8 >= day ? new Color(.77f,.70f,.45f) : building * .45f;
                }
                if (day >= 3 && (x * 17 + y * 11) % 79 < day - 2)
                    color = Color.Lerp(color, new Color(.43f,.46f,.43f), .25f + decay * .2f);
                if (day >= 5 && x > 180 && x < 250 && y > 50 && y < 90 + (x % 13))
                    color = Color.Lerp(color, new Color(.20f,.21f,.18f), decay * .65f);
                if (day >= 6 && (x + y / 3) % 53 == 0) color *= .7f;
                texture.SetPixel(x,y,color);
            }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.Refresh();
        var images = new Texture2D[7];
        for(int i=0;i<7;i++)
        {
            string path="Assets/Art/WindowViews/Day"+(i+1).ToString("00")+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            images[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        const string sequencePath="Assets/ScriptableObjects/WindowViews/BedroomWindowViews.asset";
        var sequence=AssetDatabase.LoadAssetAtPath<WindowViewSequence>(sequencePath);
        if(sequence==null){sequence=ScriptableObject.CreateInstance<WindowViewSequence>();AssetDatabase.CreateAsset(sequence,sequencePath);}
        sequence.SetImages(images);EditorUtility.SetDirty(sequence);
        var bedroom=EditorSceneManager.OpenScene(Rooms+"Bedroom.unity");
        var point=GameObject.Find("Bedroom window interaction");
        if(point==null)point=new GameObject("Bedroom window interaction");
        point.transform.position=new Vector3(-.2f,1.10f,0);
        var area=point.GetComponent<BoxCollider2D>();if(area==null)area=point.AddComponent<BoxCollider2D>();area.isTrigger=true;area.size=new Vector2(2.8f,1.3f);
        var lookout=point.GetComponent<WindowLookout2D>();if(lookout==null)lookout=point.AddComponent<WindowLookout2D>();lookout.Configure(sequence);
        EnsureTime();EditorSceneManager.MarkSceneDirty(bedroom);EditorSceneManager.SaveScene(bedroom);
        var living=EditorSceneManager.OpenScene(Rooms+"LivingRoom.unity");EnsureTime();
        foreach(var door in UnityEngine.Object.FindObjectsByType<RoomDoor2D>(FindObjectsSortMode.None))
            if(door.destinationScene.EndsWith("Corridor.unity"))door.arrivalPosition=new Vector2(0,-.95f);
        EditorSceneManager.MarkSceneDirty(living);EditorSceneManager.SaveScene(living);
        var corridor=EditorSceneManager.OpenScene(Rooms+"Corridor.unity");
        foreach(var t in corridor.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
        {
            bool center=Mathf.Abs(t.position.x)<1;
            if(t.name=="Apartment entrance"){t.position=new Vector3(0,-1.75f,0);t.localScale=new Vector3(t.localScale.x,.25f,t.localScale.z);}
            if(t.name=="Door handle"&&center){t.position=new Vector3(t.position.x,-1.75f,0);t.localScale=new Vector3(t.localScale.x,.06f,1);}
            if(t.name=="Door mat"&&center)t.position=new Vector3(0,-1.1f,0);
            if(t.name=="Wall lamp"&&center)t.gameObject.SetActive(false);
            if((t.name=="Door left jamb"||t.name=="Door right jamb")&&center){t.position=new Vector3(t.position.x,-1.75f,0);t.localScale=new Vector3(t.localScale.x,.25f,1);}
            if(t.name=="South skirting")t.gameObject.SetActive(false);
        }
        var back=UnityEngine.Object.FindObjectsByType<RoomDoor2D>(FindObjectsSortMode.None).Single();
        back.transform.position=new Vector3(0,-1.72f,0);back.GetComponent<BoxCollider2D>().size=new Vector2(1.2f,.5f);
        GameObject.FindWithTag("Player").transform.position=new Vector3(0,-.95f,0);
        EditorSceneManager.MarkSceneDirty(corridor);EditorSceneManager.SaveScene(corridor);
        AssetDatabase.SaveAssets();
        for(int day=1;day<=7;day++)if(sequence.GetImage(day)!=images[day-1])throw new Exception("Window sequence mapping failed");
        PolishApartmentDetails.ValidatePaths();
        EditorSceneManager.OpenScene(Rooms+"Bedroom.unity");
        Debug.Log("BEDROOM_WINDOW_BUILD_OK: Days 1-7 mapped, corridor entrance south, walking routes checked.");
    }
    static void EnsureTime()
    {
        if(UnityEngine.Object.FindFirstObjectByType<TimeManager>()==null)new GameObject("Time Manager").AddComponent<TimeManager>();
    }
}
