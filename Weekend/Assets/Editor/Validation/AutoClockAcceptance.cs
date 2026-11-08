using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Interaction;
public static class AutoClockAcceptance
{
    static ProgressionState State=>(ProgressionState)typeof(GameManager).GetProperty("State",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(GameManager.Instance);
    static void Check(bool value,string label)=>VerifyRevision.Check(value,label);
    public static IEnumerator Run()
    {
        GameManager.Instance.StartNewJourney();yield return .7f;
        StoryDebugActions.SetEnabled(true);StoryDebugActions.Jump("CityStreet",2,0);yield return 1.5f;
        StoryDebugActions.SetEnabled(false);var clock=TimeManager.Instance;TimeManager.AutoTimeSuppressed=false;
        float before=clock.GameMinute;clock.AdvanceExplorationSeconds(1);Check(clock.GameMinute>before,"Exploration advances continuous game minutes");
        TimeManager.AutoTimeSuppressed=true;before=clock.GameMinute;clock.AdvanceExplorationSeconds(1);Check(clock.GameMinute==before,"Acceptance suppression freezes automatic time");TimeManager.AutoTimeSuppressed=false;
        NeighborSlice.Instance.Show("crowbar");before=clock.GameMinute;clock.AdvanceExplorationSeconds(1);Check(clock.GameMinute==before,"Narrative cards pause automatic time");NeighborSlice.Instance.Close();yield return .1f;
        Time.timeScale=0;before=clock.GameMinute;clock.AdvanceExplorationSeconds(1);Check(clock.GameMinute==before,"Pause freezes automatic time");Time.timeScale=1;
        StoryDebugActions.SetEnabled(true);before=clock.GameMinute;clock.AdvanceExplorationSeconds(1);Check(clock.GameMinute==before,"Free debug does not run automatic clock");clock.DebugSetTime(2,TimePeriod.Night);StoryDebugActions.SetEnabled(false);yield return .1f;
        float speed=clock.periodLengthMinutes;clock.periodLengthMinutes=1;for(int i=0;i<61;i++)clock.AdvanceExplorationSeconds(1);
        Check(clock.Days==3&&clock.Period==TimePeriod.Morning,"Staying awake crosses 02:00 without sleep input");
        Check(State.sleeplessDays==1,"Automatic overnight crossing records sleep debt");
        Check(clock.ClockText.StartsWith("07:"),"New morning starts at 07:00");clock.periodLengthMinutes=speed;
        ProgressionSave.Write(State);var saved=ProgressionSave.Load();Check(Mathf.Abs(saved.savedMinute-clock.GameMinute)<.01f,"v7 roundtrip preserves the continuous minute");
        TimeManager.AutoTimeSuppressed=true;
    }
}
