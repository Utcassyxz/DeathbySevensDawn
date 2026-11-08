using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Interaction
{
    [DefaultExecutionOrder(-100)]
    public sealed class NeighborSlice : MonoBehaviour
    {
        public const string Crowbar="crowbar",Oil="cooking-oil",Medium="sealed-maintenance-medium",Tool="sealed-ember-tool";
        public static NeighborSlice Instance {get;private set;}
        public static bool WorldBlocked=>NeighborRoom.HatRevealActive||Instance!=null&&(Instance.open||Instance.deathTimer>0);
        public static bool IsOffsiteScene(string scene)=>scene=="NeighborRoom2D"||scene=="NeighborArena3D"||scene=="BuildingService2D"||VenueRoom.IsVenue(scene);
        public static bool OwnsDoor=>TimeManager.Instance!=null&&TimeManager.Instance.Days>=2&&S.neighborPlotHeard;
        public string CurrentNode=>node?.id;
        public bool IsOpen=>open;
        public bool CanAskFoil=>TimeManager.Instance.Days==2&&GameManager.Instance.LoopCount==0&&S.cultDayOneTalked&&Investigation.Has("cult-door-note")&&!S.inventory.Contains(CultNeighbor.Token)&&!S.aquariumSealed;
        public void ShowPreparation(){Show("sealed-ember-tool");}
        public string Notice {get;private set;}="";
        SliceNode node;string panel,evidence;
        NeighborDialogue graph;
        bool open;
        int frame;
        float deathTimer,toastUntil; string toast;
        Sprite pickupSprite;Texture2D pickupPixel;GameObject serviceMarker;
        public static bool ItemTaken(string id)=>S.neighborTakenItems.Contains(id)||S.inventory.Contains(id);
        static ProgressionState S=>GameManager.Instance.State;
        public static SliceLine Text(string id)=>StoryText.Current.neighborSlice.Find(id);
        public static string ItemName(string id)=>(StoryText.Current.neighborSlice.Find(id)?.title??id).Split('·')[0].Trim();
        void Awake(){pickupPixel=new Texture2D(1,1);pickupPixel.SetPixel(0,0,Color.white);pickupPixel.Apply();pickupSprite=Sprite.Create(pickupPixel,new Rect(0,0,1,1),Vector2.one*.5f,1);Instance=this;graph=JsonUtility.FromJson<NeighborDialogue>(Resources.Load<TextAsset>("NeighborDialogue").text);}
        void OnEnable(){SceneManager.sceneLoaded+=Loaded;GameManager.Instance.JourneyReset+=Reset;LoopManager.Instance.LoopRestarted+=Reset;}
        void OnDisable(){SceneManager.sceneLoaded-=Loaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=Reset;if(LoopManager.Instance!=null)LoopManager.Instance.LoopRestarted-=Reset;}
        void Reset(){open=false;node=null;deathTimer=0;Notice="";Time.timeScale=1;Time.fixedDeltaTime=.02f;}
        void Loaded(Scene scene,LoadSceneMode mode)
        {
            open=false;node=null;panel=evidence=null;
            if(scene.name=="Stairwell")serviceMarker=Prop("Basement service door",new Vector2(-1.6f,-1.86f),new Vector2(.8f,.25f),new Color(.24f,.46f,.48f));

        }
        GameObject Prop(string name,Vector2 position,Vector2 size,Color color)
        {var go=new GameObject(name);go.transform.position=position;go.transform.localScale=new Vector3(size.x,size.y,1);var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=pickupSprite;renderer.color=color;renderer.sortingOrder=8;return go;}
        public void CancelForDebug(){if(StoryDebugActions.Enabled){S.neighborDeathPending=false;Reset();}}
        public void Close(){open=false;node=null;panel=evidence=null;frame=Time.frameCount;}
        public bool Near(Vector2 p,float range=1.15f){var actor=GameObject.FindWithTag("Player");return actor!=null&&Vector2.Distance(actor.transform.position,p)<range;}
        public bool Use(string id)
        {
            if(GameManager.Instance.HasEnded||CognitiveRupture.IsActive||TimeManager.Instance.Days<2||!S.tutorialComplete)return false;
            string scene=SceneManager.GetActiveScene().name;
            bool reachable=id==Crowbar&&scene=="LivingRoom"&&Near(new Vector2(-4.5f,1.05f)) || id==Oil&&scene=="LivingRoom"&&Near(new Vector2(-2,1.1f)) || id==Medium&&scene=="Bathroom"&&Near(new Vector2(-1,1)) || id=="pipe-pulse"&&scene=="Bathroom"&&Near(new Vector2(1,1)) || id=="force-door"&&scene=="Corridor"&&Near(new Vector2(-4.7f,.42f)) || id=="service-entry"&&scene=="Stairwell"&&RooftopAccess.Instance.CurrentFloor==1&&Near(new Vector2(-1.6f,-1.6f));
            if(!reachable)return false;
            if((id==Crowbar||id==Oil||id==Medium)&&ItemTaken(id))return false;
            if(id==Crowbar&&!S.neighborCabinetOpened){Show("cabinet");return true;}
            if(id=="service-entry"){SceneManager.LoadScene("BuildingService2D");return true;}
            if(id=="force-door")
            {
                if(!S.neighborPlotHeard)return false;
                if(S.neighborDoorForced){SceneManager.LoadScene("NeighborRoom2D");return true;}
                Show("force-door",null);return true;
            }
            Show(id,id==Crowbar?"prep-manual":id==Oil?"prep-oil":id==Medium?"prep-medium":id=="pipe-pulse"?id:null);return true;
        }
        public void Show(string id,string clue=null){panel=id;evidence=clue;node=null;open=true;frame=Time.frameCount;Notice="";}
        public bool TakeItem()
        {
            if(!open||node!=null||panel!=Crowbar&&panel!=Oil&&panel!=Medium)return false;
            if(S.inventory.Contains(panel)||S.neighborTakenItems.Contains(panel)){Notice=Text("already-taken").body;return false;}
            S.inventory.Add(panel);S.neighborTakenItems.Add(panel);ProgressionSave.Write(S);toast=string.Format(Text("collected").body,ItemName(panel));toastUntil=Time.unscaledTime+2;Close();return true;
        }
        public bool Record()
        {
            if(!open||string.IsNullOrEmpty(evidence))return false;
            bool result=Investigation.Collect(evidence);Notice=Text(result?"recorded":"already-recorded").body;return result;
        }
        public bool Prepare()
        {
            if(!Investigation.Has("deduction-sealed-tool")||!S.inventory.Contains(Oil)||!S.inventory.Contains(Medium)||S.inventory.Contains(Tool)){Notice=Text("need-preparation").body;return false;}
            S.inventory.Remove(Oil);S.inventory.Remove(Medium);S.inventory.Add(Tool);ProgressionSave.Write(S);Notice=Text("prepared").body;return true;
        }
        public bool ForceEntry()
        {
            if(!open||panel!="force-door"||!S.inventory.Contains(Crowbar))return false;
            S.neighborDoorForced=true;Close();ProgressionSave.Write(S);SceneManager.LoadScene("NeighborRoom2D");return true;
        }
        public bool BeginDialogue()
        {
            if(SceneManager.GetActiveScene().name!="NeighborRoom2D"||S.neighborDefeated)return false;
            if(S.neighborHostile)return StartFight();
            Go(S.buildingCrisisResolved?"aftermath":S.neighborTrusted?"recruit":"intro");return true;
        }
        void Go(string id)
        {
            node=System.Array.Find(graph.nodes,n=>n.id==id);if(node==null){Close();return;}
            if(id=="end"){Close();return;}
            panel=node.line;evidence=node.evidence;open=true;frame=Time.frameCount;Notice="";
            if(node.effect=="hostile"){S.neighborHostile=true;Close();StartFight();}
            else if(node.effect=="trust")S.neighborTrusted=true;
            else if(node.effect=="join"||node.effect=="refuse"||node.effect=="deceive"){S.neighborMembership=node.effect;if(node.effect=="join"&&!S.inventory.Contains("invitation"))S.inventory.Add("invitation");}
            ProgressionSave.Write(S);
        }
        bool Allowed(string condition)=>string.IsNullOrEmpty(condition)||condition=="known-order"&&Investigation.Has("neighbor-order")||condition=="known-group"&&Investigation.Has("deduction-group-assignment");
        public bool Choose(int index)
        {
            if(!open)return false;
            if(node!=null)
            {if(index<0||node.choices==null||index>=node.choices.Length||!Allowed(node.choices[index].condition))return false;Go(node.choices[index].to);return true;}
            if(panel=="cabinet"&&index==0){S.neighborCabinetOpened=true;ProgressionSave.Write(S);Show(Crowbar,"prep-manual");return true;}
            if(panel=="door-conversation"){if(index==0){S.neighborDoorForced=true;Close();ProgressionSave.Write(S);SceneManager.LoadScene("NeighborRoom2D");return true;}Close();return true;}
            if(panel=="force-door"){if(index==0){Close();Show("door-conversation");return true;}if(index==1)return ForceEntry();if(index==2&&CanAskFoil){Close();return CultNeighbor.Instance.KnockDoor();}Close();return true;}
            if(panel=="routing"){if(index==0&&Investigation.Has("deduction-building-plan")&&Investigation.Has("deduction-source-location")&&TimeManager.Instance.Days<=3){S.serviceRouteIsolated=true;ProgressionSave.Write(S);Notice=Text("isolated").body;return true;}Notice=Text("need-deduction").body;return false;}
            if(panel=="source-core")
            {if(index==0&&S.serviceRouteIsolated&&CrisisRules.CanResolve(CrisisKind.Building,TimeManager.Instance.Days,S)){S.buildingCrisisResolved=true;EndingCollection.Unlock("building-saved");ProgressionSave.Write(S);Notice=Text("building-saved").body;return true;}Notice=Text("need-isolation").body;return false;}
            if(panel==Tool&&index==0)return Prepare();
            if(index==0&&TakeItem())return true;
            Close();return true;
        }
        public bool StartFight()
        {
            if(CognitiveRupture.IsActive||NeighborRoom.HatRevealActive)return false;
            var room=Object.FindAnyObjectByType<NeighborRoom>();if(room==null||room.Arena||room.Service)return false;
            Close();return room.BeginHatReveal();
        }
        public bool TryHearPlot()
        {
            if(S.neighborPlotHeard||TimeManager.Instance.Days<2||TimeManager.Instance.Days>3||SceneManager.GetActiveScene().name!="Corridor"||!Near(new Vector2(-4.7f,.42f),2.6f)||open)return false;
            S.neighborPlotHeard=true;Show("overheard");ProgressionSave.Write(S);return true;
        }
        public bool TryOfferGuidance()
        {
            string scene=SceneManager.GetActiveScene().name;
            if(StoryDebugActions.Enabled||S.neighborGuidanceSeen||TimeManager.Instance.Days<2||TimeManager.Instance.Days>3||!S.tutorialComplete||open||GameManager.Instance.HasEnded||scene!="LivingRoom"&&scene!="Bedroom")return false;
            S.neighborGuidanceSeen=true;Show("foreshadow");ProgressionSave.Write(S);return true;
        }
        public void Victory()
        {
            S.neighborDefeated=true;S.neighborHostile=false;ProgressionSave.Write(S);
            CognitiveRupture.ReturnFromSpecial();
        }
        public void BeginNeighborDeath()
        {
            if(deathTimer>0||GameManager.Instance.HasEnded)return;
            S.neighborDeathPending=true;S.lastDeathCause="neighbor";S.neighborDeathLearned=true;Investigation.Collect("neighbor-death-memory");EndingCollection.Unlock("neighbor-killed");
            Close();deathTimer=2.5f;ApartmentAudio.Instance?.PlayEndingScream();ProgressionSave.Write(S);
        }
        void Update()
        {
            if(serviceMarker!=null)serviceMarker.SetActive(RooftopAccess.Instance.CurrentFloor==1);
            if(S.neighborDeathPending&&deathTimer<=0&&!GameManager.Instance.HasEnded&&!MainMenuUI.WorldBlocked)BeginNeighborDeath();
            if(deathTimer>0){deathTimer-=Time.unscaledDeltaTime;if(deathTimer<=0){S.neighborDeathPending=false;GameManager.Instance.Damage(GameManager.Instance.MaxVitality);}return;}
            var k=Keyboard.current;if(k==null||MainMenuUI.WorldBlocked||PauseMenuUI.WorldBlocked||ProgressionHUD.DebugConsoleOpen)return;
            if(open)
            {
                if(Time.frameCount<=frame)return;
                if(k.escapeKey.wasPressedThisFrame){Close();return;}
                if(k.rKey.wasPressedThisFrame)Record();
                for(int i=0;i<4;i++)if(k[(Key)((int)Key.Digit1+i)].wasPressedThisFrame){Choose(i);return;}
                if(k.eKey.wasPressedThisFrame){if(panel==Tool)Prepare();else if(panel=="cabinet")Choose(0);else TakeItem();}
                if(k.enterKey.wasPressedThisFrame&&node==null&&panel!="force-door"&&panel!="routing"&&panel!="source-core")Close();
                return;
            }
            if(StoryJournal.WorldBlocked||CognitiveRupture.IsActive)return;
            if(TryOfferGuidance()||TryHearPlot())return;
            string scene=SceneManager.GetActiveScene().name,id=null;
            if(scene=="LivingRoom")id=Near(new Vector2(-4.5f,1.05f))?Crowbar:Near(new Vector2(-2,1.1f))?Oil:null;
            if(scene=="Bathroom")id=Near(new Vector2(-1,1))?Medium:Near(new Vector2(1,1))?"pipe-pulse":null;
            if(scene=="Corridor"&&OwnsDoor&&Near(new Vector2(-4.7f,.42f)))id="force-door";
            if(scene=="Stairwell"&&RooftopAccess.Instance.CurrentFloor==1&&Near(new Vector2(-1.6f,-1.6f),.6f))id="service-entry";
            if(id!=null&&(id==Crowbar||id==Oil||id==Medium)&&ItemTaken(id))id=null;
            if(id!=null&&TimeManager.Instance.Days>=2&&S.tutorialComplete)
            {WorldPromptUI.Offer("neighbor-slice","E "+Text(id==Crowbar&&!S.neighborCabinetOpened?"cabinet":id).title,100);if(k.eKey.wasPressedThisFrame)Use(id);}
        }
        void OnDestroy(){if(pickupSprite!=null)Destroy(pickupSprite);if(pickupPixel!=null)Destroy(pickupPixel);if(Instance==this)Instance=null;}
        void OnGUI()
        {
            if(ProgressionHUD.DebugConsoleOpen)return;
            if(!open&&deathTimer<=0){if(Time.unscaledTime<toastUntil)NarrativeCard.Caption("",toast);return;}
            var choices=new List<string>();var indices=new List<int>();
            if(deathTimer<=0)
            {
                if(node!=null&&node.choices!=null){for(int i=0;i<node.choices.Length;i++)if(Allowed(node.choices[i].condition)){choices.Add((i+1)+". "+Text(node.choices[i].line).body);indices.Add(i);}}
                else if(panel=="cabinet"||panel=="door-conversation"){choices.Add(Text(panel=="cabinet"?"open-cabinet":"enter-choice").body);indices.Add(0);}
                else if(panel=="force-door"||panel=="routing"||panel=="source-core")
                {choices.Add("1. "+Text(panel=="force-door"?"knock-choice":panel=="routing"?"isolate-choice":"lock-choice").body);indices.Add(0);if(panel=="force-door"&&S.inventory.Contains(Crowbar)){choices.Add("2. "+Text("force-choice").body);indices.Add(1);}if(panel=="force-door"&&CanAskFoil){choices.Add("3. "+Text("foil-choice").body);indices.Add(2);}}
                else if(panel==Tool){choices.Add(Text("craft-choice").body);indices.Add(0);}
                else if(panel==Crowbar||panel==Oil||panel==Medium){choices.Add(Text("take").body);indices.Add(0);}
            }
            var line=Text(deathTimer>0?"neighbor-killed":panel);
            int pick=NarrativeCard.Draw(line.title,line.body,Notice,choices.ToArray(),deathTimer>0?"":Text(string.IsNullOrEmpty(evidence)?"close-controls":"controls").body);
            if(pick>=0)Choose(indices[pick]);
        }
    }
}
