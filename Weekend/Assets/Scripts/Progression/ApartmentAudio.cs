using UnityEngine;
using UnityEngine.Audio;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
namespace DeathBySeventhsDawn.Progression
{
    public sealed class ApartmentAudio:MonoBehaviour
    {
        public static ApartmentAudio Instance{get;private set;}
        AudioSource home,hidden,reveal,title,scream;float blend,duck=1,master,menuBlend=1,cinematicDuck=1;bool previousRevelation,opening;
        public float HiddenBlend=>blend;public float MenuBlend=>menuBlend;
        public bool Ready=>home!=null&&hidden!=null&&home.clip!=null&&hidden.clip!=null;
        public int RevelationCount{get;private set;}
        public bool ScreamPlaying=>scream!=null&&scream.isPlaying;
        void Awake(){Instance=this;home=Source("ApartmentHome",true);hidden=Source("BehindTheFilter",true);reveal=Source("Revelation",false);title=Source("TitleOrbit",true);scream=Source("OpeningScream",false);menuBlend=MainMenuUI.Suppressed?0:1;}
        AudioSource Source(string clip,bool loop){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=loop;s.spatialBlend=0;s.volume=0;s.clip=Resources.Load<AudioClip>("Audio/"+clip);var m=Resources.Load<AudioMixer>("ApartmentMix");if(m!=null){var g=m.FindMatchingGroups("Master");if(g.Length>0)s.outputAudioMixerGroup=g[0];}return s;}
        void Start(){RestartMusic();}
        void OnEnable(){if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=ResetJourney;}
        void OnDisable(){if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=ResetJourney;foreach(var s in new[]{home,hidden,reveal,title,scream})if(s!=null)s.Stop();}
        void RestartMusic(){if(!Ready)return;double start=AudioSettings.dspTime+.15;home.PlayScheduled(start);hidden.PlayScheduled(start);if(title.clip!=null)title.PlayScheduled(start);}
        // A new journey preserves the synchronized music clock, so the title-to-dream handoff never restarts a phrase.
        void ResetJourney(){blend=0;previousRevelation=false;RevelationCount=0;reveal.Stop();}
        public void BeginOpening(){if(opening)return;opening=true;if(scream.clip!=null){scream.volume=0;scream.Play();}}
        public void EndOpening(){opening=false;}
        void Update(){if(!Ready||GameManager.Instance==null)return;var story=Object.FindAnyObjectByType<StoryJournal>();bool revelation=story!=null&&story.IsRevelation;
            if(revelation&&!previousRevelation){reveal.volume=.65f;reveal.Play();RevelationCount++;}previousRevelation=revelation;
            float dt=Time.unscaledDeltaTime,step=1-Mathf.Exp(-dt*1.6f);float target=GameManager.Instance.Sanity<=GameManager.Instance.Settings.fracturedSanity||revelation?1:0;blend=Mathf.Lerp(blend,target,step);
            menuBlend=Mathf.MoveTowards(menuBlend,MainMenuUI.IsVisible?1:0,dt/4.5f);
            duck=Mathf.Lerp(duck,(StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy) ? .65f : 1,step);master=Mathf.MoveTowards(master,GameManager.Instance.HasEnded?0:.55f,dt*.3f);
            float angle=menuBlend*Mathf.PI*.5f,gameGain=Mathf.Cos(angle)*master*duck;
            home.volume=Mathf.Cos(blend*Mathf.PI*.5f)*gameGain;hidden.volume=Mathf.Sin(blend*Mathf.PI*.5f)*gameGain;
            float targetDuck=MainMenuUI.IsOpening?Mathf.Lerp(1,.38f,MainMenuUI.OpeningProgress):MainMenuUI.IsVisible?1:cinematicDuck;cinematicDuck=Mathf.MoveTowards(cinematicDuck,targetDuck,dt*.15f);title.volume=Mathf.Sin(angle)*master*.83f*cinematicDuck;
            float screamTarget=opening?.7f:0;scream.volume=Mathf.MoveTowards(scream.volume,screamTarget,dt*(opening?.3f:.39f));if(!opening&&scream.volume<.001f&&scream.isPlaying)scream.Stop();
            if(!revelation)reveal.volume=Mathf.MoveTowards(reveal.volume,0,dt*.8f);
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
