using System.Collections;
using System.Reflection;
using System.IO;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.City;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Interaction;

public static class CityAcceptance
{
    static ProgressionState S => (ProgressionState)typeof(GameManager).GetProperty("State",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(GameManager.Instance);
    static void Check(bool condition,string label)=>VerifyRevision.Check(condition,label);
    static void Set(int day,TimePeriod period=TimePeriod.Morning){StoryDebugActions.SetEnabled(true);TimeManager.Instance.DebugSetTime(day,period);StoryDebugActions.SetEnabled(false);}
    static void Place(Vector3 p)
    {
        var actor=CityWorld.Instance.PlayerObject;var cc=actor.GetComponent<CharacterController>();if(cc!=null)cc.enabled=false;actor.transform.position=p;var rb=actor.GetComponent<Rigidbody2D>();if(rb!=null){rb.position=p;rb.linearVelocity=Vector2.zero;}if(cc!=null)cc.enabled=true;Physics.SyncTransforms();Physics2D.SyncTransforms();
    }
    static bool Open(float x,float z){CityWorld.Instance.CloseReading();Place(CityWorld.Instance.InEncounter?new Vector3(x,0,z-1.8f):new Vector3(x,z-1.8f,0));return CityWorld.Instance.TryInteract();}
    static void Ready()
    {
        MainMenuUI.Instance.DebugSkip();StoryDebugActions.SkipForcedStory();StoryDebugActions.SetEnabled(false);
        S.dreamSeen=S.tutorialComplete=S.journalUnlocked=S.investigationUnlocked=true;S.morningStage=10;
    }
    static bool RecordNeighborPrerequisites()
    {
        // Scenario fixture: city regression starts after the separately verified neighbor chapter.
        foreach(var id in new[]{"neighbor-order","skt079-broadcast","pipe-pulse"})Investigation.Collect(id);
        return Investigation.Connect("neighbor-order","skt079-broadcast",true)&&Investigation.Connect("deduction-group-assignment","pipe-pulse",true)&&Investigation.Connect("deduction-source-location","city-fuse",true);
    }
    static void Capture(string name)
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        Directory.CreateDirectory("Documentation/Validation/Day2-3/City");
        var camera=Camera.main;var texture=new RenderTexture(1280,800,24);var previous=camera.targetTexture;var active=RenderTexture.active;
        camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
        var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
        File.WriteAllBytes("Documentation/Validation/Day2-3/City/"+name+".png",image.EncodeToPNG());
        camera.targetTexture=previous;RenderTexture.active=active;Object.Destroy(texture);Object.Destroy(image);
    }
    public static IEnumerator Run()
    {
        Ready();var game=GameManager.Instance;var clock=TimeManager.Instance;
        foreach(var scene in new[]{"CityStreet","CityService","CityBroadcast","CityObservatory","CityEncounter3D"})Check(Application.CanStreamedLevelBeLoaded(scene),"City scene loadable: "+scene);
        Check(CrisisRules.Due(3,S)==CrisisKind.Building&&CrisisRules.Due(5,S)==CrisisKind.City&&CrisisRules.Due(7,S)==CrisisKind.Cosmic,"Three distinct crises on Days 3, 5 and 7");
        Check(CrisisRules.Due(2,S)==CrisisKind.None&&!CrisisRules.CanResolve(CrisisKind.Building,3,S),"No premature death and no intervention without deduction");
        var original=StoryText.Current.dayOneBroadcast.pages;
        SceneManager.LoadScene("CityStreet");yield return 1f;Ready();Set(2);yield return .2f;
        Check(CityWorld.Instance.Actor==null&&CityWorld.Instance.PlayerObject.GetComponent<Rigidbody2D>()!=null&&Camera.main.orthographic,"Ordinary city exploration stays 2D");
        Check(StoryText.Current.city.Find("circus").body==original[0]&&StoryText.Current.city.Find("sushi").body==original[1],"Original circus and sushi bulletins preserved verbatim");
        var keyboard=Keyboard.current;
        CityWorld.Travel("Stairwell");yield return .8f;
        Check(SceneManager.GetActiveScene().name=="Stairwell"&&RooftopAccess.Instance.CurrentFloor==1,"City returns to first-floor landing");
        Check(RooftopAccess.Instance.UseStairs(),"First-floor door opens the city through normal travel");yield return .8f;
        Capture("city-street");
        Check(!Investigation.Has("city-maintenance"),"Entering city does not collect evidence automatically");
        Check(Open(11,0),"Handover opens at physical interaction distance");
        var frozen=CityWorld.Instance.PlayerObject.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return .2f;
        Check(Vector3.Distance(frozen,CityWorld.Instance.PlayerObject.transform.position)<.05f,"Reading evidence blocks movement");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R));yield return .1f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .1f;
        Check(Investigation.Has("city-maintenance"),"R actively records handover before Day 3");CityWorld.Instance.CloseReading();
        SceneManager.LoadScene("CityService");yield return .7f;
        Check(Open(9,16)&&!CityWorld.Instance.ActSelected(),"Physical terminal refuses intervention without confirmed deduction");CityWorld.Instance.CloseReading();
        Check(Open(-11,5)&&CityWorld.Instance.RecordSelected(),"Service wiring can be actively recorded before Day 3");CityWorld.Instance.CloseReading();
        Check(RecordNeighborPrerequisites()&&Investigation.Has("deduction-building-plan"),"Existing reasoning graph confirms building intervention");
        Check(Open(9,16)&&!CityWorld.Instance.ActSelected()&&!S.buildingCrisisResolved,"Retired city switch cannot bypass new neighbor-source investigation");CityWorld.Instance.CloseReading();
        SceneManager.LoadScene("BuildingService2D");yield return .7f;
        var sourceRoom=Object.FindAnyObjectByType<NeighborRoom>();var sourceActor=GameObject.FindWithTag("Player");sourceActor.transform.position=new Vector3(-2,-.3f,0);sourceActor.GetComponent<Rigidbody2D>().position=sourceActor.transform.position;Physics2D.SyncTransforms();
        Check(sourceRoom.UseNearest()&&NeighborSlice.Instance.Choose(0),"Protected routing field action");NeighborSlice.Instance.Close();
        sourceActor.transform.position=new Vector3(2,-.3f,0);sourceActor.GetComponent<Rigidbody2D>().position=sourceActor.transform.position;Physics2D.SyncTransforms();
        Check(sourceRoom.UseNearest()&&NeighborSlice.Instance.Choose(0)&&S.buildingCrisisResolved,"Verified source lockout resolves bombing");NeighborSlice.Instance.Close();
        Set(3,TimePeriod.Night);clock.AdvanceTime();yield return .8f;
        Check(clock.Days==4&&!WorldEndSequence.IsPresenting,"Resolved third night reaches Day 4 alive");
        Check(!MorningRoutine.Instance.FishMorningPlaying&&S.fishMorningLoop!=game.LoopCount,"Fish morning does not play inside the offsite basement source room");
        // Existing fish owner may bring the actor back to the apartment for her morning sequence.
        if(MorningRoutine.WorldBlocked){MorningRoutine.Instance.DebugSkip();yield return .7f;}
        Check(RadiationRules.Fish(4,false)==FishStage.Human,"Fish radiation rule remains available after third-night intervention");
        SceneManager.LoadScene("LivingRoom");yield return .8f;
        Check(MorningRoutine.Instance.FishMorningPlaying,"Returning home on Day 4 triggers existing human Fish morning scene");
        MorningRoutine.Instance.DebugSkip();yield return .3f;
        Check(AquariumEntity.Instance.CompanionPresent,"Human Fish remains after her morning scene");
        SceneManager.LoadScene("CityBroadcast");yield return .7f;
        Check(Object.FindObjectsByType<CityEnemy>(FindObjectsSortMode.None).Length==0&&Camera.main.orthographic,"Day 4 investigation remains 2D until an actual encounter");
        Check(Open(-11,5)&&CityWorld.Instance.RecordSelected(),"Record city search orders");CityWorld.Instance.CloseReading();
        Check(Open(11,5)&&CityWorld.Instance.RecordSelected(),"Record broadcast routing");CityWorld.Instance.CloseReading();
        Check(Investigation.Connect("city-orders","city-routing",true),"City crisis has independent deduction");
        Check(Open(0,18)&&CityWorld.Instance.ActSelected()&&S.cityCrisisResolved,"Broadcast intervention resolves fifth-day city crisis");CityWorld.Instance.CloseReading();yield return .2f;
        Set(5,TimePeriod.Night);clock.AdvanceTime();yield return .3f;
        Check(clock.Days==6&&!WorldEndSequence.IsPresenting,"Resolved city crisis reaches Day 6");
        SceneManager.LoadScene("CityObservatory");yield return .7f;
        Check(Open(-11,5)&&CityWorld.Instance.RecordSelected(),"Record sixth-day observations");CityWorld.Instance.CloseReading();
        Check(Open(11,5)&&CityWorld.Instance.RecordSelected(),"Record array manual");CityWorld.Instance.CloseReading();
        Check(Investigation.Connect("city-spectrum","city-lens",true),"Observatory reasoning conclusion");
        Check(Open(0,18)&&CityWorld.Instance.ActSelected()&&S.daySevenCrisisResolved,"Cosmic route can be shut down through normal action");CityWorld.Instance.CloseReading();
        Set(7,TimePeriod.Night);clock.AdvanceTime();yield return .3f;
        Check(clock.Days==8&&!game.HasEnded&&!WorldEndSequence.IsPresenting,"Seven-day route reaches Day 8 without forced return");
        Place(new Vector3(3,-8,0));game.Damage(9);ProgressionSave.Write(S);var saved=ProgressionSave.Load();
        Check(saved.version==7&&saved.savedDay==8&&saved.savedScene=="CityObservatory"&&Mathf.Abs(saved.savedX-3)<.1f&&Mathf.Abs(saved.savedVitality-game.Vitality)<.01f,"v7 checkpoint stores date, scene, position and vitality");
        Check(JourneyCheckpoint.Resume(),"Continue accepts valid saved scene");yield return .8f;
        Check(Vector3.Distance(CityWorld.Instance.PlayerObject.transform.position,new Vector3(3,-8,0))<.2f,"Continue restores 2D exploration position");
        foreach(int version in new[]{1,2,3,4,5})
        {
            ProgressionSave.Write(new ProgressionState{version=version,loopCount=1,yogDreamSeen=true,knowledge=5,mutation=8});var migrated=ProgressionSave.Load();
            Check(migrated.version==7&&migrated.yogDreamSeen&&migrated.knowledge==5&&migrated.cityActions!=null,"Legacy v"+version+" migrates with permanent progress");
        }
        ProgressionSave.Write(S);
        Set(4);S.cityCrisisResolved=false;SceneManager.LoadScene("CityStreet");yield return .8f;
        Check(CognitiveRupture.BeginEncounter("CityStreet"),"A real hostile encounter starts perceptual rupture");
        Check(CognitiveRupture.IsActive&&SceneManager.GetActiveScene().name=="CityStreet","Tear presentation precedes scene transition");yield return 2.2f;
        if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)ScreenCapture.CaptureScreenshot("Documentation/Validation/Day2-3/City/filter-tear.png");
        yield return 1.4f;
        Check(SceneManager.GetActiveScene().name=="CityEncounter3D"&&!Camera.main.orthographic&&CityWorld.Instance.Actor!=null,"Only chase scene uses third-person 3D");
        var motor=CityWorld.Instance.Actor;var beforeMove=motor.transform.position;float walkUntil=Time.time+.4f;int inputGuard=0;
        while(Time.time<walkUntil&&inputGuard++<200){InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return .03f;}
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .1f;
        Check(Vector3.Distance(beforeMove,motor.transform.position)>.6f,"WASD input moves the third-person encounter actor");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftCtrl));yield return .2f;
        Check(motor.Crouching&&motor.Noise<=1,"Crouch lowers encounter noise");InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .1f;
        float stamina=motor.Stamina;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));yield return .3f;
        Check(motor.Stamina<stamina,"Sprint consumes encounter stamina");InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .1f;
        Capture("encounter-3d");
        var ai=Object.FindAnyObjectByType<CityEnemy>();var actor=CityWorld.Instance.Actor;
        ai.transform.position=new Vector3(0,0,2);ai.transform.forward=Vector3.back;Place(new Vector3(0,0,-2));
        Check(ai.CanSeeTarget(),"Enemy detects an unobstructed target in view");yield return .2f;
        Check(ai.Mode==EnemyMode.Chase,"Detection enters chase state");
        ai.transform.position=new Vector3(0,0,4);ai.transform.forward=Vector3.back;
        var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.transform.position=new Vector3(0,1,0);cover.transform.localScale=new Vector3(36,3,1);Physics.SyncTransforms();
        Check(!ai.CanSeeTarget(),"Solid cover occludes enemy vision");float searchUntil=Time.time+1.8f;int searchGuard=0;while(Time.time<searchUntil&&searchGuard++<200)yield return .03f;
        Check(ai.Mode==EnemyMode.Search,"Losing sight transitions from chase to search");Object.Destroy(cover);yield return .2f;
        ai.enabled=false;ai.transform.position=new Vector3(0,0,-.5f);Place(new Vector3(0,0,-2));actor.transform.forward=Vector3.forward;
        int charges=S.defenseCharges;Check(actor.Defend()&&S.defenseCharges==charges-1&&ai.Mode==EnemyMode.Stunned,"Limited defense consumes one charge and stuns nearby threat");
        Check(!actor.Defend()&&S.defenseCharges==charges-1,"Defense cooldown prevents repeated charge use in same instant");
        S.defenseCharges=0;Check(!actor.Defend(),"Empty defense supply cannot attack");
        Check(Open(0,19),"Escape requires reaching the actual 3D exit");
        Check(CognitiveRupture.IsActive,"Escape starts closure transition");yield return 1.8f;
        Check(SceneManager.GetActiveScene().name=="CityStreet"&&Camera.main.orthographic&&CityWorld.Instance.Actor==null,"Escaping restores 2D exploration");
        Check(S.cityActions.Contains("escaped-CityStreet")&&!S.cityCrisisResolved,"Escaping does not automatically solve city crisis");
        Check(CognitiveRupture.BeginEncounter("CityStreet"),"A second actual combat encounter can enter 3D");yield return 3.6f;
        CityWorld.Instance.CloseReading();game.RecordAncientGodEncounter();game.GrantLoopAbility();S.fishUnderstanding=3;S.fishAlly=true;
        int loop=game.LoopCount;float knowledge=game.Knowledge,mutation=game.Mutation;
        game.Damage(game.MaxVitality);yield return .8f;
        Check(game.LoopCount==loop+1&&clock.Days==1,"Lethal city damage returns exactly once through existing loop owner");
        Check(!S.cityCrisisResolved&&!S.buildingCrisisResolved&&!S.daySevenCrisisResolved&&S.cityActions.Count==0&&S.defenseCharges==3&&!S.fishAlly&&S.fishUnderstanding==0,"Death clears per-life interventions, supplies and Fish relationship");
        Check(game.HasLoopAbility&&Investigation.Has("deduction-building-plan")&&game.Knowledge==knowledge&&game.Mutation==mutation,"Death retains Bubble, recorded reasoning, knowledge and mutation");
        Set(3,TimePeriod.Night);clock.AdvanceTime();Check(WorldEndSequence.IsPresenting&&S.deathCrisis==(int)CrisisKind.Building,"Unresolved third night starts building death");
        StoryDebugActions.SetEnabled(true);WorldEndSequence.Instance.DebugSkip();StoryDebugActions.SetEnabled(false);
    }
}
