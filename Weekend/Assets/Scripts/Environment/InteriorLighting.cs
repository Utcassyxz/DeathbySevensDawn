// 职责：可见室内灯具的柔和光晕；鱼缸不自发光，不改变户外夜色。
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
            public bool fixture;
        }

        readonly List<Lamp> lamps = new List<Lamp>();
        readonly List<GameObject> fixtures = new List<GameObject>();
        GameObject root;
        Texture2D glow;
        Sprite glowSprite;
        Texture2D solid;
        Sprite solidSprite;
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
            solid = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            solid.SetPixel(0, 0, Color.white);
            solid.Apply(false);
            solidSprite = Sprite.Create(solid, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
        // Keep the renderer-provided 2D material, as CorridorDetail does.
        // A legacy Sprites/Default material can lose the per-renderer texture in URP:
        // its tight mesh then appears as a flat orange polygon instead of a gradient.
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
            fixtures.Clear();
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
                // Only the visible floor lamp emits light. The aquarium follows room ambient.
                var aquarium = GameObject.Find("Aquarium and cabinet");
                if (aquarium != null)
                {
                    tank = aquarium.GetComponent<SpriteRenderer>();
                    var b = tank.bounds;
                }

                var floorLamp = new Vector2(-4.35f, -2.18f);
                BuildFloorLamp(floorLamp);
                Add(floorLamp + new Vector2(0, .82f), new Vector2(3.2f, 2.6f), new Color(1f, .72f, .42f), false, true);
            }
            else if (scene.name == "Bedroom")
            {
                var tableLamp = new Vector2(-2.02f, 1.64f);
                BuildTableLamp(tableLamp);
                Add(tableLamp + new Vector2(0, .45f), new Vector2(2.6f, 2.1f), new Color(1f, .76f, .46f), false, true);
            }
            else if (scene.name == "Bathroom")
            {
                var mirrorLamp = new Vector2(2.08f, 1.62f);
                BuildMirrorLamp(mirrorLamp);
                Add(mirrorLamp + new Vector2(0, -.48f), new Vector2(2.0f, 1.7f), new Color(.66f, .82f, 1f), false, true);
            }
        }

        void Part(GameObject parent, string name, Vector2 local, Vector2 size, Color color, int order)
        {
            var p = new GameObject(name);
            p.transform.SetParent(parent.transform, false);
            p.transform.localPosition = local;
            p.transform.localScale = new Vector3(size.x, size.y, 1);
            var r = p.AddComponent<SpriteRenderer>();
            r.sprite = solidSprite;
            r.color = color;
            r.sortingOrder = order;
        }

        GameObject LampRoot(string name, Vector2 position)
        {
            var g = new GameObject(name);
            g.transform.SetParent(root.transform, false);
            g.transform.position = position;
            fixtures.Add(g);
            return g;
        }

        void BuildFloorLamp(Vector2 at)
        {
            var g = LampRoot("Floor lamp", at);
            Part(g, "lamp shade", new Vector2(0, 1.48f), new Vector2(.62f, .28f), new Color(.78f, .55f, .30f), 18);
            Part(g, "lamp shade lip", new Vector2(0, 1.34f), new Vector2(.48f, .05f), new Color(.98f, .78f, .45f), 19);
            Part(g, "lamp pole", new Vector2(0, .72f), new Vector2(.055f, 1.45f), new Color(.20f, .18f, .15f), 18);
            Part(g, "lamp base", new Vector2(0, .02f), new Vector2(.54f, .10f), new Color(.14f, .13f, .11f), 18);
        }

        void BuildTableLamp(Vector2 at)
        {
            var g = LampRoot("Bedside lamp", at);
            Part(g, "lamp base", new Vector2(0, 0), new Vector2(.34f, .09f), new Color(.22f, .19f, .15f), 18);
            Part(g, "lamp pole", new Vector2(0, .16f), new Vector2(.05f, .30f), new Color(.25f, .21f, .16f), 18);
            Part(g, "lamp shade", new Vector2(0, .39f), new Vector2(.46f, .24f), new Color(.86f, .61f, .34f), 19);
        }

        void BuildMirrorLamp(Vector2 at)
        {
            var g = LampRoot("Mirror light", at);
            Part(g, "light bar", new Vector2(0, 0), new Vector2(.92f, .08f), new Color(.24f, .25f, .23f), 18);
            Part(g, "left bulb", new Vector2(-.30f, -.07f), new Vector2(.12f, .12f), new Color(1f, .90f, .68f), 19);
            Part(g, "right bulb", new Vector2(.30f, -.07f), new Vector2(.12f, .12f), new Color(1f, .90f, .68f), 19);
        }

        void Add(Vector2 position, Vector2 size, Color color, bool aquarium, bool fixture = false)
        {
            var g = new GameObject(aquarium ? "Glass spill" : fixture ? "Owned lamp pool" : "Light pool");
            g.transform.SetParent(root.transform, false);
            g.transform.position = position;
            g.transform.localScale = size;
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = glowSprite;
            r.sortingOrder = 18;
            var initial = color;
            initial.a = 0;
            r.color = initial;
            lamps.Add(new Lamp { renderer = r, color = color, aquarium = aquarium, fixture = fixture });
        }

        void LateUpdate()
        {
            var clock = TimeManager.Instance;
            if (clock == null)
                return;
            float strength = clock.Period == TimePeriod.Night ? .22f : clock.Period == TimePeriod.Evening ? .12f : .035f;
            foreach (var fixture in fixtures)
                if (fixture != null)
                    fixture.SetActive(true);
            foreach (var l in lamps)
            {
                if (l.renderer == null)
                    continue;
                var c = l.color;
                // Very slow, tiny water variation; never flash the entire cabinet green.
                c.a = l.aquarium ? .10f + Mathf.PerlinNoise(Time.unscaledTime * .13f, 3.7f) * .008f : l.fixture ? (clock.Period == TimePeriod.Night ? .20f : clock.Period == TimePeriod.Evening ? .07f : 0f) : strength;
                l.renderer.color = c;
            }
        }

        void OnDestroy()
        {
            Clear();
            if (glowSprite != null)
                Destroy(glowSprite);
            if (glow != null)
                Destroy(glow);
            if (solid != null)
                Destroy(solid);
        }
    }
}
