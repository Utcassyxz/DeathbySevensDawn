// Shared framing for spoken lines, captions and tactile menu controls.
using UnityEngine;

namespace DeathBySeventhsDawn.Progression
{
    public static class NarrativeUI
    {
        public static readonly Color Ink = new Color(.92f, .88f, .77f), Accent = new Color(.64f, .46f, .43f);
        public static Rect DialogueRect(float w, float h) => new Rect(Mathf.Max(24, w * .08f), h - 190, w - Mathf.Max(48, w * .16f), 180);
        static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public static void Panel(Rect r)
        {
            Fill(new Rect(r.x + 5, r.y + 6, r.width, r.height), new Color(0, 0, 0, .65f));
            Fill(r, new Color(.34f, .30f, .27f, .99f));
            Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), new Color(.075f, .068f, .075f, .99f));
            Fill(new Rect(r.x + 7, r.y + 7, r.width - 14, 1), new Color(.25f, .21f, .22f));
            Fill(new Rect(r.x + 2, r.y + 2, 3, r.height - 4), Accent);
        }

        public static void Dialogue(Rect r, string speaker, string prose, string hint, GUIStyle body)
        {
            Panel(r);
            var name = new GUIStyle(body)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(),
                alignment = TextAnchor.UpperLeft
            };
            name.normal.textColor = Accent;
            var line = new GUIStyle(body)
            {
                fontSize = 23,
                padding = new RectOffset(),
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            line.normal.textColor = Ink;
            var foot = new GUIStyle(name)
            {
                fontSize = 16,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleRight
            };
            foot.normal.textColor = new Color(.65f, .61f, .55f);
            GUI.Label(new Rect(r.x + 25, r.y + 15, r.width - 50, 26), speaker ?? "", name);
            GUI.Label(new Rect(r.x + 25, r.y + 47, r.width - 50, r.height - 86), prose ?? "", line);
            GUI.Label(new Rect(r.x + 25, r.yMax - 33, r.width - 50, 24), hint ?? "", foot);
        }
    }
}
