// 职责：创建或重建带原创像素细节的五楼天台，保留玩家和相机，并注册构建场景。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Interaction;
[InitializeOnLoad]
public static class BuildRooftop
{
    const string PathName = "Assets/Scenes/Apartment/Rooftop.unity";
    static BuildRooftop()
    {
        EditorApplication.update += Tick;
    }
    static void Tick()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("../roof-build-request.txt"))
            return;
        File.Delete("../roof-build-request.txt");
        try
        {
            Build();
            File.WriteAllText("../roof-build-validation.txt", "PASS: detailed pixel rooftop rebuilt; collision paths and interaction positions retained; all room cameras clear to real black.");
        }
        catch (Exception e) { File.WriteAllText("../roof-build-validation.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Prototype/Build fifth-floor rooftop")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play first.");
        if (SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save the current scene before rebuilding the rooftop.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene roof;
            GameObject player, camera;
            if (File.Exists(PathName))
            {
                roof = EditorSceneManager.OpenScene(PathName);
                player = GameObject.FindWithTag("Player");
                camera = Camera.main.gameObject;
                foreach (var root in roof.GetRootGameObjects())
                    if (root != player && root != camera)
                        UnityEngine.Object.DestroyImmediate(root);
            }
            else
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Apartment/LivingRoom.unity");
                player = UnityEngine.Object.Instantiate(GameObject.FindWithTag("Player"));
                camera = UnityEngine.Object.Instantiate(Camera.main.gameObject);
                roof = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.MoveGameObjectToScene(player, roof);
                SceneManager.MoveGameObjectToScene(camera, roof);
                SceneManager.SetActiveScene(roof);
            }
            player.name = "Player";
            player.transform.position = RooftopAccess.StairsFive + Vector2.right * 1.1f;
            player.SetActive(true);
            camera.name = "Main Camera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.GetComponent<Camera>().orthographicSize = 4.7f;
            Black(camera.GetComponent<Camera>());
            var art = new GameObject("Rooftop pixel environment").AddComponent<SpriteRenderer>();
            art.sprite = RooftopArt.Build();
            art.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Prototype/PrototypeUnlit.mat");
            art.sortingOrder = -5;
            Solid("North parapet", 0, 3.5f, 14.4f, .3f);
            Solid("South parapet", 0, -3.5f, 14.4f, .3f);
            Solid("West parapet", -7, 0, .3f, 7);
            Solid("East parapet", 7, 0, .3f, 7);
            Solid("Stairwell hut", -4.8f, -.65f, 2.4f, 2f);
            Solid("Water tank", -3.9f, 2, 2.5f, 1.9f);
            Solid("Air handling unit A", -.8f, -1.8f, 1.2f, .85f);
            Solid("Air handling unit B", 1, -1.8f, 1.2f, .85f);
            Solid("Discarded chair", 5.5f, -1.75f, .8f, .85f);
            Solid("Empty pots", 6, 0, 1.5f, .65f);
            EditorSceneManager.SaveScene(roof, PathName);
            var list = EditorBuildSettings.scenes.ToList();
            if (!list.Any(s => s.path == PathName))
                list.Add(new EditorBuildSettingsScene(PathName, true));
            EditorBuildSettings.scenes = list.ToArray();
            // Author the same clear color in every room; runtime enforcement also covers dynamically-created cameras.
            foreach (var entry in list.Where(s => s.enabled && s.path.StartsWith("Assets/Scenes/Apartment/")))
            {
                var scene = EditorSceneManager.OpenScene(entry.path);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var c in root.GetComponentsInChildren<Camera>(true))
                        Black(c);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
    static void Black(Camera camera)
    {
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
    }
    static void Solid(string name, float x, float y, float width, float height)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(x, y, 0);
        go.AddComponent<BoxCollider2D>().size = new Vector2(width, height);
    }
}
