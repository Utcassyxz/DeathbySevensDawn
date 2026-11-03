// 职责：验证北墙门与真实墙面碰撞、关系线晨间动画、真实凳子搬迁和存档去重。
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Player;
using DeathBySeventhsDawn.World;

public static class DoorMorningAcceptance
{
    static ProgressionState State => (ProgressionState)typeof(GameManager).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameManager.Instance);
    static void Check(bool yes, string message) => VerifyRevision.Check(yes, message);
    static void Place(Vector2 p)
    {
        var player = GameObject.FindWithTag("Player");
        player.transform.position = p;
        var rb = player.GetComponent<Rigidbody2D>();
        rb.position = p;
        rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
    static IEnumerator Push(Vector2 start, Vector2 direction, Func<Vector2, bool> remainsOutside, string message)
    {
        Place(start);
        var player = GameObject.FindWithTag("Player");
        var movement = player.GetComponent<PlayerController2D>();
        movement.enabled = false;
        var rb = player.GetComponent<Rigidbody2D>();
        for (int n = 0; n < 14; n++)
        {
            rb.linearVelocity = direction * 3;
            yield return .05f;
        }
        rb.linearVelocity = Vector2.zero;
        movement.enabled = true;
        Check(remainsOutside(rb.position), message);
    }
    public static IEnumerator Corridor()
    {
        StoryDebugActions.SkipForcedStory();
        SceneManager.LoadScene("Corridor");
        yield return .7f;
        var exit = GameObject.Find("EXIT fire door leaf").GetComponent<SpriteRenderer>();
        var neighbour = GameObject.Find("neighbor wood door").GetComponent<SpriteRenderer>();
        Check(Mathf.Abs(exit.bounds.center.y - neighbour.bounds.center.y) < .05f && exit.sortingLayerID == neighbour.sortingLayerID, "EXIT shares the neighbours' wall plane and sorting layer");
        Check(exit.sortingOrder >= neighbour.sortingOrder && GameObject.Find("EXIT push bar") != null, "Metal fire door is visible above the wall with a real push bar");
        foreach (float x in new[] { -4.7f, 0f, 4.7f, 2f })
        {
            var movement = Push(new Vector2(x, .35f), Vector2.up, p => p.y < 1.0f, "Player cannot walk onto north wall or door face at " + x);
            while (movement.MoveNext())
                yield return movement.Current;
        }
        foreach (float x in new[] { -3f, 3f })
        {
            var movement = Push(new Vector2(x, -.8f), Vector2.down, p => p.y > -1.7f, "South wall physically stops walking at " + x);
            while (movement.MoveNext())
                yield return movement.Current;
        }
        foreach (int side in new[] { -1, 1 })
        {
            var movement = Push(new Vector2(side * 6.2f, 0), Vector2.right * side, p => Mathf.Abs(p.x) < 6.9f, "Corridor side wall physically stops walking " + side);
            while (movement.MoveNext())
                yield return movement.Current;
        }
        Place(RooftopAccess.StairsFour);
        yield return .2f;
        VerifyRevision.Capture("north-wall-exit");
        yield return .2f;
        Check(RooftopAccess.Instance.UseStairs(), "EXIT remains usable from the walkable floor");
        yield return .6f;
        Check(RooftopAccess.Instance.UseStairs(), "Fourth-floor stairwell door returns to the corridor");
        yield return .6f;
        Check(SceneManager.GetActiveScene().name=="Corridor","Return uses the fire door rather than descending a floor");
        var door = UnityEngine.Object.FindObjectsByType<RoomDoor2D>().Single(d => d.destinationScene.EndsWith("LivingRoom.unity"));
        Place(door.transform.position + Vector3.up * .8f);
        var actor = GameObject.FindWithTag("Player");
        var controller = actor.GetComponent<PlayerController2D>();
        controller.enabled = false;
        var body = actor.GetComponent<Rigidbody2D>();
        for (int i = 0; i < 30 && SceneManager.GetActiveScene().name == "Corridor"; i++)
        {
            body.linearVelocity = Vector2.down * 2;
            yield return .05f;
        }
        if (controller != null)
            controller.enabled = true;
        Check(SceneManager.GetActiveScene().name == "LivingRoom", "New wall solids preserve walking through the actual 402 doorway");
        yield return .5f;
    }
    public static IEnumerator Welcome()
    {
        for (int i = 0; i < 35 && !MorningRoutine.Instance.FishMorningPlaying; i++)
            yield return .2f;
        var morning = MorningRoutine.Instance;
        Check(morning.FishMorningPlaying && SceneManager.GetActiveScene().name == "Bedroom", "Earned relationship starts the next loop with Fish waking Hugo in his bedroom");
        Check(GameObject.Find("Fish morning visitor") != null && !GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>().simulated, "Morning visitor animates while player movement is locked");
        VerifyRevision.Capture("fish-wake-hungry");
        yield return .2f;
        int steps = 0;
        while (morning.FishMorningPlaying && steps++ < 70)
        {
            int page = morning.FishMorningPage;
            yield return page == 4 ? 1.2f : .38f;
            if (page == 4 && GameObject.Find("Stool being borrowed") != null)
            {
                Check(GameObject.Find("Stool being borrowed") != null && GameObject.Find("Dining chair right") == null, "Fish drags the actual dining stool rather than leaving a duplicate behind");
                VerifyRevision.Capture("fish-borrows-stool");
            }
            if(page == 4 && GameObject.Find("Stool being borrowed") == null) VerifyRevision.Capture("fish-pushes-tank");
            morning.AdvanceStory();
            yield return .12f;
        }
        Check(!morning.FishMorningPlaying && SceneManager.GetActiveScene().name == "LivingRoom", "Finishing the animated sequence restores free movement in the living room");
        var fish = AquariumEntity.Instance;
        Check(State.fishMorningLoop == GameManager.Instance.LoopCount && State.fishStoolBorrowed, "Welcome completion is saved once per actual loop");
        var saved = ProgressionSave.Load();
        Check(saved.fishStoolBorrowed && saved.fishMorningLoop == GameManager.Instance.LoopCount, "Welcome and furniture ownership survive a disk save round trip");
        var tank = GameObject.Find("Aquarium and cabinet").GetComponent<SpriteRenderer>().bounds;
        var actor = GameObject.Find("Seated fish companion");
        Check(actor != null && Vector2.Distance(actor.transform.parent.position, fish.CompanionSeatPosition) < .02f && tank.center.x > fish.CompanionSeatPosition.x + 1.5f, "Seated fish occupies the former tank position, and the tank has been pushed aside");
        Check(GameObject.Find("Fish companion stool seat") != null && GameObject.Find("Dining chair right") == null, "Borrowed stool stays beside the tank and is absent from the dining table");
        Check(GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>().simulated && !StoryJournal.WorldBlocked, "Player physics and world interaction unlock after the sequence");
        VerifyRevision.Capture("fish-seat-raised");
        yield return .3f;
        SceneManager.LoadScene("Bedroom");
        yield return .4f;
        SceneManager.LoadScene("LivingRoom");
        yield return .5f;
        Check(!morning.FishMorningPlaying && GameObject.Find("Seated fish companion") != null && GameObject.Find("Dining chair right") == null, "Returning to rooms cannot replay the welcome or duplicate the borrowed stool");
    }
}
