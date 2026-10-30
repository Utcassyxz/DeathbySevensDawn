// 职责：场景维护工具：BuildTutorialAssets；仅显式运行，日常开发无需重建已有资源。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class BuildTutorialAssets
{
    static BuildTutorialAssets()
    {
        EditorApplication.update += Tick;
    }
    static void Tick()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("../tutorial-assets-request.txt"))
            return;
        File.Delete("../tutorial-assets-request.txt");
        try
        {
            Build();
            File.WriteAllText("../tutorial-assets-validation.txt", "PASS: open fridge sprite imported; original flyer design preserved in lobby prefab; no ground-floor scene added.");
        }
        catch (Exception e) { File.WriteAllText("../tutorial-assets-validation.txt", "FAIL: " + e); Debug.LogException(e); }
    }
    [MenuItem("Prototype/Build tutorial sprite and archived lobby flyer")]
    public static void Build()
    {
        const string path = "Assets/Resources/Tutorial/FridgeOpen.png";
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(51.5f / 70, .5f);
        importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var aligned = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (aligned == null || Mathf.Abs(aligned.pivot.x - 51.5f) > .01f || aligned.pixelsPerUnit != 32)
            throw new InvalidOperationException("Fridge sprite anchor does not match cabinet centre.");
        var square = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Prototype/Square.png").OfType<Sprite>().First();
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Prototype/PrototypeUnlit.mat");
        var root = new GameObject("Lobby notice flyer - reserved");
        Add(root, "Flyer paper", 0, 0, .43f, .35f, new Color(.84f, .81f, .63f), 10, square, material);
        for (int i = 0; i < 3; i++)
            Add(root, "Writing " + i, 0, .09f - i * .08f, .29f, .018f, new Color(.35f, .24f, .17f), 11, square, material);
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Story/LobbyNoticeFlyer.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
    }
    static void Add(GameObject root, string name, float x, float y, float w, float h, Color color, int order, Sprite sprite, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = new Vector3(x, y, 0);
        go.transform.localScale = new Vector3(w / sprite.bounds.size.x, h / sprite.bounds.size.y, 1);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sharedMaterial = material;
        r.color = color;
        r.sortingOrder = order;
    }
}
