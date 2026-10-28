using UnityEngine;
using DeathBySeventhsDawn.Progression;
namespace DeathBySeventhsDawn.Knowledge
{
    [CreateAssetMenu(menuName="Death by Seventh's Dawn/Knowledge Data",fileName="KnowledgeData")]
    public class KnowledgeData : ScriptableObject
    {
        public string id, title;
        [Tooltip("日记插图的 Resources 相对路径，不含扩展名，可留空") ] public string journalImageResource;
        [TextArea(2,8)] public string description;
        public KnowledgeTier tier;
        public bool overrideEffect;
        public KnowledgeEffect effect=new KnowledgeEffect(5,0,0);
    }
}