// 职责：梦境的缓慢显隐与不对称眼睑曲线，字幕保持稳定。
using UnityEngine;

namespace DeathBySeventhsDawn.Progression
{
    // Asymmetric, chapter-relative fades: no looping sine-wave blackouts or text flashing.
    public static class DreamMotion
    {
        public static float Opacity(float age, int chapter)
        {
            float arrival = Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / 1.1f));
            float breath = .78f + .08f * Mathf.PerlinNoise(chapter * 3.7f, age * .19f);
            float dip = 0;
            foreach (float at in new[]
            {
                2.65f + chapter * .31f,
                7.4f + chapter * .47f
            }

            )
            {
                float d = age - at;
                if (d >= 0 && d < 1.15f)
                    dip = Mathf.Max(dip, .2f * Mathf.Sin(d / 1.15f * Mathf.PI));
            }

            return arrival * Mathf.Clamp01(breath - dip);
        }

        public static float WakeOpenness(float age)
        {
            if (age < .65f)
                return 0;
            if (age < 1.03f)
                return Mathf.SmoothStep(0, .32f, (age - .65f) / .38f);
            if (age < 1.18f)
                return Mathf.SmoothStep(.32f, .03f, (age - 1.03f) / .15f);
            if (age < 2.08f)
                return Mathf.SmoothStep(.03f, .82f, (age - 1.18f) / .9f);
            if (age < 2.27f)
                return Mathf.SmoothStep(.82f, .53f, (age - 2.08f) / .19f);
            return Mathf.SmoothStep(.53f, 1, Mathf.Clamp01((age - 2.27f) / .8f));
        }

        // One screen-shaking impulse per kick, triggered by input rather than a timer.
        public static Vector2 KickPulse(float since)
        {
            if (since < 0 || since > .30f)
                return Vector2.zero;
            float strength = 1 - since / .30f;
            return new Vector2(Mathf.Sin(since * 110f) * 12f, Mathf.Cos(since * 83f) * 8f) * strength;
        }

        public static void Eyelids(float w, float h, float openness)
        {
            var old = GUI.color;
            GUI.color = Color.black;
            // Curved lid silhouettes leave the center open first and lift at different speeds.
            const int strips = 48;
            for (int i = 0; i < strips; i++)
            {
                float x = (i + .5f) / strips * 2 - 1;
                float gap = openness >= .999f ? h : openness * h * (1 - .3f * x * x);
                float top = (h - gap) * .48f;
                GUI.DrawTexture(new Rect(i * w / strips, 0, w / strips + 1, top), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(i * w / strips, top + gap, w / strips + 1, h - top - gap), Texture2D.whiteTexture);
            }

            GUI.color = old;
        }
    }
}
