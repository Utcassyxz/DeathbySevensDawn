using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Knowledge;
namespace DeathBySeventhsDawn.Progression
{
    // Directs authored beats through the existing clock, modal surface, scene and save owners.
    public sealed class ApartmentEventDirector:MonoBehaviour
    {
        public static ApartmentEventDirector Instance{get;private set;}
        public static bool WorldBlocked=>Instance!=null&&!string.IsNullOrEmpty(Instance.card);
        public static bool NightInProgress=>GameManager.Instance!=null&&GameManager.Instance.State.neighborNightBeat>0&&GameManager.Instance.State.neighborNightBeat<7;
        static ProgressionState S=>GameManager.Instance.State;
        string card;int opened;float freeSeconds,ambientSeconds,arrivalDelay,nextNightCall,nextFinalCall;AudioSource ambient;
        public string CurrentCard=>card;
        public bool ChaseActive=>!CrisisRules.LegacyValidation&&S.neighborNightBeat==6;
        void Awake(){Instance=this;ambient=gameObject.AddComponent<AudioSource>();ambient.playOnAwake=false;ambient.volume=.16f;var mix=Resources.Load<UnityEngine.Audio.AudioMixer>("ApartmentMix");if(mix!=null){var groups=mix.FindMatchingGroups("Master");if(groups.Length>0)ambient.outputAudioMixerGroup=groups[0];}}
        void OnEnable(){SceneManager.sceneLoaded+=Loaded;GameManager.Instance.JourneyReset+=Reset;LoopManager.Instance.LoopRestarted+=Reset;}
        void OnDisable(){SceneManager.sceneLoaded-=Loaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=Reset;if(LoopManager.Instance!=null)LoopManager.Instance.LoopRestarted-=Reset;}
        public void CloseForDebug(){card=null;}
        void Reset(){card=null;freeSeconds=ambientSeconds=0;}
        void Loaded(Scene scene,LoadSceneMode mode){arrivalDelay=.7f;if(scene.name=="NeighborRoom2D"&&S.neighborNightBeat==1)S.neighborNightBeat=2;}
        public static bool InterceptSleep()
        {
            if(CrisisRules.LegacyValidation||StoryDebugActions.Enabled||Instance==null)return false;
            var clock=TimeManager.Instance;
            if(clock.Days==2&&!S.neighborNightResolved){S.nightSleepRequested=true;clock.SetNarrativeTime(2,TimePeriod.Night,1390);Instance.RequestNight();return true;}
            if(clock.Days==7&&!S.day7BombDefused){clock.SetNarrativeTime(7,TimePeriod.Night,1380);Instance.Open("day7-call");return true;}
            return false;
        }
        public bool RetryNight(){if(S.neighborNightCaughtCount!=1)return false;S.neighborNightResolved=false;S.neighborNightLocked=false;S.neighborNightBeat=0;return RequestNight();}
        public bool RequestNight()
        {
            if(S.neighborNightResolved||S.neighborNightBeat>0)return false;
            S.neighborNightRuptureSeen=true;S.neighborDefeated=false;S.neighborHealth=-1;S.neighborNightBeat=1;Open("night-call");return true;
        }
        public void Open(string id){card=id;opened=Time.frameCount;freeSeconds=0;ProgressionSave.Write(S);}
        string[] Choices()
        {
            switch(card){
                case "day7-call":return new[]{"night-go","night-delay"};
                case "night-call":return new[]{"night-go","night-delay"};
                case "night-intro":return new[]{"night-eye","night-calm","night-logic","night-observe"};
                case "night-twitch":return new[]{"night-ask","night-back","night-observe","night-calm"};
                case "day7-confrontation":return new[]{"day7-defuse","day7-persuade","day7-rescue","day7-leave"};
                case "stage-complete":return new[]{"stage-title"};
                default:return new[]{"stage-continue"};
            }
        }
        public bool Choose(int index)
        {
            if(card==null||index<0||index>=Choices().Length)return false;
            string previous=card;card=null;opened=Time.frameCount;
            if(previous=="day7-call"){nextFinalCall=Time.unscaledTime+70;if(index==0){if(!S.stageEvents.Contains("7:confrontation-call"))S.stageEvents.Add("7:confrontation-call");S.neighborIntroduced=true;SceneManager.LoadScene("NeighborRoom2D");}ProgressionSave.Write(S);return true;}
            if(previous=="night-call")
            {
                if(index==1){nextNightCall=Time.unscaledTime+45;S.neighborNightBeat=0;ProgressionSave.Write(S);return true;}
                S.neighborDoorForced=true;S.neighborIntroduced=true;StoryTransition.Cover(.7f);SceneManager.LoadScene("NeighborRoom2D");return true;
            }
            if(previous=="night-intro"){S.neighborNightBeat=3;S.neighborEyeTwitchSeen=true;Open("night-twitch");Play("Audio/DreamMudStep",.18f);return true;}
            if(previous=="night-twitch"){S.neighborNightBeat=4;Open("night-body");Play("Audio/WoodHammer",.3f);return true;}
            if(previous=="night-body")
            {
                S.neighborNightBeat=5;NeighborSlice.Instance.Close();var room=Object.FindAnyObjectByType<NeighborRoom>();
                if(room!=null)room.BeginHatReveal();else{S.neighborNightBeat=1;SceneManager.LoadScene("NeighborRoom2D");}return true;
            }
            if(previous=="night-controls"){S.neighborNightControlsSeen=true;ProgressionSave.Write(S);return true;}
            if(previous=="day7-confrontation")
            {
                if(index==0){if(S.serviceRouteIsolated&&Investigation.Has("deduction-day7-plan"))CompleteStage();else Open("day7-need-plan");}
                else if(index==1){if(S.neighborTrusted&&Investigation.Has("neighbor-order")&&Investigation.Has("deduction-day7-plan"))CompleteStage();else Open("day7-no-persuasion");}
                else if(index==2){S.stageEvents.Add("7:evacuated");if(S.serviceRouteIsolated&&Investigation.Has("deduction-day7-plan"))CompleteStage();else Open("day7-rescue-result");}
                else {if(SceneManager.GetActiveScene().name=="NeighborRoom2D")DeathBySeventhsDawn.World.RoomDoor2D.TravelTo("Corridor",new Vector2(-4.7f,-.55f));}
            }
            if(previous=="stage-complete")MainMenuUI.ReturnToMainMenu();
            ProgressionSave.Write(S);return true;
        }
        public bool BeginFinalConfrontation()
        {
            if(TimeManager.Instance.Days!=7||S.stageOneComplete||S.day7BombDefused)return false;
            S.neighborBombArmed=true;S.stageEvents.Add("7:confronted");Open("day7-confrontation");return true;
        }
        public void CompleteStage()
        {
            if(TimeManager.Instance.Days!=7||S.stageOneComplete)return;
            S.day7BombDefused=S.buildingCrisisResolved=S.stageOneComplete=true;
            EndingCollection.Unlock("building-saved");Open("stage-complete");
        }
        public bool EscapeNight()
        {
            if(!ChaseActive)return false;
            S.neighborNightResolved=true;S.neighborNightBeat=7;S.neighborHostile=false;GameManager.Instance.LoseSanity(5);
            Investigation.Collect("neighbor-death-memory");CognitiveRupture.ReturnFromSpecial();ProgressionSave.Write(S);return true;
        }
        public bool CatchNight()
        {
            if(!ChaseActive)return false;
            S.neighborNightCaughtCount++;
            if(S.neighborNightCaughtCount==1)
            {
                S.neighborNightResolved=true;S.neighborNightBeat=7;S.neighborNightLocked=true;S.neighborHostile=false;
                GameManager.Instance.LoseSanity(10);GameManager.Instance.Heal(GameManager.Instance.MaxVitality);
                S.encounterDestination="CityEncounter3D";DeathBySeventhsDawn.World.RoomDoor2D.TravelTo("Corridor",new Vector2(-4.7f,-.55f));Open("night-caught");
            }
            else {S.stageReturnDay=2;S.lastDeathCause="neighbor";S.neighborNightBeat=7;GameManager.Instance.Damage(GameManager.Instance.MaxVitality);}
            ProgressionSave.Write(S);return true;
        }
        void Play(string resource,float volume){ambient.pitch=Random.Range(.94f,1.05f);ambient.PlayOneShot(Resources.Load<AudioClip>(resource),volume);}
        void Update()
        {
            if(CrisisRules.LegacyValidation||StoryDebugActions.Enabled||ProgressionHUD.DebugConsoleOpen||MainMenuUI.WorldBlocked||PauseMenuUI.WorldBlocked)return;
            var clock=TimeManager.Instance;if(clock==null)return;
            if(card!=null)
            {
                if(S.neighborNightBeat>=2&&S.neighborNightBeat<=4)Object.FindAnyObjectByType<NeighborRoom>()?.PresentNightTwitch(S.neighborNightBeat);
                if(Time.frameCount<=opened+1)return;var k=Keyboard.current;if(k==null)return;
                for(int i=0;i<Choices().Length;i++)if(k[(Key)((int)Key.Digit1+i)].wasPressedThisFrame){Choose(i);return;}
                if(k.enterKey.wasPressedThisFrame&&Choices().Length==1)Choose(0);
                return;
            }
            if(GameManager.Instance.HasEnded)return;
            arrivalDelay=Mathf.Max(0,arrivalDelay-Time.unscaledDeltaTime);if(arrivalDelay>0)return;
            string scene=SceneManager.GetActiveScene().name;
            if(S.stageReturnNoticeDay>0&&!Interaction.StoryJournal.WorldBlocked){int day=S.stageReturnNoticeDay;S.stageReturnNoticeDay=0;Open(day==7?"day7-return":"day2-return");return;}
            if(S.neighborNightBeat>=2&&S.neighborNightBeat<=4&&scene=="NeighborRoom2D"){NeighborSlice.Instance.Close();Open(S.neighborNightBeat==2?"night-intro":S.neighborNightBeat==3?"night-twitch":"night-body");return;}
            if(S.neighborNightBeat==5&&scene=="NeighborRoom2D"&&!NeighborRoom.HatRevealActive&&!CognitiveRupture.IsActive){Object.FindAnyObjectByType<NeighborRoom>()?.BeginHatReveal();return;}
            if(S.neighborNightBeat==5&&scene=="NeighborArena3D"&&!CognitiveRupture.IsActive){S.neighborNightBeat=6;
                var combat=Object.FindAnyObjectByType<NeighborCombat>();if(combat!=null){var cc=combat.actor.GetComponent<CharacterController>();cc.enabled=false;combat.actor.transform.position=new Vector3(NeighborRoom.NightPlayerPoint.x,0,NeighborRoom.NightPlayerPoint.y);combat.actor.SetViewYaw(-90);cc.enabled=true;Physics.SyncTransforms();}
                if(!S.neighborNightControlsSeen)Open("night-controls");return;}
            if(S.neighborNightBeat==7&&scene!="NeighborArena3D"&&!CognitiveRupture.IsActive&&S.nightSleepRequested&&!Interaction.StoryJournal.WorldBlocked){S.nightSleepRequested=false;clock.SleepUntilNextMorning();return;}
            if(Interaction.StoryJournal.WorldBlocked||Interaction.FurnitureInteraction.IsBusy||CognitiveRupture.IsActive)return;
            if(clock.Days==2&&clock.GameMinute>=1390&&Time.unscaledTime>=nextNightCall&&!S.neighborNightResolved&&S.neighborNightBeat==0){RequestNight();return;}
            if(clock.Days==7&&clock.GameMinute>=1440&&!S.day7BombDefused){WorldEndSequence.BeginFatalCrisis(CrisisKind.Building);return;}
            if(clock.Days==7&&clock.GameMinute>=1380&&!S.stageEvents.Contains("7:confrontation-call")&&Time.unscaledTime>=nextFinalCall){Open("day7-call");return;}
            if(clock.Days==7&&clock.GameMinute>=1380&&scene=="NeighborRoom2D"&&!S.stageEvents.Contains("7:confronted")){BeginFinalConfrontation();return;}
            if(scene.EndsWith("3D"))return;
            freeSeconds+=Time.unscaledDeltaTime;ambientSeconds+=Time.unscaledDeltaTime;
            if(ambientSeconds>=30){ambientSeconds=0;Play(clock.Days<=2?"Audio/DoorKnock":"Audio/DreamMudStep",.18f);}
            if((clock.Days>1&&!S.tutorialComplete)||freeSeconds<70)return;
            string key=clock.Days+":"+(int)clock.Period;
            if(clock.Days<=2||clock.Days==3||clock.Days>=5&&clock.Days<=7)
            {
                if(!S.stageEvents.Contains(key)){S.stageEvents.Add(key);if(clock.Days>=3)S.neighborBombArmed=true;Open("beat-"+clock.Days+"-"+(int)clock.Period);}
                else if(clock.Days<=2)
                {
                    // Authored variations cover the whole six-minute period, even if Hugo stays put.
                    for(int beat=0;beat<4;beat++){string second=key+":echo"+beat;if(S.stageEvents.Contains(second))continue;S.stageEvents.Add(second);Open(beat==0?"echo-"+clock.Days+"-"+(int)clock.Period:"after-"+clock.Days+"-"+(int)clock.Period+"-"+beat);break;}
                }
            }
        }
        void OnGUI()
        {
            if(card==null||MainMenuUI.WorldBlocked||PauseMenuUI.WorldBlocked||ProgressionHUD.DebugConsoleOpen||StoryDebugActions.Enabled||CrisisRules.LegacyValidation)return;
            var line=NeighborSlice.Text(card);var ids=Choices();var options=new string[ids.Length];
            for(int i=0;i<ids.Length;i++)options[i]=(i+1)+". "+NeighborSlice.Text(ids[i]).body;
            int choice=NarrativeCard.Draw(line.title,line.body,"",options,NeighborSlice.Text("stage-choice-controls").body);if(choice>=0)Choose(choice);
        }
    }
}
