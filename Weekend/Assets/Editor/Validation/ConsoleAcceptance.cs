using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

public static class ConsoleAcceptance
{
    static void Check(bool result, string label) => VerifyRevision.Check(result, "CONSOLE " + label);
    static void Call(ProgressionHUD hud, string method) => typeof(ProgressionHUD).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(hud, null);
    static string Run(string text) => GameCommandConsole.Execute(text);
    public static IEnumerator RunAll()
    { var commands=Run();while(commands.MoveNext())yield return commands.Current;CrisisRules.LegacyValidation=true;var regression=StageOneAcceptance.RunAll();while(regression.MoveNext())yield return regression.Current; }
    public static IEnumerator Run()
    {
        CrisisRules.LegacyValidation = false;
        GameManager.Instance.StartNewJourney(); yield return 1f;
        var game = GameManager.Instance; var clock = TimeManager.Instance;
        var hud = UnityEngine.Object.FindAnyObjectByType<ProgressionHUD>();
        clock.SetNarrativeTime(2, TimePeriod.Afternoon, 801);
        var stateProperty = typeof(GameManager).GetProperty("State", BindingFlags.NonPublic | BindingFlags.Instance);
        var original = stateProperty.GetValue(game);
        Call(hud, "ToggleConsole");
        Check(ProgressionHUD.DebugConsoleOpen && NeighborDebugCommands.Isolated && StoryDebugActions.Enabled, "backquote opens directly, without PIN");
        Check(!ReferenceEquals(original, stateProperty.GetValue(game)), "session uses cloned state");
        foreach (var syntax in GameCommandConsole.Syntax)
        {
            string verb = syntax.Split(' ')[0];
            Check(GameCommandConsole.Help(verb).Contains(syntax) && !GameCommandConsole.Description(verb).StartsWith("console."), "documented and localized: " + verb);
        }
        string before = JsonUtility.ToJson(stateProperty.GetValue(game));
        foreach (var invalid in new[] { "day 999999999999999", "day -1", "day 0", "day 10000", "sanity NaN", "sanity Infinity", "sanity 101", "damage -2", "heal 3 extra", "time 4", "date 3 06:00", "date 3 26:00", "date 3 23:60", "scene NotARealScene", "item give collected", "event unknown", "reset", "crisis building maybe", "knowledge 999", "mutation unlock fake" })
            Check(Run(invalid).StartsWith(StoryText.UI("console.usage")), "rejects invalid input: " + invalid);
        Check(before == JsonUtility.ToJson(stateProperty.GetValue(game)), "invalid commands leave state unchanged");
        Check(Run("unknown-command").Contains(StoryText.UI("console.unknown")), "unknown command has useful feedback");
        Run("  DaY \t 5  "); Check(clock.Days == 5, "case-insensitive verbs and whitespace");
        Run("time night"); Check(clock.Period == TimePeriod.Night && clock.GameMinute == 1260, "named period");
        Run("date 7 00:12"); Check(clock.Days == 7 && clock.Period == TimePeriod.Night && clock.GameMinute == 1452, "post-midnight belongs to current narrative day");
        Run("day set 3"); Run("time set 1"); Check(clock.Days == 3 && clock.Period == TimePeriod.Afternoon, "legacy date aliases");
        Run("sanity set 40"); Check(Mathf.Approximately(game.Sanity, 40), "sanity legacy alias");
        Run("item give CROWBAR"); Run("item give crowbar");
        Check(((ProgressionState)stateProperty.GetValue(game)).inventory.Count(x => x == "crowbar") == 1, "canonical item IDs, duplicate-safe inventory");
        Run("evidence unlock prep-oil"); Check(DeathBySeventhsDawn.Knowledge.Investigation.Has("prep-oil"), "actual evidence collection");
        Check(Run("deduce prep-oil prep-manual") == StoryText.UI("console.deduceFailed"), "deduction does not bypass prerequisites");
        Run("crisis building off"); Check(((ProgressionState)stateProperty.GetValue(game)).day7BombDefused, "building switch targets current Day7 crisis");
        Run("crisis building on"); Check(!((ProgressionState)stateProperty.GetValue(game)).day7BombDefused, "building crisis can be restored");
        Check(GameCommandConsole.Complete("sc").Contains("scene") && GameCommandConsole.Complete("scene Roo").Contains("scene Rooftop"), "verb and scene completion");
        Check(GameCommandConsole.Complete("item give cro").Single() == "item give crowbar", "item completion");
        Check(GameCommandConsole.Complete("event neighbor_").Length >= 6, "event completion");
        var buffer = new ConsoleBuffer(); buffer.Record("help"); buffer.Record("status"); buffer.Record("status");
        Check(buffer.History.Count == 2, "consecutive history duplicates suppressed");
        Check(buffer.Navigate(-1, "unfinished") == "status" && buffer.Navigate(-1, "status") == "help" && buffer.Navigate(1, "help") == "status" && buffer.Navigate(1, "status") == "unfinished", "up/down restores unfinished draft");
        for (int i = 0; i < 240; i++) { buffer.Add("line " + i); buffer.Record("command " + i); }
        Check(buffer.Lines.Count == 200 && buffer.History.Count == 100, "scrollback and history bounded");
        foreach (var size in new[] { new Vector2(1280,720), new Vector2(1920,1080), new Vector2(3440,1440) })
        { float scale = Mathf.Clamp(size.y/900f,.8f,2.4f); var rect = ProgressionHUD.ConsoleRect(size.x/scale,size.y/scale); Check(rect.xMin>=0 && rect.yMin>=0 && rect.xMax<=size.x/scale && rect.yMax<=size.y/scale, "console fits " + size); }
        var field = typeof(ProgressionHUD).GetField("command", BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(hud, "day 6"); Call(hud,"SubmitCommand");
        Check(clock.Days == 6 && (string)field.GetValue(hud) == "", "UI submit executes exactly one command and clears input");
        field.SetValue(hud, "clear"); Call(hud,"SubmitCommand");
        var uiBuffer = (ConsoleBuffer)typeof(ProgressionHUD).GetField("console",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(hud);
        Check(uiBuffer.Lines.Count == 0 && uiBuffer.History.Count == 2, "clear removes output, retaining history");
        Run("debug off"); Check(!StoryDebugActions.Enabled, "free debug can be disabled while isolated");
        Check(Run("scene Rooftop") == StoryText.UI("console.debugRequired") && !StoryDebugActions.Enabled, "disabled backend never silently skips story");
        Run("session end"); yield return .8f;
        Check(!NeighborDebugCommands.Isolated && !StoryDebugActions.Enabled && !ProgressionHUD.DebugConsoleOpen, "session end works while debug is off");
        Check(ReferenceEquals(original, stateProperty.GetValue(game)) && clock.Days == 2 && clock.Period == TimePeriod.Afternoon && Mathf.Abs(clock.GameMinute-801)<.1f, "original state, day, period and minute restored");
        Check(!((ProgressionState)stateProperty.GetValue(game)).inventory.Contains("crowbar"), "temporary inventory never reaches original journey");
        Check(Run("day 4") == StoryText.UI("console.noSession"), "mutations refused outside isolated session");
        Call(hud, "ToggleConsole");
        DeathBySeventhsDawn.Interaction.NeighborSlice.Instance.BeginNeighborDeath();
        Check(((ProgressionState)stateProperty.GetValue(game)).neighborDeathPending, "temporary delayed death is actually armed");
        Run("session end"); yield return 3f;
        Check(!game.HasEnded && game.Vitality > 0 && !((ProgressionState)stateProperty.GetValue(game)).neighborDeathPending && ReferenceEquals(original,stateProperty.GetValue(game)), "exiting during delayed death cannot kill restored journey");
        Call(hud, "ToggleConsole"); WorldEndSequence.BeginFatalCrisis(true);
        Check(WorldEndSequence.IsPresenting, "temporary explosion is actually running");
        Run("session end"); yield return WorldEndSequence.Duration + .3f;
        Check(!WorldEndSequence.IsPresenting && !game.HasEnded && ReferenceEquals(original,stateProperty.GetValue(game)), "exiting during explosion cancels pending presentation");
        Call(hud, "ToggleConsole"); Run("reset confirm"); yield return .8f;
        Check(NeighborDebugCommands.Isolated && !ReferenceEquals(original,stateProperty.GetValue(game)) && clock.Days == 1, "reset only restarts the isolated journey");
        Run("session end"); yield return .8f;
        Check(ReferenceEquals(original,stateProperty.GetValue(game)) && clock.Days == 2, "original journey remains recoverable after temporary reset");
    }
}
