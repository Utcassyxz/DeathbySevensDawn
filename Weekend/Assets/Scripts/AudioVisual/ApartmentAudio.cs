// CORE CONTRACT: 文案任务不要改动控制流程；工程变更先读 docs/CORE_CODE_MAP.md 并同步验收。
// 职责：同步环境音乐和开场声源，并按状态混音。
using UnityEngine;
using UnityEngine.Audio;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;

namespace DeathBySeventhsDawn.Progression
{
    public sealed class ApartmentAudio : MonoBehaviour
    {
        public static ApartmentAudio Instance { get; private set; }

        AudioSource uiPress, bubbleScore, goodScore, badScore, trueScore;
        float specialBlend;
        public int UiPressCount { get; private set; }

        public enum EndingMood
        {
            Good,
            Bad,
            True
        }

        EndingMood endingMood = EndingMood.Bad;
        public bool SpecialMusicReady => bubbleScore != null && bubbleScore.clip != null && goodScore.clip != null && badScore.clip != null && trueScore.clip != null;

        public void SetEndingMood(EndingMood mood)
        {
            endingMood = mood;
        }

        public void PlayUiPress()
        {
            if (uiPress == null || uiPress.clip == null)
                return;
            uiPress.volume = .26f;
            uiPress.PlayOneShot(uiPress.clip);
            UiPressCount++;
        }

        AudioSource endingVoice;
        float endingDelay = -1, endingAge = -1;
        public bool EndingScreamPlaying => endingVoice != null && endingVoice.isPlaying;

        AudioSource home, hidden, reveal, title, scream, dreamKick, dreamHammer;
        float blend, duck = 1, master, menuBlend = 1, cinematicDuck = 1;
        bool previousRevelation, opening;
        public float DreamKickGain => dreamKick != null ? dreamKick.volume : 0;
        public bool HammerLayerReady => dreamHammer != null && dreamHammer.clip != null && dreamHammer.volume > 0;
        public float HiddenBlend => blend;
        public float MenuBlend => menuBlend;
        public bool Ready => home != null && hidden != null && home.clip != null && hidden.clip != null;
        public int RevelationCount { get; private set; }
        public bool ScreamPlaying => scream != null && scream.isPlaying;

        void Awake()
        {
            Instance = this;
            home = Source("ApartmentHome", true);
            hidden = Source("BehindTheFilter", true);
            reveal = Source("Revelation", false);
            title = Source("TitleOrbit", true);
            scream = Source("OpeningScream", false);
            endingVoice = Source("OpeningScream", false);
            dreamKick = Source("DreamKick", false);
            dreamHammer = Source("WoodHammer", false);
            dreamHammer.volume = .65f;
            uiPress = Source("UiPress", false);
            bubbleScore = Source("BubbleThreshold", true);
            goodScore = Source("EndingGood", true);
            badScore = Source("EndingBad", true);
            trueScore = Source("EndingTrue", true);
            menuBlend = MainMenuUI.Suppressed ? 0 : 1;
        }

        // The dream kick grows from a wet step to a full crack; the neighbour's wood
        // hammer arrives underneath it, so the two sounds end up sharing the same beat.
        public void PlayDreamKick(int index, int total)
        {
            if (dreamKick != null && dreamKick.clip != null)
            {
                dreamKick.pitch = .92f + .08f * Mathf.Clamp01(index / (float)Mathf.Max(1, total));
                dreamKick.volume = Mathf.Lerp(.22f, .92f, Mathf.Clamp01((index - 1) / 2f));
                dreamKick.Play();
            }

            if (index >= 2 && dreamHammer != null && dreamHammer.clip != null)
                dreamHammer.PlayOneShot(dreamHammer.clip, Mathf.Lerp(.10f, .55f, Mathf.Clamp01((index - 2) / 1f)));
        }

        AudioSource Source(string clip, bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0;
            s.volume = 0;
            s.clip = Resources.Load<AudioClip>("Audio/" + clip);
            var m = Resources.Load<AudioMixer>("ApartmentMix");
            if (m != null)
            {
                var g = m.FindMatchingGroups("Master");
                if (g.Length > 0)
                    s.outputAudioMixerGroup = g[0];
            }

            return s;
        }

        void Start()
        {
            RestartMusic();
        }

        void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.JourneyReset += ResetJourney;
                GameManager.Instance.EndingTriggered += Ending;
            }
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.JourneyReset -= ResetJourney;
                GameManager.Instance.EndingTriggered -= Ending;
            }

            foreach (var s in new[]
            {
                home,
                hidden,
                reveal,
                title,
                scream,
                endingVoice
            }

            )
                if (s != null)
                    s.Stop();
        }

        void Ending(string id)
        {
            endingMood = EndingMood.Bad;
            endingDelay = id == "world-end" ? WorldEndSequence.Duration - 1 : .15f;
            endingAge = -1;
        }

        void RestartMusic()
        {
            if (!Ready)
                return;
            double start = AudioSettings.dspTime + .15;
            home.PlayScheduled(start);
            hidden.PlayScheduled(start);
            if (title.clip != null)
                title.PlayScheduled(start);
        }

        // A new journey preserves the synchronized music clock, so the title-to-dream handoff never restarts a phrase.
        void ResetJourney()
        {
            endingDelay = endingAge = -1;
            if (endingVoice != null)
                endingVoice.Stop();
            blend = 0;
            previousRevelation = false;
            RevelationCount = 0;
            reveal.Stop();
        }

        public void BeginOpening()
        {
            if (opening)
                return;
            opening = true;
            if (scream.clip != null)
            {
                scream.volume = 0;
                scream.Play();
            }
        }

        // The settlement page calls this explicitly so both endings have a scream even
        // when the cinematic path is skipped or a test jumps straight to the result.
        public void PlayEndingScream()
        {
            if (endingVoice == null || endingVoice.clip == null || endingAge >= 0 || endingDelay >= 0)
                return;
            endingDelay = .05f;
        }

        public void EndOpening()
        {
            opening = false;
        }

        void Update()
        {
            if (!Ready || GameManager.Instance == null)
                return;
            if (endingDelay >= 0)
            {
                endingDelay -= Time.unscaledDeltaTime;
                if (endingDelay < 0 && !MainMenuUI.IsVisible && endingVoice.clip != null)
                {
                    endingAge = 0;
                    endingVoice.Play();
                }
            }

            if (endingAge >= 0)
            {
                endingAge += Time.unscaledDeltaTime;
                float duration = Mathf.Min(8.5f, endingVoice.clip.length * .96f);
                float envelope = Mathf.SmoothStep(0, 1, endingAge / 1.2f) * Mathf.SmoothStep(0, 1, (duration - endingAge) / 3f);
                endingVoice.volume = MainMenuUI.IsVisible ? Mathf.MoveTowards(endingVoice.volume, 0, Time.unscaledDeltaTime * .5f) : envelope * .52f;
                if (endingAge >= duration || MainMenuUI.IsVisible && endingVoice.volume < .001f)
                {
                    endingVoice.Stop();
                    endingAge = -1;
                }
            }

            bool special = NightDreamSequence.Instance != null && NightDreamSequence.Instance.IsBubble || GameManager.Instance.HasEnded && !MainMenuUI.IsVisible;
            specialBlend = Mathf.MoveTowards(specialBlend, special ? 1 : 0, Time.unscaledDeltaTime / 2.4f);
            var wanted = GameManager.Instance.HasEnded ? (endingMood == EndingMood.Good ? goodScore : endingMood == EndingMood.True ? trueScore : badScore) : bubbleScore;
            foreach (var score in new[]
            {
                bubbleScore,
                goodScore,
                badScore,
                trueScore
            }

            )
            {
                if (score == null || score.clip == null)
                    continue;
                float targetVolume = special && score == wanted ? .36f : 0;
                if (targetVolume > 0 && !score.isPlaying)
                    score.Play();
                score.volume = Mathf.MoveTowards(score.volume, targetVolume, Time.unscaledDeltaTime * .19f);
                if (targetVolume == 0 && score.volume < .001f && score.isPlaying)
                    score.Stop();
            }

            var story = Object.FindAnyObjectByType<StoryJournal>();
            bool revelation = (story != null && story.IsRevelation) || RooftopAccess.Instance != null && RooftopAccess.Instance.IsRevelation;
            if (revelation && !previousRevelation)
            {
                reveal.volume = .65f;
                reveal.Play();
                RevelationCount++;
            }

            previousRevelation = revelation;
            float dt = Time.unscaledDeltaTime, step = 1 - Mathf.Exp(-dt * 1.6f);
            float target = GameManager.Instance.Sanity <= GameManager.Instance.Settings.fracturedSanity || revelation ? 1 : 0;
            blend = Mathf.Lerp(blend, target, step);
            menuBlend = Mathf.MoveTowards(menuBlend, MainMenuUI.IsVisible ? 1 : 0, dt / 4.5f);
            duck = Mathf.Lerp(duck, (StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy) ? .65f : 1, step);
            master = Mathf.MoveTowards(master, GameManager.Instance.HasEnded && !MainMenuUI.IsVisible ? 0 : .55f, dt * .3f);
            float angle = menuBlend * Mathf.PI * .5f, gameGain = Mathf.Cos(angle) * master * duck;
            home.volume = Mathf.Cos(blend * Mathf.PI * .5f) * gameGain * (1 - specialBlend);
            hidden.volume = Mathf.Sin(blend * Mathf.PI * .5f) * gameGain * (1 - specialBlend);
            float targetDuck = MainMenuUI.IsOpening ? Mathf.Lerp(1, .38f, MainMenuUI.OpeningProgress) : MainMenuUI.IsVisible ? 1 : cinematicDuck;
            cinematicDuck = Mathf.MoveTowards(cinematicDuck, targetDuck, dt * .15f);
            title.volume = Mathf.Sin(angle) * master * .83f * cinematicDuck;
            float screamTarget = opening ? .7f : 0;
            scream.volume = Mathf.MoveTowards(scream.volume, screamTarget, dt * (opening ? .3f : .39f));
            if (!opening && scream.volume < .001f && scream.isPlaying)
                scream.Stop();
            if (!revelation)
                reveal.volume = Mathf.MoveTowards(reveal.volume, 0, dt * .8f);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
