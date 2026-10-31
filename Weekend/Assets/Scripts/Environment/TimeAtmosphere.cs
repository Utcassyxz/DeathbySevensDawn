// 职责：时间配色与天台天空的同步渐变；不接管其他系统的动态着色。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Progression
{
    // Tint world sprites, never UI or the black camera border. Capture base colours once per scene.
    [DefaultExecutionOrder(800)]
    public sealed class TimeAtmosphere : MonoBehaviour
    {
        readonly Dictionary<SpriteRenderer, Color> originals = new Dictionary<SpriteRenderer, Color>();
        Color current = Color.white, from = Color.white, target = Color.white;
        float transition = 2, napStarted = -100;
        SpriteRenderer sky;
        Texture2D skyTexture;
        Sprite skySprite;
        TimeManager clock;
        Color32[] skyFrom, skyTo, skyBlend;
        float skyAge = 2;
        public Color CurrentTint => current;

        public static Color Palette(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.Morning:
                    return new Color(.81f, .92f, 1);
                case TimePeriod.Afternoon:
                    return Color.white;
                case TimePeriod.Evening:
                    return new Color(1, .74f, .52f);
                default:
                    return new Color(.25f, .29f, .42f);
            }
        }

        public void BeginDuskNap()
        {
            napStarted = Time.unscaledTime;
        }

        void OnEnable()
        {
            clock = TimeManager.Instance;
            clock.TimeChanged += Changed;
            SceneManager.sceneLoaded += Loaded;
        }

        void OnDisable()
        {
            if (clock != null)
                clock.TimeChanged -= Changed;
            SceneManager.sceneLoaded -= Loaded;
            Restore();
            DestroySky();
        }

        void Start()
        {
            Loaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void Restore()
        {
            foreach (var p in originals)
                if (p.Key != null)
                    p.Key.color = p.Value;
            originals.Clear();
        }

        void DestroySky()
        {
            if (sky != null)
                Destroy(sky.gameObject);
            if (skySprite != null)
                Destroy(skySprite);
            if (skyTexture != null)
                Destroy(skyTexture);
            sky = null;
            skySprite = null;
            skyTexture = null;
            skyFrom = skyTo = skyBlend = null;
            skyAge = 2;
        }

        void Loaded(Scene scene, LoadSceneMode mode)
        {
            Restore();
            DestroySky();
            current = from = target = Palette(clock.Period);
            transition = 2;
            if (scene.name == "Rooftop")
            {
                var go = new GameObject("Time sky and Milky Way");
                SceneManager.MoveGameObjectToScene(go, scene);
                sky = go.AddComponent<SpriteRenderer>();
                skyTexture = new Texture2D(480, 128, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };
                skySprite = Sprite.Create(skyTexture, new Rect(0, 0, 480, 128), Vector2.one * .5f, 32);
                sky.sprite = skySprite;
                sky.sortingOrder = -20;
                go.transform.position = new Vector3(0, 4.45f, 0);
                go.transform.localScale = new Vector3(1.14f, 1, 1);
                var floor = GameObject.Find("Rooftop pixel environment");
                if (floor != null)
                    sky.sharedMaterial = floor.GetComponent<SpriteRenderer>().sharedMaterial;
                var camera = Camera.main;
                if (camera != null)
                {
                    camera.orthographicSize = 5.4f;
                    camera.transform.position = new Vector3(0, .65f, -10);
                }

                DrawSky(clock.Period);
            }

            Capture();
        }

        static bool Dynamic(Transform t)
        {
            for (; t != null; t = t.parent)
                if (t.name.StartsWith("Aquarium") || t.name.StartsWith("Perception clue") || t.name.StartsWith("Distant ") || t.name.StartsWith("阳台外景照片"))
                    return true;
            return false;
        }

        void Capture()
        {
            foreach (var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (r != sky && r.gameObject.scene == SceneManager.GetActiveScene() && !Dynamic(r.transform) && !originals.ContainsKey(r))
                    originals.Add(r, r.color);
        }

        void Changed(int day, TimePeriod period)
        {
            from = current;
            target = Palette(period);
            transition = 0;
            if (sky != null)
                DrawSky(period);
        }

        public void DrawSky(TimePeriod period)
        {
            if (skyTexture == null)
                return;
            var pixels = new Color32[480 * 128];
            bool night = period == TimePeriod.Night;
            bool dusk = period == TimePeriod.Evening;
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 480; x++)
                {
                    float t = y / 127f;
                    Color top = night ? new Color(.015f, .018f, .045f) : dusk ? new Color(.24f, .09f, .23f) : new Color(.28f, .43f, .54f);
                    Color bottom = night ? new Color(.055f, .065f, .12f) : dusk ? new Color(.72f, .3f, .29f) : new Color(.7f, .77f, .77f);
                    var c = Color.Lerp(bottom, top, t);
                    if (night)
                    {
                        float band = Mathf.Exp(-Mathf.Pow((y - (x * .19f + 9)) / 16, 2));
                        float dust = Mathf.PerlinNoise(x * .065f, y * .085f);
                        c += new Color(.14f, .12f, .21f) * band * (.3f + .7f * dust);
                        uint hash = unchecked((uint)(x * 374761393 + y * 668265263));
                        hash = (hash ^ (hash >> 13)) * 1274126177u;
                        hash ^= hash >> 16;
                        if (hash % 1000 < 3)
                        {
                            float light = .45f + (hash % 4096) / 8192f;
                            c = new Color(light * .82f, light * .89f, light);
                        }
                    }

                    pixels[y * 480 + x] = c;
                }

            if (skyTo == null)
            {
                skyFrom = skyTo = pixels;
                skyTexture.SetPixels32(pixels);
                skyTexture.Apply();
                skyAge = 2;
            }
            else
            {
                skyFrom = skyTexture.GetPixels32();
                skyTo = pixels;
                skyBlend = new Color32[pixels.Length];
                skyAge = 0;
            }
        }

        void LateUpdate()
        {
            if (clock == null)
                return;
            if (skyTexture != null && skyAge < 2)
            {
                skyAge = Mathf.Min(2, skyAge + Time.unscaledDeltaTime);
                float blend = Mathf.SmoothStep(0, 1, skyAge / 2);
                for (int i = 0; i < skyBlend.Length; i++)
                    skyBlend[i] = Color32.Lerp(skyFrom[i], skyTo[i], blend);
                skyTexture.SetPixels32(skyBlend);
                skyTexture.Apply();
            }

            if (Time.frameCount % 30 == 0)
                Capture();
            transition += Time.unscaledDeltaTime;
            current = Color.Lerp(from, target, Mathf.SmoothStep(0, 1, Mathf.Clamp01(transition / 2f)));
            float elapsed = Time.unscaledTime - napStarted;
            Color tint = current;
            if (elapsed >= 0 && elapsed < 5.3f)
                tint = Color.Lerp(current, new Color(1, .43f, .66f), Mathf.Sin(elapsed / 5.3f * Mathf.PI) * .6f);
            foreach (var p in originals)
                if (p.Key != null)
                {
                    var c = p.Value * tint;
                    c.a = p.Value.a;
                    p.Key.color = c;
                }
        }
    }
}
