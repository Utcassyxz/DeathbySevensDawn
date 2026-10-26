using UnityEngine;

namespace DeathBySeventhsDawn.Knowledge
{
    [CreateAssetMenu(menuName = "Death by Seventh's Dawn/Knowledge Data", fileName = "KnowledgeData")]
    public class KnowledgeData : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea(2, 8)] public string description;
    }
}
