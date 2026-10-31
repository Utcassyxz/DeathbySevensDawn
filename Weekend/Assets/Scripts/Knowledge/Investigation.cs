// 职责：线索目录、待证关系、推理确认及旧存档规范化。
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Knowledge
{
    [Serializable]
    public sealed class EvidenceDefinition
    {
        public string id, title, body, source, image;
    }

    [Serializable]
    public sealed class DeductionDefinition
    {
        public string id, left, right, relation, title, body;
    }

    [Serializable]
    public sealed class InvestigationLink
    {
        public string left, right, relation, conclusion;
    }

    [Serializable]
    public sealed class InvestigationCatalog
    {
        public EvidenceDefinition[] evidence;
        public DeductionDefinition[] deductions;
    }

    // The catalog describes possibilities. Only explicitly collected entries are visible to the player.
    public static class Investigation
    {
        static InvestigationCatalog catalog;
        public static void ReloadLanguage()
        {
            catalog = null;
        }

        public static string LocalTitle(string id, string fallback)
        {
            if (id == "prologue-entry")
                return StoryText.Current.firstLoop.chapterTitle;
            if (id == "first-evening-journal")
                return StoryText.Current.morning.journalTitle;
            var e = Evidence(id);
            if (e != null)
                return e.title;
            var d = Array.Find(Catalog.deductions, x => x.id == id);
            if (d != null)
                return d.title;
            var s = StoryText.Current;
            if (id == "opening-dream")
                return s.dream.journalTitle;
            if (id.Contains("broadcast"))
                return s.dayOneBroadcast.journalTitle;
            return fallback;
        }

        public static string LocalBody(string id, string fallback)
        {
            if (id == "prologue-entry")
                return StoryText.Current.firstLoop.chapterText;
            if (id == "first-evening-journal")
                return StoryText.Current.morning.journalBody;
            var e = Evidence(id);
            if (e != null)
                return e.body;
            var d = Array.Find(Catalog.deductions, x => x.id == id);
            if (d != null)
                return d.body;
            var s = StoryText.Current;
            if (id == "opening-dream")
                return s.dream.journalBody;
            return fallback;
        }

        public static InvestigationCatalog Catalog
        {
            get
            {
                if (catalog != null)
                    return catalog;
                var asset = Resources.Load<TextAsset>("Investigation");
                catalog = asset != null ? JsonUtility.FromJson<InvestigationCatalog>(asset.text) : new InvestigationCatalog();
                if (catalog == null)
                    catalog = new InvestigationCatalog();
                ApplyStoryText(catalog);
                return catalog;
            }
        }

        static void ApplyStoryText(InvestigationCatalog target)
        {
            // 文案统一放在 Resources/StoryText.json；Investigation.json 只保留结构 id。
            var text = StoryText.Current.investigation;
            if (text == null)
                return;
            if (target.evidence != null && text.evidence != null)
                foreach (var entry in target.evidence)
                {
                    if (entry == null)
                        continue;
                    var copy = Array.Find(text.evidence, e => e != null && e.id == entry.id);
                    if (copy == null)
                        continue;
                    entry.title = copy.title;
                    entry.body = copy.body;
                    entry.source = copy.source;
                }

            if (target.deductions != null && text.deductions != null)
                foreach (var entry in target.deductions)
                {
                    if (entry == null)
                        continue;
                    var copy = Array.Find(text.deductions, d => d != null && d.id == entry.id);
                    if (copy == null)
                        continue;
                    entry.relation = copy.relation;
                    entry.title = copy.title;
                    entry.body = copy.body;
                }
        }

        static readonly string[] FallbackRelations =
        {
            "习惯与身份",
            "观察工具",
            "时间关联",
            "行为动机",
            "交叉印证"
        };
        static readonly string[] EnglishRelations =
        {
            "Habit and identity",
            "Observation tool",
            "Time connection",
            "Behavior and motive",
            "Cross-check"
        };
        // Persist the original relation keys; UI language must never change graph identity.
        static string RelationKey(string value)
        {
            int index = Array.IndexOf(EnglishRelations, value);
            return index >= 0 ? FallbackRelations[index] : value;
        }

        public static string RelationLabel(string value)
        {
            int index = Array.IndexOf(FallbackRelations, RelationKey(value));
            return index >= 0 ? Relations[index] : value;
        }

        public static string[] Relations
        {
            get
            {
                var relations = StoryText.Current.investigation?.relations;
                return relations != null && relations.Length > 0 ? relations : FallbackRelations;
            }
        }

        static ProgressionState State => GameManager.Instance.State;

        public static void Normalize(ProgressionState state)
        {
            if (state.version == 1)
            {
                if (state.journal != null && state.journal.Count > 0)
                {
                    state.journalUnlocked = state.investigationUnlocked = state.tutorialComplete = true;
                    state.morningStage = 10;
                }

                state.version = 2;
            }

            if (!state.tutorialComplete && state.morningStage == 4 && state.dayOneSupernovaSeen)
                state.morningStage = 5;
            if (state.initialEvidenceIds == null)
                state.initialEvidenceIds = new List<string>();
            if (state.inventory == null)
                state.inventory = new List<string>();
            if (state.journal == null)
                state.journal = new List<JournalEntry>();
            if (state.observedKnowledgeIds == null)
                state.observedKnowledgeIds = new List<string>();
            if (state.investigationLinks == null)
                state.investigationLinks = new List<InvestigationLink>();
            // Retire redundant identity deductions without removing collected facts.
            var retired = new[]
            {
                "deduction-astronomy",
                "deduction-deliberate",
                "deduction-book-reader"
            };
            state.journal.RemoveAll(e => e != null && Array.IndexOf(retired, e.id) >= 0);
            state.knowledgeIds.RemoveAll(id => Array.IndexOf(retired, id) >= 0);
            state.investigationLinks.RemoveAll(l => l == null || Array.IndexOf(retired, l.left) >= 0 || Array.IndexOf(retired, l.right) >= 0 || Array.IndexOf(retired, l.conclusion) >= 0);
        }

        public static EvidenceDefinition Evidence(string id) => Array.Find(Catalog.evidence, e => e.id == id);
        public static bool Has(string id) => State.journal.Exists(e => e != null && e.id == id);
        public static bool IsNode(string id) => Evidence(id) != null || Array.Exists(Catalog.deductions, d => d.id == id);
        public static List<JournalEntry> Nodes() => State.journal.Where(e => e != null && IsNode(e.id)).ToList();
        public static bool Collect(string id)
        {
            var definition = Evidence(id);
            if (!State.investigationUnlocked || definition == null || Has(id) || GameManager.Instance.HasEnded)
                return false;
            var entry = new JournalEntry
            {
                id = id,
                title = definition.title,
                body = definition.body,
                imageResource = definition.image,
                isKnowledge = true,
                day = TimeManager.Instance.Days,
                loop = GameManager.Instance.LoopCount
            };
            // Collecting evidence never adds a second cosmic exposure reward.
            State.journal.Add(entry);
            if (!State.knowledgeIds.Contains(id))
                State.knowledgeIds.Add(id);
            ProgressionSave.Write(State);
            JournalUpdates.NotifyAdded(entry);
            return true;
        }

        public static DeductionDefinition Match(string left, string right, string relation)
        {
            if (left == right || !Has(left) || !Has(right))
                return null;
            return Array.Find(Catalog.deductions, d => (d.id != "deduction-warning" || State.neighborFlyersSeen) && RelationKey(d.relation) == RelationKey(relation) && ((d.left == left && d.right == right) || (d.left == right && d.right == left)));
        }

        public static DeductionDefinition Match(string left, string right)
        {
            if (left == right || !Has(left) || !Has(right))
                return null;
            return Array.Find(Catalog.deductions, d => (d.id != "deduction-warning" || State.neighborFlyersSeen) && ((d.left == left && d.right == right) || (d.left == right && d.right == left)));
        }

        static bool SamePair(InvestigationLink link, string a, string b) => (link.left == a && link.right == b) || (link.left == b && link.right == a);
        public static InvestigationLink FindLink(string a, string b, string relation) => State.investigationLinks.Find(l => l != null && RelationKey(l.relation) == RelationKey(relation) && SamePair(l, a, b));
        public static InvestigationLink FindLink(string a, string b)
        {
            var links = State.investigationLinks;
            return links.Find(l => l != null && !string.IsNullOrEmpty(l.conclusion) && SamePair(l, a, b)) ?? links.Find(l => l != null && SamePair(l, a, b));
        }

        public static bool Connect(string left, string right, string relation, bool writeConclusion)
        {
            if (!State.investigationUnlocked || GameManager.Instance.HasEnded || left == right || !IsNode(left) || !IsNode(right) || !Has(left) || !Has(right) || !FallbackRelations.Contains(RelationKey(relation)))
                return false;
            relation = RelationKey(relation);
            var match = Match(left, right, relation);
            if (writeConclusion && match == null)
                return false;
            var link = FindLink(left, right, relation);
            if (link != null && (!writeConclusion || !string.IsNullOrEmpty(link.conclusion)))
                return false;
            if (link == null)
            {
                link = new InvestigationLink
                {
                    left = left,
                    right = right,
                    relation = relation
                };
                State.investigationLinks.Add(link);
            }

            JournalEntry added = null;
            if (writeConclusion)
            {
                link.conclusion = match.id;
                if (!Has(match.id))
                {
                    added = new JournalEntry
                    {
                        id = match.id,
                        title = match.title,
                        body = match.body + DeathBySeventhsDawn.Story.StoryText.UI("\n\n依据：") + Title(left) + " ＋ " + Title(right) + DeathBySeventhsDawn.Story.StoryText.UI("\n关系：") + RelationLabel(relation),
                        isKnowledge = true,
                        day = TimeManager.Instance.Days,
                        loop = GameManager.Instance.LoopCount
                    };
                    State.journal.Add(added);
                }
            }

            ProgressionSave.Write(State);
            if (added != null)
                JournalUpdates.NotifyAdded(added);
            return true;
        }

        public static bool Connect(string left, string right, bool writeConclusion)
        {
            if (!State.investigationUnlocked || GameManager.Instance.HasEnded || left == right || !IsNode(left) || !IsNode(right) || !Has(left) || !Has(right))
                return false;
            var match = writeConclusion ? Match(left, right) : null;
            if (writeConclusion && match == null)
                return false;
            var link = FindLink(left, right);
            if (link != null && (!writeConclusion || !string.IsNullOrEmpty(link.conclusion)))
                return false;
            if (link == null)
            {
                link = new InvestigationLink
                {
                    left = left,
                    right = right,
                    relation = RelationKey(Relations[0])
                };
                State.investigationLinks.Add(link);
            }

            JournalEntry added = null;
            if (writeConclusion)
            {
                link.relation = RelationKey(match.relation);
                link.conclusion = match.id;
                // Old saves may contain extra tentative lines for this pair; keep only the one being concluded.
                for (int i = State.investigationLinks.Count - 1; i >= 0; i--)
                {
                    var other = State.investigationLinks[i];
                    if (other != null && other != link && SamePair(other, left, right) && string.IsNullOrEmpty(other.conclusion))
                        State.investigationLinks.RemoveAt(i);
                }

                if (!Has(match.id))
                {
                    added = new JournalEntry
                    {
                        id = match.id,
                        title = match.title,
                        body = match.body + DeathBySeventhsDawn.Story.StoryText.UI("\n\n依据：") + Title(left) + " ＋ " + Title(right) + DeathBySeventhsDawn.Story.StoryText.UI("\n关系：") + RelationLabel(match.relation),
                        isKnowledge = true,
                        day = TimeManager.Instance.Days,
                        loop = GameManager.Instance.LoopCount
                    };
                    State.journal.Add(added);
                }
            }

            ProgressionSave.Write(State);
            if (added != null)
                JournalUpdates.NotifyAdded(added);
            return true;
        }

        public static bool RemoveTentative(string left, string right, string relation)
        {
            var link = FindLink(left, right, relation);
            if (link == null || !string.IsNullOrEmpty(link.conclusion))
                return false;
            State.investigationLinks.Remove(link);
            ProgressionSave.Write(State);
            return true;
        }

        public static bool RemoveTentative(string left, string right)
        {
            bool removed = false;
            for (int i = State.investigationLinks.Count - 1; i >= 0; i--)
            {
                var link = State.investigationLinks[i];
                if (link != null && SamePair(link, left, right) && string.IsNullOrEmpty(link.conclusion))
                {
                    State.investigationLinks.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
                ProgressionSave.Write(State);
            return removed;
        }

        public static bool CanCombine(string first, string second)
        {
            var match = Match(first, second);
            return match != null && !Has(match.id);
        }

        public static string Title(string id) => LocalTitle(id, State.journal.Find(e => e != null && e.id == id)?.title ?? StoryText.Local("未收录", "Not recorded"));
        public static IReadOnlyList<InvestigationLink> Links => State.investigationLinks;
    }
}
