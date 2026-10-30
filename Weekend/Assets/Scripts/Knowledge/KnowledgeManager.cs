// 职责：知识奖励与观察记录，防止同一知识重复发奖。
using System.Collections.Generic;
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Interaction;
namespace DeathBySeventhsDawn.Knowledge
{
    public class KnowledgeManager : MonoBehaviour
    {
        public static KnowledgeManager Instance
        {
            get; private set;
        }
        StoryContent content;
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            content = Resources.Load<StoryContent>("StoryContent");
            if (content == null)
                content = ScriptableObject.CreateInstance<StoryContent>();
        }
        public bool Unlock(string knowledgeId) => Unlock(knowledgeId, KnowledgeTier.Ordinary);
        public bool Unlock(string id, KnowledgeTier tier)
        {
            content.Describe(id, out string title, out string body, out string image);
            return Grant(id, GameManager.Instance.Settings.Effect(tier), tier, title, body, image);
        }
        public bool Unlock(KnowledgeData data) => data != null && Grant(data.id, data.overrideEffect ? data.effect : GameManager.Instance.Settings.Effect(data.tier), data.tier, data.title, data.description, data.journalImageResource);
        bool Grant(string id, KnowledgeEffect effect, KnowledgeTier tier, string title, string body, string image)
        {
            var game = GameManager.Instance;
            if (game.HasEnded || string.IsNullOrWhiteSpace(id) || effect == null || game.State.knowledgeIds.Contains(id))
                return false;
            game.State.knowledgeIds.Add(id);
            if (game.State.journal == null)
                game.State.journal = new List<JournalEntry>();
            if (!game.State.journalUnlocked)
            {
                if (!game.State.initialEvidenceIds.Contains(id))
                    game.State.initialEvidenceIds.Add(id);
                game.Acquire(effect);
                return true;
            }
            game.State.journal.Add(new JournalEntry { id = id, title = string.IsNullOrEmpty(title) ? DeathBySeventhsDawn.Story.StoryText.UI("新的发现") : title, body = body, imageResource = image, tier = tier, day = TimeSystem.TimeManager.Instance != null ? TimeSystem.TimeManager.Instance.Days : 1, loop = game.LoopCount });
            // 知识、日记和属性变化在同一次存档中写入，重复观察不会重复奖励或重复记日记。
            game.Acquire(game.State.observedKnowledgeIds.Contains(id) ? new KnowledgeEffect(0, 0, 0) : effect);
            JournalUpdates.NotifyAdded(game.State.journal[game.State.journal.Count - 1]);
            return true;
        }
        public bool Observe(string id, KnowledgeTier tier)
        {
            var game = GameManager.Instance;
            if (game.HasEnded || game.State.observedKnowledgeIds.Contains(id) || Has(id))
                return false;
            game.State.observedKnowledgeIds.Add(id);
            game.Acquire(game.Settings.Effect(tier));
            return true;
        }
        public bool Experienced(string id) => Has(id) || GameManager.Instance.State.observedKnowledgeIds.Contains(id);
        public bool Has(string id) => GameManager.Instance.State.knowledgeIds.Contains(id);
        public IReadOnlyCollection<string> GetAll() => GameManager.Instance.State.knowledgeIds.AsReadOnly();
        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
