// 职责：场景切换/梦境醒来时的全屏黑场，避免加载帧露出房间。
using UnityEngine;
namespace DeathBySeventhsDawn.Progression
{
    [DefaultExecutionOrder(3000)]
    public sealed class StoryTransition : MonoBehaviour
    {
        static StoryTransition instance;
        float until, fade = .35f;
        void Awake() { instance = this; }
        void OnDestroy() { if (instance == this) instance = null; }
        public static void Cover(float seconds, float fadeSeconds = .35f)
        {
            if (instance == null)
                return;
            instance.until = Mathf.Max(instance.until, Time.unscaledTime + seconds);
            instance.fade = Mathf.Max(.01f, fadeSeconds);
        }
        void OnGUI()
        {
            if (Time.unscaledTime >= until)
                return;
            float alpha = Mathf.Clamp01((until - Time.unscaledTime) / fade);
            if (alpha <= 0)
                return;
            int depth = GUI.depth;
            var color = GUI.color;
            GUI.depth = -19;
            GUI.color = new Color(0, 0, 0, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = color;
            GUI.depth = depth;
        }
    }
}
