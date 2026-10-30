// 职责：统一 IMGUI 按钮的文字和交互外观。
using UnityEngine;
namespace DeathBySeventhsDawn.Progression
{
    public static class PrototypeButtons
    {
        static Texture2D normal, hover, active;
        static Texture2D Pixel(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
        public static void Apply(GUIStyle style)
        {
            if (style == null)
                return;
            if (normal == null)
            {
                normal = Pixel(new Color(.10f, .15f, .15f));
                hover = Pixel(new Color(.18f, .25f, .23f));
                active = Pixel(new Color(.07f, .10f, .10f));
            }
            style.fontSize = 20;
            style.padding = new RectOffset(14, 14, 8, 8);
            style.fixedHeight = 44;
            style.wordWrap = true;
            foreach (var s in new[] { style.normal, style.hover, style.active, style.focused, style.onNormal, style.onHover, style.onActive, style.onFocused })
            {
                s.textColor = new Color(.9f, .88f, .79f);
                s.background = normal;
            }
            style.hover.background = hover;
            style.active.background = active;
        }
    }
}
