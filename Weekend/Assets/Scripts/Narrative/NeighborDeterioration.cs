using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Interaction
{
    // A consumer of the one game clock, never another timer. Decline cannot heal by revisiting a room.
    public static class NeighborDeterioration
    {
        public static float Progress(ProgressionState state) => Mathf.Clamp01(state.neighborDecline + state.neighborDialoguePressure);
        public static float Sanity(ProgressionState state) => 100 * (1 - Progress(state));
        public static float Mutation(ProgressionState state) => 100 * Progress(state);
        public static void Observe(ProgressionState state, int day, float minute)
        {
            // 19-hour narrative days, Day1 07:00 -> Day2 23:10; midnight remains part of the same night.
            float elapsed = (Mathf.Max(1, day) - 1) * 1140 + Mathf.Clamp(minute - 420, 0, 1140);
            state.neighborDecline = Mathf.Max(state.neighborDecline, Mathf.Clamp01(elapsed / 2110));
        }
        public static void Press(ProgressionState state) => state.neighborDialoguePressure = Mathf.Min(1, state.neighborDialoguePressure + .12f);
        public static void Reset(ProgressionState state)
        {
            state.neighborDecline = state.neighborDialoguePressure = 0;
            state.neighborDialogueActive = state.neighborDialogueResolved = false;
            state.neighborDialogueNode = "";
            state.neighborDialogueChoices.Clear();
        }
        public static bool FinalFight(ProgressionState state) => state.stageEvents.Contains("7:final-fight");
        public static bool DeviceAvailable(ProgressionState state) => state.stageEvents.Contains("7:controller-dropped");
    }
}
