// 职责：剧情推进只使用 Enter；世界物件使用 E。
using UnityEngine.InputSystem;

namespace DeathBySeventhsDawn.Progression
{
    public static class StoryControls
    {
        public static bool ContinuePressed => Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
        public static bool UsePressed => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }
}
