// 职责：梦境和望远镜资源配置，并为旧知识编号提供日记描述。
using UnityEngine;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
namespace DeathBySeventhsDawn.Interaction
{
    [CreateAssetMenu(menuName = "Death by Seventh's Dawn/梦境与望远镜剧情", fileName = "StoryContent")]
    public sealed class StoryContent : ScriptableObject
    {
        // 文案统一在 Assets/Resources/StoryText.json（见 StoryText.cs 顶部规则）。
        // 这里只保留图片、知识档位、望远镜位置等不可文本化的配置。
        // 【换古神图片】替换 Assets/Resources/Story/AncientGodDay2.png，保留文件名和 .meta。
        public string ancientGodImageResource = "Story/AncientGodDay2";
        // 【高级知识配置】默认使用已有的宇宙知识档位：知识+30、变异+20，并降低理智。
        public KnowledgeTier ancientGodTier = KnowledgeTier.Cosmic;
        public Vector2 telescopePosition = new Vector2(2.3f, -1.15f);
        [Min(.2f)] public float telescopeRange = 1.2f;
        public const string AncientKnowledgeId = "balcony-telescope-day2-ancient-outline";
        public const string WindowKnowledgeId = "bedroom-window-day2-observation";
        public void Describe(string id, out string title, out string body, out string image)
        {
            var text = StoryText.Current.knowledge;
            image = "";
            if (id == AncientKnowledgeId)
            {
                title = text?.ancientTitle ?? "天穹后的轮廓";
                body = text?.ancientBody ?? "";
                image = ancientGodImageResource;
            }
            else if (id == WindowKnowledgeId)
            {
                title = text?.windowTitle ?? "第二天的窗外观察";
                body = text?.windowBody ?? "";
            }
            else
            {
                title = text?.fallbackTitle ?? "新的发现";
                body = (text?.fallbackBodyPrefix ?? "他记下了一条线索。\n\n线索编号：") + id;
            }
        }
    }
}
