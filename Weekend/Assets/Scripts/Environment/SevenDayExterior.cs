// One seven-day radiation chronology, rendered in coarse pixels for both outdoor viewpoints.
using UnityEngine;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Interaction
{
    public static class SevenDayExterior
    {
        static Texture2D[] window = new Texture2D[28], courtyard = new Texture2D[28];
        public static int Phase(int day) => Mathf.Clamp(day, 1, 7);
        public static Texture2D View(int day, TimePeriod period, bool overhead = false)
        {
            day = Phase(day);
            int index = (day - 1) * 4 + (int)period;
            var cache = overhead ? courtyard : window;
            if (cache[index] != null)
                return cache[index];
            int w = 192, h = overhead ? 64 : 112;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Outside day " + day + " " + period
            };
            Color tint = period == TimePeriod.Night ? new Color(.29f, .34f, .53f) : period == TimePeriod.Evening ? new Color(1, .62f, .47f) : period == TimePeriod.Morning ? new Color(.77f, .91f, 1) : Color.white;
            Color road = new Color(.31f, .35f, .35f), stone = new Color(.48f, .51f, .46f), leaf = new Color(.26f, .38f, .29f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = road;
                    if (y % 14 == 0 || x % 24 == 0)
                        c *= .87f;
                    if (x < 42 || x > 151)
                        c = stone;
                    if (!overhead && y > 78)
                    {
                        c = new Color(.56f, .68f, .70f);
                        if (period == TimePeriod.Night)
                        {
                            c = new Color(.04f, .06f, .13f);
                            if ((x * 23 + y * 31) % 181 == 0)
                                c = new Color(.63f, .66f, .71f);
                        }
                    }

                    if ((overhead || y < 78) && ((x >= 12 && x < 46) || (x > 145 && x < 183)))
                    {
                        int by = y % 44;
                        c = by < 4 ? new Color(.19f, .24f, .25f) : new Color(.43f, .46f, .43f);
                        if (by > 12 && by < 24 && x % 14 > 5 && x % 14 < 11)
                            c = period == TimePeriod.Night ? new Color(.70f, .57f, .29f) : new Color(.25f, .36f, .40f);
                        if (day >= 2 && ((x / 14 + y / 44) % 3 == 0) && by > 12 && by < 24)
                            c = new Color(.16f, .22f, .27f);
                        if (day >= 6 && by > 12 && by < 24 && x % 14 > 6 && x % 14 < 10)
                            c = new Color(.07f, .11f, .14f);
                    }

                    foreach (var tree in new[]
                    {
                        new Vector2(56, 21),
                        new Vector2(133, 48),
                        new Vector2(50, 66)
                    }

                    )
                    {
                        float d = (x - tree.x) * (x - tree.x) + (y - tree.y) * (y - tree.y);
                        if (d < 110)
                            c = (x + y) % 5 < 2 ? leaf : new Color(.19f, .28f, .24f);
                        if (day >= 3 && d < 40 && (x + y) % 7 < 2)
                            c = new Color(.45f, .25f, .34f);
                        if (day >= 5 && d < 60 && x % 5 == 0)
                            c = new Color(.16f, .12f, .22f);
                    }

                    if (x > 82 && x < 111 && y % 20 > 8 && y % 20 < 11)
                        c = new Color(.60f, .59f, .45f);
                    // Readable little street silhouettes, never realistic people or gore.
                    for (int i = 0; i < 3; i++)
                    {
                        int px = 72 + i * 23, py = 15 + i * 14;
                        if (x >= px && x <= px + 2 && y >= py && y <= py + 5)
                            c = new Color(.13f, .19f, .23f);
                        if (day >= 3 && x == px + 3 && y == py + 3)
                            c = new Color(.33f, .19f, .26f);
                        if (day >= 5 && x >= px - 2 && x <= px + 5 && (y == py + 2 || y == py + 4))
                            c = new Color(.13f, .15f, .22f);
                        if (day >= 6 && x >= px - 1 && x <= px + 5 && y == py)
                            c = new Color(.09f, .11f, .15f);
                    }

                    if (day >= 4 && x > 65 && x < 123 && ((x * 3 + y * 7) % 103 < 3))
                        c = new Color(.31f, .21f, .30f);
                    if (day >= 6 && y > 28 && y < 32 && x > 65 && x < 135)
                        c = (x + y) % 7 < 3 ? new Color(.51f, .38f, .23f) : new Color(.12f, .16f, .18f);
                    if (day >= 7 && (Mathf.Abs(x - (90 + (y / 7 % 3) * 4)) < 2 || (x * 7 + y * 3) % 173 < 5))
                        c = new Color(.94f, .30f, .12f);
                    if (day >= 7 && y > h - 25 && (x + y) % 13 < 3)
                        c = new Color(.27f, .12f, .21f);
                    c *= tint;
                    c.a = 1;
                    t.SetPixel(x, y, c);
                }

            t.Apply();
            cache[index] = t;
            return t;
        }
    }
}
