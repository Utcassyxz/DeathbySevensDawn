// 职责：场景维护工具：PolishCorridor402；仅显式运行，日常开发无需重建已有资源。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class PolishCorridor402
{
    static Sprite square; static Material material; static Transform root;
    [MenuItem("Prototype/Set corridor rooms 401 402 403")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play first.");
        EditorSceneManager.SaveOpenScenes();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Apartment/Corridor.unity");
        square = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Prototype/Square.png").OfType<Sprite>().First();
        material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Prototype/PrototypeUnlit.mat");
        var old = GameObject.Find("Corridor room identity");
        if (old != null)
            UnityEngine.Object.DestroyImmediate(old);
        root = new GameObject("Corridor room identity").transform;
        foreach (var item in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true)))
            if (item.name == "Door mat" && Mathf.Abs(item.transform.position.x) < .1f)
                item.sortingOrder = -2;
        Plaque(401, -4.7f, 1.69f, false);
        Plaque(402, 0, -1.52f, true);
        Plaque(403, 4.7f, 1.69f, false);
        foreach (float x in new[] { -4.7f, 4.7f })
        {
            Box("Door side trim", x - .63f, 1.65f, .045f, .55f, new Color(.58f, .50f, .36f), 5, root);
            Box("Door side trim", x + .63f, 1.65f, .045f, .55f, new Color(.58f, .50f, .36f), 5, root);
            Box("Door threshold", x, 1.375f, 1.30f, .05f, new Color(.54f, .49f, .38f), 5, root);
        }
        Box("South skirting left", -3.775f, -1.83f, 6.05f, .055f, new Color(.23f, .22f, .21f), -1, root);
        Box("South skirting right", 3.775f, -1.83f, 6.05f, .055f, new Color(.23f, .22f, .21f), -1, root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        PolishApartmentDetails.ValidatePaths();
        EditorSceneManager.OpenScene("Assets/Scenes/Apartment/Corridor.unity");
        Debug.Log("CORRIDOR_401_402_403_OK: north neighbors and south apartment, decoration has no colliders.");
    }
    public static void RunAndVerify()
    {
        Run();
        VerifyFeedbackFlow.Run();
    }
    static void Plaque(int number, float x, float y, bool own)
    {
        var plaque = new GameObject("Room " + number + " plaque").transform;
        plaque.SetParent(root);
        plaque.position = new Vector3(x, y, 0);
        int layer = own ? -1 : 8;
        Box("Brass border", x, y, .70f, .27f, new Color(.60f, .53f, .35f), layer, plaque);
        Box("Room plate", x, y, .64f, .22f, own ? new Color(.17f, .32f, .30f) : new Color(.23f, .23f, .20f), layer + 1, plaque);
        string[] glyphs = { "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001" };
        string label = number.ToString();
        for (int digit = 0; digit < 3; digit++)
        {
            string glyph = glyphs[label[digit] - '0'];
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 3; col++)
                    if (glyph[row * 3 + col] == '1')
                        Box("Digit " + label[digit], x + (digit - 1) * .15f + (col - 1) * .035f, y + (2 - row) * .035f, .031f, .031f, new Color(.96f, .90f, .69f), layer + 2, plaque);
        }
    }
    static void Box(string name, float x, float y, float width, float height, Color color, int order, Transform parent)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent);
        g.transform.position = new Vector3(x, y, 0);
        g.transform.localScale = new Vector3(width / square.bounds.size.x, height / square.bounds.size.y, 1);
        var renderer = g.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.sharedMaterial = material;
        renderer.color = color;
        renderer.sortingOrder = order;
    }
}
