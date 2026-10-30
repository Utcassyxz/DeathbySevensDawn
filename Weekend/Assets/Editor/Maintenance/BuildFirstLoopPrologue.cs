// 职责：场景维护工具：BuildFirstLoopPrologue；仅显式运行，日常开发无需重建已有资源。
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DeathBySeventhsDawn.Interaction;
public static class BuildFirstLoopPrologue
{
    [MenuItem("Prototype/Prepare 401 first-loop prologue")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play first.");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Story"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Story");
        const string prefabPath = "Assets/Prefabs/Story/Neighbor401.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            var root = new GameObject("Neighbor401");
            var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Prototype/Square.png").OfType<Sprite>().First();
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Prototype/PrototypeUnlit.mat");
            Add(root, "Coat", 0, 0, .45f, .47f, new Color(.34f, .28f, .21f), sprite, material, 12);
            Add(root, "Head", 0, .33f, .27f, .24f, new Color(.68f, .60f, .42f), sprite, material, 13);
            Add(root, "Hood", 0, .46f, .34f, .13f, new Color(.19f, .22f, .19f), sprite, material, 14);
            Add(root, "Hair", -.13f, .33f, .08f, .28f, new Color(.20f, .22f, .18f), sprite, material, 14);
            Add(root, "Left boot", -.12f, -.31f, .17f, .18f, new Color(.17f, .18f, .16f), sprite, material, 12);
            Add(root, "Right boot", .12f, -.31f, .17f, .18f, new Color(.17f, .18f, .16f), sprite, material, 12);
            Add(root, "Flyer in hand", .29f, .06f, .17f, .23f, new Color(.88f, .84f, .66f), sprite, material, 15);
            prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }
        const string path = "Assets/Resources/FirstLoopPrologue.asset";
        var content = AssetDatabase.LoadAssetAtPath<FirstLoopPrologueContent>(path);
        if (content == null)
        {
            content = ScriptableObject.CreateInstance<FirstLoopPrologueContent>();
            AssetDatabase.CreateAsset(content, path);
        }
        content.neighborPrefab = prefab;
        EditorUtility.SetDirty(content);
        AssetDatabase.SaveAssets();
        Debug.Log("FIRST_LOOP_PROLOGUE_READY: editable story resource and Neighbor401 prefab.");
    }
    public static void RunAndVerify()
    {
        Run();
        VerifyFirstLoopPrologue.Run();
    }
    static void Add(GameObject root, string name, float x, float y, float width, float height, Color color, Sprite sprite, Material material, int order)
    {
        var part = new GameObject(name);
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = new Vector3(x, y, 0);
        part.transform.localScale = new Vector3(width / sprite.bounds.size.x, height / sprite.bounds.size.y, 1);
        var renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.color = color;
        renderer.sortingOrder = order;
    }
}
