using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.TimeSystem;
public static class DeathCycleAcceptance
{
    static ProgressionState S => (ProgressionState)typeof(GameManager).GetProperty("State",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(GameManager.Instance);
    static void Check(bool ok,string label)=>VerifyRevision.Check(ok,label);
    static void Set(int day,TimePeriod period)
    {StoryDebugActions.SetEnabled(true);TimeManager.Instance.DebugSetTime(day,period);StoryDebugActions.SetEnabled(false);}
    static void Place(Vector2 p)
    {var go=GameObject.FindWithTag("Player");go.transform.position=p;go.GetComponent<Rigidbody2D>().position=p;Physics2D.SyncTransforms();}
    public static IEnumerator Run()
    {
        var game=GameManager.Instance;var clock=TimeManager.Instance;
        game.StartNewJourney();yield return .6f;
        StoryDebugActions.SetEnabled(true);
        Check(!TutorialGate.IsBlocked("aquarium")&&!TutorialGate.IsBlocked("telescope"),"Free debug bypasses tutorial interaction gates");
        StoryDebugActions.Jump("Corridor",2,0);yield return .7f;
        Check(SceneManager.GetActiveScene().path=="Assets/Apartment/Corridor.unity","Scene jumps use consolidated Assets/Apartment scene");
        var actor=GameObject.FindWithTag("Player");
        Check(actor.GetComponent<SpriteRenderer>().bounds.size.x<.65f,"Corridor avatar is smaller than the old square");
        foreach(var roomDoor in Object.FindObjectsByType<DeathBySeventhsDawn.World.RoomDoor2D>(FindObjectsSortMode.None))
            Check(Application.CanStreamedLevelBeLoaded(roomDoor.destinationScene),"Relocated door destination remains loadable: "+roomDoor.destinationScene);
        VerifyRevision.Capture("right-side-exit-small-player");yield return .2f;
        var door=GameObject.Find("EXIT stairwell door");
        Check(door!=null&&door.transform.position.x>6,"EXIT is on the right side wall");
        Place(RooftopAccess.StairsFour);yield return .2f;
        Check(RooftopAccess.Instance.UseStairs(),"Right-hand EXIT can be entered");yield return .7f;
        Check(SceneManager.GetActiveScene().name=="Stairwell","Right-hand EXIT reaches separate stairwell");
        StoryDebugActions.Jump("LivingRoom",1,0);yield return .7f;
        Check(GameObject.Find("Bookcase")==null&&GameObject.Find("Removed bookcase")==null,"Living room shelf removed from visible active scene");
        StoryDebugActions.SetEnabled(false);S.buildingCrisisResolved=S.cityCrisisResolved=S.daySevenCrisisResolved=true;
        Place(new Vector2(0,-1.65f));yield return .3f;var at=(Vector2)GameObject.FindWithTag("Player").transform.position;
        for(int n=0;n<4;n++)clock.AdvanceTime();yield return .1f;
        Check(clock.Days==2&&clock.Period==TimePeriod.Morning,"Waiting crosses midnight without forcing sleep");
        Check(SceneManager.GetActiveScene().name=="LivingRoom"&&Vector2.Distance(at,GameObject.FindWithTag("Player").transform.position)<.08f,"Waiting preserves scene and position: "+SceneManager.GetActiveScene().name+" "+at+" -> "+GameObject.FindWithTag("Player").transform.position);
        float sanity=game.Sanity;
        for(int n=0;n<4;n++)clock.AdvanceTime();
        Check(Mathf.Abs(game.Sanity-sanity)<.01f,"Two sleepless days do not charge SAN early");
        for(int n=0;n<4;n++)clock.AdvanceTime();
        Check(Mathf.Abs(game.Sanity-(sanity-5))<.01f,"Third sleepless midnight costs exactly five SAN");yield return .3f;
        Check(clock.Days==4&&MorningRoutine.Instance.FishMorningPlaying,"Fish becomes human and starts her Day 4 morning visit");
        Check(SceneManager.GetActiveScene().name=="LivingRoom","Fish visit does not teleport a sleepless Hugo into bed");
        VerifyRevision.Capture("day-four-human-fish");yield return .2f;
        MorningRoutine.Instance.DebugSkip();yield return .3f;
        Check(AquariumEntity.Instance.CompanionPresent,"Human fish remains present within the current life");
        Check(!RadiationRules.CanSeal(3)&&RadiationRules.CanSeal(2),"Foil still only works before the face appears");
        Set(7,TimePeriod.Night);S.buildingCrisisResolved=S.daySevenCrisisResolved=true;
        int loops=game.LoopCount;
        LoopManager.Instance.RestartLoop();
        Check(game.LoopCount==loops,"Healthy Hugo cannot call the loop entry point to rewind");
        clock.AdvanceTime();yield return .2f;
        Check(clock.Days==8&&game.LoopCount==loops&&!WorldEndSequence.IsPresenting,"Surviving resolved crises continues to Day 8 without looping");
        S.sleeplessDays=3;clock.SleepUntilNextMorning();yield return .7f;
        Check(S.sleeplessDays==0&&clock.Days==9&&SceneManager.GetActiveScene().name=="Bedroom","Actual bed sleep resets sleep debt and wakes beside bed");
        game.RecordAncientGodEncounter();Check(game.GrantLoopAbility(),"Only completed Bubble gift grants return ability");
        S.fishAlly=S.fishCompanionUnlocked=S.aquariumSealed=true;S.fishUnderstanding=3;S.fishStoolBorrowed=true;
        game.Damage(game.MaxVitality);yield return .7f;
        Check(game.LoopCount==loops+1&&clock.Days==1,"Actual lethal damage causes exactly one return");
        Check(!S.fishAlly&&!S.fishCompanionUnlocked&&!S.aquariumSealed&&S.fishUnderstanding==0&&!S.fishStoolBorrowed,"Fish body, relationship, furniture and foil protection reset on death");
        Check(S.bubbleDreamUnlocked&&game.HasLoopAbility,"Bubble gift remains global after death");
        Set(3,TimePeriod.Night);S.firstApocalypseExplained=false;S.buildingCrisisResolved=false;
        loops=game.LoopCount;clock.AdvanceTime();
        Check(WorldEndSequence.FirstCycleActive&&S.deathWasBuilding&&clock.Days==3,"Unresolved third-night bombing starts its own fatal cinematic");
        yield return 2f;VerifyRevision.Capture("building-explosion");yield return WorldEndSequence.Duration;
        Check(WorldEndSequence.InReunion&&game.LoopCount==loops,"First bombing waits for Bubble reunion before rewinding");
        VerifyRevision.Capture("bubble-after-explosion");yield return .2f;
        int guard=0;while(WorldEndSequence.InReunion&&guard++<160){WorldEndSequence.Instance.AdvanceReunion();yield return .18f;}
        yield return .7f;
        Check(!WorldEndSequence.IsPresenting&&game.LoopCount==loops+1&&S.firstApocalypseExplained,"Read reunion completes one death and records explanation");
        Set(3,TimePeriod.Night);loops=game.LoopCount;clock.AdvanceTime();yield return WorldEndSequence.Duration+.5f;
        Check(game.LoopCount==loops+1&&!WorldEndSequence.InReunion,"Later unresolved bombing kills again without repeating exposition");
        Set(7,TimePeriod.Night);S.buildingCrisisResolved=true;S.daySevenCrisisResolved=false;loops=game.LoopCount;
        clock.AdvanceTime();yield return WorldEndSequence.Duration+.5f;
        Check(game.LoopCount==loops+1,"Unresolved seventh-day lethal crisis repeats on each life");
        StoryDebugActions.SetEnabled(true);clock.DebugSetTime(3,TimePeriod.Night);clock.AdvanceTime();
        Check(clock.Days==4&&!WorldEndSequence.IsPresenting,"Free debug can cross unresolved lethal dates for testing");
        var hud=Object.FindAnyObjectByType<ProgressionHUD>();
        var hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(ProgressionHUD).GetMethod("ToggleConsole",hidden).Invoke(hud,null);
        foreach(char c in "1997")typeof(ProgressionHUD).GetMethod("EnterDigit",hidden).Invoke(hud,new object[]{c});
        Check(ProgressionHUD.DebugConsoleOpen&&StoryDebugActions.Enabled,"Debug PIN activates free exploration and exposes controls");
        VerifyRevision.Capture("debug-free-exploration");yield return .4f;
        typeof(ProgressionHUD).GetMethod("Close",hidden).Invoke(hud,null);
        Check(!ProgressionHUD.DebugConsoleOpen&&StoryDebugActions.Enabled,"Closing the console keeps the explicit session debug mode active");
        Check(Resources.Load<AudioClip>("Audio/BuildingBlast")?.length>3,"Explosion has its own decay and debris audio tail");
        StoryDebugActions.SetEnabled(false);
        Check(ProgressionSave.Load().version==6,"Save v6 round-trips revised death-cycle fields");
        ProgressionSave.Write(new ProgressionState{version=4,loopCount=2,yogDreamSeen=true,bubbleDreamUnlocked=true,fishCompanionUnlocked=true,fishStoolBorrowed=true,fishMorningLoop=1});
        var migrated=ProgressionSave.Load();
        Check(migrated.version==6&&migrated.yogDreamSeen&&migrated.bubbleDreamUnlocked,"v4 migration preserves Bubble gift");
        Check(!migrated.fishCompanionUnlocked&&!migrated.fishStoolBorrowed&&migrated.fishMorningLoop==-1,"v4 migration removes permanent human fish carryover");
        ProgressionSave.Write(S);
        game.StartNewJourney();yield return .7f;Set(3,TimePeriod.Night);
        Check(!game.HasLoopAbility,"Fresh journey has no return gift");
        clock.AdvanceTime();yield return WorldEndSequence.Duration+.6f;
        Check(game.HasEnded&&game.LoopCount==0&&!WorldEndSequence.InReunion,"Unawakened bombing is death without an unearned rewind or reunion");
        StoryDebugActions.SetEnabled(false);
    }
}
