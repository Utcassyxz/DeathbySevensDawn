using UnityEngine;
using UnityEngine.Audio;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
namespace DeathBySeventhsDawn.Progression
{
    public sealed class ApartmentAudio : MonoBehaviour
    {
        public static ApartmentAudio Instance {get;private set;}
        AudioSource home,hidden,reveal;float blend,duck=1,master=0;bool previousRevelation;
        public float HiddenBlend=>blend;
        public bool Ready=>home!=null&&hidden!=null&&home.clip!=null&&hidden.clip!=null;
        public int RevelationCount{get;private set;}
        void Awake(){Instance=this;home=Source("ApartmentHome",true);hidden=Source("BehindTheFilter",true);reveal=Source("Revelation",false);}
        AudioSource Source(string clip,bool loop)
        {
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;source.volume=0;source.clip=Resources.Load<AudioClip>("Audio/"+clip);
            var mixer=Resources.Load<AudioMixer>("ApartmentMix");if(mixer!=null){var groups=mixer.FindMatchingGroups("Master");if(groups.Length>0)source.outputAudioMixerGroup=groups[0];}
            return source;
        }
        void Start(){RestartMusic();}
        void OnEnable(){if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=ResetJourney;}
        void OnDisable(){if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=ResetJourney;if(home!=null)home.Stop();if(hidden!=null)hidden.Stop();if(reveal!=null)reveal.Stop();}
        void RestartMusic(){if(!Ready)return;double start=AudioSettings.dspTime+.15;home.PlayScheduled(start);hidden.PlayScheduled(start);}
        void ResetJourney(){blend=master=0;previousRevelation=false;RevelationCount=0;reveal.Stop();home.Stop();hidden.Stop();RestartMusic();}
        void Update()
        {
            if(!Ready||GameManager.Instance==null)return;
            var story=Object.FindAnyObjectByType<StoryJournal>();bool revelation=story!=null&&story.IsRevelation;
            if(revelation&&!previousRevelation){reveal.volume=.65f;reveal.Play();RevelationCount++;}
            previousRevelation=revelation;
            float target=GameManager.Instance.Sanity<=GameManager.Instance.Settings.fracturedSanity?1:0;
            if(revelation)target=1;
            float step=1-Mathf.Exp(-Time.unscaledDeltaTime*1.6f);blend=Mathf.Lerp(blend,target,step);
            bool reading=StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy;
            duck=Mathf.Lerp(duck,reading?.48f:1,step);master=Mathf.MoveTowards(master,GameManager.Instance.HasEnded?0:.55f,Time.unscaledDeltaTime*.4f);
            home.volume=Mathf.Cos(blend*Mathf.PI*.5f)*master*duck;
            hidden.volume=Mathf.Sin(blend*Mathf.PI*.5f)*master*duck;
            if(!revelation)reveal.volume=Mathf.MoveTowards(reveal.volume,0,Time.unscaledDeltaTime*.8f);
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
