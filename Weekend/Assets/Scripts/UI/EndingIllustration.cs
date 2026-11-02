using UnityEngine;

namespace DeathBySeventhsDawn.Progression
{
    public static class EndingIllustration
    {
        static Texture2D atlas;
        public static void Draw(Rect area, bool fish, float alpha = 1)
        {
            if (atlas == null)
                atlas = Resources.Load<Texture2D>("Story/EndingDistortedPair");
            if (atlas == null)
                return;
            var color = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.DrawTextureWithTexCoords(area, atlas, new Rect(0, fish ? 0 : .5f, 1, .5f));
            GUI.color = color;
        }
    }
}
