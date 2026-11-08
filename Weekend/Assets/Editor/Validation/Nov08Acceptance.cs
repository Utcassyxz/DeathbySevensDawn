using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.City;

public static class Nov08Acceptance
{
    static ProgressionState S=>(ProgressionState)typeof(GameManager).GetProperty("State",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(GameManager.Instance);
    static void Check(bool value,string label)=>VerifyRevision.Check(value,label);
    static void Place(Vector3 p)
    {
        var actor=GameObject.FindWithTag("Player");var cc=actor.GetComponent<CharacterController>();if(cc!=null)cc.enabled=false;actor.transform.position=p;
        var rb=actor.GetComponent<Rigidbody2D>();if(rb!=null){rb.position=p;rb.linearVelocity=Vector2.zero;}if(cc!=null)cc.enabled=true;Physics.SyncTransforms();Physics2D.SyncTransforms();
    }
    static void Capture(string name)
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        System.IO.Directory.CreateDirectory("Documentation/Validation/Nov08");
        var camera=Camera.main;var rt=new RenderTexture(1280,800,24);var target=camera.targetTexture;var active=RenderTexture.active;
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
        System.IO.File.WriteAllBytes("Documentation/Validation/Nov08/"+name+"-scene.png",image.EncodeToPNG());camera.targetTexture=target;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(image);
        ScreenCapture.CaptureScreenshot("Documentation/Validation/Nov08/"+name+".png");
    }
    public static IEnumerator Run()
    {
        var game=GameManager.Instance;game.StartNewJourney();yield return .7f;
        StoryDebugActions.SetEnabled(true);TimeManager.Instance.DebugSetTime(2,TimePeriod.Morning);StoryDebugActions.SetEnabled(false);
        var state=S;state.dreamSeen=state.tutorialComplete=state.journalUnlocked=state.investigationUnlocked=true;state.morningStage=10;
        SceneManager.LoadScene("LivingRoom");yield return .7f;
        Check(state.neighborGuidanceSeen&&NeighborSlice.Instance.IsOpen,"Natural preparation beat appears once on Day 2");Capture("foreshadow");yield return .3f;NeighborSlice.Instance.Close();
        Check(!NeighborSlice.Instance.TryOfferGuidance(),"The same thought does not interrupt exploration twice");
        Place(new Vector3(-4.5f,1.05f,0));Check(NeighborSlice.Instance.Use(NeighborSlice.Crowbar),"Rusty crowbar inspection remains a real interaction");
        if(!StoryText.English)StoryText.ToggleLanguage();Capture("item-card-en");yield return .3f;StoryText.ToggleLanguage();Capture("item-card-zh");yield return .3f;
        Check(NeighborSlice.Text("crowbar").body.Contains("锈迹")&&NeighborSlice.Text("foreshadow").body.Contains("浴室"),"Chinese object prose and in-world setup are localized");NeighborSlice.Instance.Close();
        SceneManager.LoadScene("NeighborRoom2D");yield return .6f;NeighborSlice.Instance.Close();
        Check(!state.neighborHatRevealed&&GameObject.Find("Neighbor removable hat").activeSelf,"A peaceful visit keeps the brain concealed");
        float burden=state.sanityBurden;Check(NeighborSlice.Instance.StartFight(),"Threat starts the staged 2D reveal");yield return 1.35f;Capture("hat-reveal-2d");yield return .3f;
        Check(SceneManager.GetActiveScene().name=="NeighborRoom2D"&&state.neighborHatRevealed&&Mathf.Abs(state.sanityBurden-burden-20)<.01f,"The 2D exposure commits exactly 20 sanity damage before loading 3D");yield return 4f;
        var combat=Object.FindAnyObjectByType<NeighborCombat>();Check(combat!=null&&!Camera.main.orthographic,"Reveal hands off to the existing 3D encounter owner");
        Check(combat.actor.FirstPerson&&Vector3.Distance(Camera.main.transform.position,combat.actor.transform.position+Vector3.up*1.62f)<.03f&&GameObject.Find("Hugo first-person hands")!=null,"Neighbor combat uses Hugo's actual first-person eye position with visible hands");
        // First long-range cast aims at the old standing position; actual movement evades it.
        Place(new Vector3(-4,0,-2.5f));float vitality=game.Vitality;yield return 2.3f;
        Check(!combat.IsSnared&&Mathf.Abs(game.Vitality-vitality)<.01f,"A telegraphed brain cast can be dodged without damage");
        // Wait for an actual AI cast, then stand in its marked aim point.
        bool seen=false;float deadline=Time.unscaledTime+12;
        while(Time.unscaledTime<deadline&&!combat.IsSnared)
        {if(combat.WindingUp&&combat.AttackKind==NeighborAttack.BrainCast)seen=true;yield return .05f;}
        Check(seen&&combat.IsSnared&&combat.actor.MovementLocked,"Brain cast catches an undodging Hugo and binds movement");
        var keyboard=Keyboard.current;var held=combat.actor.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return .35f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        Check(Vector3.Distance(held,combat.actor.transform.position)<.05f&&!combat.Strike(),"Entanglement blocks walking and physical counters");Capture("brain-bind-3d");yield return .3f;
        yield return NeighborRoom.Settings.brainBindDuration+.2f;
        Check(!combat.actor.MovementLocked&&!combat.IsSnared,"Neural entanglement releases automatically after its bounded duration");
        // Revisit in the same life: the memory is still disturbing, but the same reveal is not charged twice.
        SceneManager.LoadScene("NeighborRoom2D");yield return .6f;NeighborSlice.Instance.Close();burden=state.sanityBurden;
        Check(NeighborSlice.Instance.StartFight(),"Repeat hostile visit uses the same transition framework");yield return 5.7f;
        Check(Mathf.Abs(state.sanityBurden-burden)<.01f,"Re-entry does not charge the same hat exposure twice in one life");
        combat=Object.FindAnyObjectByType<NeighborCombat>();Place(new Vector3(1,0,-1));bool punch=false,kick=false;deadline=Time.unscaledTime+11;vitality=game.Vitality;
        while(Time.unscaledTime<deadline&&(!punch||!kick))
        {if(combat.WindingUp){punch|=combat.AttackKind==NeighborAttack.Punch;kick|=combat.AttackKind==NeighborAttack.Kick;}yield return .05f;}
        Check(punch&&kick&&game.Vitality<vitality,"Close combat alternates readable fist and kick attacks that actually damage Hugo");
        game.StartNewJourney();yield return .7f;Check(!S.neighborGuidanceSeen&&!S.neighborHatRevealed,"New journey resets the guidance and exposure flags");
        StoryDebugActions.SetEnabled(true);StoryDebugActions.SkipForcedStory();StoryDebugActions.SetEnabled(false);S.neighborGuidanceSeen=true;
        foreach(string scene in new[]{"CityStreet","CityService","CityBroadcast","CityObservatory"})
        {
            SceneManager.LoadScene(scene);yield return .5f;
            Check(Camera.main.orthographic&&Camera.main.orthographicSize<7&&GameObject.Find("Hand-drawn pocket scenery")!=null,"Illustrated compact 2D location: "+scene);Capture(scene);yield return .3f;
        }
        S.hasCheckpoint=true;S.savedScene="CityStreet";S.savedDay=2;S.savedPeriod=0;S.savedX=0;S.savedY=-17;S.savedZ=0;
        Check(JourneyCheckpoint.Resume(),"Legacy oversized city checkpoint can resume");yield return .6f;
        Check(CityWorld.Instance.PlayerObject.transform.position.y>=-4.1f,"Legacy city coordinates are safely relocated inside the smaller scene");
        StoryDebugActions.SetEnabled(true);NeighborDebugCommands.BeginSession();
        foreach(string venue in new[]{"sushi","circus"})
        {
            GameManager.Instance.DebugSetSanity(100);SceneManager.LoadScene("CityStreet");yield return .5f;
            Place(CityWorld.Instance.SitePosition(venue)+Vector3.down*.8f);
            Check(CityWorld.Instance.TryInteract()&&CityWorld.Instance.ActSelected(),"Street venue doorway explicitly enters "+venue);yield return 3.6f;
            var room=Object.FindAnyObjectByType<VenueRoom>();Check(room!=null&&room.Tier==0&&!Camera.main.orthographic,"High-sanity venue has normal 3D dressing: "+venue);Capture(venue+"-san100");yield return .3f;
            game.DebugSetSanity(15);yield return .2f;Check(room.Tier==3&&CognitiveRupture.IsActive,"Lowering sanity tears the filter in place: "+venue);yield return 3.5f;
            Check(Object.FindAnyObjectByType<VenueRoom>()==room&&GameObject.Find("Venue perception tier 3")!=null,"Perception changes geometry without changing scene or collision: "+venue);Capture(venue+"-san15");yield return .3f;
            Place(new Vector3(0,0,-7.5f));Check(room.Exit(),"Venue exit uses saved street return: "+venue);yield return 1.8f;Check(SceneManager.GetActiveScene().name=="CityStreet"&&Camera.main.orthographic,"Venue returns to 2D exploration: "+venue);
            foreach(string suffix in new[]{"normal","corrupt"})
            {Check(NeighborDebugCommands.Execute("event trigger "+venue+"_"+suffix).StartsWith("Venue"),"Tested venue debug event: "+venue+"_"+suffix);yield return .5f;Check(Object.FindAnyObjectByType<VenueRoom>().Tier==(suffix=="normal"?0:3),"Venue debug entry applies requested perception tier");}
        }
        NeighborDebugCommands.EndSession();yield return .6f;StoryDebugActions.SetEnabled(false);
        var credits=Resources.Load<TextAsset>("MusicCredits");Check(credits!=null&&credits.text.Contains("Harry Sun")&&credits.text.Contains("EndingTrue"),"Project music manifest names Harry Sun and covers ending tracks");
    }
}
