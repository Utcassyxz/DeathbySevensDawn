using System;
using System.Collections.Generic;
namespace DeathBySeventhsDawn.Progression
{
    // Only permanent progress is saved. Loading starts a fresh Day 1 without an aging penalty.
    [Serializable] public class ProgressionState
    {
        public int version=1, loopCount;
        public bool badEndingOne, dreamSeen;
        public bool neighborFlyersSeen, skt079NewsSeen, neighborBarricadeSeen, prologueEntered;
        public List<Knowledge.JournalEntry> journal=new List<Knowledge.JournalEntry>();
        public float knowledge, mutation, sanityBurden;
        public List<string> knowledgeIds=new List<string>();
        public List<string> observedKnowledgeIds=new List<string>();
        public List<Knowledge.InvestigationLink> investigationLinks=new List<Knowledge.InvestigationLink>();
    }
}