// 职责：两处望远镜共用的俯视像素外观；保留原互动锚点。
using UnityEngine;

namespace DeathBySeventhsDawn.Progression
{
    public static class TelescopeVisual
    {
        static Sprite sprite;
        public static void Attach(GameObject root)
        {
            if (sprite == null)
                Build();
            var image = new GameObject("Optical tube and metal tripod");
            image.transform.SetParent(root.transform, false);
            var renderer = image.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 12;
            var collider = root.GetComponent<BoxCollider2D>();
            if (collider == null)
                collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(.78f, .72f);
            collider.offset = new Vector2(0, .18f);
            collider.isTrigger = true;
        }

        static void Build()
        {
            var t = new Texture2D(128, 128, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[128 * 128];
            t.SetPixels(pixels);
            // Elliptical ground shadow, three separated feet and a visible equatorial mount.
            Ellipse(t, 65, 18, 41, 9, new Color(.015f, .02f, .025f, .30f));
            Line(t, 63, 63, 30, 17, 5, new Color(.08f, .11f, .14f));
            Line(t, 63, 63, 98, 21, 5, new Color(.08f, .11f, .14f));
            Line(t, 63, 63, 65, 8, 5, new Color(.08f, .11f, .14f));
            Line(t, 62, 62, 29, 19, 2, new Color(.57f, .62f, .64f));
            Line(t, 64, 62, 96, 23, 2, new Color(.46f, .51f, .54f));
            Line(t, 62, 60, 64, 10, 2, new Color(.64f, .68f, .68f));
            Line(t, 25, 16, 34, 16, 4, new Color(.08f, .09f, .10f));
            Line(t, 94, 20, 103, 20, 4, new Color(.08f, .09f, .10f));
            Line(t, 60, 8, 69, 8, 4, new Color(.08f, .09f, .10f));
            Line(t, 63, 58, 64, 80, 9, new Color(.12f, .17f, .19f));
            Line(t, 60, 58, 61, 80, 2, new Color(.59f, .64f, .63f));
            Ellipse(t, 63, 69, 10, 8, new Color(.09f, .12f, .15f));
            Ellipse(t, 62, 72, 6, 5, new Color(.50f, .55f, .57f));
            // A shaded cylindrical tube, aimed up and right, not a floating flat rectangle.
            for (int y = 63; y < 115; y++)
                for (int x = 24; x < 112; x++)
                {
                    float dx = x - 30, dy = y - 76;
                    float along = dx * .94f + dy * .34f, side = -dx * .34f + dy * .94f;
                    if (along < 0 || along > 76 || Mathf.Abs(side) > 12)
                        continue;
                    float shade = Mathf.Clamp01((side + 12) / 24);
                    Color c = Color.Lerp(new Color(.12f, .23f, .25f), new Color(.56f, .72f, .70f), shade);
                    if (side > 7)
                        c = Color.Lerp(c, new Color(.83f, .87f, .78f), .4f);
                    if (along < 5 || along > 68)
                        c *= .50f;
                    if (along > 38 && along < 43)
                        c = new Color(.20f, .28f, .29f);
                    t.SetPixel(x, y, c);
                }

            Ellipse(t, 101, 102, 8, 12, new Color(.07f, .12f, .16f));
            Ellipse(t, 102, 103, 5, 8, new Color(.19f, .38f, .49f));
            Line(t, 102, 108, 105, 103, 1, new Color(.64f, .81f, .83f));
            Line(t, 30, 77, 22, 70, 7, new Color(.07f, .11f, .14f));
            Line(t, 22, 70, 19, 65, 6, new Color(.10f, .14f, .17f));
            Line(t, 55, 103, 77, 111, 5, new Color(.11f, .17f, .19f));
            Line(t, 54, 105, 76, 113, 1, new Color(.64f, .69f, .67f));
            Ellipse(t, 56, 72, 4, 4, new Color(.65f, .67f, .59f));
            t.Apply(false);
            sprite = Sprite.Create(t, new Rect(0, 0, 128, 128), new Vector2(.5f, .25f), 92);
        }

        static void Ellipse(Texture2D t, int cx, int cy, int rx, int ry, Color color)
        {
            for (int y = Mathf.Max(0, cy - ry); y < Mathf.Min(128, cy + ry + 1); y++)
                for (int x = Mathf.Max(0, cx - rx); x < Mathf.Min(128, cx + rx + 1); x++)
                    if (Mathf.Pow((x - cx) / (float)rx, 2) + Mathf.Pow((y - cy) / (float)ry, 2) <= 1)
                        t.SetPixel(x, y, color);
        }

        static void Line(Texture2D t, int x1, int y1, int x2, int y2, int width, Color color)
        {
            int count = Mathf.Max(Mathf.Abs(x2 - x1), Mathf.Abs(y2 - y1));
            for (int i = 0; i <= count; i++)
                Ellipse(t, Mathf.RoundToInt(Mathf.Lerp(x1, x2, i / (float)count)), Mathf.RoundToInt(Mathf.Lerp(y1, y2, i / (float)count)), Mathf.Max(1, width / 2), Mathf.Max(1, width / 2), color);
        }
    }
}
