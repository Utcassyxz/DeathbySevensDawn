// 职责：走廊演出的预制体和时长配置。
using UnityEngine;
namespace DeathBySeventhsDawn.Interaction
{
    [CreateAssetMenu(menuName = "Death by Seventh's Dawn/401 first-loop prologue")]
    public sealed class FirstLoopPrologueContent : ScriptableObject
    {
        // 对白与章节文案统一在 Assets/Resources/StoryText.json。
        // 这里只保留 401 邻居的视觉 prefab 配置。
        public GameObject neighborPrefab;
    }
}
