// 职责：室内夜间灯具与鱼缸的局部光晕；不改变户外夜色。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Progression
{
    [DefaultExecutionOrder(1200)]
    public sealed class InteriorLighting : MonoBehaviour
    {
        struct Lamp
        {
            public SpriteRenderer renderer;
            public Color color;
            public bool aquarium;
        }

        readonly List<Lamp> lamps = new List<Lamp>();
        GameObject root;
        Texture2D glow;
        Sprite glowSprite;
        Material spriteMaterial;
        SpriteRenderer tank;
        public static bool Indoors => SceneManager.GetActiveScene().name == "LivingRoom" || SceneManager.GetActiveScene().name == "Bedroom" || SceneManager.GetActiveScene().name == "Bathroom";
        public int LightCount => lamps.Count;

        public static Color LitTint(Color ambient)
        {
            // Electric lights preserve a readable warm room, while the exterior stays blue-black.
            return Indoors ? Color.Lerp(ambient, new Color(.82f, .74f, .64f), Mathf.Clamp01(1 - ambient.maxColorComponent) * .78f) : ambient;
        }

        void Awake()
        {
            glow = new Texture2D(128, 128, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float r = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 63.5f;
                    float a = Mathf.Pow(Mathf.Clamp01(1 - r * r), 3);
                    glow.SetPixel(x, y, new Color(1, 1, 1, a));
                }

            glow.Apply(false);
            glowSprite = Sprite.Create(glow, new Rect(0, 0, 128, 128), Vector2.one * .5f, 128);
            // Use the built-in sprite material. The previous additive custom material
            // was not rendered consistently by the 2D renderer and produced white blobs
            // where it did render. Alpha blending keeps the pools local and warm.
            spriteMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += Loaded;
        }

        void Start()
        {
            Loaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= Loaded;
            Clear();
        }

        void Clear()
        {
            if (root != null)
                Destroy(root);
            lamps.Clear();
            tank = null;
        }

        void Loaded(Scene scene, LoadSceneMode mode)
        {
            Clear();
            if (!Indoors)
                return;
            root = new GameObject("Apartment lighting");
            SceneManager.MoveGameObjectToScene(root, scene);
            if (scene.name == "LivingRoom")
            {
                Add(new Vector2(-4.05f, -1.9f), new Vector2(3.5f, 2.5f), new Color(1, .71f, .36f), false);
                Add(new Vector2(-2.5f, 2.1f), new Vector2(3.2f, 2.0f), new Color(1, .83f, .57f), false);
                var aquarium = GameObject.Find("Aquarium and cabinet");
                if (aquarium != null)
                {
                    tank = aquarium.GetComponent<SpriteRenderer>();
                    var b = tank.bounds;
                    Add(new Vector2(b.center.x, b.min.y + .12f), new Vector2(b.size.x * 1.6f, 1.45f), new Color(.24f, .65f, .63f), true);
                    Add(new Vector2(b.center.x, b.center.y + b.size.y * .14f), new Vector2(b.size.x * .95f, b.size.y * .40f), new Color(.25f, .58f, .60f), true);
                }
            }
            else if (scene.name == "Bedroom")
            {
                Add(new Vector2(-2.5f, 1.65f), new Vector2(2.8f, 2.3f), new Color(1, .72f, .40f), false);
                Add(new Vector2(2.6f, -.9f), new Vector2(2.6f, 1.9f), new Color(1, .79f, .48f), false);
            }
            else
                Add(Vector2.zero, new Vector2(3.5f, 3.5f), new Color(.76f, .87f, 1), false);
        }

        void Add(Vector2 position, Vector2 size, Color color, bool aquarium)
        {
            var g = new GameObject(aquarium ? "Glass spill" : "Warm lamp pool");
            g.transform.SetParent(root.transform, false);
            g.transform.position = position;
            g.transform.localScale = size;
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = glowSprite;
            r.sharedMaterial = spriteMaterial;
            r.sortingOrder = 18;
            var initial = color;
            initial.a = 0;
            r.color = initial;
            lamps.Add(new Lamp { renderer = r, color = color, aquarium = aquarium });
        }

        void LateUpdate()
        {
            var clock = TimeManager.Instance;
            if (clock == null)
                return;
            float strength = clock.Period == TimePeriod.Night ? .22f : clock.Period == TimePeriod.Evening ? .12f : .035f;
            foreach (var l in lamps)
            {
                if (l.renderer == null)
                    continue;
                var c = l.color;
                // Very slow, tiny water variation; never flash the entire cabinet green.
                c.a = l.aquarium ? .11f + Mathf.PerlinNoise(Time.unscaledTime * .13f, 3.7f) * .015f : strength;
                l.renderer.color = c;
            }

            if (tank != null && clock.Period == TimePeriod.Night)
                tank.color = Color.Lerp(tank.color, new Color(.68f, .80f, .78f), Time.unscaledDeltaTime * 2);
        }

        void OnDestroy()
        {
            Clear();
            if (spriteMaterial != null)
                Destroy(spriteMaterial);
            if (glowSprite != null)
                Destroy(glowSprite);
            if (glow != null)
                Destroy(glow);
        }
    }
}
