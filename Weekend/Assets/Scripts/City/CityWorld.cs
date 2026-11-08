using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Player;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.City
{
    // Runtime greybox: scene assets remain in Assets/Apartment, no second Unity project.
    public sealed class CityWorld : MonoBehaviour
    {
        public static CityWorld Instance { get; private set; }
        public static bool Reading => Instance != null && Instance.reading;
        public static bool IsCity(string name) => name=="CityStreet"||name=="CityService"||name=="CityBroadcast"||name=="CityObservatory"||name=="CityEncounter3D";
        public ThirdPersonMotor Actor { get; private set; }
        public GameObject PlayerObject { get; private set; }
        public bool InEncounter => SceneManager.GetActiveScene().name=="CityEncounter3D";
        Sprite pixelSprite; Texture2D pixelTexture;
        Interaction.PocketSceneArt art;
        public Vector3 SitePosition(string id)=>sites.Find(s=>s.id==id)?.position??new Vector3(float.NaN,0,0);
        readonly List<Site> sites=new List<Site>();
        readonly List<Material> materials=new List<Material>();
        sealed class Site {public string id,destination;public Vector3 position;public CrisisKind crisis;}
        Site selected;
        bool reading;
        string result="";
        int openedFrame;
        static ProgressionState State => GameManager.Instance.State;
        static Vector3 V(float x,float z)=>new Vector3(x,0,z);

        void Start()
        {
            Instance=this;
            string region=SceneManager.GetActiveScene().name;
            if(!InEncounter){ BuildExploration(region); return; }
            Box("Ground",new Vector3(0,-.3f,0),new Vector3(36,.5f,46),new Color(.10f,.035f,.045f));
            Box("West boundary",new Vector3(-18,2,0),new Vector3(1,4,46),new Color(.18f,.24f,.27f));
            Box("East boundary",new Vector3(18,2,0),new Vector3(1,4,46),new Color(.18f,.24f,.27f));
            Box("North boundary",new Vector3(0,2,23),new Vector3(36,4,1),new Color(.18f,.24f,.27f));
            Box("South boundary",new Vector3(0,2,-23),new Vector3(36,4,1),new Color(.18f,.24f,.27f));
            for(int i=0;i<5;i++)
            {
                float z=-11+i*6;
                Box("Solid cover",new Vector3(i%2==0?-5:6,1,z),new Vector3(3,2,2),new Color(.24f,.28f,.25f));
            }
            for(int rib=0;rib<8;rib++)
            {
                float z=-15+rib*4;
                Box("Exposed fleshy seam",new Vector3(rib%2==0?-15:15,2,z),new Vector3(.35f,4,2),new Color(.31f,.035f,.055f));
                Box("Dark blood trail",new Vector3((rib%3-1)*2,-.035f,z),new Vector3(1.4f,.02f,3),new Color(.17f,.008f,.018f));
            }
            var player=new GameObject();player.name="Hugo 3D";player.tag="Player";player.layer=8;
            player.transform.position=V(0,-17);
            var visual=GameObject.CreatePrimitive(PrimitiveType.Capsule);visual.name="Hugo silhouette";visual.transform.SetParent(player.transform,false);visual.transform.localPosition=Vector3.up*.9f;visual.transform.localScale=new Vector3(.65f,.9f,.65f);visual.layer=8;Destroy(visual.GetComponent<Collider>());visual.GetComponent<Renderer>().sharedMaterial=Material(new Color(.55f,.68f,.70f));visual.GetComponent<Renderer>().enabled=false;
            PlayerObject=player;Actor=player.AddComponent<ThirdPersonMotor>();
            var cameraObject=new GameObject("City third-person camera");cameraObject.tag="MainCamera";var camera=cameraObject.AddComponent<Camera>();camera.fieldOfView=58;camera.nearClipPlane=.1f;camera.farClipPlane=100;cameraObject.AddComponent<AudioListener>();Actor.view=camera;
            Add("escape",V(0,19));
            // Patrols become a city-wide threat from Day 4; disabling orders removes them next visit.
            if(InEncounter)
                for(int i=0;i<2;i++)
                {
                    var enemy=GameObject.CreatePrimitive(PrimitiveType.Capsule);enemy.name="Search patrol";enemy.layer=8;enemy.transform.position=V(i==0?-2:2,-13);
                    Destroy(enemy.GetComponent<Collider>());enemy.GetComponent<Renderer>().sharedMaterial=Material(new Color(.65f,.23f,.18f));
                    var ai=enemy.AddComponent<CityEnemy>();ai.target=Actor;ai.patrol=new[]{V(0,-17),V(-10,10+i*6),V(10,10+i*6),V(10,-4)};
                }
            JourneyCheckpoint.RestoreActor(player);
            ProgressionSave.Write(State);
        }
        void Flat(string name,Vector3 position,Vector2 size,Color tint,int order,bool solid=false)
        {
            var go=new GameObject(name);go.transform.position=position;go.transform.localScale=new Vector3(size.x,size.y,1);
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=pixelSprite;sr.color=tint;sr.sortingOrder=order;
            if(solid)go.AddComponent<BoxCollider2D>();
        }
        void BuildExploration(string region)
        {
            State.cityRegion=region;
            pixelTexture=new Texture2D(1,1);pixelTexture.SetPixel(0,0,Color.white);pixelTexture.Apply();pixelTexture.filterMode=FilterMode.Point;
            pixelSprite=Sprite.Create(pixelTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            art=gameObject.AddComponent<Interaction.PocketSceneArt>();art.Backdrop(region);
            Flat("West wall",new Vector3(-9,0,0),new Vector2(.3f,12),Color.black,0,true);Flat("East wall",new Vector3(9,0,0),new Vector2(.3f,12),Color.black,0,true);
            Flat("North wall",new Vector3(0,6,0),new Vector2(18,.3f),Color.black,0,true);Flat("South wall",new Vector3(0,-6,0),new Vector2(18,.3f),Color.black,0,true);
            Flat("Facade collision",new Vector3(0,4.8f,0),new Vector2(18,2),new Color(0,0,0,0),-3,true);
            PlayerObject=new GameObject("Hugo 2D");PlayerObject.tag="Player";PlayerObject.transform.position=new Vector3(0,-3,0);
            var sr=PlayerObject.AddComponent<SpriteRenderer>();sr.sprite=pixelSprite;sr.color=new Color(.65f,.76f,.74f);sr.sortingOrder=5;PlayerObject.transform.localScale=new Vector3(.7f,1,1);
            sr.sprite=art.Person();sr.color=Color.white;PlayerObject.transform.localScale=Vector3.one;
            var body=PlayerObject.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;var feet=PlayerObject.AddComponent<BoxCollider2D>();feet.size=new Vector2(.5f,.6f);feet.offset=new Vector2(0,-.5f);PlayerObject.AddComponent<PlayerController2D>();
            var cameraObject=new GameObject("City 2D camera");cameraObject.tag="MainCamera";cameraObject.transform.position=new Vector3(0,0,-10);var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6.6f;cameraObject.AddComponent<AudioListener>();
            Add("return",V(0,-4.5f),region=="CityStreet"?"Stairwell":"CityStreet");
            if(region=="CityStreet")
            {
                Add("circus",V(-6,-1.4f));Add("sushi",V(6,-1.4f));Add("caretaker",V(-3,-.8f));Add("city-maintenance",V(3,-.8f));
                Add("service",V(-7.6f,.1f),"CityService");Add("broadcast",V(7.6f,.1f),"CityBroadcast");Add("observatory",V(6,-4.3f),"CityObservatory");Add("rest",V(0,-2.2f));
            }
            else if(region=="CityService"){Add("city-fuse",V(-4,1));Add("building-terminal",V(4,1));}
            else if(region=="CityBroadcast"){Add("city-orders",V(-4,1));Add("city-routing",V(4,1));Add("city-terminal",V(0,3),null,CrisisKind.City);}
            else {Add("city-spectrum",V(-4,1));Add("city-lens",V(4,1));Add("cosmic-terminal",V(0,3),null,CrisisKind.Cosmic);}
            CognitiveRupture.RestoreExploration(PlayerObject);
            JourneyCheckpoint.RestoreActor(PlayerObject);
            var p=PlayerObject.transform.position;p.x=Mathf.Clamp(p.x,-8,8);p.y=Mathf.Clamp(p.y,-4,3);p.z=0;PlayerObject.transform.position=p;body.position=p;
            ProgressionSave.Write(State);
        }
        Material Material(Color tint)
        {
            var template=Resources.Load<Material>("CityGreybox");
            var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color");
            var material=template!=null?new Material(template):new Material(shader);material.color=tint;materials.Add(material);return material;
        }
        void Box(string name,Vector3 p,Vector3 size,Color color)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=Material(color);}
        void Add(string id,Vector3 p,string destination=null,CrisisKind crisis=CrisisKind.None)
        {
            if(!InEncounter)p=new Vector3(p.x,p.z,0);
            sites.Add(new Site{id=id,position=p,destination=destination,crisis=crisis});
            var tint=destination!=null?new Color(.2f,.48f,.55f):crisis!=CrisisKind.None?new Color(.62f,.42f,.15f):new Color(.3f,.46f,.32f);
            if(InEncounter)Box(id,p+Vector3.up*.65f,new Vector3(1.2f,1.3f,1.2f),tint);
            else {art.Marker(id,p,destination!=null);Flat(id+" interaction collider",p,new Vector2(.55f,.5f),new Color(0,0,0,0),-3,true);}
        }
        CityLine Text(string id)=>StoryText.Current.city.Find(id);
        void Update()
        {
            var k=Keyboard.current;if(k==null||PlayerObject==null)return;
            if(reading)
            {
                if(Time.frameCount<=openedFrame)return;
                if(k.escapeKey.wasPressedThisFrame||k.enterKey.wasPressedThisFrame){reading=false;selected=null;return;}
                if(k.rKey.wasPressedThisFrame)RecordSelected();
                if(k.eKey.wasPressedThisFrame)ActSelected();
                return;
            }
            if(ThirdPersonMotor.Blocked)return;
            FindNearest();
            if(k.eKey.wasPressedThisFrame)TryInteract();
        }
        public bool TryInteract()
        {
            if(PlayerObject==null||ThirdPersonMotor.Blocked)return false;
            FindNearest();
            if(selected==null)return false;
            if(selected.destination!=null){Travel(selected.destination);return true;}
            if(selected.id=="escape"){CognitiveRupture.Escape();return true;}
            if(selected.id=="rest"){TimeManager.Instance.AdvanceTime();return true;}
            reading=true;result="";openedFrame=Time.frameCount;return true;
        }
        void FindNearest()
        {
            selected=null;float closest=InEncounter?2.5f:1.35f;
            foreach(var site in sites){float d=Vector3.Distance(PlayerObject.transform.position,site.position);if(d<closest){closest=d;selected=site;}}
        }
        public void CloseReading(){reading=false;selected=null;}
        public bool RecordSelected()
        {
            if(selected==null)return false;
            int day=TimeManager.Instance.Days;
            if((selected.id=="city-orders"||selected.id=="city-routing")&&day<4 || (selected.id=="city-spectrum"||selected.id=="city-lens")&&day<6){result=Text(selected.id).locked;return false;}
            if(selected.id=="caretaker")
            {
                // A human interview or Fish's explicitly recorded testimony: neither is automatically granted.
                if(!GameManager.Instance.CanUseHumanRoute(60)&&!(State.fishAlly&&Investigation.Has("fish-help-promise"))){result=Text(selected.id).locked;return false;}
                bool ok=Investigation.Collect("city-maintenance");result=Text(selected.id).success;return ok;
            }
            bool recorded=Investigation.Collect(selected.id);result=recorded?Text(selected.id).success:Text(selected.id).locked;return recorded;
        }
        public bool ActSelected()
        {
            if(selected!=null&&(selected.id=="sushi"||selected.id=="circus"))
            {
                string id=selected.id;reading=false;selected=null;
                return CognitiveRupture.BeginSpecialEncounter(id,id=="sushi"?"SushiInterior3D":"CircusInterior3D",false,Interaction.NeighborRoom.Settings.Tier(GameManager.Instance.Sanity)>0);
            }
            if(selected==null||selected.crisis==CrisisKind.None)return false;
            if(!CrisisRules.CanResolve(selected.crisis,TimeManager.Instance.Days,State)){result=Text(selected.id).locked;return false;}
            if(selected.crisis==CrisisKind.Building){result=DeathBySeventhsDawn.Interaction.NeighborSlice.Text("use-service-core").body;return false;}
            if(selected.crisis==CrisisKind.City)State.cityCrisisResolved=true;
            if(selected.crisis==CrisisKind.Cosmic)State.daySevenCrisisResolved=true;
            if(!State.cityActions.Contains(selected.id))State.cityActions.Add(selected.id);
            result=Text(selected.id).success;ProgressionSave.Write(State);
            if(selected.crisis==CrisisKind.City)foreach(var enemy in FindObjectsByType<CityEnemy>(FindObjectsSortMode.None))Destroy(enemy.gameObject);
            return true;
        }
        public static void Travel(string scene)
        {
            if(!IsCity(scene)&&scene!="Stairwell")return;
            if(!Application.CanStreamedLevelBeLoaded(scene)){Debug.LogError("City scene absent from build settings: "+scene);return;}
            ProgressionSave.Write(State);
            if(scene=="Stairwell") { DeathBySeventhsDawn.Interaction.RooftopAccess.Instance.EnterFromStreet(); return; }
            SceneManager.LoadScene(scene);
        }
        void OnGUI()
        {
            if(PlayerObject==null||CognitiveRupture.IsActive||MainMenuUI.WorldBlocked||PauseMenuUI.WorldBlocked)return;
            if(!reading&&DeathBySeventhsDawn.Interaction.StoryJournal.WorldBlocked)return;
            var old=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1200f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            try
            {
                GUI.depth=-30;var style=new GUIStyle(GUI.skin.label){font=GameTypography.Current,fontSize=20,wordWrap=true};
                if(reading&&selected!=null)
                {
                    GUI.matrix=old;var text=Text(selected.id);
                    NarrativeCard.Draw(text.title,text.body,result,null,StoryText.UI(selected.crisis!=CrisisKind.None?"site-act":selected.id=="sushi"||selected.id=="circus"?"site-enter":"site-read"));
                }
                else
                {
                    int day=Mathf.Clamp(TimeManager.Instance.Days-1,0,StoryText.Current.city.days.Length-1);
                    style.fontSize=16;style.normal.textColor=new Color(.75f,.74f,.66f);
                    GUI.Label(new Rect(24,h-90,w-48,35),InEncounter?StoryText.UI("copy.CityWorld.74"):StoryText.UI("copy.CityWorld.75"),style);
                    string location=SceneManager.GetActiveScene().name;
                    GUI.Label(new Rect(24,h-50,w-48,35),(InEncounter?StoryText.UI("copy.CityWorld.76")+Mathf.RoundToInt(Actor.Stamina)+StoryText.UI("copy.CityWorld.77")+State.defenseCharges:location=="CityStreet"?StoryText.UI("copy.CityWorld.78"):location=="CityService"?StoryText.UI("copy.CityWorld.79"):location=="CityBroadcast"?StoryText.UI("copy.CityWorld.80"):StoryText.UI("copy.CityWorld.81"))+(selected!=null?"   E "+Text(selected.id).title:""),style);
                }
            }
            finally{GUI.matrix=old;GUI.color=color;GUI.depth=depth;}
        }
        void OnDestroy(){if(Instance==this)Instance=null;foreach(var material in materials)Destroy(material);if(pixelSprite!=null)Destroy(pixelSprite);if(pixelTexture!=null)Destroy(pixelTexture); }
    }
}
