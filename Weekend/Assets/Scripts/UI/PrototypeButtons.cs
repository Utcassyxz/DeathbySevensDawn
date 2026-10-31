// Original nine-slice surfaces shared by all runtime buttons.
using UnityEngine;

namespace DeathBySeventhsDawn.Progression
{
    public static class PrototypeButtons
    {
        static Texture2D normal, hover, active;
        static Texture2D Plate(float lift, bool pressed)
        {
            var t = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float grain = ((x * 17 + y * 31) % 13) / 1300f;
                    Color c = new Color(.105f + lift + grain, .095f + lift + grain, .105f + lift + grain);
                    if (x == 0 || y == 0 || x == 31 || y == 31)
                        c = new Color(.035f, .028f, .035f);
                    else if (x == 1 || y == 30)
                        c = pressed ? new Color(.12f, .10f, .11f) : new Color(.55f, .45f, .36f);
                    else if (y == 1 || x == 30)
                        c = new Color(.13f, .11f, .12f);
                    else if (x == 3 || y == 28 || x == 28 || y == 3)
                        c = new Color(.27f + lift, .23f + lift, .22f + lift);
                    t.SetPixel(x, y, c);
                }

            t.Apply();
            return t;
        }

        public static void Apply(GUIStyle style)
        {
            if (style == null)
                return;
            if (normal == null)
            {
                normal = Plate(0, false);
                hover = Plate(.055f, false);
                active = Plate(-.025f, true);
            }

            style.fontSize = 21;
            style.padding = new RectOffset(18, 18, 10, 10);
            style.border = new RectOffset(6, 6, 6, 6);
            style.fixedHeight = 48;
            style.wordWrap = true;
            style.alignment = TextAnchor.MiddleCenter;
            foreach (var s in new[]
            {
                style.normal,
                style.hover,
                style.active,
                style.focused,
                style.onNormal,
                style.onHover,
                style.onActive,
                style.onFocused
            }

            )
            {
                s.textColor = NarrativeUI.Ink;
                s.background = normal;
            }

            style.hover.background = style.focused.background = hover;
            style.active.background = style.onActive.background = active;
        }
    }
}
