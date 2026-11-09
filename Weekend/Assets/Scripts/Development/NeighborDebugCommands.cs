using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Progression
{
    // Commands live in the existing PIN console. A cloned state and separate disk paths isolate tests.
    public static class NeighborDebugCommands
    {
        public static readonly string[] Scenes={"SampleScene","Bedroom","LivingRoom","Bathroom","Balcony","Corridor","Stairwell","Rooftop","CityStreet","CityService","CityBroadcast","CityObservatory","CityEncounter3D","NeighborRoom2D","NeighborArena3D","BuildingService2D","SushiInterior3D","CircusInterior3D"};
        public static bool Isolated=>snapshot!=null;
        public static float SanityOverride=-1;
        static ProgressionState snapshot;static string scene;static Vector3 position;static int day,period;static float vitality,minute;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){snapshot=null;SanityOverride=-1;ProgressionSave.DebugStorage=false;}
        public static void BeginSession()
        {
            if(Isolated)return;
            snapshot=GameManager.Instance.State;scene=SceneManager.GetActiveScene().name;day=TimeManager.Instance.Days;period=(int)TimeManager.Instance.Period;vitality=GameManager.Instance.Vitality;minute=TimeManager.Instance.GameMinute;
            var actor=GameObject.FindWithTag("Player");position=actor!=null?actor.transform.position:Vector3.zero;
            ProgressionSave.DebugStorage=true;GameManager.Instance.UseDebugState(JsonUtility.FromJson<ProgressionState>(JsonUtility.ToJson(snapshot)),vitality);EndingCollection.Reload();
        }
        public static bool EndSession(bool travel=true)
        {
            if(!Isolated)return false;
            // Cancel asynchronous effects while they still belong to the temporary state/storage.
            // Enabling debug here is deliberate: DebugSkip cancels, rather than completing, fatal sequences.
            if(travel && GameManager.Instance!=null)
            {
                StoryDebugActions.SetEnabled(true);
                NeighborSlice.Instance?.CancelForDebug();
                EndingCollectionUI.ClosePreview();
            }
            var restored=snapshot;snapshot=null;SanityOverride=-1;ProgressionSave.DebugStorage=false;if(GameManager.Instance!=null)GameManager.Instance.UseDebugState(restored,vitality);EndingCollection.Reload();
            if(!travel||GameManager.Instance==null)return true;
            restored.hasCheckpoint=true;restored.savedScene=scene;restored.savedDay=day;restored.savedPeriod=period;restored.savedX=position.x;restored.savedY=position.y;restored.savedZ=position.z;
            TimeManager.Instance.SetNarrativeTime(day,(TimePeriod)period,minute);restored.savedMinute=minute;JourneyCheckpoint.Resume();return true;
        }
        public static string Execute(string command)
        {
            if(!StoryDebugActions.Enabled)return "Unlock the existing console first.";
            var a=(command??"").Trim().Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);if(a.Length==0)return "No command.";
            if(command=="scene list")return string.Join(" · ",Scenes);
            if(command=="session end")
            {
                bool restored=EndSession();StoryDebugActions.SetEnabled(false);UnityEngine.Object.FindAnyObjectByType<ProgressionHUD>()?.CloseForEvent();
                return restored?"Normal journey restored.":"No isolated session.";
            }
            BeginSession();var state=GameManager.Instance.State;
            try
            {
                if(command=="session begin")return "Isolated session active. session end restores the normal journey.";
                if(a.Length==3&&a[0]=="scene"&&a[1]=="goto"&&Array.IndexOf(Scenes,a[2])>=0){if(a[2]=="NeighborArena3D"){state.encounterDestination="NeighborArena3D";state.encounterReturnScene="NeighborRoom2D";state.encounterId="neighbor";}
                    if(VenueRoom.IsVenue(a[2])){state.encounterDestination=a[2];state.encounterReturnScene="CityStreet";state.encounterReturnX=0;state.encounterReturnY=-1;}
                    StoryDebugActions.Jump(a[2],TimeManager.Instance.Days,(int)TimeManager.Instance.Period);return "Scene: "+a[2];}
                if(a.Length==3&&a[0]=="day"&&a[1]=="set"){TimeManager.Instance.DebugSetTime(int.Parse(a[2]),TimeManager.Instance.Period);return "Day set.";}
                if(a.Length==3&&a[0]=="time"&&a[1]=="set"){TimeManager.Instance.DebugSetTime(TimeManager.Instance.Days,(TimePeriod)Mathf.Clamp(int.Parse(a[2]),0,3));return "Period set: 0 morning, 1 afternoon, 2 evening, 3 night.";}
                if(a.Length==3&&a[0]=="sanity"&&a[1]=="set"){GameManager.Instance.DebugSetSanity(float.Parse(a[2],System.Globalization.CultureInfo.InvariantCulture));return "Perception updated.";}
                if(command=="mutation unlock shoulder_eye"){state.shoulderEyeUnlocked=true;return "Shoulder eye enabled in isolated debug.";}
                if(a.Length==3&&a[0]=="item"&&a[1]=="give")
                {if(Array.IndexOf(GameCommandConsole.Items,a[2])<0)return "Unknown item.";if(!state.inventory.Contains(a[2]))state.inventory.Add(a[2]);return "Item added.";}
                if(a.Length==3&&a[0]=="evidence"&&a[1]=="unlock"){state.journalUnlocked=state.investigationUnlocked=true;return Investigation.Collect(a[2])?"Evidence recorded.":"Unknown or already recorded.";}
                if(command=="flags reset"){NeighborDeterioration.Reset(state);state.neighborIntroduced=state.neighborDoorForced=state.neighborTrusted=state.neighborHostile=state.neighborDefeated=state.serviceRouteIsolated=state.buildingCrisisResolved=false;state.neighborGuidanceSeen=state.neighborHatRevealed=false;state.neighborTakenItems.Clear();state.neighborMembership="";state.neighborHealth=-1;state.eyeReadyUtcTicks=0;NeighborSlice.Instance.CancelForDebug();return "Temporary neighbor flags reset in isolated session.";}
                if(a.Length==3&&a[0]=="event"&&a[1]=="trigger")
                {
                    NeighborSlice.Instance.Close();UnityEngine.Object.FindAnyObjectByType<ProgressionHUD>()?.CloseForEvent();
                    if(a[2]=="neighbor_encounter"||a[2]=="neighbor_peace"||a[2]=="neighbor_known"||a[2]=="neighbor_prepared"||a[2]=="neighbor_eye")
                    {NeighborDeterioration.Reset(state);state.neighborNightBeat=0;state.neighborNightResolved=state.neighborNightLocked=false;}
                    switch(a[2])
                    {
                        case "sushi_normal":case "sushi_corrupt":case "circus_normal":case "circus_corrupt":
                            GameManager.Instance.DebugSetSanity(a[2].EndsWith("normal")?100:15);string venue=a[2].StartsWith("sushi")?"SushiInterior3D":"CircusInterior3D";state.encounterDestination=venue;state.encounterReturnScene="CityStreet";state.encounterReturnX=0;state.encounterReturnY=-1;StoryDebugActions.Jump(venue,2,0);return "Venue perception ready.";
                        case "neighbor_encounter":state.neighborIntroduced=false;state.neighborDefeated=false;state.neighborHealth=-1;state.neighborDoorForced=state.neighborHostile=true;StoryDebugActions.Jump("NeighborRoom2D",2,0);return "At the opened room. E near the tenant starts combat; close console and disable free debug only to test fatal crises.";
                        case "neighbor_peace":state.neighborHealth=-1;state.neighborIntroduced=false;state.neighborDoorForced=true;state.neighborTrusted=state.neighborHostile=state.neighborDefeated=false;StoryDebugActions.Jump("NeighborRoom2D",2,0);return "Peaceful conversation start.";
                        case "neighbor_known":
                            foreach(var id in new[]{"neighbor-order","skt079-broadcast"})Investigation.Collect(id);Investigation.Connect("neighbor-order","skt079-broadcast",true);
                            state.neighborIntroduced=state.neighborTrusted=state.neighborHostile=state.neighborDefeated=false;state.neighborDoorForced=true;state.neighborHealth=-1;StoryDebugActions.Jump("NeighborRoom2D",2,0);return "Known-order branch: choice 3 recalls the order; recruitment choice 3 deceives.";
                        case "neighbor_prepared":
                            state.neighborHealth=-1;state.neighborDefeated=false;state.shoulderEyeUnlocked=false;
                            foreach(var id in new[]{"prep-oil","prep-medium","prep-manual"})Investigation.Collect(id);
                            Investigation.Connect("prep-oil","prep-medium",true);Investigation.Connect("deduction-sealed-materials","prep-manual",true);
                            if(!state.inventory.Contains(NeighborSlice.Oil))state.inventory.Add(NeighborSlice.Oil);if(!state.inventory.Contains(NeighborSlice.Medium))state.inventory.Add(NeighborSlice.Medium);state.inventory.Remove(NeighborSlice.Tool);NeighborSlice.Instance.Prepare();
                            state.encounterDestination="NeighborArena3D";state.encounterReturnScene="NeighborRoom2D";state.encounterId="neighbor";StoryDebugActions.Jump("NeighborArena3D",2,0);return "Prepared route: G uses the single-use fictional tool.";
                        case "neighbor_eye":state.shoulderEyeUnlocked=true;state.eyeReadyUtcTicks=0;state.neighborHealth=-1;state.neighborDefeated=false;if(!state.inventory.Contains(NeighborSlice.Crowbar))state.inventory.Add(NeighborSlice.Crowbar);state.encounterDestination="NeighborArena3D";state.encounterReturnScene="NeighborRoom2D";state.encounterId="neighbor";StoryDebugActions.Jump("NeighborArena3D",2,0);return "Eye route: V slows perception, Q dodges, left mouse attacks.";
                        case "neighbor_death":NeighborSlice.Instance.BeginNeighborDeath();return "Neighbor ending presentation.";
                        case "neighbor_victory":if(SceneManager.GetActiveScene().name!="NeighborArena3D")return "Jump to NeighborArena3D first.";NeighborSlice.Instance.Victory();return "Victory return.";
                        case "apartment_explosion":WorldEndSequence.BeginFatalCrisis(true);return "Explosion preview: free debug prevents death.";
                        case "building_saved":
                            foreach(var id in new[]{"neighbor-order","skt079-broadcast","pipe-pulse","city-fuse"})Investigation.Collect(id);
                            Investigation.Connect("neighbor-order","skt079-broadcast",true);Investigation.Connect("deduction-group-assignment","pipe-pulse",true);Investigation.Connect("deduction-source-location","city-fuse",true);
                            if(!CrisisRules.LegacyValidation){Investigation.Collect("city-maintenance");Investigation.Connect("deduction-building-plan","city-maintenance",true);}
                            state.serviceRouteIsolated=state.buildingCrisisResolved=state.day7BombDefused=false;StoryDebugActions.Jump("BuildingService2D",CrisisRules.LegacyValidation?3:7,0);return CrisisRules.LegacyValidation?"Use real deductions and the two field controls.":"Use the handover archive as evidence; the final controller follows the neighbor encounter.";
                    }
                }
                return "Unknown command.";
            }
            catch(OverflowException){return "Invalid number.";}
            catch(FormatException){return "Invalid number.";}
        }
    }
}
