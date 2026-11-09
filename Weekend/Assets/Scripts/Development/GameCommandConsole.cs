using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Progression
{
    // This registry also generates the handbook. Syntax and stable IDs are never translated.
    public static class GameCommandConsole
    {
        public static readonly string[] Syntax = {
            "help [command]", "clear", "history", "status", "close", "session begin|end",
            "debug on|off", "day <1..9999>", "time <morning|afternoon|evening|night>",
            "date <day> <HH:mm>", "next period|day", "sleep", "heal [amount]", "damage <amount>",
            "kill", "sanity <0..100>", "knowledge <ordinary|forbidden|cosmic>",
            "mutation unlock shoulder_eye", "scene list|<scene>", "item list|give <id>",
            "evidence list|unlock <id>", "deduce <left-id> <right-id>", "flags reset",
            "crisis <building|city|cosmic> <on|off>", "event list|<id>", "replay news",
            "ending <1..3>", "skip", "board", "reset confirm"
        };
        public static readonly string[] Items = { NeighborSlice.Crowbar, NeighborSlice.Oil, NeighborSlice.Medium, NeighborSlice.Tool, CultNeighbor.Token, "invitation" };
        public static readonly string[] Events = { "sushi_normal", "sushi_corrupt", "circus_normal", "circus_corrupt", "neighbor_encounter", "neighbor_peace", "neighbor_known", "neighbor_prepared", "neighbor_eye", "neighbor_death", "neighbor_victory", "apartment_explosion", "building_saved" };
        public static string Description(string verb) => StoryText.UI("console.cmd." + verb);
        static string Message(string id) => StoryText.UI("console." + id);
        public static string Help(string verb = null)
        {
            var selected = Syntax.Where(s => verb == null || s.Split(' ')[0].Equals(verb, StringComparison.OrdinalIgnoreCase)).ToArray();
            return selected.Length == 0 ? Message("unknown") : string.Join("\n", selected.Select(s => s + "  —  " + Description(s.Split(' ')[0])));
        }
        static string Usage(string verb) => Message("usage") + " " + Syntax.First(s => s.Split(' ')[0] == verb);
        static bool Number(string s, float min, float max, out float n) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out n) && !float.IsNaN(n) && !float.IsInfinity(n) && n >= min && n <= max;
        static bool Day(string s, out int n) => int.TryParse(s, out n) && n >= 1 && n <= 9999;
        static string Canonical(IEnumerable<string> ids, string input) => ids.FirstOrDefault(s => s.Equals(input, StringComparison.OrdinalIgnoreCase));
        public static string Execute(string input)
        {
            var a = (input ?? "").Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (a.Length == 0) return "";
            string v = a[0].ToLowerInvariant();
            if (!Syntax.Any(s => s.Split(' ')[0] == v)) return Message("unknown") + " " + input;
            var game = GameManager.Instance; var clock = TimeManager.Instance;
            if (v == "help") return a.Length <= 2 ? Help(a.Length == 2 ? a[1] : null) : Usage(v);
            if (v == "clear" || v == "history" || v == "close") return a.Length == 1 ? Message(v) : Usage(v);
            if (game == null || clock == null) return Message("unavailable");
            // Commands never touch the normal journey, including after debug off.
            if (!NeighborDebugCommands.Isolated) return Message("noSession");
            var state = game.State;
            string arg = a.Length > 1 ? a[1].ToLowerInvariant() : "";
            float number; int day;
            switch (v)
            {
                case "status":
                    if (a.Length != 1) return Usage(v);
                    return $"{Message("isolated")} | debug {(StoryDebugActions.Enabled ? "on" : "off")}\nDay {clock.Days} / {clock.Period} / {clock.GameMinute / 60:00}:{clock.GameMinute % 60:00} | {SceneManager.GetActiveScene().name}\nHP {game.Vitality:0.#}/{game.MaxVitality:0.#} | SAN {game.Sanity:0.#} | Mutation {game.Mutation:0.#} | Knowledge {game.Knowledge:0.#} | Loops {game.LoopCount}\n" + string.Join(", ", game.Settings.abilities.Where(x => x != null && game.HasAbility(x.id)).Select(x => x.id));
                case "session":
                    if (a.Length != 2 || (arg != "begin" && arg != "end")) return Usage(v);
                    if (arg == "end") { NeighborDebugCommands.EndSession(); StoryDebugActions.SetEnabled(false); UnityEngine.Object.FindAnyObjectByType<ProgressionHUD>()?.CloseForEvent(); return Message("restored"); }
                    return Message("isolated");
                case "debug":
                    if (a.Length != 2 || (arg != "on" && arg != "off")) return Usage(v);
                    StoryDebugActions.SetEnabled(arg == "on"); return "debug " + arg;
                case "day":
                    if (a.Length == 3 && arg == "set") a = new[] { v, a[2] };
                    if (a.Length != 2 || !Day(a[1], out day)) return Usage(v);
                    clock.SetNarrativeTime(day, clock.Period, clock.GameMinute); return Message("done");
                case "time":
                    if (a.Length == 3 && arg == "set") a = new[] { v, a[2] };
                    var names = new[] { "morning", "afternoon", "evening", "night" };
                    int period = a.Length == 2 ? Array.IndexOf(names, a[1].ToLowerInvariant()) : -1;
                    if (period < 0 && a.Length == 2 && int.TryParse(a[1], out int p) && p >= 0 && p <= 3) period = p;
                    if (period < 0) return Usage(v);
                    clock.SetNarrativeTime(clock.Days, (TimePeriod)period, new[] { 420, 720, 1020, 1260 }[period]); return Message("done");
                case "date":
                    if (a.Length != 3 || !Day(a[1], out day)) return Usage(v);
                    var parts = a[2].Split(':');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int h) || !int.TryParse(parts[1], out int m) || h < 0 || h > 25 || m < 0 || m > 59 || (h >= 2 && h < 7)) return Usage(v);
                    if (h < 2) h += 24;
                    clock.SetNarrativeTime(day, h < 12 ? TimePeriod.Morning : h < 17 ? TimePeriod.Afternoon : h < 21 ? TimePeriod.Evening : TimePeriod.Night, h * 60 + m); return Message("done");
                case "next":
                    if (a.Length != 2 || (arg != "period" && arg != "day")) return Usage(v);
                    if (arg == "day") { if (clock.Days >= 9999) return Usage("day"); clock.SetNarrativeTime(clock.Days + 1, TimePeriod.Morning, 420); }
                    else clock.AdvanceTime(); return Message("done");
                case "sanity":
                    if (a.Length == 3 && arg == "set") a = new[] { v, a[2] };
                    if (a.Length != 2 || !Number(a[1], 0, 100, out number)) return Usage(v);
                    if (!StoryDebugActions.Enabled) return Message("debugRequired"); game.DebugSetSanity(number); return Message("done");
                case "heal": case "damage":
                    if (v == "heal" && a.Length == 1) { game.Heal(game.MaxVitality); return Message("done"); }
                    if (a.Length != 2 || !Number(a[1], 0, 100000, out number)) return Usage(v);
                    if (v == "heal") game.Heal(number); else game.Damage(number); return Message("done");
                case "kill": if (a.Length != 1) return Usage(v); game.Damage(game.MaxVitality); return Message("done");
                case "sleep": if (a.Length != 1) return Usage(v); UnityEngine.Object.FindAnyObjectByType<ProgressionHUD>()?.CloseForEvent(); clock.SleepUntilNextMorning(); return Message("done");
                case "knowledge":
                    if (a.Length != 2 || !Enum.TryParse(a[1], true, out KnowledgeTier tier) || !Enum.IsDefined(typeof(KnowledgeTier), tier)) return Usage(v);
                    KnowledgeManager.Instance.Unlock("console-" + Guid.NewGuid().ToString("N"), tier); return Message("done");
                case "scene":
                    if (a.Length == 2 && arg == "list") return string.Join("\n", NeighborDebugCommands.Scenes);
                    string target = a.Length == 2 ? a[1] : a.Length == 3 && arg == "goto" ? a[2] : "";
                    string scene = Canonical(NeighborDebugCommands.Scenes, target);
                    if (scene == null) return Usage(v);
                    return Legacy("scene goto " + scene);
                case "item":
                    if (a.Length == 2 && arg == "list") return string.Join("\n", Items);
                    string item = a.Length == 3 && arg == "give" ? Canonical(Items, a[2]) : null;
                    if (item == null) return Usage(v);
                    if (!state.inventory.Contains(item)) state.inventory.Add(item); ProgressionSave.Write(state); return Message("done");
                case "evidence":
                    if (a.Length == 2 && arg == "list") return string.Join("\n", Investigation.Catalog.evidence.Select(e => e.id + " — " + e.title));
                    string evidence = a.Length == 3 && arg == "unlock" ? Canonical(Investigation.Catalog.evidence.Select(e => e.id), a[2]) : null;
                    if (evidence == null) return Usage(v);
                    state.journalUnlocked = state.investigationUnlocked = true; Investigation.Collect(evidence); return Message("done");
                case "deduce":
                    if (a.Length != 3) return Usage(v);
                    return Investigation.Connect(a[1], a[2], true) ? Message("done") : Message("deduceFailed");
                case "mutation":
                    if (a.Length != 3 || arg != "unlock" || !a[2].Equals("shoulder_eye", StringComparison.OrdinalIgnoreCase)) return Usage(v);
                    state.shoulderEyeUnlocked = true; ProgressionSave.Write(state); return Message("done");
                case "flags": if (a.Length != 2 || arg != "reset") return Usage(v); return Legacy("flags reset");
                case "crisis":
                    if (a.Length != 3 || !new[] { "building", "city", "cosmic" }.Contains(arg) || !new[] { "on", "off" }.Contains(a[2].ToLowerInvariant())) return Usage(v);
                    bool resolved = a[2].Equals("off", StringComparison.OrdinalIgnoreCase);
                    if (arg == "building") { state.buildingCrisisResolved = state.serviceRouteIsolated = state.day7BombDefused = resolved; }
                    else if (arg == "city") state.cityCrisisResolved = resolved; else state.daySevenCrisisResolved = resolved;
                    ProgressionSave.Write(state); return Message("done");
                case "event":
                    if (a.Length == 2 && arg == "list") return string.Join("\n", Events);
                    string id = a.Length == 2 ? a[1] : a.Length == 3 && arg == "trigger" ? a[2] : "";
                    string ev = Canonical(Events, id); return ev == null ? Usage(v) : Legacy("event trigger " + ev);
                case "replay": if (a.Length != 2 || arg != "news") return Usage(v); UnityEngine.Object.FindAnyObjectByType<ProgressionHUD>()?.CloseForEvent(); DayOneSupernovaEvent.Replay(); return Message("done");
                case "ending":
                    if (a.Length != 2 || !int.TryParse(a[1], out int ending) || ending < 1 || ending > 3) return Usage(v);
                    StoryDebugActions.PreviewEnding(ending); return Message("preview");
                case "skip": if (a.Length != 1) return Usage(v); StoryDebugActions.SkipForcedStory(); return Message("done");
                case "board": if (a.Length != 1) return Usage(v); UnityEngine.Object.FindAnyObjectByType<ProgressionHUD>()?.CloseForEvent(); InvestigationDevTools.Open(); return Message("done");
                case "reset": if (a.Length != 2 || arg != "confirm") return Usage(v); game.StartNewJourney(); return Message("resetDone");
            }
            return Message("unknown");
        }
        static string Legacy(string command) => StoryDebugActions.Enabled ? NeighborDebugCommands.Execute(command) : Message("debugRequired");
        public static string[] Complete(string input)
        {
            string value = (input ?? "").TrimStart();
            var pool = Syntax.Select(s => s.Split(' ')[0]).Distinct().ToList();
            pool.AddRange(NeighborDebugCommands.Scenes.Select(s => "scene " + s));
            pool.AddRange(NeighborDebugCommands.Scenes.Select(s => "scene goto " + s));
            pool.AddRange(Syntax.Select(s => "help " + s.Split(' ')[0]));
            pool.AddRange(new[] { "ordinary", "forbidden", "cosmic" }.Select(s => "knowledge " + s));
            pool.AddRange(new[] { "building", "city", "cosmic" }.SelectMany(s => new[] { "crisis " + s + " on", "crisis " + s + " off" }));
            pool.AddRange(Items.Select(s => "item give " + s)); pool.AddRange(Events.Select(s => "event " + s));
            pool.AddRange(Investigation.Catalog.evidence.Select(s => "evidence unlock " + s.id));
            pool.AddRange(new[] { "session begin", "session end", "debug on", "debug off", "time morning", "time afternoon", "time evening", "time night", "next period", "next day", "flags reset", "replay news", "reset confirm", "mutation unlock shoulder_eye", "scene list", "item list", "event list", "evidence list" });
            return pool.Where(s => s.StartsWith(value, StringComparison.OrdinalIgnoreCase)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    // UI-independent scrollback and command history; never stores history in the player's save.
    public sealed class ConsoleBuffer
    {
        public readonly List<string> Lines = new List<string>();
        public readonly List<string> History = new List<string>();
        int cursor; string draft = "";
        public void Add(string text) { Lines.AddRange((text ?? "").Replace("\r", "").Split('\n')); if (Lines.Count > 200) Lines.RemoveRange(0, Lines.Count - 200); }
        public void Record(string text) { if (History.Count == 0 || History[History.Count - 1] != text) History.Add(text); if (History.Count > 100) History.RemoveAt(0); cursor = History.Count; draft = ""; }
        public string Navigate(int direction, string current) { if (cursor == History.Count) draft = current; cursor = Mathf.Clamp(cursor + direction, 0, History.Count); return cursor == History.Count ? draft : History[cursor]; }
    }
}
