// 职责：阳台时段图片配置。
using UnityEngine;
namespace DeathBySeventhsDawn.Interaction
{
    [CreateAssetMenu(menuName = "Death by Seventh's Dawn/阳台外景配置", fileName = "BalconyViewSettings")]
    public sealed class BalconyViewSettings : ScriptableObject
    {
        // 【改颜色看这里】四个元素依次是：早晨、下午、傍晚、深夜。
        // 第一天天空偏蓝，第七天偏灰紫；中间天数自动插值，无须填写 28 组颜色。
        [Tooltip("第1天：早晨、下午、傍晚、深夜的天空颜色")]
        public Color[] firstDaySky = { new Color(.58f, .74f, .82f), new Color(.35f, .62f, .82f), new Color(.70f, .40f, .34f), new Color(.07f, .10f, .19f) };
        [Tooltip("第7天：早晨、下午、傍晚、深夜的天空颜色")]
        public Color[] seventhDaySky = { new Color(.35f, .38f, .41f), new Color(.34f, .33f, .40f), new Color(.38f, .20f, .27f), new Color(.07f, .035f, .10f) };
        [Tooltip("第1天远楼颜色")]
        public Color firstDayBuildings = new Color(.32f, .41f, .48f);
        [Tooltip("第7天远楼颜色")]
        public Color seventhDayBuildings = new Color(.16f, .12f, .19f);
        [Tooltip("远楼各时段亮度：早晨、下午、傍晚、深夜")]
        public float[] buildingBrightness = { .85f, 1f, .65f, .35f };
        public Color litWindows = new Color(1f, .76f, .35f), darkWindows = new Color(.20f, .25f, .29f);

        // 【以后换照片看这里】照片导入 Unity 后，Texture Type 设为 Sprite (2D and UI)，点 Apply。
        // 在 Resources/BalconyViewSettings 的此数组中拖入照片：Element 0=第1天，...，Element 6=第7天。
        // 某一天留空就继续用当天的颜色占位；不会因为缺照片报错。
        [Tooltip("7张外景照片，数组0至6对应第1至7天；留空保留远楼占位")]
        public Sprite[] dayPhotos = new Sprite[7];
        [Tooltip("勾选后照片也受当前天空颜色影响；照片自身已有日夜光线时建议关闭")]
        public bool tintPhotosWithTime;
        // 默认照片等比放进现有远楼区域，不改栏杆、地板和碰撞。
        // 要改变构图，在 Inspector 调 photoOffset；要放大/缩小，调 photoAreaScale。
        public Vector2 photoOffset = Vector2.zero;
        [Min(.1f)] public float photoAreaScale = 1f;

        public Color Sky(int day, int period)
        {
            int p = Mathf.Clamp(period, 0, 3);
            Color from = firstDaySky != null && firstDaySky.Length > p ? firstDaySky[p] : new Color(.4f, .5f, .6f);
            Color to = seventhDaySky != null && seventhDaySky.Length > p ? seventhDaySky[p] : new Color(.15f, .12f, .2f);
            return Color.Lerp(from, to, Mathf.Clamp01((day - 1) / 6f));
        }
        public float Brightness(int period) => buildingBrightness != null && buildingBrightness.Length > Mathf.Clamp(period, 0, 3) ? Mathf.Max(0, buildingBrightness[Mathf.Clamp(period, 0, 3)]) : 1f;
        public Sprite Photo(int day) => dayPhotos != null && dayPhotos.Length >= Mathf.Clamp(day, 1, 7) ? dayPhotos[Mathf.Clamp(day, 1, 7) - 1] : null;
    }
}
