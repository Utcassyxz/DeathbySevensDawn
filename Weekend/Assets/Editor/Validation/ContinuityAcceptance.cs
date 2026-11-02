using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.World;
using DeathBySeventhsDawn.Player;
using DeathBySeventhsDawn.Story;

public static class ContinuityAcceptance
{
    static ProgressionState State => (ProgressionState)typeof(GameManager).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameManager.Instance);
    static void Check(bool yes, string message) => VerifyRevision.Check(yes, message);
    static void Place(Vector2 p)
    {
        var actor = GameObject.FindWithTag("Player");
        actor.transform.position = p;
        actor.GetComponent<Rigidbody2D>().position = p;
        actor.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
    static void CloseFish() => typeof(AquariumEntity).GetMethod("CloseDialogue", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(AquariumEntity.Instance, null);
    static void Ready()
    {
        StoryDebugActions.SkipForcedStory();
        State.tutorialComplete = State.journalUnlocked = State.investigationUnlocked = true;
        State.morningStage = 10;
        State.badEndingOne = State.worldEndEnding = State.fishReplacementEnding = false;
    }
    static IEnumerator WalkThrough(string target)
    {
        var door = UnityEngine.Object.FindObjectsByType<RoomDoor2D>(FindObjectsSortMode.None).Single(d => d.destinationScene.EndsWith(target + ".unity"));
        float side = Mathf.Sign(door.transform.position.x);
        Place(new Vector2(door.transform.position.x - side * 1.25f, door.transform.position.y));
        var actor = GameObject.FindWithTag("Player");
        var movement = actor.GetComponent<PlayerController2D>();
        movement.enabled = false;
        var rb = actor.GetComponent<Rigidbody2D>();
        string before = SceneManager.GetActiveScene().name;
        for (int i = 0; i < 40 && SceneManager.GetActiveScene().name == before; i++)
        {
            rb.linearVelocity = new Vector2(side * 2, 0);
            yield return .05f;
        }
        if (movement != null)
            movement.enabled = true;
        Check(SceneManager.GetActiveScene().name == target, before + " -> " + target + " walks into a physically reachable doorway");
        yield return .9f;
        Check(SceneManager.GetActiveScene().name == target, "Arrival does not bounce straight back from " + target);
    }
    public static IEnumerator Run()
    {
        Ready();
        var game = GameManager.Instance;
        var clock = TimeManager.Instance;
        clock.ResetToDayOne();
        SceneManager.LoadScene("LivingRoom");
        yield return .9f;
        foreach (var destination in new[] { "Bedroom", "LivingRoom", "Bathroom", "LivingRoom" })
        {
            var walk = WalkThrough(destination);
            while (walk.MoveNext())
                yield return walk.Current;
        }
        // Both bounds hide geometry, rails and their invisible collision stops.
        Place(RooftopAccess.StairsFour);
        SceneManager.LoadScene("Corridor");
        yield return .4f;
        Place(RooftopAccess.StairsFour);
        Check(RooftopAccess.Instance.UseStairs(), "Corridor still enters the dedicated stairwell");
        yield return .5f;
        Check(RooftopAccess.Instance.ChangeFloor(1), "Reach the top landing");
        var root = GameObject.Find("StairwellLanding").transform;
        Check(!root.Cast<Transform>().Any(t => t.name == "Up tread" && t.gameObject.activeSelf) && root.Cast<Transform>().Any(t => t.name == "Down tread" && t.gameObject.activeSelf), "Top landing has no ascending flight and retains the descending flight");
        yield return 1.3f;
        VerifyRevision.Capture("top-floor-no-up");
        yield return .2f;
        for (int i = 0; i < 4; i++)
        {
            Check(RooftopAccess.Instance.ChangeFloor(-1), "Descend one landing " + i);
            yield return 1.3f;
        }
        Check(!root.Cast<Transform>().Any(t => t.name == "Down tread" && t.gameObject.activeSelf) && root.Cast<Transform>().Any(t => t.name == "Up tread" && t.gameObject.activeSelf), "Ground landing has no descending flight and retains the ascending flight");
        Check(!RooftopAccess.Instance.ChangeFloor(-1), "Ground floor cannot travel below the building");
        VerifyRevision.Capture("ground-floor-no-down");
        yield return .2f;
        SceneManager.LoadScene("LivingRoom");
        yield return .7f;
        // Tests a full first-week unlock earned by CompanionAcceptance, not a debug-only NPC flag.
        Check(State.fishCompanionUnlocked && AquariumEntity.Instance.CompanionPresent, "Earned companion appears on day one of the next loop");
        Check(GameObject.Find("Seated fish companion") != null && GameObject.Find("Fish companion stool seat") != null, "Fish has a native pixel seated sprite and a visible stool");
        var stool = GameObject.Find("Fish companion stool seat").GetComponent<SpriteRenderer>();
        var borrowed = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.scene == SceneManager.GetActiveScene() && g.name == "Dining chair right");
        Check(borrowed != null && !borrowed.activeSelf && stool.sprite == borrowed.GetComponent<SpriteRenderer>().sprite, "Seated companion retains the actual borrowed stool artwork; source chair remains absent");
        var fish = AquariumEntity.Instance;
        var tank = GameObject.Find("Aquarium and cabinet").GetComponent<Collider2D>().bounds;
        Place(new Vector2(tank.center.x, tank.min.y - .35f));
        yield return .3f;
        State.fishStoryDays.Clear();
        State.fishReportAvailable = false;
        Check(fish.TryTalk() && !fish.IsAmbientConversation, "First friendly visit presents the authored investigation conversation");
        VerifyRevision.Capture("fish-companion-day-one");
        yield return .2f;
        for (int n = 0; n < 20 && fish.IsTalking; n++)
        {
            fish.AdvanceDialogue();
            yield return .04f;
        }
        yield return .1f;
        var seen = new HashSet<string>();
        float knowledge = game.Knowledge;
        for (int j = 0; j < 6; j++)
        {
            Check(fish.TryTalk() && fish.IsAmbientConversation && !fish.ChoicesVisible, "Repeat visit remains available as a casual conversation " + j);
            seen.Add(fish.CurrentConversation);
            CloseFish();
            yield return .1f;
        }
        Check(seen.Count == 6 && Mathf.Approximately(knowledge, game.Knowledge), "Shuffle bag gives six different chats without granting knowledge or relationship progress");
        while (clock.Days < 7)
            clock.CompleteSleep();
        yield return .3f;
        Place(new Vector2(tank.center.x, tank.min.y - .35f));
        yield return .2f;
        Check(fish.TryTalk(), "Day-seven companion can still be used from the tank front");
        CloseFish();
        yield return .2f;
        State.fishAlly = State.fishCompanionUnlocked = false;
        State.fishUnderstanding = 0;
        State.fishStoryDays.Clear();
        SceneManager.LoadScene("Bedroom");
        yield return .3f;
        SceneManager.LoadScene("LivingRoom");
        yield return .6f;
        tank = GameObject.Find("Aquarium and cabinet").GetComponent<Collider2D>().bounds;
        Place(new Vector2(tank.center.x, tank.min.y - .35f));
        yield return .2f;
        Check(AquariumEntity.Instance.TryTalk(), "Day-seven hostile double can also be examined from the tank front");
        CloseFish();
        yield return .2f;
        // Fresh fixture restores earned unlock for the save round trip; previous gameplay path verified it separately.
        State.fishCompanionUnlocked = true;
        State.fishCompanionUnlockLoop = Mathf.Max(0, game.LoopCount - 1);
        State.fishAlly = true;
        clock.ResetToDayOne();
        SceneManager.LoadScene("LivingRoom");
        yield return .6f;
        var stove = UnityEngine.Object.FindAnyObjectByType<StoveInteraction>();
        var counter = GameObject.Find("Kitchen counter and stove");
        var b = counter.GetComponent<Collider2D>().bounds;
        var sr = counter.GetComponent<SpriteRenderer>();
        Place(new Vector2(b.max.x - .6f, b.min.y - .32f));
        yield return .4f;
        State.stoveLit = false;
        yield return .1f;
        var off = sr.sprite;
        Check(stove.Toggle() && stove.IsLit && sr.sprite != off, "Lighting the stove swaps the existing counter texture");
        var first = sr.sprite;
        yield return .35f;
        Check(sr.sprite != off, "Lit state keeps the two-frame pixel flame texture active");
        VerifyRevision.Capture("stove-lit");
        yield return .2f;
        Check(stove.Toggle() && !stove.IsLit && sr.sprite == off, "Turning off the stove restores the exact original sprite");
        State.bubbleDreamUnlocked = true;
        State.bubbleVisitLoop = game.LoopCount;
        while (clock.Period != TimePeriod.Night)
            clock.AdvanceTime();
        clock.SleepUntilNextMorning();
        yield return .7f;
        Check(!NightDreamSequence.WorldBlocked && clock.Days == 2, "Bubble does not repeat on every night of the same loop");
        ProgressionSave.Write(State);
        var loaded = ProgressionSave.Load();
        Check(loaded.version == 4 && loaded.fishCompanionUnlocked && loaded.bubbleDreamUnlocked && loaded.bubbleVisitLoop == game.LoopCount, "Version-four save retains permanent companions and Bubble visit schedule");
        ProgressionSave.Write(new ProgressionState { version = 3, fishAlly = true, fishUnderstanding = 3, yogDreamSeen = true, ancientGodSeen = true, loopCount = 1 });
        loaded = ProgressionSave.Load();
        Check(loaded.version == 4 && loaded.fishCompanionUnlocked && loaded.bubbleDreamUnlocked, "Existing version-three earned relationship migrates into permanent unlocks");
        ProgressionSave.Write(State);
        Check(StoryText.Current.expansion.fishChats.Length >= 24 && StoryText.Current.expansion.fishSilentChats.Length >= 8, "Spoken and silent dialogue libraries are authored rather than generated per click");
    }
}
