using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.City;
public static class InteractionReworkAcceptance
{
    static ProgressionState S=>(ProgressionState)typeof(GameManager).GetProperty("State",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(GameManager.Instance);
    static NeighborSlice Slice=>NeighborSlice.Instance;
    static void Check(bool b,string label)=>VerifyRevision.Check(b,label);
    static void Place(Vector3 p){var a=GameObject.FindWithTag("Player");var cc=a.GetComponent<CharacterController>();if(cc)cc.enabled=false;a.transform.position=p;var rb=a.GetComponent<Rigidbody2D>();if(rb){rb.position=p;rb.linearVelocity=Vector2.zero;}if(cc)cc.enabled=true;Physics.SyncTransforms();Physics2D.SyncTransforms();}
    static void Capture(string name)
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        string dir="Documentation/Rework/Visuals";System.IO.Directory.CreateDirectory(dir);
        var cam=Camera.main;var rt=new RenderTexture(1280,800,24);var previous=cam.targetTexture;var active=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
        var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();System.IO.File.WriteAllBytes(dir+"/"+name+".png",image.EncodeToPNG());cam.targetTexture=previous;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(image);
    }
    static void Ready(){StoryDebugActions.SetEnabled(true);TimeManager.Instance.DebugSetTime(2,TimePeriod.Morning);StoryDebugActions.SkipForcedStory();StoryDebugActions.SetEnabled(false);S.dreamSeen=S.tutorialComplete=S.journalUnlocked=S.investigationUnlocked=true;S.morningStage=10;}
    public static IEnumerator Run()
    {
        var game=GameManager.Instance;game.StartNewJourney();yield return .7f;Ready();SceneManager.LoadScene("LivingRoom");yield return .6f;
        Check(S.neighborGuidanceSeen&&Slice.IsOpen&&!S.neighborPlotHeard,"Day two begins with exploration, no pre-known conspiracy");Slice.Close();
        Place(new Vector3(-4.5f,1.05f,0));Check(Slice.Use(NeighborSlice.Crowbar)&&!Slice.TakeItem(),"Closed cupboard cannot directly grant a crowbar");
        Check(Slice.Choose(0)&&S.neighborCabinetOpened&&Slice.Record(),"Open cupboard then examine and actively record its contents");
        Check(Slice.TakeItem()&&!Slice.IsOpen&&S.inventory.Contains(NeighborSlice.Crowbar),"Pickup closes card immediately and commits inventory");
        Check(!Slice.Use(NeighborSlice.Crowbar)&&!Slice.IsOpen,"Taken crowbar has no repeat world inspection or take option");
        Place(new Vector3(-2,1.1f,0));Check(Slice.Use(NeighborSlice.Oil)&&Slice.Record()&&Slice.TakeItem()&&!Slice.IsOpen,"Oil inspection records before pickup, then closes");
        SceneManager.LoadScene("Bathroom");yield return .4f;Place(new Vector3(-1,1,0));Check(Slice.Use(NeighborSlice.Medium)&&Slice.Record()&&Slice.TakeItem(),"Repair box has the same single-use pickup contract");
        Place(new Vector3(1,1,0));Check(Slice.Use("pipe-pulse")&&Slice.Record(),"Physical pipe anomaly remains investigable");Slice.Close();
        Check(Investigation.Connect("prep-oil","prep-medium",true)&&Investigation.Connect("deduction-sealed-materials","prep-manual",true)&&Slice.Prepare(),"Recorded materials still yield abstract preparation through reasoning");
        SceneManager.LoadScene("LivingRoom");yield return .4f;Place(new Vector3(-2,1.1f,0));Check(!Slice.Use(NeighborSlice.Oil),"Consumed materials do not respawn after scene reload");
        SceneManager.LoadScene("Corridor");yield return .5f;Check(!S.neighborPlotHeard,"Entering corridor alone does not reveal distant dialogue");Place(new Vector3(-4.7f,.42f,0));yield return .2f;
        Check(S.neighborPlotHeard&&Slice.IsOpen,"Approaching 401 actually triggers overheard demolition plan");Slice.Close();Check(!Slice.TryHearPlot(),"Eavesdropping does not repeat in the same life");ProgressionSave.Write(S);var saved=ProgressionSave.Load();Check(saved.neighborPlotHeard&&saved.neighborCabinetOpened&&saved.neighborTakenItems.Contains(NeighborSlice.Crowbar),"New discovery and exhaustion fields survive a real save roundtrip");
        Check(!Slice.CanAskFoil,"Unmet foil dialogue is not offered");S.cultDayOneTalked=true;Investigation.Collect("cult-door-note");Check(Slice.CanAskFoil&&Slice.Use("force-door")&&Slice.Choose(2)&&CultNeighbor.Instance.DoorOpen,"New knock menu preserves eligible original foil conversation");CultNeighbor.Instance.DebugSkip();yield return .1f;
        Check(Slice.Use("force-door")&&Slice.Choose(0)&&Slice.IsOpen&&!S.neighborDoorForced,"Knocking initiates a conversation before entry");
        Check(Slice.Choose(0),"Neighbor invites player inside after confrontation");yield return .5f;
        Check(SceneManager.GetActiveScene().name=="NeighborRoom2D"&&Camera.main.orthographic&&Slice.CurrentNode=="intro","Invitation enters a peaceful 2D room");
        Capture("neighbor-2d");Check(!S.neighborHatRevealed&&GameObject.Find("Neighbor removable hat").activeSelf,"No exposed brain before hostility");
        Slice.Close();Place(new Vector3(-5,0,0));Check(Object.FindAnyObjectByType<NeighborRoom>().UseNearest(),"Leave via neighbor door");yield return .5f;
        Check(SceneManager.GetActiveScene().name=="Corridor"&&Vector2.Distance(GameObject.FindWithTag("Player").transform.position,new Vector2(-4.7f,-.55f))<.1f,"Neighbor exit lands outside 401 rather than Hugo's front door");
        Place(new Vector3(-4.7f,.42f,0));Check(Slice.Use("force-door"),"Opened neighbor room remains enterable");yield return .5f;Slice.BeginDialogue();float sanity=game.Sanity;
        Check(Slice.Choose(1),"Player threat, not unsolicited guidance, initiates hostility");yield return 5.8f;
        var combat=Object.FindAnyObjectByType<NeighborCombat>();Check(combat!=null&&combat.actor.FirstPerson&&Mathf.Abs(sanity-game.Sanity-20)<.1f,"Hat reveal enters first-person combat and costs exactly twenty SAN");
        Check(GameObject.Find("Left sleeve")==null&&GameObject.Find("Right sleeve")==null&&GameObject.Find("Hugo first-person weapon")!=null,"Camera shows the weapon, not two block hands");
        Check(!game.HasAbility("shoulder_eye")&&GameObject.Find("Unnatural shoulder eye")==null&&!combat.ActivateEye(),"Unearned shoulder eye neither appears nor activates");
        Capture("neighbor-first-person");Check(combat.Health>=200,"Neighbor has miniboss health");
        Place(new Vector3(-3,0,0));Check(combat.UsePreparedTool()&&combat.Health>0&&!S.inventory.Contains(NeighborSlice.Tool),"Prepared tool wounds and staggers boss, cannot instantly win");
        var mouse=InputSystem.AddDevice<Mouse>("ReworkAcceptanceMouse");mouse.MakeCurrent();float before=combat.Health;
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));yield return .12f;InputSystem.QueueStateEvent(mouse,new MouseState());yield return .5f;
        Check(GameObject.Find("Hugo first-person weapon")!=null&&combat.Health==before,"Left click can swing out of reach without remotely damaging the boss");
        Place(new Vector3(1,0,-1.6f));combat.actor.SetViewYaw(0);yield return .1f;before=combat.Health;
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));yield return .3f;InputSystem.QueueStateEvent(mouse,new MouseState());
        Check(combat.Health<before,"Mouse left swing resolves directional damage at contact time");
        combat.actor.ReceiveImpact(Vector3.back,5);var origin=combat.actor.transform.position;yield return .15f;
        Check(combat.actor.Staggered&&Vector3.Distance(origin,combat.actor.transform.position)>.15f,"Hugo visibly staggers and moves backward on impact");yield return .5f;
        Check(!combat.actor.Staggered,"Hit recovery is bounded");
        // Stand at casting range and observe an actual AI tether, then the follow-up pressure.
        Place(new Vector3(-3,0,0));float deadline=Time.unscaledTime+15;bool bound=false,followup=false;float hp=game.Vitality;
        while(Time.unscaledTime<deadline&&!bound){bound=combat.IsSnared;yield return .05f;}
        Check(bound,"Boss can land an actual neural bind");hp=game.Vitality;deadline=Time.unscaledTime+3;
        while(Time.unscaledTime<deadline){followup|=combat.WindingUp&&combat.AttackKind!=NeighborAttack.BrainCast;yield return .05f;}
        Check(followup,"Boss continues melee pressure while its brain holds Hugo");
        InputSystem.RemoveDevice(mouse);
        game.StartNewJourney();yield return .6f;Check(!S.neighborPlotHeard&&!S.neighborCabinetOpened,"New journey resets clue discovery and cupboard state");Ready();S.neighborGuidanceSeen=true;
        SceneManager.LoadScene("CityStreet");yield return .5f;
        Capture("city-street");Check(CityWorld.Instance.SitePosition("service").x<-7&&CityWorld.Instance.SitePosition("broadcast").x>7&&CityWorld.Instance.SitePosition("observatory").y<-4,"Distant city destinations are street-edge routes, not adjacent apartment doors");
        Place(CityWorld.Instance.SitePosition("caretaker")+Vector3.down*.8f);Check(CityWorld.Instance.TryInteract()&&!CityWorld.Instance.ActSelected(),"Caretaker inspection has no fake executable action");CityWorld.Instance.CloseReading();
        // Preserve previously established crisis, death-loop, and city contracts.
        game.StartNewJourney();yield return .6f;var death=DeathCycleAcceptance.Run();while(death.MoveNext())yield return death.Current;
        NeighborDebugCommands.EndSession(false);game.StartNewJourney();yield return .6f;var city=CityAcceptance.Run();while(city.MoveNext())yield return city.Current;
    }
}

