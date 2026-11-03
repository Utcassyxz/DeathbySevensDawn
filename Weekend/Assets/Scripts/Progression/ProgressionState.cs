// 职责：可序列化旅程数据；封缸、古神目击和末日标志独立于线索图。
using System;
using System.Collections.Generic;

namespace DeathBySeventhsDawn.Progression
{
    // Only permanent progress is saved. Loading starts a fresh Day 1 without an aging penalty.
    [Serializable]
    public class ProgressionState
    {
        public int version = 4, loopCount;
        // Persistent unlocks survive a loop; visits and daily story flags do not.
        public bool bubbleDreamUnlocked, fishCompanionUnlocked;
        public bool firstApocalypsePending, firstApocalypseExplained;
        public int bubbleVisitLoop = -1, fishCompanionUnlockLoop;
        public int fishMorningLoop = -1;
        public bool fishStoolBorrowed;
        public List<int> fishStoryDays = new List<int>();
        public int ancientObservationDay, ancientObservationLoop = -1;
        public bool bubblePortraitUnlocked, bubbleIntroduced, bedroomCurtainsClosed, fishAlly, fishReportAvailable;
        public int fishUnderstanding;
        public List<string> bubblePurchases = new List<string>();
        public bool badEndingOne, dreamSeen;
        public bool worldEndEnding, ancientGodSeen, aquariumSealed, yogDreamSeen, fishReplacementEnding;
        public bool firstNightWatchComplete;
        public bool stoveLit;
        public bool breadEaten, telescopeCarried, telescopeOnRoof, firstNightObjectSeen;
        public bool neighborFlyersSeen, skt079NewsSeen, neighborBarricadeSeen, prologueEntered;
        public bool dayOneSupernovaSeen;
        public int morningStage;
        public bool fridgeOpened, fridgeDoorOpen, dayOneNewsStarted, journalUnlocked, investigationUnlocked, tutorialComplete;
        public int newsPage;
        public List<string> initialEvidenceIds = new List<string>();
        public bool cultDayOneTalked, cultDayOneStarted, cultTokenOffered;
        public List<string> inventory = new List<string>();
        public List<Knowledge.JournalEntry> journal = new List<Knowledge.JournalEntry>();
        public float knowledge, mutation, sanityBurden;
        public List<string> knowledgeIds = new List<string>();
        public List<string> observedKnowledgeIds = new List<string>();
        public List<Knowledge.InvestigationLink> investigationLinks = new List<Knowledge.InvestigationLink>();
    }
}
