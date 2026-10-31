// 职责：给四楼走廊加一层运行时细节（护墙板、踢脚线、地毯、壁灯、管线和门框），不改碰撞。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Progression
{
    [DefaultExecutionOrder(1100)]
    public sealed class CorridorDetail : MonoBehaviour
    {
        GameObject root;
        Texture2D solidBitmap, glowBitmap;
        Sprite square, glow;
        readonly List<SpriteRenderer> lampGlows = new List<SpriteRenderer>();

        void OnEnable() => SceneManager.sceneLoaded += Loaded;
        void OnDisable()
        {
            SceneManager.sceneLoaded -= Loaded;
            Clear();
        }

        void Start() => Loaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        void Loaded(Scene scene, LoadSceneMode mode)
        {
            Clear();
            if (scene.name != "Corridor")
                return;
            Build();
        }

        void Clear()
        {
            if (root != null)
                Destroy(root);
            root = null;
            lampGlows.Clear();
            if (square != null)
                Destroy(square);
            if (glow != null)
                Destroy(glow);
            if (solidBitmap != null)
                Destroy(solidBitmap);
            if (glowBitmap != null)
                Destroy(glowBitmap);
            square = null;
            glow = null;
            solidBitmap = null;
            glowBitmap = null;
        }

        void EnsureSprites()
        {
            if (square != null)
                return;
            solidBitmap = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            solidBitmap.SetPixel(0, 0, Color.white);
            solidBitmap.Apply(false);
            square = Sprite.Create(solidBitmap, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);

            glowBitmap = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float r = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f;
                    glowBitmap.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - r * r), 3)));
                }
            glowBitmap.Apply(false);
            glow = Sprite.Create(glowBitmap, new Rect(0, 0, 64, 64), Vector2.one * .5f, 64);
        }

        void Build()
        {
            EnsureSprites();
            root = new GameObject("Corridor detail");
            SceneManager.MoveGameObjectToScene(root, SceneManager.GetActiveScene());

            Color wallpaper = new Color(.30f, .34f, .31f);
            Color rail = new Color(.50f, .45f, .34f);
            Color wainscot = new Color(.15f, .21f, .20f);
            Color carpet = new Color(.17f, .13f, .12f);
            Color brass = new Color(.64f, .52f, .30f);
            Color pipe = new Color(.24f, .28f, .27f);

            // Lower panelled wall, its top rail and the darker skirting.
            Block("lower wainscot", new Vector2(0, -.62f), new Vector2(11.1f, 1.16f), wainscot, 1);
            Block("dado rail", new Vector2(0, -.03f), new Vector2(11.1f, .055f), rail, 3);
            Block("skirting", new Vector2(0, -1.79f), new Vector2(11.1f, .09f), new Color(.10f, .12f, .12f), 3);
            for (int i = 0; i < 11; i++)
            {
                float x = -5.05f + i * .98f;
                Block("panel stile", new Vector2(x, -.64f), new Vector2(.035f, 1.10f), new Color(.10f, .14f, .14f), 2);
                Block("panel inlay", new Vector2(x + .49f, -.64f), new Vector2(.36f, .64f), new Color(.18f, .25f, .23f), 1);
            }

            // Upper wall gets a subtle wallpaper seam, not another flat colour.
            for (int i = 0; i < 7; i++)
                Block("wall seam", new Vector2(-4.8f + i * 1.6f, .84f), new Vector2(.018f, 1.32f), new Color(.25f, .29f, .27f, .55f), 1);
            Block("crown rail", new Vector2(0, 1.54f), new Vector2(11.1f, .055f), rail, 3);
            Block("ceiling", new Vector2(0, 1.94f), new Vector2(11.1f, .20f), new Color(.10f, .12f, .12f), 4);

            // Runner and its stitched edge; leaves the tile border visible.
            Block("runner", new Vector2(0, -1.47f), new Vector2(9.7f, .44f), carpet, 2);
            Block("runner edge", new Vector2(0, -1.25f), new Vector2(9.7f, .025f), new Color(.34f, .24f, .18f), 3);
            Block("runner edge", new Vector2(0, -1.69f), new Vector2(9.7f, .025f), new Color(.11f, .09f, .08f), 3);
            for (int i = 0; i < 9; i++)
                Block("runner stitch", new Vector2(-4.1f + i * 1.02f, -1.47f), new Vector2(.42f, .012f), new Color(.42f, .30f, .22f, .65f), 3);

            // Pipes and cable tray give the corridor a lived-in silhouette.
            Block("pipe tray", new Vector2(0, 1.78f), new Vector2(10.9f, .045f), pipe, 5);
            for (int i = 0; i < 3; i++)
            {
                float x = -4.5f + i * 4.5f;
                Block("pipe bracket", new Vector2(x, 1.78f), new Vector2(.045f, .16f), new Color(.42f, .38f, .30f), 5);
            }

            // Three wall lamps: actual shades and warm pools, switched at dusk/night.
            for (int i = 0; i < 3; i++)
            {
                float x = -3.2f + i * 3.2f;
                Block("lamp bracket", new Vector2(x, 1.24f), new Vector2(.045f, .22f), new Color(.20f, .18f, .15f), 5);
                Block("lamp shade", new Vector2(x, 1.12f), new Vector2(.40f, .12f), new Color(.78f, .58f, .31f), 5);
                Block("lamp bulb", new Vector2(x, 1.03f), new Vector2(.10f, .08f), new Color(1f, .86f, .57f), 6);
                var pool = new GameObject("corridor lamp glow");
                pool.transform.SetParent(root.transform, false);
                pool.transform.position = new Vector2(x, .92f);
                pool.transform.localScale = new Vector2(2.5f, 2.1f);
                var r = pool.AddComponent<SpriteRenderer>();
                r.sprite = glow;
                r.color = new Color(1f, .72f, .42f, 0);
                r.sortingOrder = 7;
                lampGlows.Add(r);
            }

            // Small brass details: unit underline, handles and mailboxes.
            for (int i = 0; i < 3; i++)
            {
                float x = -4.7f + i * 4.7f;
                Block("room underline", new Vector2(x, 1.46f), new Vector2(.62f, .025f), brass, 5);
                Block("door handle", new Vector2(x + .37f, -.68f), new Vector2(.08f, .035f), brass, 5);
                Block("door handle", new Vector2(x + .37f, -.60f), new Vector2(.025f, .10f), new Color(.20f, .18f, .14f), 5);
            }
            for (int i = 0; i < 6; i++)
            {
                float x = -4.5f + i * .34f;
                Block("mail slot", new Vector2(x, -.95f), new Vector2(.22f, .30f), new Color(.31f, .30f, .25f), 4);
                Block("mail slot line", new Vector2(x, -.92f), new Vector2(.15f, .012f), new Color(.12f, .12f, .10f), 5);
            }

            // Extinguisher and a service sign at the ends.
            Block("extinguisher", new Vector2(-3.9f, -.84f), new Vector2(.16f, .38f), new Color(.54f, .12f, .10f), 5);
            Block("extinguisher band", new Vector2(-3.9f, -.71f), new Vector2(.16f, .035f), new Color(.80f, .74f, .58f), 6);
            Block("service sign", new Vector2(4.05f, .84f), new Vector2(.50f, .30f), new Color(.76f, .72f, .58f), 5);
            Block("service sign line", new Vector2(4.05f, .88f), new Vector2(.36f, .018f), new Color(.32f, .35f, .30f), 6);
        }

        void Block(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = square;
            r.color = color;
            // Keep details above the flat corridor wall (order 1) but below the player
            // and the door interaction plates (order 10).
            r.sortingOrder = order + 2;
        }

        void LateUpdate()
        {
            var clock = TimeManager.Instance;
            bool night = clock != null && (clock.Period == TimePeriod.Night || clock.Period == TimePeriod.Evening);
            foreach (var glow in lampGlows)
                if (glow != null)
                    glow.color = new Color(1f, .72f, .42f, night ? .15f : 0);
        }
    }
}
