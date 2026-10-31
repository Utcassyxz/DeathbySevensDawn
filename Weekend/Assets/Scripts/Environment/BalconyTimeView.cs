// CORE VISUAL OWNER: balcony sky, city and time-of-day are one bounded sprite, never camera clear-color.
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Interaction
{
    public sealed class BalconyTimeView : MonoBehaviour
    {
        TimeManager clock;
        Texture2D panorama;
        Sprite image;
        SpriteRenderer renderer;
        public Color SkyColor { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
        }

        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Balcony" || Object.FindAnyObjectByType<BalconyTimeView>() != null)
                return;
            var g = new GameObject("Balcony vista owner");
            SceneManager.MoveGameObjectToScene(g, scene);
            g.AddComponent<BalconyTimeView>();
        }

        void Awake()
        {
            foreach (var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (r.gameObject.scene == gameObject.scene && (r.name.StartsWith("Distant ") || r.name.StartsWith("阳台外景照片")))
                    r.enabled = false;
            panorama = new Texture2D(640, 224, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            image = Sprite.Create(panorama, new Rect(0, 0, 640, 224), Vector2.one * .5f, 56);
            var g = new GameObject("Balcony vista sky and city");
            g.transform.SetParent(transform, false);
            g.transform.position = new Vector3(0, -4.05f, 0);
            g.transform.localScale = new Vector3(1.32f, 1, 1);
            renderer = g.AddComponent<SpriteRenderer>();
            renderer.sprite = image;
            renderer.sortingOrder = -12;
            var camera = Camera.main;
            if (camera != null)
            {
                camera.orthographicSize = 5.3f;
                camera.transform.position = new Vector3(0, -.65f, -10);
            }
        }

        void OnEnable()
        {
            clock = TimeManager.Instance;
            if (clock != null)
            {
                clock.TimeChanged += Refresh;
                Refresh(clock.Days, clock.Period);
            }
        }

        void OnDisable()
        {
            if (clock != null)
                clock.TimeChanged -= Refresh;
        }

        void Refresh(int day, TimePeriod period)
        {
            bool night = period == TimePeriod.Night;
            bool dusk = period == TimePeriod.Evening;
            SkyColor = night ? new Color(.025f, .035f, .095f) : dusk ? new Color(.56f, .24f, .28f) : period == TimePeriod.Morning ? new Color(.56f, .77f, .85f) : new Color(.39f, .67f, .87f);
            float decay = Mathf.Clamp01((day - 1) / 6f);
            for (int y = 0; y < 224; y++)
                for (int x = 0; x < 640; x++)
                {
                    float t = y / 223f;
                    Color c = Color.Lerp(SkyColor * .58f, SkyColor, t);
                    c.a = 1;
                    if (night)
                    {
                        float band = Mathf.Exp(-Mathf.Pow((y - (105 + x * .10f)) / 27, 2)) * .12f;
                        c += new Color(.23f, .19f, .37f) * band * (.4f + Mathf.PerlinNoise(x * .028f, y * .035f));
                        uint hash = unchecked((uint)(x * 73856093 ^ y * 19349663));
                        hash ^= hash >> 13;
                        hash *= 1274126177u;
                        if (hash % 877 == 0 && y > 72)
                            c = new Color(.67f, .75f, .90f);
                    }
                    else
                    {
                        float cloud = Mathf.PerlinNoise(x * .011f, y * .021f + day * .03f);
                        if (cloud > .62f && y > 80)
                            c = Color.Lerp(c, new Color(.91f, .90f, .84f), Mathf.Clamp01((cloud - .62f) * 2.5f));
                    }

                    int column = x / 31;
                    int roof = 32 + (column * 17 % 39);
                    if (y < roof)
                    {
                        c = night ? new Color(.045f, .065f, .105f) : new Color(.27f, .36f, .40f);
                        if (x % 31 < 2)
                            c *= .68f;
                        if (x % 9 >= 3 && x % 9 <= 5 && y % 13 >= 4 && y % 13 <= 7 && y < roof - 6)
                            c = night && ((column + y / 13) % 8 >= day - 1) ? new Color(.83f, .63f, .30f) : c * .7f;
                        if (y == roof - 1)
                            c *= 1.28f;
                    }

                    c = Color.Lerp(c, new Color(.15f, .13f, .21f), decay * .28f);
                    c.a = 1;
                    panorama.SetPixel(x, y, c);
                }

            panorama.Apply(false);
        }

        void OnDestroy()
        {
            if (image != null)
                Destroy(image);
            if (panorama != null)
                Destroy(panorama);
        }
    }
}
