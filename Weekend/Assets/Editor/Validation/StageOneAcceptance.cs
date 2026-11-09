using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Story;
public static class StageOneAcceptance
{
    static ProgressionState S=>(ProgressionState)typeof(GameManager).GetProperty("State",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(GameManager.Instance);
    static ApartmentEventDirector D=>ApartmentEventDirector.Instance;
    static TimeManager Clock=>TimeManager.Instance;
    static void Check(bool value,string label)=>VerifyRevision.Check(value,"STAGE1 "+label);
    static void Place(Vector3 p){var actor=GameObject.FindWithTag("Player");var cc=actor.GetComponent<CharacterController>();if(cc)cc.enabled=false;actor.transform.position=p;var rb=actor.GetComponent<Rigidbody2D>();if(rb){rb.position=p;rb.linearVelocity=Vector2.zero;}if(cc)cc.enabled=true;Physics.SyncTransforms();Physics2D.SyncTransforms();}
    static IEnumerator Setup(int day=2,TimePeriod period=TimePeriod.Morning,string scene="CityStreet")
    {
        CrisisRules.LegacyValidation=false;TimeManager.AutoTimeSuppressed=true;GameManager.Instance.StartNewJourney();yield return .7f;
        StoryDebugActions.SetEnabled(true);StoryDebugActions.SkipForcedStory();Clock.SetNarrativeTime(day,period,period==TimePeriod.Night?1260:period==TimePeriod.Evening?1020:period==TimePeriod.Afternoon?720:420);SceneManager.LoadScene(scene);yield return 1.3f;
        S.neighborGuidanceSeen=true;StoryDebugActions.SetEnabled(false);yield return .9f;
    }
    static bool Plan()
    {
        foreach(string id in new[]{"neighbor-order","skt079-broadcast","pipe-pulse","city-fuse","city-maintenance"})if(!Investigation.Has(id))Investigation.Collect(id);
        foreach(var pair in new[]{new[]{"neighbor-order","skt079-broadcast"},new[]{"deduction-group-assignment","pipe-pulse"},new[]{"deduction-source-location","city-fuse"},new[]{"deduction-building-plan","city-maintenance"}})
            if(Investigation.Match(pair[0],pair[1])!=null&&!Investigation.Has(Investigation.Match(pair[0],pair[1]).id))Investigation.Connect(pair[0],pair[1],true);
        return Investigation.Has("deduction-day7-plan");
    }
    static IEnumerator Night()
    {
        Check(D.RequestNight(),"late-night call opens through the director");Check(StoryJournal.WorldBlocked,"call pauses the world");
        Check(D.Choose(0),"accept call through normal choice");yield return 1.1f;
        Check(SceneManager.GetActiveScene().name=="NeighborRoom2D"&&D.CurrentCard=="night-intro","2D neighbor visit starts before mutation");
        Check(Vector2.Distance(Object.FindAnyObjectByType<NeighborRoom>().Actor.transform.position,NeighborRoom.NightPlayerPoint)<.1f,"2D and first-person scenes share Hugo's position across the rupture");
        Check(D.Choose(0)&&S.neighborEyeTwitchSeen,"eye movement precedes body loss of control");yield return .15f;
        Check(GameObject.Find("Night eye twitch")!=null,"eye twitch is visible geometry");
        Check(D.Choose(1)&&D.CurrentCard=="night-body","body escalation follows the second choice");
        Check(D.Choose(0),"body loss of control requests the owned rupture");yield return 5.9f;
        Check(SceneManager.GetActiveScene().name=="NeighborArena3D"&&D.ChaseActive,"tear enters first-person escape encounter");
        if(D.CurrentCard=="night-controls")D.Choose(0);
        Check(S.neighborNightControlsSeen,"one-time controls card committed");
    }
    static void Capture(string name,int w=1280,int h=800)
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        string dir="Documentation/StageOne/Visuals";System.IO.Directory.CreateDirectory(dir);var cam=Camera.main;var rt=new RenderTexture(w,h,24);var old=cam.targetTexture;var active=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();System.IO.File.WriteAllBytes(dir+"/"+name+".png",image.EncodeToPNG());cam.targetTexture=old;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(image);
    }
    public static IEnumerator RunAll(){var old=InteractionReworkAcceptance.Run();while(old.MoveNext())yield return old.Current;var fresh=Run();while(fresh.MoveNext())yield return fresh.Current;}
    public static IEnumerator Run()
    {
        CrisisRules.LegacyValidation=false;TimeManager.AutoTimeSuppressed=true;
        MainMenuUI.ReturnToMainMenu();MainMenuUI.Instance.StartGame();yield return 12.5f;
        Check(SceneManager.GetActiveScene().name=="Bedroom"&&!StoryDebugActions.Enabled,"fresh title starts the real bedroom opening without free debug");
        var opening=Object.FindAnyObjectByType<StoryJournal>();for(int i=0;i<7;i++){opening.AdvanceOpeningDream();yield return .4f;}yield return 1.1f;
        for(int i=0;i<15;i++){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Enter));yield return .15f;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return .25f;}
        Check(S.dreamSeen&&S.morningStage>=2,"opening inputs reach the actual waking and neighbor handoff");
        var apartmentActor=GameObject.FindWithTag("Player");var apartmentRenderer=apartmentActor.GetComponent<SpriteRenderer>();var apartmentCollider=apartmentActor.GetComponent<BoxCollider2D>();
        Check(apartmentRenderer.sprite.name=="Apartment Hugo"&&apartmentRenderer.sprite.rect.height>apartmentRenderer.sprite.rect.width,"apartment protagonist uses the existing pixel character rather than a teal square");
        Check(Mathf.Abs(apartmentRenderer.bounds.min.y-apartmentCollider.bounds.min.y)<.02f,"pixel actor stays anchored to the original collision feet");
        Capture("apartment-hugo-bedroom");
        var setup=Setup();while(setup.MoveNext())yield return setup.Current;
        Check(CrisisRules.Due(3,S)==CrisisKind.None&&CrisisRules.Due(5,S)==CrisisKind.None,"Day3 installation and Day5 pressure are not mandatory deaths");
        Check(CrisisRules.Due(7,S)==CrisisKind.Building,"Day7 purification is the only scheduled fatal crisis");
        Check(Investigation.ExplainFailure("first-night-silhouette","first-night-recheck")==InvestigationFailure.MissingEvidence,"unrecorded matching evidence is not NoRelation");
        Check(Investigation.ExplainFailure("tank-label","city-lens")==InvestigationFailure.NoRelation,"unrelated facts have distinct feedback");
        Investigation.Collect("401-warning");Investigation.Collect("skt079-broadcast");Check(Investigation.ExplainFailure("401-warning","skt079-broadcast")==InvestigationFailure.MissingPrerequisite,"flyer visit is still a real prerequisite");
        Plan();Check(Investigation.ExplainFailure("deduction-building-plan","city-maintenance")==InvestigationFailure.LockedByDay,"final plan waits for the installation stage");
        Clock.SetNarrativeTime(3,TimePeriod.Morning,420);Check(Plan()&&Investigation.Has("deduction-day7-plan"),"handover is necessary in the final fourth inference");
        Check(Investigation.Evidence("city-maintenance").flavor==false,"handover is no longer a dead flavor clue");
        Clock.SetNarrativeTime(2,TimePeriod.Night,1390);yield return .1f;
        if(D.CurrentCard==null){var night=Night();while(night.MoveNext())yield return night.Current;}
        else
        {
            Check(D.CurrentCard=="night-call","normal 23:10 update catches the player offsite");D.Choose(0);yield return 1.1f;D.Choose(0);yield return .1f;D.Choose(0);D.Choose(0);yield return 5.9f;if(D.CurrentCard=="night-controls")D.Choose(0);
        }
        var combat=Object.FindAnyObjectByType<NeighborCombat>();Check(combat!=null&&combat.actor.FirstPerson,"night chase uses first person");
        Check(combat.actor.TryDodge()&&combat.actor.Dodging,"Q dodge has an actual invulnerability window");
        Capture("night-chase-1280x800");Capture("night-chase-1920x1080",1920,1080);Capture("night-chase-window",1024,768);
        Check(D.CatchNight(),"first actual capture is handled by the chase owner");yield return .6f;
        Check(!GameManager.Instance.HasEnded&&S.neighborNightCaughtCount==1&&S.neighborNightLocked&&SceneManager.GetActiveScene().name=="Corridor","first capture wakes alive outside locked 401");
        if(D.CurrentCard!=null)D.Choose(0);Check(D.RetryNight(),"second attempt remains physically reachable after the warning");D.Choose(0);yield return 1.1f;D.Choose(0);D.Choose(0);D.Choose(0);yield return 5.9f;if(D.CurrentCard=="night-controls")D.Choose(0);
        Check(D.CatchNight(),"second capture becomes actual lethal damage");yield return .5f;Check(GameManager.Instance.HasEnded&&!GameManager.Instance.HasLoopAbility,"unawakened second capture gives a formal ending");
        setup=Setup(2,TimePeriod.Night);while(setup.MoveNext())yield return setup.Current;GameManager.Instance.RecordAncientGodEncounter();GameManager.Instance.GrantLoopAbility();
        var repeat=Night();while(repeat.MoveNext())yield return repeat.Current;S.neighborNightCaughtCount=1;int loop=GameManager.Instance.LoopCount;
        Check(D.CatchNight(),"awakened second-capture fixture uses the same lethal path");yield return .7f;
        Check(!GameManager.Instance.HasEnded&&Clock.Days==2&&Clock.Period==TimePeriod.Morning&&GameManager.Instance.LoopCount==loop+1,"night death returns to Day2 morning through LoopManager");
        Check(!S.neighborHatRevealed&&!S.neighborEyeTwitchSeen&&S.sleeplessDays==0,"Day2 return restores the human disguise and sleep state without losing knowledge");
        setup=Setup(2,TimePeriod.Night);while(setup.MoveNext())yield return setup.Current;
        var escapeRun=Night();while(escapeRun.MoveNext())yield return escapeRun.Current;
        Place(new Vector3(-5,0,0));InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));yield return .15f;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return 1.7f;
        Check(S.neighborNightResolved&&SceneManager.GetActiveScene().name=="NeighborRoom2D","actual E input escapes to the matching 2D room without killing");
        Place(new Vector3(-5,0,0));Check(Object.FindAnyObjectByType<NeighborRoom>().UseNearest(),"escape room still has a usable corridor door");yield return .5f;Check(SceneManager.GetActiveScene().name=="Corridor","escape returns through 401 rather than Hugo's home door");
        setup=Setup(7,TimePeriod.Night);while(setup.MoveNext())yield return setup.Current;GameManager.Instance.RecordAncientGodEncounter();GameManager.Instance.GrantLoopAbility();S.firstApocalypseExplained=true;Plan();loop=GameManager.Instance.LoopCount;
        Clock.SetNarrativeTime(7,TimePeriod.Night,1440);yield return .3f;Check(WorldEndSequence.IsPresenting,"midnight starts the purification explosion");yield return 6f;
        Check(Clock.Days==7&&Clock.Period==TimePeriod.Morning&&GameManager.Instance.LoopCount==loop+1&&S.day7LoopCount==1,"purification death returns to Day7 morning");
        Check(Investigation.Has("deduction-day7-plan")&&!S.day7BombDefused,"short loop preserves device knowledge and resets ignition");
        setup=Setup(7,TimePeriod.Night);while(setup.MoveNext())yield return setup.Current;Clock.SetNarrativeTime(7,TimePeriod.Night,1440);yield return 6.3f;
        Check(GameManager.Instance.HasEnded&&!GameManager.Instance.HasLoopAbility,"unawakened purification fails permanently");
        setup=Setup(7,TimePeriod.Morning,"BuildingService2D");while(setup.MoveNext())yield return setup.Current;
        Check(Plan(),"full plan can be confirmed on Day7");Place(new Vector3(-2,-.3f,0));var room=Object.FindAnyObjectByType<NeighborRoom>();
        Check(room.UseNearest()&&!NeighborSlice.Instance.Choose(0)&&!S.serviceRouteIsolated,"handover archive supplies evidence rather than a rescue valve");NeighborSlice.Instance.Close();
        var final=NeighborRevisionAcceptance.FinalResolution(0);while(final.MoveNext())yield return final.Current;
        Check(D.CurrentCard=="stage-complete","earned controller resolution has its own stage settlement");
        setup=Setup(7);while(setup.MoveNext())yield return setup.Current;Plan();final=NeighborRevisionAcceptance.FinalResolution(1);while(final.MoveNext())yield return final.Current;
        setup=Setup(7);while(setup.MoveNext())yield return setup.Current;Plan();final=NeighborRevisionAcceptance.FinalResolution(2);while(final.MoveNext())yield return final.Current;
        setup=Setup(7,TimePeriod.Night,"LivingRoom");while(setup.MoveNext())yield return setup.Current;var sofaBounds=GameObject.Find("Old sofa").GetComponent<SpriteRenderer>().bounds;Place(new Vector3(sofaBounds.center.x,sofaBounds.min.y-.3f,0));yield return .2f;TimeManager.AutoTimeSuppressed=false;float restedAt=Clock.GameMinute;
        Check(Object.FindAnyObjectByType<FurnitureInteraction>().RestOnePeriod()&&Clock.GameMinute>=restedAt+44&&Clock.GameMinute<restedAt+47&&!WorldEndSequence.IsPresenting,"ordinary rest spends 45 minutes and cannot detonate Day7 at 21:00");TimeManager.AutoTimeSuppressed=true;
        setup=Setup(2,TimePeriod.Morning,"Corridor");while(setup.MoveNext())yield return setup.Current;S.neighborPlotHeard=true;S.neighborFlyersSeen=false;Place(new Vector3(-3.65f,.65f,0));yield return .3f;
        Check(EvidenceInspection.Instance.TryOpen("401-flyers")&&S.neighborFlyersSeen&&!Investigation.Has("401-flyers"),"reading the real original flyer, rather than hammering, unlocks its prerequisite");EvidenceInspection.Instance.Close();
        var corridorActor=GameObject.FindWithTag("Player");var corridorCollider=corridorActor.GetComponent<BoxCollider2D>();var corridorRenderer=corridorActor.GetComponent<SpriteRenderer>();Check(corridorRenderer.sprite.name=="Apartment Hugo"&&corridorCollider.size==Vector2.one&&Mathf.Abs(corridorActor.transform.localScale.x-.455f)<.001f&&corridorRenderer.sortingOrder>GameObject.Find("north wallpaper face").GetComponent<SpriteRenderer>().sortingOrder,"corridor character preserves collision and draws in front of the wall face");Capture("apartment-hugo-corridor");
        setup=Setup(3,TimePeriod.Morning,"LivingRoom");while(setup.MoveNext())yield return setup.Current;
        foreach(int day in new[]{3,4,5,6,7})
        {
            Clock.SetNarrativeTime(day,TimePeriod.Morning,420);yield return .2f;var expected=day==3?FishStage.HumanFace:day==4?FishStage.Speaking:day==5?FishStage.GrowingLimbs:day==6?FishStage.Imitating:FishStage.Replacement;
            Check(RadiationRules.Fish(day,false)==expected,"distinct fish phase Day"+day);Check(!AquariumEntity.Instance.CompanionPresent,"ununderstood fish is not automatically a companion on Day"+day);Capture("fish-day"+day);
        }
        setup=Setup(2,TimePeriod.Morning,"LivingRoom");while(setup.MoveNext())yield return setup.Current;S.inventory.Add(CultNeighbor.Token);
        var tank=GameObject.Find("Aquarium and cabinet");var bounds=tank.GetComponent<SpriteRenderer>().bounds;Place(new Vector3(bounds.center.x,bounds.min.y-.3f,0));yield return .1f;
        Check(!AquariumEntity.Instance.ThrowFoil()&&AquariumEntity.Instance.FoilConfirmation&&S.inventory.Contains(CultNeighbor.Token),"first foil input does not silently close the fish route");
        Check(AquariumEntity.Instance.ConfirmFoil(false)&&!S.aquariumSealed&&S.inventory.Contains(CultNeighbor.Token),"foil cancellation preserves the item and route");AquariumEntity.Instance.ThrowFoil();Check(AquariumEntity.Instance.ConfirmFoil(true)&&S.aquariumSealed&&!S.inventory.Contains(CultNeighbor.Token),"explicit second action seals irreversibly");
        for(int day=3;day<=7;day++)Check(RadiationRules.Fish(day,true)==FishStage.Ordinary,"sealed fish remains ordinary Day"+day);
        setup=Setup(4,TimePeriod.Morning,"LivingRoom");while(setup.MoveNext())yield return setup.Current;
        tank=GameObject.Find("Aquarium and cabinet");bounds=tank.GetComponent<SpriteRenderer>().bounds;Place(new Vector3(bounds.center.x,bounds.min.y-.3f,0));yield return .1f;Check(AquariumEntity.Instance.TryTalk(),"Day4 first speech is reachable");
        for(int i=0;i<40&&!AquariumEntity.Instance.ChoicesVisible;i++)AquariumEntity.Instance.AdvanceDialogue();Check(AquariumEntity.Instance.ChooseUnderstanding(true)&&!S.fishAlly,"one understanding choice is not full companionship");
        for(int i=0;i<20&&AquariumEntity.Instance.IsTalking;i++)AquariumEntity.Instance.AdvanceDialogue();Clock.SetNarrativeTime(5,TimePeriod.Morning,420);yield return .3f;Check(AquariumEntity.Instance.TryTalk(),"Day5 limb dialogue is reachable");
        for(int i=0;i<40&&!AquariumEntity.Instance.ChoicesVisible;i++)AquariumEntity.Instance.AdvanceDialogue();Check(AquariumEntity.Instance.ChooseUnderstanding(true)&&S.fishAlly,"two choices before Day7 establish understanding");
        Check(!NightDreamSequence.BeginReplacement(),"understanding prevents the replacement sequence");
        setup=Setup(3,TimePeriod.Morning,"Corridor");while(setup.MoveNext())yield return setup.Current;
        S.tutorialComplete=false;S.morningStage=2;S.cultDayOneTalked=S.cultDayOneStarted=false;Place(new Vector3(-4.7f,.42f,0));yield return .1f;
        Check(!NeighborSlice.Instance.TryHearPlot()&&CultNeighbor.Instance.TalkDayOne(),"late tutorial warning is reachable instead of being replaced by the conspiracy");for(int i=0;i<40;i++)CultNeighbor.Instance.Advance();yield return .1f;
        Check(S.cultDayOneTalked&&S.morningStage>=3,"late first meeting advances the original tutorial owner");
        SceneManager.LoadScene("LivingRoom");yield return 1.1f;var lateFridge=GameObject.Find("Refrigerator").GetComponent<Collider2D>().bounds;Place(new Vector3(lateFridge.center.x,lateFridge.min.y-.36f,0));yield return .1f;
        Check(MorningRoutine.Instance.OpenFridge(),"late fridge encounter is still a usable real interaction");yield return 1f;MorningRoutine.Instance.AdvanceStory();yield return .3f;Check(DayOneSupernovaEvent.Instance.IsPresenting||S.dayOneNewsStarted,"missed Day1 news catches up rather than locking stage three");
        setup=Setup(3,TimePeriod.Night,"Balcony");while(setup.MoveNext())yield return setup.Current;S.tutorialComplete=false;S.morningStage=7;S.journalUnlocked=true;
        var lateScope=GameObject.Find("阳台望远镜");var scopeBounds=lateScope.GetComponent<Collider2D>().bounds;Place(new Vector3(scopeBounds.center.x,scopeBounds.min.y-.30f,0));yield return .2f;
        Check(Object.FindAnyObjectByType<StoryJournal>().OpenTelescopeFromWorld(),"late first-night telescope lesson remains reachable");
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Enter));yield return .15f;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        for(int i=0;i<20;i++){Object.FindAnyObjectByType<StoryJournal>().AdvanceObservation();yield return .3f;}
        Check(S.firstNightWatchComplete&&Object.FindAnyObjectByType<StoryJournal>().CollectTelescopeEvidence(),"late real observation and recheck can still record the original pair");
        var invalid=new ProgressionState{neighborNightBeat=99,stageReturnDay=4,day7LoopCount=-1,stageOneComplete=true,savedMinute=float.NaN};
        Check(SaveValidation.CheckStage(invalid).Count>=4,"save validation detects impossible stage combinations");SaveValidation.NormalizeStage(invalid);Check(SaveValidation.CheckStage(invalid).Count==0,"corrupt new-stage fields normalize without discarding the journey");
        for(int version=1;version<=7;version++){ProgressionSave.Write(new ProgressionState{version=version,knowledge=3},false);var loaded=ProgressionSave.Load();Check(loaded.version==7&&loaded.stageEvents!=null&&loaded.savedMinute==-1,"old v"+version+" migration retains safe stage defaults");}ProgressionSave.Write(S);
        if(!StoryText.English)StoryText.ToggleLanguage();Check(NeighborSlice.Text("night-intro").body.Contains("cups"),"night narrative resolves in English");StoryText.ToggleLanguage();Check(NeighborSlice.Text("night-intro").body.Contains("杯"),"night narrative resolves in Chinese");
        CrisisRules.LegacyValidation=false;TimeManager.AutoTimeSuppressed=true;
    }
}
