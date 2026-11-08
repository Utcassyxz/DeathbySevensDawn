// 职责：可序列化旅程数据；封缸、古神目击和末日标志独立于线索图。
using System;
using System.Collections.Generic;

namespace DeathBySeventhsDawn.Progression
{
    // Durable journey/checkpoint data. GameManager alone resets per-life fields after a real death.
    [Serializable]
    public class ProgressionState
    {
        public int version = 7, loopCount;
        public int sleeplessDays;
        public bool neighborDeathPending,neighborIntroduced,neighborDoorForced,neighborTrusted,neighborHostile,neighborDefeated,neighborDeathLearned,shoulderEyeUnlocked,serviceRouteIsolated;
        public List<string> neighborTakenItems=new List<string>();
        public float neighborHealth=-1;
        public bool neighborGuidanceSeen,neighborHatRevealed;
        public long eyeReadyUtcTicks;
        public string lastDeathCause="";
        public string neighborMembership="",encounterDestination="CityEncounter3D";
        public bool cityCrisisResolved, hasCheckpoint;
        public int savedDay = 1, savedPeriod, deathCrisis, savedFloor = 4;
        public string savedScene = "", cityRegion = "";
        public float savedX, savedY, savedZ, savedVitality = -1;
        public int defenseCharges = 3;
        public string encounterReturnScene="", encounterId="";
        public float encounterReturnX,encounterReturnY;
        public List<string> cityActions = new List<string>();
        public bool deathWasBuilding;
        public bool buildingCrisisResolved, daySevenCrisisResolved;
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
