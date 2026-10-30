// 职责：场景维护工具：PolishApartmentDetails；仅显式运行，日常开发无需重建已有资源。
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DeathBySeventhsDawn.World;

public static class PolishApartmentDetails
{
    const string Root = "Assets/Scenes/Apartment/";
    static readonly string[] Rooms = { "LivingRoom", "Bedroom", "Bathroom", "Balcony", "Corridor" };
    static Sprite square; static Material mat; static Transform details;
    static readonly Color Edge = new Color(.23f, .22f, .21f), Highlight = new Color(.51f, .48f, .40f);
    [MenuItem("Prototype/Polish approved apartment details")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play first.");
        EditorSceneManager.SaveOpenScenes();
        square = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Prototype/Square.png").OfType<Sprite>().First();
        mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Prototype/PrototypeUnlit.mat");
        foreach (string room in Rooms)
        {
            var scene = EditorSceneManager.OpenScene(Root + room + ".unity");
            var old = GameObject.Find("Room edge finishing");
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old);
            details = new GameObject("Room edge finishing").transform;
            Camera.main.orthographicSize = 5;
            var player = GameObject.FindWithTag("Player");
            player.transform.localScale = new Vector3(.65f, .65f, 1);
            if (room == "LivingRoom")
            {
                Box("North skirting", 0, 2.77f, 13.6f, .08f, Edge);
                Box("North trim highlight", 0, 2.82f, 13.6f, .025f, Highlight);
                Vertical(-6.82f, -3.8f, 2.77f, 0, .80f);
                Vertical(6.82f, -3.8f, 2.77f, -.35f, .80f);
                Horizontal(-3.82f, -6.8f, 6.8f, 3.7f, 1.38f);
            }
            if (room == "Bedroom")
            {
                var window = GameObject.Find("Large story window");
                window.transform.localScale = new Vector3(.78f, .78f, 1);
                window.transform.localPosition = new Vector3(-.2f, 2.42f, 0);
                Box("North skirting", 0, 1.80f, 7.6f, .08f, Edge);
                Box("North trim highlight", 0, 1.85f, 7.6f, .025f, Highlight);
                Vertical(-3.82f, -2.8f, 1.80f, 99, 0);
                Vertical(3.82f, -2.8f, 1.80f, -.1f, .80f);
                Horizontal(-2.82f, -3.8f, 3.8f, 99, 0);
            }
            if (room == "Bathroom")
            {
                Box("Tile wall joint", 0, 1.50f, 5.6f, .09f, new Color(.23f, .33f, .32f));
                Box("Tile wall highlight", 0, 1.55f, 5.6f, .025f, new Color(.5f, .6f, .57f));
                Vertical(-2.82f, -2.30f, 1.5f, -.5f, .80f);
                Vertical(2.82f, -2.30f, 1.5f, 99, 0);
                Horizontal(-2.32f, -2.8f, 2.8f, 99, 0);
            }
            if (room == "Corridor")
            {
                Box("North wall face", 0, 1.72f, 13.7f, .45f, new Color(.30f, .29f, .27f), -3);
                Box("North skirting", 0, 1.48f, 13.7f, .08f, Edge);
                Box("North trim highlight", 0, 1.53f, 13.7f, .025f, Highlight);
                Vertical(-6.82f, -1.8f, 1.5f, 99, 0);
                Vertical(6.82f, -1.8f, 1.5f, 99, 0);
                foreach (var g in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "Wall lamp"))
                    g.position = new Vector3(g.position.x, 2.02f, 0);
                foreach (float x in new[] { -4.7f, 0, 4.7f })
                {
                    Box("Door left jamb", x - .68f, x == 0 ? -1.75f : 1.65f, .055f, x == 0 ? .25f : .56f, Highlight, 3);
                    Box("Door right jamb", x + .68f, x == 0 ? -1.75f : 1.65f, .055f, x == 0 ? .25f : .56f, Highlight, 3);
                }
                Horizontal(-1.83f, -6.8f, 6.8f, 0, .75f);
            }
            if (room == "Balcony")
            {
                var leaf = GameObject.Find("Leaves");
                if (leaf != null)
                    leaf.SetActive(false);
                var pot = GameObject.Find("Balcony planter");
                pot.transform.localPosition = new Vector3(-3.8f, -.95f, 0);
                pot.transform.localScale = new Vector3(1, .58f, 1);
                Box("Planter soil", -3.8f, -.78f, .93f, .16f, new Color(.19f, .17f, .13f), 3);
                Box("Foliage left", -4.15f, -.54f, .36f, .40f, new Color(.22f, .34f, .23f), 4);
                Box("Foliage center", -3.80f, -.43f, .48f, .54f, new Color(.30f, .42f, .26f), 5);
                Box("Foliage right", -3.43f, -.56f, .36f, .36f, new Color(.25f, .38f, .24f), 4);
                Box("Railing top cap", 0, -1.65f, 10.12f, .055f, new Color(.38f, .44f, .43f), 6);
                Box("North threshold joint", 0, 1.78f, 1.4f, .055f, Highlight, 3);
            }
            foreach (var door in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RoomDoor2D>(true)))
            {
                var p = door.transform.position;
                var c = door.GetComponent<BoxCollider2D>();
                if (c.size.y > c.size.x)
                {
                    Box("Door frame upper cap", p.x, p.y + c.size.y * .5f + .08f, .22f, .065f, Highlight, 4);
                    Box("Door frame lower cap", p.x, p.y - c.size.y * .5f - .08f, .22f, .065f, Highlight, 4);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        ValidatePaths();
        EditorSceneManager.OpenScene(Root + "LivingRoom.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("APARTMENT_POLISH_OK");
    }
    static void Box(string name, float x, float y, float w, float h, Color color, int order = -1)
    {
        var g = new GameObject(name);
        g.transform.SetParent(details);
        g.transform.position = new Vector3(x, y, 0);
        g.transform.localScale = new Vector3(w / square.bounds.size.x, h / square.bounds.size.y, 1);
        var r = g.AddComponent<SpriteRenderer>();
        r.sprite = square;
        r.sharedMaterial = mat;
        r.color = color;
        r.sortingOrder = order;
    }
    static void Vertical(float x, float bottom, float top, float gap, float half)
    {
        if (gap == 99)
        {
            Box("Side skirting", x, (bottom + top) * .5f, .055f, top - bottom, Edge);
            return;
        }
        float low = gap - half, high = gap + half;
        if (low > bottom)
            Box("Side skirting lower", x, (bottom + low) * .5f, .055f, low - bottom, Edge);
        if (high < top)
            Box("Side skirting upper", x, (top + high) * .5f, .055f, top - high, Edge);
    }
    static void Horizontal(float y, float left, float right, float gap, float half)
    {
        if (gap == 99)
        {
            Box("South skirting", (left + right) * .5f, y, right - left, .055f, Edge);
            return;
        }
        if (gap - half > left)
            Box("South skirting left", (left + gap - half) * .5f, y, gap - half - left, .055f, Edge);
        if (gap + half < right)
            Box("South skirting right", (right + gap + half) * .5f, y, right - gap - half, .055f, Edge);
    }
    [MenuItem("Prototype/Check all apartment walking routes")]
    public static void ValidatePaths()
    {
        var output = new List<string>();
        int routes = 0;
        foreach (string room in Rooms)
        {
            var scene = EditorSceneManager.OpenScene(Root + room + ".unity");
            Physics2D.SyncTransforms();
            var player = GameObject.FindWithTag("Player");
            var playerCollider = player.GetComponent<Collider2D>();
            var doors = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RoomDoor2D>()).ToArray();
            float halfW = room == "Bedroom" ? 4 : room == "Bathroom" ? 3 : room == "Balcony" ? 5 : 7;
            float halfH = room == "Bedroom" ? 3 : room == "Bathroom" ? 2.5f : room == "Balcony" || room == "Corridor" ? 2 : 4;
            var start = new Vector2Int(Mathf.RoundToInt(player.transform.position.x * 10), Mathf.RoundToInt(player.transform.position.y * 10));
            var queue = new Queue<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            queue.Enqueue(start);
            seen.Add(start);
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            var reached = new HashSet<RoomDoor2D>();
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var point = new Vector2(cell.x * .1f, cell.y * .1f);
                foreach (var door in doors)
                {
                    Bounds b = door.GetComponent<Collider2D>().bounds;
                    b.Expand(new Vector3(.62f, .62f, 0));
                    if (b.Contains(new Vector3(point.x, point.y, 0)))
                        reached.Add(door);
                }
                foreach (var direction in directions)
                {
                    var next = cell + direction;
                    if (seen.Contains(next))
                        continue;
                    Vector2 p = new Vector2(next.x * .1f, next.y * .1f);
                    if (Mathf.Abs(p.x) > halfW || Mathf.Abs(p.y) > halfH)
                        continue;
                    bool blocked = Physics2D.OverlapBoxAll(p, new Vector2(.66f, .66f), 0).Any(c => c != playerCollider && !c.isTrigger);
                    if (blocked)
                        continue;
                    seen.Add(next);
                    queue.Enqueue(next);
                }
            }
            foreach (var door in doors)
            {
                if (!reached.Contains(door))
                    throw new Exception(room + " inaccessible door: " + door.name);
                output.Add(room + " -> " + Path.GetFileNameWithoutExtension(door.destinationScene) + ": reachable with player clearance");
                routes++;
            }
            output.Add(room + ": camera size=" + Camera.main.orthographicSize + ", reachable cells=" + seen.Count);
        }
        output.Add("PASS: " + routes + " walking routes. No decorative finishing adds colliders.");
        File.WriteAllText("../apartment-polish-validation.txt", string.Join("\n", output));
        Debug.Log("APARTMENT_WALKING_ROUTES_OK " + routes);
    }
}
