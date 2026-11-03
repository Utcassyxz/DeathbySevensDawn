// Author the shared landing and two opposite flights as a reviewable prefab + independent scene.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Interaction;

[InitializeOnLoad]
public static class BuildStairwell
{
    const string ScenePath = "Assets/Apartment/Stairwell.unity";
    const string ArtPath = "Assets/Art/Apartment/Stairwell";
    static Sprite square;
    static Material material;
    static Transform model;
    static BuildStairwell()
    {
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("../stairwell-build-request.txt"))
            return;
        File.Delete("../stairwell-build-request.txt");
        try
        {
            Build();
            File.WriteAllText("../stairwell-build-report.txt", "PASS: Stairwell scene and two-flight landing prefab saved; scene registered; editor setup restored.");
        }
        catch (Exception e)
        {
            File.WriteAllText("../stairwell-build-report.txt", e.ToString());
            Debug.LogException(e);
        }
    }

    [MenuItem("Prototype/Build dedicated stairwell")]
    public static void Build()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
            throw new Exception("Save current scene and exit Play first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Directory.CreateDirectory(ArtPath);
            AssetDatabase.Refresh();
            string pixelPath = ArtPath + "/StairPixel.asset";
            square = AssetDatabase.LoadAllAssetsAtPath(pixelPath).OfType<Sprite>().FirstOrDefault();
            if (square == null)
            {
                var pixel = new Texture2D(1, 1)
                {
                    name = "Stair pixel",
                    filterMode = FilterMode.Point
                };
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
                AssetDatabase.CreateAsset(pixel, pixelPath);
                square = Sprite.Create(pixel, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
                square.name = "Stair square";
                AssetDatabase.AddObjectToAsset(square, pixelPath);
            }

            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Prototype/PrototypeUnlit.mat");
            EditorSceneManager.OpenScene("Assets/Apartment/LivingRoom.unity");
            var player = UnityEngine.Object.Instantiate(GameObject.FindWithTag("Player"));
            var camera = UnityEngine.Object.Instantiate(Camera.main.gameObject);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.MoveGameObjectToScene(player, scene);
            SceneManager.MoveGameObjectToScene(camera, scene);
            SceneManager.SetActiveScene(scene);
            player.name = "Player";
            player.transform.position = RooftopAccess.LandingDoor + Vector2.left * .85f;
            player.SetActive(true);
            camera.name = "Main Camera";
            camera.transform.position = new Vector3(0, 0, -10);
            var cam = camera.GetComponent<Camera>();
            cam.orthographicSize = 2.85f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            model = new GameObject("Two-flight stairwell landing").transform;
            var stone = new Color(.43f, .45f, .42f);
            var dark = new Color(.10f, .15f, .17f);
            var rail = new Color(.29f, .36f, .36f);
            var cap = new Color(.64f, .66f, .57f);
            Block("Landing floor", 0, 0, 10.6f, 6.0f, new Color(.34f, .38f, .36f), -5);
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 17; x++)
                {
                    float px = -5 + x * .625f, py = -2.7f + y * .6f;
                    Block("Stone tile", px, py, .60f, .57f, (x + y) % 4 == 0 ? stone : new Color(.38f, .41f, .38f), -4);
                }

            Block("North wall face", 0, 3.03f, 10.9f, .70f, new Color(.27f, .34f, .32f), 1);
            Block("North wall top", 0, 3.43f, 10.9f, .12f, new Color(.48f, .53f, .46f), 2);
            Block("North skirting", 0, 2.68f, 10.7f, .10f, dark, 2);
            foreach (float x in new[]
            {
                -5.35f,
                5.35f
            }

            )
            {
                Block("Side wall", x, 0, .18f, 6.15f, dark, 2);
                Solid("Side boundary", x, 0, .18f, 6.2f);
            }

            Solid("North boundary", 0, 2.95f, 10.7f, .2f);
            Solid("South boundary", 0, -3, 10.7f, .2f);
            // Up flight: bright horizontal treads rising toward the northern half landing.
            Block("Upper flight well", -2.2f, 1.25f, 2.36f, 2.85f, dark, 0);
            for (int i = 0; i < 9; i++)
            {
                float y = .02f + i * .285f;
                Block("Up tread", -2.2f, y, 1.94f, .22f, stone + Color.white * (i * .009f), 3);
                Block("Up riser", -2.2f, y - .12f, 1.94f, .07f, new Color(.21f, .27f, .27f), 4);
                Block("Up nosing", -2.2f, y + .10f, 1.98f, .035f, cap, 5);
            }

            Block("Upper half landing", -2.2f, 2.58f, 2.02f, .27f, new Color(.51f, .53f, .46f), 5);
            Solid("Upper stair approach stop", -2.2f, 1.40f, 2.24f, 2.8f);
            // Opposed down flight: descending treads disappear into the lower well.
            Block("Lower flight well", 1.65f, -1.22f, 2.36f, 2.8f, new Color(.075f, .11f, .14f), 0);
            for (int i = 0; i < 9; i++)
            {
                float y = .03f - i * .285f;
                float k = 1 - i * .055f;
                Block("Down tread", 1.65f, y, 1.94f, .23f, new Color(.40f * k, .46f * k, .46f * k), 3);
                Block("Down riser", 1.65f, y - .13f, 1.94f, .065f, new Color(.12f * k, .18f * k, .20f * k), 4);
                Block("Down nosing", 1.65f, y + .10f, 1.96f, .03f, new Color(.60f * k, .64f * k, .60f * k), 5);
            }

            Solid("Lower stair approach stop", 1.65f, -1.35f, 2.24f, 2.7f);
            foreach (var flight in new[]
            {
                new Vector2(-2.2f, 1.15f),
                new Vector2(1.65f, -1.25f)
            }

            )
                foreach (float side in new[]
                {
                    -1.12f,
                    1.12f
                }

                )
                {
                    Block("Rail side shadow", flight.x + side + .05f, flight.y, .10f, 2.86f, dark, 6);
                    Block("Rail top", flight.x + side, flight.y + .06f, .08f, 2.88f, rail, 7);
                    Block("Rail highlight", flight.x + side - .025f, flight.y + .07f, .025f, 2.88f, cap, 8);
                    for (int p = 0; p < 5; p++)
                        Block("Baluster cap", flight.x + side, flight.y - 1.1f + p * .54f, .14f, .09f, new Color(.46f, .53f, .48f), 8);
                }

            // Clear connecting landing around both flights and a recessed fire door embedded in the north wall.
            Block("Door recess", -.3f, -2.7f, 1.3f, 0.6f, dark, 3);
            Block("Door steel frame", -.3f, -2.68f, 1.16f, 0.5f, new Color(.54f, .58f, .51f), 4);
            Block("Fire door", -.3f, -2.68f, 0.98f, 0.34f, new Color(.22f, .32f, .29f), 5);
            Block("Door push bar", -.3f, -2.57f, 0.7f, 0.04f, cap, 6);
            Block("Door threshold", -.3f, -2.36f, 1.2f, 0.06f, new Color(.62f, .63f, .54f), 6);
            Block("Floor number plate", 3.88f, 3.18f, .82f, .52f, dark, 4);
            Block("Door direction plate", -.3f, -2.15f, 1.62f, 0.25f, dark, 4);
            Solid("Door leaf collision", -.3f, -2.91f, 1.05f, 0.28f);
            Block("Fire pipe", -4.55f, 1.2f, .07f, 2.9f, new Color(.40f, .22f, .19f), 4);
            Block("Fire pipe elbow", -4.28f, 2.60f, .60f, .07f, new Color(.40f, .22f, .19f), 4);
            model.localScale = Vector3.one * .68f;
            foreach (var r in model.GetComponentsInChildren<SpriteRenderer>())
            {
                var c = r.color;
                r.color = new Color(c.r * .64f, c.g * .72f, c.b * .76f, c.a);
            }

            string prefab = "Assets/Prefabs/Extensions/StairwellLanding.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(model.gameObject, prefab);
            UnityEngine.Object.DestroyImmediate(model.gameObject);
            PrefabUtility.InstantiatePrefab(saved, scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = EditorBuildSettings.scenes.ToList();
            if (!list.Any(s => s.path == ScenePath))
                list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (Application.isBatchMode && setup.Any(s => string.IsNullOrEmpty(s.path)))
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    static void Block(string name, float x, float y, float w, float h, Color c, int order)
    {
        var g = new GameObject(name);
        g.transform.SetParent(model, false);
        g.transform.localPosition = new Vector2(x, y);
        g.transform.localScale = new Vector3(w, h, 1);
        var r = g.AddComponent<SpriteRenderer>();
        r.sprite = square;
        r.sharedMaterial = material;
        r.color = c;
        r.sortingOrder = order;
    }

    static void Solid(string name, float x, float y, float w, float h)
    {
        var g = new GameObject(name);
        g.transform.SetParent(model, false);
        g.transform.localPosition = new Vector2(x, y);
        g.AddComponent<BoxCollider2D>().size = new Vector2(w, h);
    }
}
