// CORE CONTRACT: 文案任务不要改动控制流程；工程变更先读 docs/CORE_CODE_MAP.md 并同步验收。
// 职责：旅程状态入口：管理知识、变异、体力及两个互斥的第七天结局。
using System;
using UnityEngine;
using DeathBySeventhsDawn.Progression;

namespace DeathBySeventhsDawn.Core
{
    public enum SanityLayer
    {
        Stable,
        Unstable,
        Fractured,
        Revelation
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance
        {
            get; private set;
        }
        public ProgressionSettings Settings
        {
            get; private set;
        }
        internal ProgressionState State
        {
            get; private set;
        }
        public int LoopCount => State.loopCount;
        public bool HasEnded => State.stageOneComplete || State.badEndingOne || State.worldEndEnding || State.fishReplacementEnding;
        public bool HasSeenAncientGod => State.ancientGodSeen;
        public bool HasLoopAbility => State.yogDreamSeen;
        public bool FishReplacementEnding => State.fishReplacementEnding;
        public bool AquariumSealed => State.aquariumSealed;
        public bool WorldEndEnding => State.worldEndEnding;

        // Only the physical rooftop encounter calls this; old balcony evidence is not a substitute.
        public bool RecordAncientGodEncounter()
        {
            if (HasEnded)
                return false;
            bool first = !State.ancientGodSeen;
            State.ancientGodSeen = State.bubbleDreamUnlocked = true;
            State.ancientObservationDay = TimeSystem.TimeManager.Instance.Days;
            State.ancientObservationLoop = LoopCount;
            ProgressionSave.Write(State);
            Changed?.Invoke();
            return first;
        }

        // CORE FLOW: sighting is evidence; only the completed Bubble encounter grants return.
        public bool GrantLoopAbility()
        {
            if (HasEnded || !State.ancientGodSeen || State.yogDreamSeen)
                return false;
            State.yogDreamSeen = true;
            State.bubbleIntroduced = State.bubblePortraitUnlocked = true;
            ProgressionSave.Write(State);
            Changed?.Invoke();
            return true;
        }

        public bool TriggerReplacementEnding()
        {
            if (HasEnded || AquariumSealed || State.fishAlly || TimeSystem.TimeManager.Instance.Days != 7)
                return false;
            State.fishReplacementEnding = true;
            ProgressionSave.Write(State);
            EndingCollection.Unlock("fish-replacement");
            EndingTriggered?.Invoke("fish-replacement");
            Changed?.Invoke();
            return true;
        }

        public event Action<string> EndingTriggered;
        // Unawakened Hugo gets all seven days; the last-night boundary resolves world destruction.
        // Awakened aging remains a separate ending, and journal evidence cannot grant awakening.
        public bool TryTriggerBadEnding(int day)
        {
            if (HasEnded)
                return true;
            if (day < 7)
                return false;
            if (!HasLoopAbility)
                return false;
            if (MaxVitality > Settings.minimumVitality + .001f)
                return false;
            State.badEndingOne = true;
            ProgressionSave.Write(State);
            EndingCollection.Unlock("bad-ending-1");
            EndingTriggered?.Invoke("bad-ending-1");
            Changed?.Invoke();
            return true;
        }

        public event Action JourneyReset;
        public void StartNewJourney()
        {
            StoryTransition.Cover(1.2f);
            EndingCollectionUI.ResetResult();
            State = new ProgressionState();
            Vitality = MaxVitality;
            State.hasCheckpoint=true;State.savedDay=1;State.savedPeriod=0;State.savedScene="Bedroom";State.savedVitality=Vitality;
            ProgressionSave.Write(State,false);
            TimeSystem.TimeManager.Instance.ResetToDayOne();
            World.RoomDoor2D.ClearPendingTravel();
            JourneyReset?.Invoke();
            Changed?.Invoke();
            if (UnityEngine.Application.CanStreamedLevelBeLoaded("Bedroom"))
                UnityEngine.SceneManagement.SceneManager.LoadScene("Bedroom");
        }

        public float Knowledge => State.knowledge;
        public float Mutation => State.mutation;
        public float MaxVitality => Mathf.Max(Settings.minimumVitality, Settings.initialVitality - LoopCount * Settings.agingPerLoop);
        public float Vitality
        {
            get; private set;
        }
        public float Sanity => StoryDebugActions.Enabled&&NeighborDebugCommands.SanityOverride>=0?NeighborDebugCommands.SanityOverride:Mathf.Clamp(Settings.maximumSanity - State.sanityBurden - Knowledge * Settings.knowledgeSanityCost - Mutation * Settings.mutationSanityCost, 0, Settings.maximumSanity);
        public int KnowledgeLevel => Mathf.FloorToInt(Knowledge / Mathf.Max(1, Settings.knowledgePerLevel));
        public SanityLayer Layer => Sanity <= Settings.revelationSanity ? SanityLayer.Revelation : Sanity <= Settings.fracturedSanity ? SanityLayer.Fractured : Sanity <= Settings.unstableSanity ? SanityLayer.Unstable : SanityLayer.Stable;

        public event Action Changed;
        public event Action<string> AbilityUnlocked;
        public event Action<SanityLayer> SanityLayerChanged;
        public event Action<float> MutationChanged; // NPC reactions subscribe; evaluate CanUseHumanRoute on entry too.
        public event Action Died;
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Settings = Resources.Load<ProgressionSettings>("SevenDayProgression");
            if (Settings == null)
                Settings = ScriptableObject.CreateInstance<ProgressionSettings>();
            State = ProgressionSave.Load();
            DeathBySeventhsDawn.Knowledge.Investigation.Normalize(State);
            if (State.worldEndEnding)
                EndingCollection.Unlock(State.lastDeathCause=="neighbor"?"neighbor-killed":"world-end", false);
            if (State.badEndingOne)
                EndingCollection.Unlock("bad-ending-1", false);
            if (State.fishReplacementEnding)
                EndingCollection.Unlock("fish-replacement", false);
            Vitality = State.hasCheckpoint && State.savedVitality > 0 ? Mathf.Min(MaxVitality,State.savedVitality) : MaxVitality;
        }

        public bool HasAbility(string id)
        {
            if(id=="shoulder_eye")return State.shoulderEyeUnlocked&&(LoopCount>0||StoryDebugActions.Enabled);
            foreach (var item in Settings.abilities)
                if (item != null && item.id == id && Mutation >= item.mutation)
                    return true;
            return false;
        }

        public bool CanUseHumanRoute(float maximumMutation) => Mutation < maximumMutation;
        public bool MeetsKnowledgeRequirement(float required) => Knowledge >= required;
        internal void Acquire(KnowledgeEffect effect)
        {
            if (HasEnded)
                return;
            float oldMutation = Mutation;
            var oldLayer = Layer;
            State.knowledge += Mathf.Max(0, effect.knowledge);
            State.mutation += Mathf.Max(0, effect.mutation);
            State.sanityBurden += Mathf.Max(0, effect.sanityCost);
            ProgressionSave.Write(State);
            foreach (var item in Settings.abilities)
                if (item != null && oldMutation < item.mutation && Mutation >= item.mutation)
                    AbilityUnlocked?.Invoke(item.id);
            if (oldMutation != Mutation)
                MutationChanged?.Invoke(Mutation);
            if (oldLayer != Layer)
                SanityLayerChanged?.Invoke(Layer);
            Changed?.Invoke();
        }

        // Total SAN price includes the SAN cost already implied by gaining knowledge.
        public bool ExchangeSanity(string id, float totalCost, float gain)
        {
            if (HasEnded || string.IsNullOrWhiteSpace(id) || float.IsNaN(totalCost) || float.IsInfinity(totalCost) || float.IsNaN(gain) || float.IsInfinity(gain) || totalCost <= 0 || gain <= 0)
                return false;
            if (State.bubblePurchases == null)
                State.bubblePurchases = new System.Collections.Generic.List<string>();
            float implicitCost = gain * Settings.knowledgeSanityCost;
            if (State.bubblePurchases.Contains(id) || totalCost < implicitCost || Sanity <= totalCost)
                return false;
            var previous = Layer;
            State.knowledge += gain;
            State.sanityBurden += totalCost - implicitCost;
            State.bubblePurchases.Add(id);
            ProgressionSave.Write(State);
            if (previous != Layer)
                SanityLayerChanged?.Invoke(Layer);
            Changed?.Invoke();
            return true;
        }

        public void RegisterNewLoop()
        {
            if (HasEnded)
                return;
            State.loopCount++;
            State.neighborNightBeat=State.neighborNightCaughtCount=State.stageReturnDay=0;
            State.neighborNightRuptureSeen=State.neighborNightResolved=State.neighborNightLocked=State.neighborEyeTwitchSeen=State.neighborNightControlsSeen=State.nightSleepRequested=false;
            State.day7BombDefused=State.stageOneComplete=State.neighborBombArmed=false;State.stageEvents.Clear();
            if(State.neighborDeathLearned && State.mutation>=20)State.shoulderEyeUnlocked=true;
            State.neighborDeathPending=State.neighborIntroduced=State.neighborDoorForced=State.neighborTrusted=State.neighborHostile=State.neighborDefeated=State.serviceRouteIsolated=false;
            State.neighborGuidanceSeen=State.neighborHatRevealed=State.neighborPlotHeard=State.neighborCabinetOpened=false;
            State.neighborTakenItems.Clear();State.neighborMembership="";State.neighborHealth=-1;State.lastDeathCause="";State.encounterDestination="CityEncounter3D";
            State.ancientObservationDay = 0;
            State.ancientObservationLoop = -1;
            State.bedroomCurtainsClosed = State.stoveLit = false;
            State.fishUnderstanding = 0;
            State.fishAlly = State.fishCompanionUnlocked = false;
            State.fishReportAvailable = false;
            State.fishStoryDays.Clear();
            State.fishStoolBorrowed = false;
            State.fishMorningLoop = -1;
            State.aquariumSealed = false;
            State.sleeplessDays = 0;
            State.buildingCrisisResolved = State.cityCrisisResolved = State.daySevenCrisisResolved = false;
            State.cityActions.Clear(); State.defenseCharges = 3; State.cityRegion = "";
            State.encounterReturnScene=State.encounterId="";
            State.savedScene = "LivingRoom"; State.savedX=State.savedY=State.savedZ=0; State.hasCheckpoint=false;
            State.cultTokenOffered = false;
            State.inventory.Clear();
            Vitality = MaxVitality;
            State.hasCheckpoint=true;State.savedDay=1;State.savedPeriod=0;State.savedVitality=Vitality;
            ProgressionSave.Write(State,false);
            Changed?.Invoke();
        }

        internal void RestoreStageLoop(int day)
        {
            State.loopCount++;if(day==7)State.day7LoopCount++;
            State.stageReturnDay=0;State.stageReturnNoticeDay=day;State.lastDeathCause="";State.neighborDeathPending=false;
            State.firstApocalypsePending=false;State.stageOneComplete=false;State.day7BombDefused=false;
            State.sleeplessDays=0;State.neighborBombArmed=false;
            State.neighborNightBeat=0;State.neighborNightLocked=false;State.neighborHostile=false;
            State.neighborHealth=-1;State.encounterDestination="CityEncounter3D";
            State.stageEvents.RemoveAll(id=>id.StartsWith(day+":"));
            if(day==2){State.neighborNightRuptureSeen=State.neighborNightResolved=State.neighborHatRevealed=State.neighborEyeTwitchSeen=false;State.neighborNightCaughtCount=0;}
            State.savedDay=day;State.savedPeriod=0;State.savedMinute=420;State.savedScene="LivingRoom";State.savedX=0;State.savedY=-1.6f;State.savedZ=0;
            Vitality=MaxVitality;Changed?.Invoke();
        }
        public void Damage(float amount)
        {
            if (HasEnded)
                return;
            Vitality = Mathf.Max(0, Vitality - Mathf.Max(0, amount));
            Changed?.Invoke();
            if (Vitality <= 0)
            {
                Died?.Invoke();
                if (!HasLoopAbility) EndWithoutReturn();
            }
        }

        internal void UseDebugState(ProgressionState state,float vitality){State=state;Vitality=Mathf.Clamp(vitality,0,MaxVitality);Changed?.Invoke();}
        public void DebugSetSanity(float value){if(!StoryDebugActions.Enabled)return;NeighborDebugCommands.SanityOverride=Mathf.Clamp(value,0,Settings.maximumSanity);Changed?.Invoke();}
        public void LoseSanity(float amount)
        {
            if(StoryDebugActions.Enabled&&NeighborDebugCommands.SanityOverride>=0)NeighborDebugCommands.SanityOverride=Mathf.Max(0,NeighborDebugCommands.SanityOverride-amount);
            State.sanityBurden += Mathf.Max(0, amount);
            ProgressionSave.Write(State); Changed?.Invoke();
        }
        public void EndWithoutReturn()
        {
            if(HasEnded)return;
            State.worldEndEnding=true;
            ProgressionSave.Write(State);
            string ending=State.lastDeathCause=="neighbor"?"neighbor-killed":!CrisisRules.LegacyValidation&&State.stageReturnDay==7?"building-destroyed":"world-end";
            EndingCollection.Unlock(ending);
            EndingTriggered?.Invoke(ending); Changed?.Invoke();
        }
        public void Heal(float amount)
        {
            if (HasEnded)
                return;
            Vitality = Mathf.Min(MaxVitality, Vitality + Mathf.Max(0, amount));
            Changed?.Invoke();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
