// 职责：场景维护工具：OrganizeApartmentRooms；仅显式运行，日常开发无需重建已有资源。
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DeathBySeventhsDawn.World;

public static class OrganizeApartmentRooms
{
    const string Folder = "Assets/Scenes/Apartment";
    [MenuItem("Prototype/Organize apartment folder and clear sofa")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode before organizing rooms.");
        var paths = new Dictionary<string, string>{
   {"Assets/Scenes/Apartment.unity",Folder+"/LivingRoom.unity"},
   {"Assets/Scenes/Bedroom.unity",Folder+"/Bedroom.unity"},
   {"Assets/Scenes/Bathroom.unity",Folder+"/Bathroom.unity"},
   {"Assets/Scenes/Balcony.unity",Folder+"/Balcony.unity"},
   {"Assets/Scenes/Corridor.unity",Folder+"/Corridor.unity"}};
        var buildScenes = EditorBuildSettings.scenes;
        EditorSceneManager.SaveOpenScenes();
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Scenes", "Apartment");
        foreach (var p in paths)
        {
            if (!File.Exists(p.Key))
            {
                if (File.Exists(p.Value))
                    continue;
                throw new FileNotFoundException(p.Key);
            }
            if (File.Exists(p.Value))
                throw new InvalidOperationException("Destination already exists: " + p.Value);
            string error = AssetDatabase.MoveAsset(p.Key, p.Value);
            if (!string.IsNullOrEmpty(error))
                throw new Exception(error);
        }
        foreach (var p in paths)
        {
            var scene = EditorSceneManager.OpenScene(p.Value);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var door in root.GetComponentsInChildren<RoomDoor2D>(true))
                {
                    if (paths.TryGetValue(door.destinationScene, out string newPath))
                    {
                        door.destinationScene = newPath;
                        EditorUtility.SetDirty(door);
                    }
                }
            if (p.Value.EndsWith("/LivingRoom.unity"))
            {
                var sofa = GameObject.Find("Old sofa");
                sofa.transform.localScale = new Vector3(.65f, .65f, 1);
                sofa.transform.localPosition = new Vector3(-5.95f, -2.35f, 0);
                var tv = GameObject.Find("Television and stand");
                if (tv != null)
                    tv.transform.localPosition = new Vector3(-3.65f, -2.35f, 0);
                Physics2D.SyncTransforms();
                Bounds sofaBox = sofa.GetComponent<Collider2D>().bounds;
                Bounds passage = GameObject.Find("Door to Bedroom").GetComponent<Collider2D>().bounds;
                passage.Expand(new Vector3(.65f, .65f, 0));
                if (sofaBox.Intersects(passage))
                    throw new Exception("Sofa still intrudes into bedroom doorway clearance.");
                Debug.Log("SOFA_CLEARANCE_OK sofa scale=.65, doorway plus player clearance unobstructed");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        EditorBuildSettings.scenes = buildScenes.Select(s => new EditorBuildSettingsScene(paths.TryGetValue(s.path, out string mapped) ? mapped : s.path, s.enabled)).ToArray();
        foreach (string file in new[] { "Assets/Editor/Validation/VerifyApartmentRuntime.cs", "Assets/Editor/Maintenance/BuildApartmentExtensions.cs", "Assets/Scripts/Time/LoopManager.cs" })
        {
            if (!File.Exists(file))
                continue;
            string content = File.ReadAllText(file);
            foreach (var p in paths)
                content = content.Replace(p.Key, p.Value);
            content = content.Replace("\"Apartment\"", "\"LivingRoom\"").Replace("\"Apartment.unity\"", "\"LivingRoom.unity\"");
            if (file.EndsWith("BuildApartmentExtensions.cs"))
                content = content.Replace("const string Scenes = \"Assets/Scenes/\";", "const string Scenes = \"Assets/Scenes/Apartment/\";");
            File.WriteAllText(file, content);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(Folder + "/LivingRoom.unity");
        var folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Folder);
        Selection.activeObject = folderAsset;
        EditorGUIUtility.PingObject(folderAsset);
        File.WriteAllText("../apartment-folder-validation.txt", "Five room scenes moved under Assets/Scenes/Apartment with their GUIDs preserved. Door paths and Build Settings updated. Sofa reduced to 65% and moved clear of bedroom entrance.\n");
        Debug.Log("APARTMENT_FOLDER_ORGANIZED_OK");
    }
}
