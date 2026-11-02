// CORE VIEW: coarse pixel architecture, one projection. Outside is intentional negative space.
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Interaction
{
    [DefaultExecutionOrder(-120)]
    public sealed class BalconyTimeView : MonoBehaviour
    {
        Texture2D pixel;
        Sprite square;
        Transform geometry;
        SpriteRenderer outside;
        Sprite exteriorSprite;
        TimeManager clock;
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
            var owner = new GameObject("Balcony vista owner");
            SceneManager.MoveGameObjectToScene(owner, scene);
            owner.AddComponent<BalconyTimeView>();
        }

        void Awake()
        {
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            square = Sprite.Create(pixel, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            // Preserve the authored door/collision relationship; replace only its visual layer.
            var plan = new GameObject("Balcony floor plan");
            SceneManager.MoveGameObjectToScene(plan, gameObject.scene);
            foreach (var item in gameObject.scene.GetRootGameObjects())
            {
                if (item == gameObject || item == plan || item.CompareTag("Player") || item.GetComponent<Camera>() != null)
                    continue;
                if (item.GetComponentInChildren<SpriteRenderer>() == null && item.GetComponent<Collider2D>() == null)
                    continue;
                item.transform.SetParent(plan.transform, false);
                foreach (var r in item.GetComponentsInChildren<SpriteRenderer>())
                    r.enabled = false;
            }

            plan.transform.localScale = new Vector3(.9f, .6f, 1);
            geometry = new GameObject("Balcony pixel architecture").transform;
            SceneManager.MoveGameObjectToScene(geometry.gameObject, gameObject.scene);
            outside = Block("Balcony vista empty exterior", 0, -3.0f, 11.8f, 3.6f, new Color(.10f, .14f, .17f), -20);
            Block("Slab underside", 0, -1.48f, 9.14f, .38f, new Color(.12f, .16f, .18f), -3);
            Block("Slab edge", 0, -1.30f, 9.20f, .12f, new Color(.33f, .35f, .34f), -2);
            Block("Floor", 0, 0, 9, 2.4f, new Color(.44f, .46f, .43f), -1);
            // Broad 8px clusters instead of tiny procedural noise.
            for (int y = 0; y < 6; y++)
                for (int x = 0; x < 18; x++)
                {
                    float px = -4.25f + x * .5f, py = -1.0f + y * .4f;
                    var c = (x + y) % 3 == 0 ? new Color(.48f, .49f, .45f) : new Color(.42f, .45f, .43f);
                    Block("Tile", px, py, .47f, .37f, c, 0);
                    Block("Tile lip", px, py + .16f, .44f, .025f, new Color(.54f, .55f, .50f), 1);
                }

            Block("North wall left", -2.60f, 1.35f, 3.80f, .45f, new Color(.26f, .30f, .31f), 3);
            Block("North wall right", 2.60f, 1.35f, 3.80f, .45f, new Color(.26f, .30f, .31f), 3);
            Block("North wall cap left", -2.60f, 1.60f, 3.84f, .08f, new Color(.54f, .55f, .50f), 4);
            Block("North wall cap right", 2.60f, 1.60f, 3.84f, .08f, new Color(.54f, .55f, .50f), 4);
            Block("Door recess", 0, 1.34f, 1.38f, .40f, new Color(.11f, .17f, .19f), 2);
            Block("Sliding door sill", 0, 1.10f, 1.38f, .10f, new Color(.56f, .59f, .56f), 5);
            Block("Sliding door track", 0, 1.06f, 1.36f, .025f, new Color(.15f, .20f, .22f), 6);
            Block("Glass upper edge", 0, 1.55f, 1.35f, .08f, new Color(.40f, .55f, .58f), 5);
            foreach (float x in new[]
            {
                -.69f,
                0f,
                .69f
            }

            )
                Block("Door jamb", x, 1.33f, .055f, .48f, new Color(.57f, .60f, .56f), 6);
            // Vertical posts occupy screen-height, top caps occupy floor-depth. Visible three faces.
            for (int i = 0; i <= 10; i++)
            {
                float x = -4.5f + i * .9f;
                Block("Rail shadow", x - .08f, -.78f, .07f, .55f, new Color(.32f, .35f, .34f), 2);
                Block("Railing post front", x, -1.13f, .09f, .53f, new Color(.19f, .24f, .26f), 20);
                Block("Railing post side", x + .055f, -1.13f, .035f, .53f, new Color(.12f, .18f, .20f), 21);
                Block("Railing post top", x, -.85f, .13f, .08f, new Color(.57f, .61f, .58f), 22);
            }

            Block("Railing lower crossbar", 0, -1.30f, 9.1f, .065f, new Color(.20f, .27f, .29f), 21);
            Block("Railing cap face", 0, -.90f, 9.2f, .12f, new Color(.25f, .32f, .34f), 23);
            Block("Railing cap top", 0, -.81f, 9.2f, .075f, new Color(.58f, .63f, .60f), 24);
            foreach (float x in new[]
            {
                -4.52f,
                4.52f
            }

            )
            {
                Block("Side parapet face", x, 0, .14f, 2.35f, new Color(.24f, .30f, .31f), 15);
                Block("Side parapet top", x - .04f, .07f, .12f, 2.42f, new Color(.54f, .59f, .55f), 16);
            }

            Block("Planter front", -3.50f, -.48f, .82f, .34f, new Color(.37f, .25f, .21f), 8);
            Block("Planter side", -3.03f, -.43f, .12f, .42f, new Color(.25f, .19f, .18f), 8);
            Block("Planter rim", -3.50f, -.26f, .96f, .11f, new Color(.60f, .43f, .31f), 9);
            Block("Planter soil", -3.50f, -.20f, .76f, .10f, new Color(.16f, .20f, .18f), 10);
            for (int i = 0; i < 5; i++)
                Block("Leaf cluster", -3.82f + i * .16f, -.06f + (i % 2) * .10f, .24f, .20f, i % 2 == 0 ? new Color(.27f, .39f, .29f) : new Color(.39f, .48f, .31f), 11);
            if (Camera.main != null)
            {
                Camera.main.orthographicSize = 3.35f;
                Camera.main.transform.position = new Vector3(0, -.85f, -10);
            }
        }

        SpriteRenderer Block(string name, float x, float y, float w, float h, Color c, int order)
        {
            var g = new GameObject(name);
            g.transform.SetParent(geometry, false);
            g.transform.localPosition = new Vector2(x, y);
            g.transform.localScale = new Vector3(w, h, 1);
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = square;
            r.color = c;
            r.sortingOrder = order;
            return r;
        }

        void Start()
        {
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
            SkyColor = period == TimePeriod.Night ? new Color(.006f, .009f, .015f) : period == TimePeriod.Evening ? new Color(.035f, .025f, .04f) : period == TimePeriod.Morning ? new Color(.035f, .05f, .06f) : new Color(.045f, .06f, .07f);
            if (outside != null)
            {
                if (exteriorSprite != null)
                    Destroy(exteriorSprite);
                var view = SevenDayExterior.View(day, period, true);
                exteriorSprite = Sprite.Create(view, new Rect(0, 0, view.width, view.height), Vector2.one * .5f, 16);
                outside.sprite = exteriorSprite;
                outside.color = Color.white;
                outside.transform.localScale = new Vector3(11.8f / exteriorSprite.bounds.size.x, 3.6f / exteriorSprite.bounds.size.y, 1);
            }
        }

        void OnDestroy()
        {
            if (exteriorSprite != null)
                Destroy(exteriorSprite);
            if (square != null)
                Destroy(square);
            if (pixel != null)
                Destroy(pixel);
        }
    }
}
