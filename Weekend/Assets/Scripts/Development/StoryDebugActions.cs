// Debug actions do not unlock endings or manufacture evidence.
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;

namespace DeathBySeventhsDawn.Progression
{
    public static class StoryDebugActions
    {
        public static bool Enabled { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { Enabled=false; }
        public static void SetEnabled(bool enabled)
        {
            Enabled=enabled;
            if(enabled) SkipForcedStory();
        }
        public static void Jump(string scene, int day, int period)
        {
            SetEnabled(true);
            TimeSystem.TimeManager.Instance.DebugSetTime(day,(TimeSystem.TimePeriod)Mathf.Clamp(period,0,3));
            World.RoomDoor2D.ClearPendingTravel();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }
        public static void SkipForcedStory()
        {
            StoryTransition.Clear();
            ApartmentAudio.Instance?.StopDreamImpacts();
            MainMenuUI.Instance?.DebugSkip();
            Object.FindAnyObjectByType<StoryJournal>()?.DebugSkip();
            MorningRoutine.Instance?.DebugSkip();
            FirstLoopPrologue.Instance?.DebugSkip();
            CultNeighbor.Instance?.DebugSkip();
            DayOneSupernovaEvent.Instance?.DebugSkip();
            RooftopAccess.Instance?.Close();
            WorldEndSequence.Instance?.DebugSkip();
            NightDreamSequence.Instance?.DebugSkip();
            var state = GameManager.Instance.State;
            state.dreamSeen = true;
            state.morningStage = 10;
            state.tutorialComplete = state.journalUnlocked = state.investigationUnlocked = true;
            ProgressionSave.Write(state);
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    body.simulated = true;
                    body.linearVelocity = Vector2.zero;
                }
            }
        }

        public static void PreviewEnding(int number)
        {
            if (number < 1 || number > 3)
                return;
            EndingCollectionUI.Preview(number);
        }
    }
}
