// 职责：用相机自身的纯黑清屏色呈现场景边界外区域；不创建屏幕蒙版或暗角。
using UnityEngine;
namespace DeathBySeventhsDawn.Progression
{
    [DefaultExecutionOrder(1000)]
    public sealed class SceneBackdrop : MonoBehaviour
    {
        void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null)
                return;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }
    }
}
