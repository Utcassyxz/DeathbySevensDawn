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
            return Indoors ? Color.Lerp(ambient, new Color(.78f, .76f, .72f), Mathf.Clamp01(1 - ambient.maxColorComponent) * .35f) : ambient;
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
                // Only light with a visible source: the aquarium itself. No unowned lamp pools.
                var aquarium = GameObject.Find("Aquarium and cabinet");
                if (aquarium != null)
                {
                    tank = aquarium.GetComponent<SpriteRenderer>();
                    var b = tank.bounds;
                    Add(new Vector2(b.center.x, b.center.y + b.size.y * .18f), new Vector2(b.size.x * 1.12f, 1.10f), new Color(.18f, .62f, .68f), true);
                    Add(new Vector2(b.center.x, b.min.y + b.size.y * .54f), new Vector2(b.size.x * .88f, .70f), new Color(.20f, .48f, .52f), true);
                }
            }
            // Bedroom and bathroom have no visible lamp fixture in the current scenes;
            // keep their ambience in TimeAtmosphere rather than inventing a light source.
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
                c.a = l.aquarium ? .10f + Mathf.PerlinNoise(Time.unscaledTime * .13f, 3.7f) * .008f : strength;
                l.renderer.color = c;
            }

            if (tank != null && clock.Period == TimePeriod.Night)
                tank.color = Color.Lerp(tank.color, new Color(.88f, 1f, .98f), Time.unscaledDeltaTime * 2);
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
