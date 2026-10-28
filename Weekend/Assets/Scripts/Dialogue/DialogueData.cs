using System;
using UnityEngine;

namespace DeathBySeventhsDawn.Dialogue
{
    [Serializable]
    public class DialogueLine
    {
        public string speaker;
        [TextArea(2, 6)] public string text;
        public string requiredKnowledgeId;
    }

    [CreateAssetMenu(menuName = "Death by Seventh's Dawn/Dialogue Data", fileName = "DialogueData")]
    public class DialogueData : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}
