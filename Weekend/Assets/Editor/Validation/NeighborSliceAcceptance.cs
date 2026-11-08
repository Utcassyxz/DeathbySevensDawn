using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
public static class NeighborSliceAcceptance
{
    static ProgressionState S=>(ProgressionState)typeof(GameManager).GetProperty("State",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(GameManager.Instance);
    static void Check(bool yes,string name)=>VerifyRevision.Check(yes,name);
    static void Ready(int day=2)
    {StoryDebugActions.SetEnabled(true);TimeManager.Instance.DebugSetTime(day,TimePeriod.Morning);StoryDebugActions.SetEnabled(false);S.dreamSeen=S.tutorialComplete=S.journalUnlocked=S.investigationUnlocked=true;S.morningStage=10;}
    static void Place(float x,float y,float z=0)
    {var go=GameObject.FindWithTag("Player");var c=go.GetComponent<CharacterController>();if(c!=null)c.enabled=false;go.transform.position=new Vector3(x,y,z);var rb=go.GetComponent<Rigidbody2D>();if(rb!=null){rb.position=new Vector2(x,y);rb.linearVelocity=Vector2.zero;}if(c!=null)c.enabled=true;Physics.SyncTransforms();Physics2D.SyncTransforms();}
    static NeighborSlice Slice=>NeighborSlice.Instance;
    static void Capture(string name,bool closeup=false)
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        var camera=Camera.main;var originalPosition=camera.transform.position;var originalRotation=camera.transform.rotation;
        if(closeup){var tenant=GameObject.Find("Recognizably human neighbor");if(tenant!=null){camera.transform.position=tenant.transform.position+new Vector3(-2.3f,1.9f,-2.3f);camera.transform.LookAt(tenant.transform.position+Vector3.up*1.3f);}}
        System.IO.Directory.CreateDirectory("Documentation/Validation/Nov08");
        var rt=new RenderTexture(1280,800,24);var previous=camera.targetTexture;var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();System.IO.File.WriteAllBytes("Documentation/Validation/Nov08/"+name+".png",image.EncodeToPNG());
        camera.targetTexture=previous;RenderTexture.active=active;camera.transform.SetPositionAndRotation(originalPosition,originalRotation);Object.Destroy(rt);Object.Destroy(image);
    }
    public static IEnumerator RunAll()
    {
        var presentation=Nov08Acceptance.Run();while(presentation.MoveNext())yield return presentation.Current;
        var current=Run();while(current.MoveNext())yield return current.Current;
        GameManager.Instance.StartNewJourney();yield return .7f;
        var death=DeathCycleAcceptance.Run();while(death.MoveNext())yield return death.Current;
        NeighborDebugCommands.EndSession(false);
        GameManager.Instance.StartNewJourney();yield return .7f;
        var city=CityAcceptance.Run();while(city.MoveNext())yield return city.Current;
    }
    public static IEnumerator Run()
    {
        var game=GameManager.Instance;var clock=TimeManager.Instance;game.StartNewJourney();yield return .8f;Ready();yield return .1f;
        foreach(string name in NeighborDebugCommands.Scenes)Check(Application.CanStreamedLevelBeLoaded(name),"Registered console scene: "+name);
        Check(NeighborRoom.Settings!=null,"Configurable perception and eye ability settings loaded");
        Check(System.Linq.Enumerable.All(UnityEditor.EditorBuildSettings.scenes,x=>!x.enabled||System.Array.IndexOf(NeighborDebugCommands.Scenes,System.IO.Path.GetFileNameWithoutExtension(x.path))>=0),"Console registry covers all enabled build scenes");
        Check(NeighborRoom.Settings.Tier(70)==0&&NeighborRoom.Settings.Tier(69)==1&&NeighborRoom.Settings.Tier(40)==1&&NeighborRoom.Settings.Tier(39)==2&&NeighborRoom.Settings.Tier(20)==2&&NeighborRoom.Settings.Tier(19)==3,"Configurable perception thresholds cover all four ranges");
        // Scenario preparation represents the already-read mandatory Day 1 news; Day 2 actions below are real interactions.
        Check(Investigation.Collect("skt079-broadcast"),"Recorded Day 1 bulletin fixture");
        float startSanity=game.Sanity;
        SceneManager.LoadScene("LivingRoom");yield return .6f;
        Check(S.neighborGuidanceSeen&&Slice.IsOpen,"Day 2 naturally introduces the crowbar, oil and fire before exploration");Slice.Close();
        Place(-4.5f,1.05f);Check(Slice.Use(NeighborSlice.Crowbar)&&Slice.TakeItem()&&S.inventory.Contains(NeighborSlice.Crowbar),"Kitchen cabinet gives an inventory crowbar");
        Check(Slice.Record(),"Actively record fictional maintenance leaflet");Slice.Close();
        var keyboard=Keyboard.current;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.I));yield return .15f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .1f;
        Check(CultNeighbor.Instance.BagOpen&&CultNeighbor.Instance.SelectInventoryItem(0)&&CultNeighbor.Instance.SelectedInventoryItem==NeighborSlice.Crowbar,"Existing inventory opens with I and selects real crowbar");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.I));yield return .15f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .1f;
        Place(-2,1.1f);Check(Slice.Use(NeighborSlice.Oil)&&Slice.TakeItem()&&Slice.Record(),"Optional kitchen oil item and record");Slice.Close();
        SceneManager.LoadScene("Bathroom");yield return .6f;Place(-1,1);Check(Slice.Use(NeighborSlice.Medium)&&Slice.TakeItem()&&Slice.Record(),"Bathroom sealed fictional medium is usable inventory");Slice.Close();
        Place(1,1);Check(Slice.Use("pipe-pulse")&&Slice.Record(),"Actively record building anomaly before confrontation");Slice.Close();
        Check(!Slice.Prepare(),"No preparation before evidence-board deduction");
        Check(Investigation.Connect("prep-oil","prep-medium",true)&&Investigation.Connect("deduction-sealed-materials","prep-manual",true),"Two-stage preparation inference uses existing board");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.I));yield return .15f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .1f;
        Check(CultNeighbor.Instance.PrepareFromInventory()&&Slice.IsOpen,"Existing inventory opens abstract preparation interaction");
        Check(Slice.Prepare()&&!S.inventory.Contains(NeighborSlice.Oil)&&!S.inventory.Contains(NeighborSlice.Medium)&&S.inventory.Contains(NeighborSlice.Tool),"Abstract inventory preparation consumes two items");Slice.Close();
        Check(S.neighborTakenItems.Contains(NeighborSlice.Oil)&&S.neighborTakenItems.Contains(NeighborSlice.Medium),"Consumed preparation ingredients remain taken for this life");
        SceneManager.LoadScene("LivingRoom");yield return .5f;Place(-2,1.1f);Check(Slice.Use(NeighborSlice.Oil)&&!Slice.TakeItem(),"Consumed oil cannot be farmed again from the same kitchen");Slice.Close();
        Check(Mathf.Abs(startSanity-game.Sanity)<.01f&&!game.HasLoopAbility&&game.LoopCount==0,"First-loop preparation does not spend sanity or grant mutation");
        SceneManager.LoadScene("Corridor");yield return .6f;Place(-4.7f,.42f);
        Check(Slice.Use("force-door")&&Slice.ForceEntry(),"Crowbar opens a real neighbor room through the hallway");yield return .6f;
        Check(SceneManager.GetActiveScene().name=="NeighborRoom2D"&&Camera.main.orthographic&&Slice.CurrentNode=="intro","Forced entry begins shocked intelligent 2D conversation");
        Capture("neighbor-room-2d");
        Check(!S.neighborHatRevealed&&GameObject.Find("Neighbor removable hat").activeSelf,"Peaceful 2D neighbor conceals his brain beneath a hat");
        Check(Slice.Choose(1),"Aggression branches into neighbor hostility");yield return 1.3f;
        Check(SceneManager.GetActiveScene().name=="NeighborRoom2D"&&NeighborRoom.HatRevealActive&&S.neighborHatRevealed,"Hat comes off in 2D before the filter tears");yield return 4.3f;
        Check(SceneManager.GetActiveScene().name=="NeighborArena3D"&&!Camera.main.orthographic,"Reality tear enters playable 3D neighbor arena");
        Check(GameObject.Find("Exposed upper brain")!=null&&GameObject.Find("Lower face and jaw")!=null,"Early mutation has exposed brain and recognizable human face");
        var combat=Object.FindAnyObjectByType<NeighborCombat>();Check(combat!=null&&combat.actor.GetComponent<CharacterController>()!=null,"Neighbor encounter has navigable third-person controller");
        var startPosition=combat.actor.transform.position;float walkUntil=Time.time+.3f;int walkGuard=0;while(Time.time<walkUntil&&walkGuard++<100){InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return .03f;}InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .1f;
        Check(combat.actor.transform.position.x>startPosition.x+.3f,"Arena starts facing the apartment interior and responds to W");Capture("neighbor-early-mutation-3d",true);
        float afterExposure=game.Sanity;Check(combat.UsePreparedTool(),"Prepared first-loop tool defeats neighbor");
        Check(!S.inventory.Contains(NeighborSlice.Tool)&&S.neighborDefeated&&!S.buildingCrisisResolved,"Single-use victory does not save building automatically");
        Check(Mathf.Abs(startSanity-afterExposure-20)<.01f&&Mathf.Abs(afterExposure-game.Sanity)<.01f,"First hat exposure costs exactly 20 sanity; preparation tool has no additional sanity cost");yield return 1.8f;
        Check(SceneManager.GetActiveScene().name=="NeighborRoom2D"&&Camera.main.orthographic,"Combat victory returns to matching 2D room");
        Place(0,.5f);var room=Object.FindAnyObjectByType<NeighborRoom>();Check(room.UseNearest()&&Slice.Record()&&Investigation.Has("neighbor-order"),"Recover critical assignment after victory from physical desk");Slice.Close();
        Check(!CrisisRules.CanResolve(CrisisKind.Building,2,S),"Neighbor information alone cannot resolve explosion");
        Check(Investigation.Connect("neighbor-order","skt079-broadcast",true),"Original Triangle bulletin corroborates assignment");
        Check(Investigation.Connect("deduction-group-assignment","pipe-pulse",true),"Building anomaly identifies actual source location");
        SceneManager.LoadScene("Stairwell");yield return .6f;RooftopAccess.Instance.RestoreFloor(1);Place(-1.6f,-1.6f);Check(Slice.Use("service-entry"),"First-floor landing connects compact basement service room");yield return .6f;
        room=Object.FindAnyObjectByType<NeighborRoom>();Place(0,.5f);Check(room.UseNearest()&&Slice.Record(),"Record wiring diagram at actual service desk");Slice.Close();
        Check(Investigation.Connect("deduction-source-location","city-fuse",true),"Final prevention deduction requires source plus wiring");
        Place(-2,-.3f);Check(room.UseNearest()&&Slice.Choose(0)&&S.serviceRouteIsolated&&!S.buildingCrisisResolved,"First physical action isolates assigned route but does not finish crisis");Slice.Close();
        Place(2,-.3f);Check(room.UseNearest()&&Slice.Choose(0)&&S.buildingCrisisResolved&&EndingCollection.Has("building-saved"),"Second physical action saves building and records nonterminal outcome");Slice.Close();
        Ready(3);yield return .1f;StoryDebugActions.SetEnabled(true);clock.DebugSetTime(3,TimePeriod.Night);StoryDebugActions.SetEnabled(false);clock.AdvanceTime();yield return .3f;
        Check(clock.Days==4&&!WorldEndSequence.IsPresenting&&!game.HasEnded,"First-loop preparation route survives third-night explosion boundary");
        // Independent peaceful branch, without manufacturing its evidence flag.
        game.StartNewJourney();yield return .7f;Ready();S.inventory.Add(NeighborSlice.Crowbar);S.neighborDoorForced=true;SceneManager.LoadScene("NeighborRoom2D");yield return .6f;
        Check(!Slice.Choose(2),"Prior-loop memory choice is unavailable before discovering the order");
        Check(Slice.CurrentNode=="intro"&&Slice.Choose(0)&&Slice.Choose(0)&&Slice.CurrentNode=="recruit","Calm choices earn intelligent recruitment conversation");
        Check(Slice.Record()&&Slice.Choose(0)&&S.neighborMembership=="join"&&S.inventory.Contains("invitation")&&!game.HasEnded,"Acceptance opens a route rather than ending game");Slice.Choose(0);
        Check(Slice.BeginDialogue()&&Slice.Choose(1)&&S.neighborMembership=="refuse"&&!S.neighborHostile,"Polite refusal remains peaceful");Slice.Choose(0);
        Investigation.Collect("skt079-broadcast");Investigation.Connect("neighbor-order","skt079-broadcast",true);
        Check(Slice.BeginDialogue()&&Slice.Choose(2)&&S.neighborMembership=="deceive","Validated prior knowledge opens deception branch");Slice.Choose(0);
        Check(!S.buildingCrisisResolved&&Investigation.Has("neighbor-order"),"Peaceful information still requires source deduction and field action");
        // Earned return fixture: existing rooftop observation + completed gift APIs, never grant a return merely for dying.
        game.RecordAncientGodEncounter();game.GrantLoopAbility();KnowledgeManager.Instance.Observe(StoryContent.AncientKnowledgeId,KnowledgeTier.Cosmic);
        int loop=game.LoopCount;Slice.BeginNeighborDeath();Check(ProgressionSave.Load().neighborDeathPending,"Neighbor fatal presentation writes a resumable death checkpoint");yield return 3f;
        Check(game.LoopCount==loop+1&&EndingCollection.Has("neighbor-killed")&&Investigation.Has("neighbor-death-memory"),"Neighbor death records its ending and eligible memory through earned return");
        Check(game.HasAbility("shoulder_eye")&&!S.neighborDefeated&&!S.neighborTrusted&&S.inventory.Count==0&&Investigation.Has("neighbor-order"),"Return keeps learning and shoulder mutation but resets body relationship and items");
        Ready();S.inventory.Add(NeighborSlice.Crowbar);S.encounterDestination="NeighborArena3D";S.encounterReturnScene="NeighborRoom2D";SceneManager.LoadScene("NeighborArena3D");yield return .6f;
        combat=Object.FindAnyObjectByType<NeighborCombat>();float sanity=game.Sanity;
        Check(GameObject.Find("Unnatural shoulder eye")!=null&&combat.ActivateEye()&&Time.timeScale<1&&game.Sanity<sanity,"Reincarnated shoulder eye is visible and trades sanity for slow motion");
        Check(!combat.ActivateEye(),"Eye cooldown prevents repeated activation");
        Place(1,0,-1);Check(combat.Strike(),"Shoulder-eye perception enables first crowbar counter");yield return .75f;
        Check(combat.Strike(),"Second mutation-assisted counter");yield return .75f;
        Check(combat.Strike()&&S.neighborDefeated&&Time.timeScale==1,"Mutation route wins and restores global time scale");yield return 1.8f;
        room=Object.FindAnyObjectByType<NeighborRoom>();StoryDebugActions.SetEnabled(true);game.DebugSetSanity(100);Check(room.PerceptionTier==0,"Same room at SAN 100 has normal geometry");yield return .1f;Capture("perception-san100");game.DebugSetSanity(15);Check(room.PerceptionTier==3&&GameObject.Find("Perceived structures tier 3")!=null,"Same room at SAN 15 gains alternate biological geometry");yield return .1f;Capture("perception-san15");NeighborDebugCommands.SanityOverride=-1;StoryDebugActions.SetEnabled(false);
        ProgressionSave.Write(S);Check(ProgressionSave.Load().version==7,"v7 round-trip includes neighbor and combat state");
        ProgressionSave.Write(new ProgressionState{version=6,loopCount=1,yogDreamSeen=true,knowledge=10});var old=ProgressionSave.Load();Check(old.version==7&&old.yogDreamSeen&&old.knowledge==10,"v6 migration preserves learned progress");ProgressionSave.Write(S);
        StoryDebugActions.SetEnabled(true);Check(NeighborDebugCommands.Execute("scene list").Contains("NeighborArena3D"),"Existing console lists every implemented scene");
        string normalPath=ProgressionSave.PathName;float oldMutation=game.Mutation;
        Check(NeighborDebugCommands.Execute("session begin").Contains("Isolated")&&ProgressionSave.PathName!=normalPath,"Console uses isolated journey and ending storage");
        foreach(string cmd in new[]{"day set 3","time set 3","sanity set 15","mutation unlock shoulder_eye","item give crowbar","evidence unlock prep-oil","flags reset"})Check(!NeighborDebugCommands.Execute(cmd).StartsWith("Unknown"),"Tested command: "+cmd);
        foreach(string destination in NeighborDebugCommands.Scenes)
        {
            Check(NeighborDebugCommands.Execute("scene goto "+destination).StartsWith("Scene:"),"Console jump dispatch: "+destination);yield return .25f;
            Check(SceneManager.GetActiveScene().name==destination,"Console jump loaded: "+destination);
        }
        Check(NeighborDebugCommands.Execute("event trigger neighbor_prepared").StartsWith("Prepared"),"Prepared battle debug entry");yield return .5f;
        combat=Object.FindAnyObjectByType<NeighborCombat>();Check(combat!=null&&S.inventory.Contains(NeighborSlice.Tool),"Prepared debug branch ready");
        Check(NeighborDebugCommands.Execute("event trigger neighbor_eye").StartsWith("Eye"),"Eye battle debug entry");yield return .5f;
        Check(Object.FindAnyObjectByType<NeighborCombat>()!=null&&game.HasAbility("shoulder_eye"),"Eye debug branch ready");
        Check(NeighborDebugCommands.Execute("event trigger neighbor_victory").StartsWith("Victory"),"Victory debug event");yield return 1.8f;
        Check(SceneManager.GetActiveScene().name=="NeighborRoom2D","Debug victory returns to 2D");
        Check(NeighborDebugCommands.Execute("event trigger neighbor_peace").StartsWith("Peaceful"),"Peace branch debug event");yield return .5f;
        Check(Slice.CurrentNode=="intro","Peace debug event starts dialogue graph");Slice.Close();
        Check(NeighborDebugCommands.Execute("event trigger neighbor_known").StartsWith("Known"),"Prior-knowledge debug event");yield return .5f;
        Check(Slice.Choose(2)&&Slice.CurrentNode=="recruit"&&Slice.Choose(2)&&S.neighborMembership=="deceive","Known-order and deception branch reproducible from console");Slice.Close();
        Check(NeighborDebugCommands.Execute("event trigger neighbor_encounter").StartsWith("At"),"Hostility debug event");yield return 5.9f;
        Check(SceneManager.GetActiveScene().name=="NeighborArena3D","Hostility debug event reaches 3D encounter");
        Check(NeighborDebugCommands.Execute("event trigger building_saved").StartsWith("Use"),"Source prevention debug event");yield return .5f;
        Check(SceneManager.GetActiveScene().name=="BuildingService2D"&&Investigation.Has("deduction-building-plan")&&!S.buildingCrisisResolved,"Source event sets a valid unsolved field scenario");
        Check(NeighborDebugCommands.Execute("event trigger apartment_explosion").StartsWith("Explosion")&&WorldEndSequence.IsPresenting,"Explosion debug event is real audiovisual preview");yield return WorldEndSequence.Duration+.3f;
        Check(!WorldEndSequence.IsPresenting&&!game.HasEnded,"Free-debug explosion preview does not kill the test journey");
        Check(NeighborDebugCommands.Execute("event trigger neighbor_death").StartsWith("Neighbor"),"Neighbor death debug event");yield return 3f;
        Check(EndingCollection.Has("neighbor-killed"),"Neighbor death debug event records only in isolated album");
        Check(NeighborDebugCommands.EndSession()&&!ProgressionSave.DebugStorage&&Mathf.Approximately(game.Mutation,oldMutation),"Ending debug session restores original state and storage");yield return .5f;StoryDebugActions.SetEnabled(false);
        var hud=Object.FindAnyObjectByType<ProgressionHUD>();typeof(ProgressionHUD).GetField("unlockedThisSession",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(hud,true);
        typeof(ProgressionHUD).GetMethod("ToggleConsole",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,null);
        Check(NeighborDebugCommands.Isolated&&ProgressionSave.DebugStorage,"Reopening an unlocked console starts isolation before legacy buttons can write");
        StoryDebugActions.SetEnabled(true);Check(NeighborDebugCommands.Execute("session end")=="Normal journey restored."&&!NeighborDebugCommands.Isolated&&!StoryDebugActions.Enabled&&!ProgressionHUD.DebugConsoleOpen,"Session end closes console and disables debug protection");yield return .5f;
        if(!StoryText.English)StoryText.ToggleLanguage();Check(StoryText.English&&NeighborSlice.ItemName("crowbar")=="Crowbar","English localization selected and persisted");
        StoryText.ToggleLanguage();Check(!StoryText.English&&UnityEngine.PlayerPrefs.GetInt("Story.Language",1)==0&&NeighborSlice.ItemName("crowbar")=="撬棍","Simplified Chinese language setting persists");
        game.StartNewJourney();yield return .7f;Ready();S.encounterDestination="NeighborArena3D";S.encounterReturnScene="NeighborRoom2D";SceneManager.LoadScene("NeighborArena3D");yield return .5f;
        game.Damage(game.Vitality-5);Place(1,0,-1);float deathDeadline=Time.unscaledTime+12;while(!game.HasEnded&&Time.unscaledTime<deathDeadline)yield return .1f;
        Check(game.HasEnded&&S.lastDeathCause=="neighbor"&&EndingCollection.Has("neighbor-killed")&&!game.HasLoopAbility,"Actual lethal neighbor attack has its distinct no-gift ending without unearned return");
        game.StartNewJourney();yield return .7f;Ready(3);StoryDebugActions.SetEnabled(true);clock.DebugSetTime(3,TimePeriod.Night);StoryDebugActions.SetEnabled(false);clock.AdvanceTime();
        Check(WorldEndSequence.IsPresenting&&EndingCollection.Has("building-destroyed"),"Ignoring crisis produces mandatory audiovisual Day 3 destruction and record");yield return WorldEndSequence.Duration+.5f;
        Check(game.HasEnded&&!game.HasLoopAbility,"Unawakened destruction never grants an unearned rewind");
    }
}
