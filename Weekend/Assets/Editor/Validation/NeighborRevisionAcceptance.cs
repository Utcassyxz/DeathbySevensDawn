using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

public static class NeighborRevisionAcceptance
{
    static ProgressionState S => (ProgressionState)typeof(GameManager).GetProperty("State",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(GameManager.Instance);
    static ApartmentEventDirector D => ApartmentEventDirector.Instance;
    static NeighborSlice Slice => NeighborSlice.Instance;
    static void Check(bool b,string label)=>VerifyRevision.Check(b,"NEIGHBOR_REVISION "+label);
    static void Place(Vector3 p)
    {var actor=GameObject.FindWithTag("Player");var cc=actor.GetComponent<CharacterController>();if(cc)cc.enabled=false;actor.transform.position=p;var rb=actor.GetComponent<Rigidbody2D>();if(rb)rb.position=p;if(cc)cc.enabled=true;Physics.SyncTransforms();Physics2D.SyncTransforms();}
    static IEnumerator Setup(int day=2,string scene="NeighborRoom2D")
    {
        CrisisRules.LegacyValidation=false;TimeManager.AutoTimeSuppressed=true;
        NeighborDebugCommands.EndSession(false);GameManager.Instance.StartNewJourney();yield return .7f;
        StoryDebugActions.SetEnabled(true);TimeManager.Instance.SetNarrativeTime(day,TimePeriod.Morning,420);
        S.neighborGuidanceSeen=S.neighborPlotHeard=true;
        foreach(string id in new[]{"neighbor-order","skt079-broadcast"})Investigation.Collect(id);
        Investigation.Connect("neighbor-order","skt079-broadcast",true);
        S.inventory.Add(NeighborSlice.Crowbar);S.inventory.Add(NeighborSlice.Tool);
        StoryDebugActions.SetEnabled(false);SceneManager.LoadScene(scene);yield return 1f;
    }
    static void Walk(NeighborDialogue graph,string id,List<int> path,List<int[]> paths)
    {
        if(id=="hostile"||id=="end"){paths.Add(path.Concat(new[]{0}).ToArray());return;}
        var node=graph.nodes.Single(n=>n.id==id);
        for(int i=0;i<node.choices.Length;i++){path.Add(i);Walk(graph,node.choices[i].to,path,paths);path.RemoveAt(path.Count-1);}
    }
    public static IEnumerator DefeatBoss()
    {
        if(D.CurrentCard=="night-controls")D.Choose(0);
        var combat=UnityEngine.Object.FindAnyObjectByType<NeighborCombat>();
        Check(combat!=null&&combat.actor.FirstPerson,"actual encounter exposes finite first-person boss");
        var tenant=(GameObject)typeof(NeighborCombat).GetField("tenant",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(combat);
        S.inventory.Add(NeighborSlice.Crowbar);if(!S.inventory.Contains(NeighborSlice.Tool))S.inventory.Add(NeighborSlice.Tool);
        Place(tenant.transform.position+Vector3.forward*1.5f);combat.actor.SetViewYaw(180);
        yield return .15f;float health=combat.Health;
        Check(combat.UsePreparedTool()&&combat.Health<health&&combat.Health>0,"prepared tool damages rather than bypasses boss");
        // Fixture healing only keeps this deterministic damage-path check from turning into a manual dodging test.
        for(int i=0;i<65&&combat!=null&&combat.Health>0;i++)
        {
            GameManager.Instance.Heal(GameManager.Instance.MaxVitality);
            if(tenant!=null){Place(tenant.transform.position+Vector3.forward*1.5f);combat.actor.SetViewYaw(180);}
            combat.Strike();yield return .7f;
        }
        Check(S.neighborDefeated,"weapon impacts can defeat the real boss without a health reset");
        yield return 1.8f;
        Check(SceneManager.GetActiveScene().name=="NeighborRoom2D","victory returns to matching 2D room");
    }
    public static IEnumerator FinalResolution(int route)
    {
        if(route==1)S.neighborTrusted=true;
        if(route==2){Investigation.Collect("city-orders");Investigation.Collect("city-routing");Investigation.Connect("city-orders","city-routing",true);}
        if(!S.inventory.Contains(NeighborSlice.Crowbar))S.inventory.Add(NeighborSlice.Crowbar);
        bool known=S.neighborHatRevealed;float sanity=GameManager.Instance.Sanity;
        D.BeginFinalConfrontation();Check(D.Choose(route)&&!S.stageOneComplete,"final dialogue cannot instantly resolve route "+route);
        Check(D.CurrentCard=="day7-loss-control"&&D.Choose(0),"final choice commits to bodily loss of control");
        yield return .3f;
        var revealing=UnityEngine.Object.FindAnyObjectByType<NeighborRoom>();
        Check(revealing!=null&&revealing.RevealTextId==(known?"relapse":"hat-reveal"),"known exposure does not repeat the first hat reveal");
        yield return 5.9f;
        Check(Mathf.Abs(sanity-GameManager.Instance.Sanity-(known?0:20))<.1f,"first-exposure sanity cost is never charged twice");
        var fight=DefeatBoss();while(fight.MoveNext())yield return fight.Current;
        Check(NeighborDeterioration.DeviceAvailable(S)&&!S.stageOneComplete,"controller drops only after the encounter");
        Place(new Vector3(1,-1.7f,0));
        Check(UnityEngine.Object.FindAnyObjectByType<NeighborRoom>().UseNearest()&&D.CurrentCard==null&&S.inventory.Contains(NeighborSlice.Controller),"controller pickup closes immediately and commits inventory");
        yield return .15f;Check(!D.TakeDevice()&&GameObject.Find("Dropped initiation controller")==null,"picked controller disappears and cannot be taken twice");
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.I));yield return .15f;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return .15f;
        Check(CultNeighbor.Instance.BagOpen&&CultNeighbor.Instance.SelectInventoryItem(S.inventory.IndexOf(NeighborSlice.Controller))&&CultNeighbor.Instance.UseControllerFromInventory()&&D.CurrentCard=="day7-device","owned controller is usable from the actual inventory");
        string option=route==0?"device-revoke":route==1?"device-witness":"device-evacuate";
        var options=(string[])typeof(ApartmentEventDirector).GetMethod("Choices",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(D,null);
        int index=Array.IndexOf(options,option);Check(index>=0&&D.Choose(index)&&S.stageOneComplete,"earned post-combat resolution "+option);
    }
    public static IEnumerator RunResetSafety()
    {
        var setup=Setup(7,"CityStreet");while(setup.MoveNext())yield return setup.Current;
        GameManager.Instance.RecordAncientGodEncounter();GameManager.Instance.GrantLoopAbility();S.firstApocalypseExplained=true;
        Investigation.Collect("city-maintenance");S.inventory.Add(NeighborSlice.Controller);
        S.stageEvents.Add("7:controller-dropped");S.stageEvents.Add("7:controller-secured");
        int loop=GameManager.Instance.LoopCount;
        TimeManager.Instance.SetNarrativeTime(7,TimePeriod.Night,1440);WorldEndSequence.BeginFatalCrisis(CrisisKind.Building);
        yield return WorldEndSequence.Duration+1f;
        Check(TimeManager.Instance.Days==7&&TimeManager.Instance.Period==TimePeriod.Morning&&GameManager.Instance.LoopCount==loop+1,"actual purification death returns to Day7 morning");
        Check(!S.inventory.Contains(NeighborSlice.Controller)&&!S.stageEvents.Contains("7:controller-secured")&&!NeighborDeterioration.DeviceAvailable(S),"future controller and its physical pickup flags reset together");
        Check(Investigation.Has("city-maintenance")&&S.inventory.Contains(NeighborSlice.Crowbar),"earlier knowledge and ordinary possessions retain existing short-loop policy");
    }
    public static IEnumerator RunWithContinuity(){var focused=Run();while(focused.MoveNext())yield return focused.Current;var continuity=StageOneContinuity.Run();while(continuity.MoveNext())yield return continuity.Current;}
    public static IEnumerator Run()
    {
        var state=new ProgressionState();NeighborDeterioration.Observe(state,1,420);float early=NeighborDeterioration.Sanity(state);
        NeighborDeterioration.Observe(state,2,900);Check(NeighborDeterioration.Sanity(state)<early&&NeighborDeterioration.Mutation(state)>0,"clock causes declining sanity and growing mutation");
        float decline=state.neighborDecline;NeighborDeterioration.Observe(state,1,420);Check(state.neighborDecline==decline,"backtracking cannot heal deterioration");
        NeighborDeterioration.Reset(state);Check(state.neighborDecline==0&&state.neighborDialogueChoices.Count==0,"new-life reset clears encounter decline");
        var graph=JsonUtility.FromJson<NeighborDialogue>(Resources.Load<TextAsset>("NeighborDialogue").text);var paths=new List<int[]>();Walk(graph,"intro",new List<int>(),paths);
        Check(paths.Count==11,"all eleven valid dialogue routes enumerated");
        for(int p=0;p<paths.Count;p++)
        {
            var setup=Setup();while(setup.MoveNext())yield return setup.Current;
            float sanity=GameManager.Instance.Sanity;
            Check(Slice.CurrentNode=="intro"&&S.neighborDialogueActive,"route starts once: "+p);
            for(int i=0;i<paths[p].Length;i++)
            {
                Check(Slice.Choose(paths[p][i]),"valid route transition "+p+"/"+i);
                if(i==0&&Slice.IsOpen)
                {
                    string node=Slice.CurrentNode;int count=S.neighborDialogueChoices.Count;
                    Slice.Close();Check(Slice.IsOpen&&!Slice.BeginDialogue()&&Slice.CurrentNode==node,"committed dialogue cannot close or restart "+p);
                    if(p==0)
                    {
                        ProgressionSave.Write(S);var saved=ProgressionSave.Load();
                        typeof(GameManager).GetMethod("UseDebugState",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(GameManager.Instance,new object[]{saved,GameManager.Instance.Vitality});
                        SceneManager.LoadScene("NeighborRoom2D");yield return 1f;
                        Check(Slice.CurrentNode==node&&S.neighborDialogueChoices.Count==count,"save and scene reload resume exact selected node");
                    }
                }
            }
            yield return 6f;
            Check(SceneManager.GetActiveScene().name=="NeighborArena3D"&&S.neighborDialogueResolved,"every dialogue route reaches actual combat "+p);
            Check(Mathf.Abs(sanity-GameManager.Instance.Sanity-20)<.1f,"first exposure charges twenty sanity once "+p);
        }
        var defeat=DefeatBoss();while(defeat.MoveNext())yield return defeat.Current;
        Check(S.neighborNightResolved&&!D.RequestNight()&&!Slice.BeginDialogue(),"victory cannot be followed by a second opening conversation or night summon");
        StoryDebugActions.SetEnabled(true);TimeManager.Instance.SetNarrativeTime(7,TimePeriod.Morning,420);StoryDebugActions.SetEnabled(false);
        SceneManager.LoadScene("BuildingService2D");yield return 1f;
        Check(S.neighborHatRevealed&&S.neighborNightResolved,"final route preserves the earlier encounter across the calendar");
        Slice.Show("routing");Check(!Slice.Choose(0)&&!S.serviceRouteIsolated,"retired valve action cannot isolate an arbitrary branch");Slice.Close();
        Slice.Show("source-core");Check(!Slice.Choose(0)&&!S.stageOneComplete,"equipment inspection cannot finish the stage");Slice.Close();
        D.CompleteStage();Check(!S.stageOneComplete,"direct stage completion requires the recovered controller");
        Check(!D.OpenDevice(),"no controller reward before battle");
        foreach(string id in new[]{"pipe-pulse","city-fuse","city-maintenance"})Investigation.Collect(id);
        Investigation.Connect("deduction-group-assignment","pipe-pulse",true);Investigation.Connect("deduction-source-location","city-fuse",true);Investigation.Connect("deduction-building-plan","city-maintenance",true);
        var final=FinalResolution(0);while(final.MoveNext())yield return final.Current;
    }
}
