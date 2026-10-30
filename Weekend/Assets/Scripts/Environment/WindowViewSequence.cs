// 职责：卧室窗外的七日图片资源配置。
using UnityEngine;

namespace DeathBySeventhsDawn.Interaction
{
    [CreateAssetMenu(menuName = "Death by Seventh's Dawn/Window view sequence")]
    public sealed class WindowViewSequence : ScriptableObject
    {
        [Tooltip("Exactly seven textures: element 0 = Day 1, element 6 = Day 7.")]
        [SerializeField] private Texture2D[] dayImages = new Texture2D[7];

        public Texture2D GetImage(int day)
        {
            int index = Mathf.Clamp(day, 1, 7) - 1;
            return dayImages != null && index < dayImages.Length ? dayImages[index] : null;
        }

        public void SetImages(Texture2D[] images)
        {
            if (images == null || images.Length != 7)
                throw new System.ArgumentException("A window sequence requires seven images.");
            dayImages = (Texture2D[])images.Clone();
        }
    }
}
