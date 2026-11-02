// CORE VIEW: one consistent overhead projection. No skyline below a top-down balcony.
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Interaction
{
    [DefaultExecutionOrder(-120)]
    public sealed class BalconyTimeView : MonoBehaviour
    {
        TimeManager clock;
        Texture2D panorama;
        Sprite image;
        SpriteRenderer vista;
        public Color SkyColor { get; private set; }
        public bool UsesOverheadProjection => true;

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
            var interior = new GameObject("Balcony floor plan");
            SceneManager.MoveGameObjectToScene(interior, gameObject.scene);
            foreach (var item in gameObject.scene.GetRootGameObjects())
                if (item != gameObject && item != interior && !item.CompareTag("Player") && item.GetComponent<Camera>() == null && (item.GetComponentInChildren<SpriteRenderer>() != null || item.GetComponent<Collider2D>() != null))
                    item.transform.SetParent(interior.transform, false);
            interior.transform.localScale = new Vector3(.9f, .6f, 1);
            panorama = new Texture2D(320, 192, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            image = Sprite.Create(panorama, new Rect(0, 0, 320, 192), Vector2.one * .5f, 32);
            var g = new GameObject("Balcony vista overhead courtyard");
            g.transform.SetParent(transform, false);
            g.transform.position = new Vector3(0, -4.18f, 0);
            vista = g.AddComponent<SpriteRenderer>();
            vista.sprite = image;
            vista.sortingOrder = -12;
            if (Camera.main != null)
            {
                Camera.main.orthographicSize = 3.95f;
                Camera.main.transform.position = new Vector3(0, -.72f, -10);
            }
        }

        void Start()
        {
            // Arrival is moved inside the compressed threshold, clear of the return trigger.
            var p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                var spot = (Vector2)p.transform.position;
                spot.y = Mathf.Clamp(spot.y, -.65f, .42f);
                p.transform.position = spot;
                var rb = p.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.position = spot;
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
            bool night = period == TimePeriod.Night, dusk = period == TimePeriod.Evening;
            SkyColor = night ? new Color(.07f, .09f, .16f) : dusk ? new Color(.74f, .42f, .43f) : period == TimePeriod.Morning ? new Color(.67f, .79f, .81f) : new Color(.83f, .84f, .79f);
            for (int y = 0; y < 192; y++)
                for (int x = 0; x < 320; x++)
                {
                    // Roof planes, paving and tree crowns seen from above. Every feature shares the room's axes.
                    Color c = new Color(.26f, .30f, .29f);
                    if (x > 111 && x < 204)
                        c = new Color(.38f, .39f, .36f); // path directly under balcony
                    if (x > 119 && x < 196 && (x % 16 == 0 || y % 16 == 0))
                        c *= .82f;
                    if (x < 94 || x > 224)
                    {
                        int rx = x < 94 ? 12 : 239;
                        int ry = 18;
                        bool roof = x > rx && x < rx + 70 && y > ry && y < 170;
                        if (roof)
                        {
                            c = new Color(.32f, .35f, .35f);
                            if (x < rx + 4 || x > rx + 64 || y < ry + 4 || y > 165)
                                c = new Color(.19f, .23f, .24f);
                            if ((x - rx) % 24 == 0)
                                c *= .82f;
                        }

                        // Small rectangular ventilation housings on a horizontal roof, never a facade.
                        if (roof && x > rx + 18 && x < rx + 41 && y > 72 && y < 104)
                        {
                            c = new Color(.47f, .49f, .46f);
                            if (y % 4 == 0)
                                c *= .72f;
                        }
                    }

                    foreach (var tree in new[]
                    {
                        new Vector2(103, 48),
                        new Vector2(214, 136),
                        new Vector2(101, 165)
                    }

                    )
                    {
                        float d = (new Vector2(x, y) - tree).sqrMagnitude;
                        if (d < 185)
                        {
                            c = d < 105 ? new Color(.20f, .32f, .23f) : new Color(.13f, .23f, .18f);
                            if ((x + y) % 7 < 2)
                                c *= 1.13f;
                        }
                    }

                    if (x > 143 && x < 168 && y > 39 && y < 85)
                    {
                        c = new Color(.32f, .38f, .44f);
                        if (y < 49 || y > 74)
                            c = new Color(.15f, .21f, .25f);
                        if (x == 145 || x == 166)
                            c *= .7f;
                    }

                    c *= SkyColor;
                    if (night && ((x - 109) * (x - 109) + (y - 105) * (y - 105) < 170))
                        c = Color.Lerp(c, new Color(.48f, .37f, .20f), .28f);
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
