using System.Collections.Generic;
using UnityEngine;

namespace DeathBySeventhsDawn.Knowledge
{
    public class KnowledgeManager : MonoBehaviour
    {
        public static KnowledgeManager Instance { get; private set; }

        private readonly HashSet<string> unlockedKnowledge = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool Unlock(string knowledgeId)
        {
            if (string.IsNullOrWhiteSpace(knowledgeId))
                return false;

            return unlockedKnowledge.Add(knowledgeId);
        }

        public bool Has(string knowledgeId)
        {
            return unlockedKnowledge.Contains(knowledgeId);
        }

        public IReadOnlyCollection<string> GetAll()
        {
            return unlockedKnowledge;
        }
    }
}
