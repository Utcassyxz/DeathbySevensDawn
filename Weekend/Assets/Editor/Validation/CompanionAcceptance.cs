using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.Knowledge;

public static class CompanionAcceptance
{
    static ProgressionState State => (ProgressionState)typeof(GameManager).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameManager.Instance);

    static void Check(bool ok, string message) => VerifyRevision.Check(ok, message);
    static void Capture(string name) => VerifyRevision.Capture(name);
    static void Call(object obj, string name) => obj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(obj, null);
    static void Place(Vector2 spot)
    {
        var p = GameObject.FindWithTag("Player");
        p.transform.position = spot;
        var rb = p.GetComponent<Rigidbody2D>();
        rb.position = spot;
        rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    static void Ready()
    {
        StoryDebugActions.SkipForcedStory();
        State.tutorialComplete = State.journalUnlocked = State.investigationUnlocked = true;
        State.morningStage = 10;
    }

    static void Night(TimeManager clock)
    {
        while (clock.Period != TimePeriod.Night)
            clock.AdvanceTime();
    }

    public static IEnumerator Run()
    {
        var game = GameManager.Instance;
        var clock = TimeManager.Instance;
        Ready();
        State.yogDreamSeen = true;
        var hashes = new System.Collections.Generic.HashSet<int>();
        for (int day = 1; day <= 7; day++)
        {
            var t = SevenDayExterior.View(day, TimePeriod.Afternoon);
            int hash = 17;
            foreach (var c in t.GetPixels32())
                hash = unchecked(hash * 31 + c.GetHashCode());
            hashes.Add(hash);
            Check(t.width == 192 && t.filterMode == FilterMode.Point, "Exterior is coarse pixel art on day " + day);
            if (!Application.isBatchMode)
            {
                System.IO.Directory.CreateDirectory("../docs/validation/companion-world");
                System.IO.File.WriteAllBytes("../docs/validation/companion-world/exterior-day" + day + ".png", t.EncodeToPNG());
            }
        }

        Check(hashes.Count == 7, "Seven radiation days have seven distinct views");
        Check(SevenDayExterior.View(3, TimePeriod.Morning) != SevenDayExterior.View(3, TimePeriod.Night), "Time of day selects a distinct exterior palette");
        Check(SmallPixelActors.Fish().All(s => s.texture.width == 24) && SmallPixelActors.Bubble.width == 72, "Fish and Bubble main actors use genuinely tiny pixel grids");
        Check(Resources.Load<Texture2D>("Story/FishDialogueInk") != null && Resources.Load<Texture2D>("Story/BubbleDialogueInk") != null && Resources.Load<Texture2D>("Story/EndingDistortedPair") != null, "Dialogue inserts and distorted ending atlas load");
        // A real chair, not a standing position near the desk.
        clock.ResetToDayOne();
        clock.AdvanceTime();
        clock.AdvanceTime();
        State.morningStage = 6;
        SceneManager.LoadScene("Bedroom");
        yield return .8f;
        var chair = GameObject.Find("Desk chair");
        var deskBounds = GameObject.Find("Writing desk").GetComponent<Collider2D>().bounds;
        Place(new Vector2(deskBounds.min.x - .38f, deskBounds.center.y));
        yield return .2f;
        var routine = MorningRoutine.Instance;
        Check(routine.BeginWriting(), "Writing begins beside the actual desk");
        yield return .3f;
        var actor = GameObject.FindWithTag("Player");
        Check(routine.IsWriting && Vector2.Distance(actor.transform.position, chair.GetComponent<SpriteRenderer>().bounds.center) < .4f && !actor.GetComponent<Rigidbody2D>().simulated, "Writing actor is seated on the chair with movement frozen");
        Capture("writing-seated");
        yield return .2f;
        yield return 2f;
        routine.AdvanceStory();
        yield return .4f;
        Ready();
        // Night curtains are reversible, save-backed, and conceal both the picture and its evidence.
        SceneManager.LoadScene("Bedroom");
        yield return .7f;
        Night(clock);
        var look = UnityEngine.Object.FindAnyObjectByType<WindowLookout2D>();
        Place((Vector2)look.transform.position + Vector2.down * .45f);
        yield return .2f;
        State.bedroomCurtainsClosed = false;
        Check(look.TryOpen(), "Night window opens its curtain choice");
        yield return .2f;
        Capture("window-night-choices");
        yield return .2f;
        Check(look.ToggleCurtains() && State.bedroomCurtainsClosed && !look.IsOpen, "Night window offers closing the actual bedroom curtains");
        yield return .2f;
        var cloth = GameObject.Find("Bedroom drawn curtains");
        var glass = GameObject.Find("Large story window").GetComponent<SpriteRenderer>();
        Check(cloth != null && Vector2.Distance(cloth.transform.position, glass.bounds.center) < .01f, "Curtain art covers the glass rather than the interaction point below it");
        Capture("bedroom-curtains-closed");
        yield return .2f;
        Check(look.TryOpen() && look.DisplayedTexture == null && !look.CollectObservation(), "Closed curtains hide the outside image and cannot grant window knowledge");
        Check(look.ToggleCurtains() && !State.bedroomCurtainsClosed, "Curtains can be opened again");
        clock.ResetToDayOne();
        Check(look.TryOpen() && !look.ToggleCurtains(), "Open curtains cannot be closed by the daytime action");
        look.CloseView();
        SceneManager.LoadScene("Stairwell");
        Night(clock);
        yield return 2.4f;
        Capture("compact-stairwell-night");
        yield return .2f;
        // Correct understanding is earned through two conversations, not passive room entry.
        Ready();
        clock.CompleteSleep();
        clock.CompleteSleep();
        clock.CompleteSleep();
        SceneManager.LoadScene("LivingRoom");
        yield return .6f;
        State.aquariumSealed = false;
        State.fishUnderstanding = 0;
        State.fishAlly = false;
        State.fishReportAvailable = false;
        var fish = AquariumEntity.Instance;
        Call(fish, "OpenDialogue");
        for (int n = 0; n < 15 && !fish.ChoicesVisible; n++)
        {
            fish.AdvanceDialogue();
            yield return .05f;
        }

        Check(fish.ChoicesVisible, "The actual dialogue flow presents its understanding choices");
        Check(fish.ChooseUnderstanding(false) && State.fishUnderstanding == 0 && !State.fishAlly, "Dismissive response does not earn understanding");
        Call(fish, "CloseDialogue");
        Call(fish, "OpenDialogue");
        for (int n = 0; n < 15 && !fish.ChoicesVisible; n++)
        {
            fish.AdvanceDialogue();
            yield return .05f;
        }

        Check(fish.ChoicesVisible, "The actual dialogue flow presents its understanding choices");
        Check(fish.ChooseUnderstanding(true) && State.fishUnderstanding == 1 && !State.fishAlly, "Day four explains listening rather than replacement");
        Call(fish, "CloseDialogue");
        clock.CompleteSleep();
        yield return .3f;
        Call(fish, "OpenDialogue");
        for (int n = 0; n < 15 && !fish.ChoicesVisible; n++)
        {
            fish.AdvanceDialogue();
            yield return .05f;
        }

        Check(fish.ChoicesVisible, "The actual dialogue flow presents its understanding choices");
        Check(fish.ChooseUnderstanding(true) && State.fishAlly && !Investigation.Has("fish-help-promise"), "Second correct explanation earns her help without auto-recording a clue");
        Call(fish, "CloseDialogue");
        Call(fish, "OpenDialogue");
        Check(State.fishReportAvailable && Investigation.Collect("fish-help-promise"), "Her report becomes evidence only after an explicit collect action");
        Capture("fish-companion-dialogue");
        yield return .2f;
        Call(fish, "CloseDialogue");
        clock.CompleteSleep();
        clock.CompleteSleep();
        yield return .3f;
        Check(!game.TriggerReplacementEnding() && GameObject.Find("Hugo double") == null, "Companion does not turn into the murderous Hugo double");
        Capture("fish-small-map");
        yield return .2f;
        // A late sighting must still summon Bubble on THAT night, not a hard-coded Day 2.
        Ready();
        clock.ResetToDayOne();
        for (int i = 0; i < 4; i++)
            clock.CompleteSleep();
        Night(clock);
        State.yogDreamSeen = State.bubbleIntroduced = State.bubblePortraitUnlocked = false;
        State.ancientGodSeen = false;
        game.RecordAncientGodEncounter();
        clock.SleepUntilNextMorning();
        yield return .5f;
        Check(NightDreamSequence.Instance.IsBubble && clock.Days == 5 && !NightDreamSequence.Instance.PortraitKnown, "First contact on day five begins as an unknown silhouette");
        Capture("bubble-unknown");
        yield return .2f;
        for (int n = 0; n < 16 && NightDreamSequence.Instance.PageIndex < 2; n++)
        {
            NightDreamSequence.Instance.Advance();
            yield return .25f;
        }

        yield return .3f;
        Check(State.bubblePortraitUnlocked, "Finishing the self-introduction unlocks the portrait");
        Capture("bubble-dissolving");
        yield return 1.8f;
        Capture("bubble-revealed");
        yield return .2f;
        for (int i = 0; i < 35 && NightDreamSequence.WorldBlocked; i++)
        {
            NightDreamSequence.Instance.Advance();
            yield return .2f;
        }

        Check(game.HasLoopAbility && State.bubbleIntroduced && clock.Days == 6, "Return is granted only after the first complete meeting");
        DeathBySeventhsDawn.TimeSystem.LoopManager.Instance.RestartLoop();
        yield return .8f;
        Ready();
        Check(game.LoopCount > 0 && State.bubblePortraitUnlocked && !State.fishAlly && State.fishUnderstanding == 0, "Bubble identity survives a loop while fish relationship resets");
        clock.CompleteSleep();
        Night(clock);
        game.RecordAncientGodEncounter();
        clock.SleepUntilNextMorning();
        yield return .4f;
        Check(NightDreamSequence.Instance.IsBubble && NightDreamSequence.Instance.PortraitKnown, "Later-loop observation returns to the known Bubble");
        for (int i = 0; i < 20 && !NightDreamSequence.Instance.IsShop; i++)
        {
            NightDreamSequence.Instance.Advance();
            yield return .2f;
        }

        Check(NightDreamSequence.Instance.IsShop && BubbleMarket.Offers.Length == 0, "Repeat meeting leads to an empty authorable market, not another introduction");
        Capture("bubble-market-framework");
        yield return .2f;
        float before = game.Sanity;
        Check(!BubbleMarket.Purchase("not-authored") && Mathf.Approximately(before, game.Sanity), "No authored offers means no accidental SAN deduction");
        State.knowledge = State.mutation = State.sanityBurden = 0;
        State.bubblePurchases.Clear();
        before = game.Sanity;
        Check(game.ExchangeSanity("acceptance-only", 10, 1) && Mathf.Abs(game.Sanity - (before - 10)) < .01f, "Sanity exchange charges exactly its total displayed price");
        Check(!game.ExchangeSanity("acceptance-only", 10, 1) && !game.ExchangeSanity("invalid", float.NaN, 1), "Repeated or invalid trades cannot charge SAN");
        NightDreamSequence.Instance.Advance();
        yield return .8f;
        Check(!NightDreamSequence.WorldBlocked, "Market exit completes sleep and restores control");
        State.bedroomCurtainsClosed = true;
        State.fishAlly = true;
        State.fishUnderstanding = 3;
        ProgressionSave.Write(State);
        var roundtrip = ProgressionSave.Load();
        Check(roundtrip.version == 3 && roundtrip.bedroomCurtainsClosed && roundtrip.fishAlly && roundtrip.bubblePurchases.Contains("acceptance-only") && roundtrip.bubblePortraitUnlocked, "Version-three save reload retains curtains, fish, portrait and shop purchases");
        ProgressionSave.Write(new ProgressionState { version = 2, loopCount = 1, yogDreamSeen = true, ancientGodSeen = true });
        var legacy = ProgressionSave.Load();
        Check(legacy.version == 3 && legacy.bubbleIntroduced && legacy.bubblePortraitUnlocked, "Existing awakened saves migrate without repeating Bubble's introduction");
        ProgressionSave.Write(new ProgressionState { version = 1, loopCount = 1, journal = new System.Collections.Generic.List<JournalEntry> { new JournalEntry { id = "legacy-note", title = "legacy" } } });
        var oldest = ProgressionSave.Load();
        Check(oldest.version == 3 && oldest.tutorialComplete && oldest.bubbleIntroduced, "Version-one tutorial and loop normalization precedes the version-three upgrade");
        ProgressionSave.Write(State);
    }
}
