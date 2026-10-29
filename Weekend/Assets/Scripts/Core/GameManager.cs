using System;
using UnityEngine;
using DeathBySeventhsDawn.Progression;
namespace DeathBySeventhsDawn.Core
{
    public enum SanityLayer { Stable, Unstable, Fractured, Revelation }
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public ProgressionSettings Settings { get; private set; }
        internal ProgressionState State { get; private set; }
        public int LoopCount => State.loopCount;
        public bool HasEnded => State.badEndingOne;
        public event Action<string> EndingTriggered;
        // Only permanent aging to the vitality floor qualifies, not ordinary combat damage.
        public bool TryTriggerBadEnding(int day)
        {
            if(HasEnded) return true;
            if(day<7 || MaxVitality>Settings.minimumVitality+.001f) return false;
            State.badEndingOne=true;
            ProgressionSave.Write(State);
            EndingTriggered?.Invoke("bad-ending-1"); Changed?.Invoke();
            return true;
        }
        public event Action JourneyReset;
        public void StartNewJourney()
        {
            State=new ProgressionState(); Vitality=MaxVitality;
            ProgressionSave.Write(State);
            TimeSystem.TimeManager.Instance.ResetToDayOne();
            World.RoomDoor2D.ClearPendingTravel();
            JourneyReset?.Invoke(); Changed?.Invoke();
            if(UnityEngine.Application.CanStreamedLevelBeLoaded("Bedroom"))
                UnityEngine.SceneManagement.SceneManager.LoadScene("Bedroom");
        }
        public float Knowledge => State.knowledge;
        public float Mutation => State.mutation;
        public float MaxVitality => Mathf.Max(Settings.minimumVitality,Settings.initialVitality-LoopCount*Settings.agingPerLoop);
        public float Vitality { get; private set; }
        public float Sanity => Mathf.Clamp(Settings.maximumSanity-State.sanityBurden-Knowledge*Settings.knowledgeSanityCost-Mutation*Settings.mutationSanityCost,0,Settings.maximumSanity);
        public int KnowledgeLevel => Mathf.FloorToInt(Knowledge/Mathf.Max(1,Settings.knowledgePerLevel));
        public SanityLayer Layer => Sanity<=Settings.revelationSanity?SanityLayer.Revelation:Sanity<=Settings.fracturedSanity?SanityLayer.Fractured:Sanity<=Settings.unstableSanity?SanityLayer.Unstable:SanityLayer.Stable;
        public event Action Changed;
        public event Action<string> AbilityUnlocked;
        public event Action<SanityLayer> SanityLayerChanged;
        public event Action<float> MutationChanged; // NPC reactions subscribe; evaluate CanUseHumanRoute on entry too.
        public event Action Died;
        void Awake()
        {
            if(Instance!=null && Instance!=this) { Destroy(gameObject); return; }
            Instance=this; DontDestroyOnLoad(gameObject);
            Settings=Resources.Load<ProgressionSettings>("SevenDayProgression");
            if(Settings==null) Settings=ScriptableObject.CreateInstance<ProgressionSettings>();
            State=ProgressionSave.Load(); Vitality=MaxVitality;
        }
        public bool HasAbility(string id)
        {
            foreach(var item in Settings.abilities) if(item!=null && item.id==id && Mutation>=item.mutation) return true;
            return false;
        }
        public bool CanUseHumanRoute(float maximumMutation) => Mutation<maximumMutation;
        public bool MeetsKnowledgeRequirement(float required) => Knowledge>=required;
        internal void Acquire(KnowledgeEffect effect)
        {
            if(HasEnded) return;
            float oldMutation=Mutation; var oldLayer=Layer;
            State.knowledge+=Mathf.Max(0,effect.knowledge);
            State.mutation+=Mathf.Max(0,effect.mutation);
            State.sanityBurden+=Mathf.Max(0,effect.sanityCost);
            ProgressionSave.Write(State);
            foreach(var item in Settings.abilities)
                if(item!=null && oldMutation<item.mutation && Mutation>=item.mutation) AbilityUnlocked?.Invoke(item.id);
            if(oldMutation!=Mutation) MutationChanged?.Invoke(Mutation);
            if(oldLayer!=Layer) SanityLayerChanged?.Invoke(Layer);
            Changed?.Invoke();
        }
        public void RegisterNewLoop()
        {
            if(HasEnded) return;
            State.loopCount++; Vitality=MaxVitality;
            ProgressionSave.Write(State); Changed?.Invoke();
        }
        public void Damage(float amount)
        {
            if(HasEnded) return;
            Vitality=Mathf.Max(0,Vitality-Mathf.Max(0,amount)); Changed?.Invoke();
            if(Vitality<=0) { Died?.Invoke(); TimeSystem.LoopManager.Instance.RestartLoop(); }
        }
        public void Heal(float amount) { if(HasEnded) return; Vitality=Mathf.Min(MaxVitality,Vitality+Mathf.Max(0,amount)); Changed?.Invoke(); }
        void OnDestroy() { if(Instance==this) Instance=null; }
    }
}