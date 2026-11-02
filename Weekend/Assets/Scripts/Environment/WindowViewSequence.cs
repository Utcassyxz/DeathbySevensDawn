// 职责：卧室窗外的七日图片资源配置。
using UnityEngine;

namespace DeathBySeventhsDawn.Interaction
{
    [CreateAssetMenu(menuName = "Death by Seventh's Dawn/Window view sequence")]
    public sealed class WindowViewSequence : ScriptableObject
    {
        [Tooltip("Exactly seven textures: element 0 = Day 1, element 6 = Day 7.")]
        [SerializeField]
        private Texture2D[] dayImages = new Texture2D[7];
        [SerializeField]
        bool dynamicRadiationView = true;
        public Texture2D GetImage(int day)
        {
            if (!dynamicRadiationView)
            {
                int index = Mathf.Clamp(day, 1, 7) - 1;
                return dayImages != null && index < dayImages.Length ? dayImages[index] : null;
            }

            return SevenDayExterior.View(day, DeathBySeventhsDawn.TimeSystem.TimeManager.Instance != null ? DeathBySeventhsDawn.TimeSystem.TimeManager.Instance.Period : DeathBySeventhsDawn.TimeSystem.TimePeriod.Morning);
        }

        public void SetImages(Texture2D[] images)
        {
            if (images == null || images.Length != 7)
                throw new System.ArgumentException("A window sequence requires seven images.");
            dayImages = (Texture2D[])images.Clone();
            dynamicRadiationView = false;
        }
    }
}
