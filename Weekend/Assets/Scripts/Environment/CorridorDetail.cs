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
            glowBitmap = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
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
            Color wall = new Color(.31f, .36f, .34f), brass = new Color(.61f, .51f, .34f), wood = new Color(.23f, .17f, .15f);
            Block("north wallpaper face", new Vector2(0, 1.55f), new Vector2(13.65f, .95f), wall, 0);
            Block("north cornice shadow", new Vector2(0, 2.06f), new Vector2(13.7f, .10f), new Color(.10f, .14f, .15f), 1);
            Block("north cornice brass", new Vector2(0, 1.99f), new Vector2(13.7f, .025f), brass, 2);
            Block("north skirting", new Vector2(0, 1.04f), new Vector2(13.7f, .065f), new Color(.11f, .16f, .16f), 1);
            for (int i = 0; i < 37; i++)
                Block("wallpaper pinstripe", new Vector2(-6.65f + i * .37f, 1.55f), new Vector2(.012f, .80f), new Color(.25f, .30f, .28f), 1);
            for (int i = 0; i < 29; i++)
            {
                float x = -6.7f + i * .48f;
                Block("tile grout", new Vector2(x, -.35f), new Vector2(.012f, 2.7f), new Color(.20f, .22f, .21f), -5);
            }

            for (int row = 0; row < 4; row++)
                Block("cross tile grout", new Vector2(0, -1.5f + row * .64f), new Vector2(13.65f, .012f), new Color(.14f, .17f, .16f), -5);
            Block("runner shadow", new Vector2(0, -.24f), new Vector2(12.1f, 1.10f), new Color(.09f, .095f, .095f), -4);
            Block("woven runner", new Vector2(0, -.19f), new Vector2(12f, .94f), new Color(.25f, .17f, .18f), -3);
            for (int i = 0; i < 2; i++)
                Block("runner binding", new Vector2(0, -.59f + i * .80f), new Vector2(11.94f, .025f), new Color(.51f, .38f, .27f), -2);
            for (int i = 0; i < 39; i++)
                Block("woven fleck", new Vector2(-5.8f + i * .30f, -.18f), new Vector2(.10f, .018f), new Color(.33f, .24f, .22f), -2);
            for (int stitch = 0; stitch < 58; stitch++)
                for (int edge = 0; edge < 2; edge++)
                    Block("runner edge stitch", new Vector2(-5.75f + stitch * .20f, -.52f + edge * .66f), new Vector2(.025f, .045f), new Color(.37f, .28f, .23f), -2);
            foreach (float x in new[]
            {
                -4.7f,
                4.7f
            }

            )
            {
                Block("door frame outer", new Vector2(x, 1.51f), new Vector2(1.48f, 1.03f), new Color(.10f, .12f, .12f), 2);
                Block("neighbor wood door", new Vector2(x, 1.49f), new Vector2(1.25f, .90f), wood, 3);
                Block("door inset", new Vector2(x, 1.41f), new Vector2(.94f, .60f), wood * 1.16f, 4);
                Block("door inset shadow", new Vector2(x, 1.12f), new Vector2(.96f, .025f), wood * .58f, 5);
                Block("handle plate", new Vector2(x + .43f, 1.40f), new Vector2(.055f, .19f), brass * .7f, 5);
                Block("lever", new Vector2(x + .39f, 1.45f), new Vector2(.15f, .035f), brass, 6);
                Block("threshold", new Vector2(x, 1.0f), new Vector2(1.42f, .06f), brass * .6f, 4);
                Block("door mat", new Vector2(x, .80f), new Vector2(1.5f, .34f), new Color(.18f, .20f, .18f), -2);
            }

            // The 402 entrance remains below the player, as requested; do not move its collider.
            foreach (float x in new[]
            {
                -3.85f,
                3.85f
            }

            )
            {
                Block("south wainscot", new Vector2(x, -1.87f), new Vector2(6.1f, .28f), new Color(.14f, .19f, .19f), 2);
                Block("south trim", new Vector2(x, -1.69f), new Vector2(6.1f, .035f), brass * .7f, 3);
            }

            Block("402 welcome mat", new Vector2(0, -1.24f), new Vector2(1.6f, .36f), new Color(.20f, .22f, .19f), -2);
            foreach (float x in new[]
            {
                -2.7f,
                0f,
                2.7f
            }

            )
            {
                Block("wall lamp backplate", new Vector2(x, 1.71f), new Vector2(.17f, .30f), brass * .65f, 4);
                Block("wall lamp shade", new Vector2(x, 1.75f), new Vector2(.34f, .18f), new Color(.65f, .48f, .29f), 5);
                Block("wall lamp glass", new Vector2(x, 1.62f), new Vector2(.20f, .11f), new Color(.91f, .79f, .57f), 5);
                var g = new GameObject("corridor lamp glow");
                g.transform.SetParent(root.transform, false);
                g.transform.position = new Vector2(x, 1.53f);
                g.transform.localScale = new Vector2(1.7f, 1.35f);
                var r = g.AddComponent<SpriteRenderer>();
                r.sprite = glow;
                r.sortingOrder = 8;
                lampGlows.Add(r);
            }

            Block("ceiling conduit", new Vector2(0, 2.16f), new Vector2(13.3f, .035f), new Color(.28f, .31f, .29f), 1);
            for (int i = 0; i < 8; i++)
                Block("conduit clasp", new Vector2(-6.2f + i * 1.8f, 2.16f), new Vector2(.04f, .095f), brass * .6f, 2);
            Block("fire cabinet", new Vector2(-6.35f, .90f), new Vector2(.45f, .48f), new Color(.36f, .14f, .13f), 3);
            Block("fire cabinet glazing", new Vector2(-6.35f, .93f), new Vector2(.29f, .26f), new Color(.15f, .20f, .21f), 4);
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
