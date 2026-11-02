using UnityEngine;

namespace DeathBySeventhsDawn.Interaction
{
    // Hand-placed logical pixels; never a downscaled painted human on the map.
    public static class SmallPixelActors
    {
        static Sprite[] fish;
        static Texture2D bubble, seated;
        public static Sprite[] Fish()
        {
            if (fish != null)
                return fish;
            fish = new Sprite[4];
            for (int stage = 0; stage < 4; stage++)
            {
                var t = new Texture2D(24, 24, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };
                for (int y = 0; y < 24; y++)
                    for (int x = 0; x < 24; x++)
                    {
                        Color c = Color.clear;
                        if (stage < 3)
                        {
                            if ((x - 11) * (x - 11) * .52f + (y - 12) * (y - 12) < 24)
                                c = new Color(.76f, .35f, .16f);
                            if (x >= 18 && x <= 21 && Mathf.Abs(y - 12) < (x - 16) / 2)
                                c = new Color(.54f, .23f, .17f);
                            if (x >= 9 && x <= 12 && y >= 9 && y <= 14)
                                c = new Color(.78f, .74f, .54f);
                            if ((x == 9 || x == 12) && y == 13)
                                c = new Color(.07f, .12f, .14f);
                            if (y == 10 && x >= 10 && x <= 12)
                                c = new Color(.11f, .15f, .16f);
                            if (stage == 1 && y == 9 && x == 11)
                                c = new Color(.09f, .10f, .12f);
                            if (stage == 2 && ((x == 7 && y >= 4 && y <= 10) || (x == 16 && y >= 5 && y <= 11) || (y == 4 && x >= 7 && x <= 9)))
                                c = new Color(.65f, .65f, .48f);
                            if (x == 5 && y == 13)
                                c = new Color(.05f, .10f, .12f);
                        }
                        else
                        {
                            if (x >= 9 && x <= 14 && y >= 14 && y <= 20)
                                c = new Color(.62f, .63f, .50f);
                            if (x >= 8 && x <= 15 && y >= 19 && y <= 22)
                                c = new Color(.08f, .12f, .15f);
                            if (x >= 8 && x <= 15 && y >= 6 && y <= 14)
                                c = new Color(.14f, .22f, .24f);
                            if ((x == 9 || x == 14) && y >= 2 && y <= 7)
                                c = new Color(.10f, .15f, .18f);
                            if ((x == 7 || x == 16) && y >= 7 && y <= 12)
                                c = new Color(.61f, .61f, .47f);
                            if ((x == 10 || x == 13) && y == 17)
                                c = new Color(.05f, .08f, .10f);
                            if (y == 15 && x >= 10 && x <= 13)
                                c = new Color(.08f, .12f, .13f);
                        }

                        t.SetPixel(x, y, c);
                    }

                t.Apply();
                fish[stage] = Sprite.Create(t, new Rect(0, 0, 24, 24), new Vector2(.5f, stage == 3 ? 0 : .5f), 24);
            }

            return fish;
        }

        public static Texture2D Bubble
        {
            get
            {
                if (bubble != null)
                    return bubble;
                bubble = new Texture2D(72, 56, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };
                var points = new[]
                {
                    new Vector3(35, 27, 13),
                    new Vector3(19, 32, 9),
                    new Vector3(52, 33, 9),
                    new Vector3(26, 44, 8),
                    new Vector3(44, 44, 7),
                    new Vector3(24, 14, 8),
                    new Vector3(46, 15, 8)
                };
                for (int y = 0; y < 56; y++)
                    for (int x = 0; x < 72; x++)
                    {
                        Color c = Color.clear;
                        foreach (var p in points)
                        {
                            float d = (x - p.x) * (x - p.x) + (y - p.y) * (y - p.y);
                            if (d < p.z * p.z)
                                c = d < (p.z - 2) * (p.z - 2) ? new Color(.28f, .32f, .52f) : new Color(.55f, .61f, .74f);
                            if (d < (p.z - 4) * (p.z - 4) && x < p.x && y > p.y)
                                c = new Color(.37f, .45f, .63f);
                        }

                        if ((x == 30 || x == 40) && y >= 27 && y <= 29)
                            c = new Color(.04f, .07f, .14f);
                        if (y == 23 && x >= 33 && x <= 37)
                            c = new Color(.73f, .51f, .63f);
                        if ((x * 13 + y * 7) % 119 == 0 && c.a > 0)
                            c = new Color(.74f, .72f, .72f);
                        bubble.SetPixel(x, y, c);
                    }

                bubble.Apply();
                return bubble;
            }
        }

        public static Sprite Seated()
        {
            if (seated == null)
            {
                seated = new Texture2D(16, 20, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };
                for (int y = 0; y < 20; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        Color c = Color.clear;
                        if (x >= 5 && x <= 10 && y >= 12 && y <= 17)
                            c = new Color(.68f, .62f, .47f);
                        if (x >= 4 && x <= 11 && y >= 16 && y <= 19)
                            c = new Color(.10f, .15f, .17f);
                        if (x >= 4 && x <= 11 && y >= 5 && y <= 12)
                            c = new Color(.20f, .32f, .32f);
                        if ((x == 3 || x == 12) && y >= 10 && y <= 15)
                            c = new Color(.65f, .59f, .45f);
                        if ((x >= 2 && x <= 5 || x >= 10 && x <= 13) && y >= 2 && y <= 5)
                            c = new Color(.11f, .17f, .19f);
                        seated.SetPixel(x, y, c);
                    }

                seated.Apply();
            }

            return Sprite.Create(seated, new Rect(0, 0, 16, 20), new Vector2(.5f, .4f), 24);
        }
    }
}
