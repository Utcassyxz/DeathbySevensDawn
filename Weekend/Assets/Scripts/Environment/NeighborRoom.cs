using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Player;

namespace DeathBySeventhsDawn.Interaction
{
    // Shared floor plan: 2D (x,y) maps to 3D (x,z), with the same door and furniture.
    public sealed class NeighborRoom : MonoBehaviour
    {
        public const float Width=18,Depth=12;
        public static readonly Vector2 Door=new Vector2(-5,0),Table=new Vector2(0,1.8f),Sofa=new Vector2(3,2.2f),Tenant=new Vector2(1,0);
        public bool Arena=>SceneManager.GetActiveScene().name=="NeighborArena3D";
        public bool Service=>SceneManager.GetActiveScene().name=="BuildingService2D";
        public GameObject Actor {get;private set;}
        public int PerceptionTier {get;private set;}=-1;
        GameObject altered;AudioSource perceptionSound;
        PocketSceneArt art;GameObject hat;Sprite concealed,revealed;float revealStarted=-1;bool newExposure,exposureApplied;
        public static bool HatRevealActive {get;private set;}
        public float RevealProgress=>revealStarted<0?0:Mathf.Clamp01((Time.unscaledTime-revealStarted)/2.2f);
        public bool BeginHatReveal()
        {
            if(Arena||Service||HatRevealActive||tenant2D==null)return false;
            HatRevealActive=true;revealStarted=Time.unscaledTime;newExposure=!GameManager.Instance.State.neighborHatRevealed;exposureApplied=false;return true;
        }
        Sprite sprite,tenantSprite;Texture2D pixel,tenantTexture;GameObject tenant2D;
        readonly System.Collections.Generic.List<Material> materials=new System.Collections.Generic.List<Material>();
        public static PerceptualFilterSettings Settings=>Resources.Load<PerceptualFilterSettings>("PerceptualFilter");
        void Start()
        {
            pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();sprite=Sprite.Create(pixel,new Rect(0,0,1,1),Vector2.one*.5f,1);
            art=gameObject.AddComponent<PocketSceneArt>();if(Arena)Build3D();else Build2D();
            perceptionSound=gameObject.AddComponent<AudioSource>();perceptionSound.playOnAwake=false;perceptionSound.volume=.12f;
            var mix=Resources.Load<UnityEngine.Audio.AudioMixer>("ApartmentMix");if(mix!=null){var groups=mix.FindMatchingGroups("Master");if(groups.Length>0)perceptionSound.outputAudioMixerGroup=groups[0];}
            GameManager.Instance.Changed+=PresentPerception;PresentPerception();
            CognitiveRupture.RestoreExploration(Actor);JourneyCheckpoint.RestoreActor(Actor);
            ProgressionSave.Write(GameManager.Instance.State);
            if(!Arena&&!Service&&!GameManager.Instance.State.neighborIntroduced&&!GameManager.Instance.State.neighborDefeated){GameManager.Instance.State.neighborIntroduced=true;NeighborSlice.Instance.BeginDialogue();}
        }
        void Flat(string name,Vector2 p,Vector2 size,Color tint,bool solid=false,int order=1)
        {
            var go=new GameObject(name);go.transform.position=p;go.transform.localScale=new Vector3(size.x,size.y,1);var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=tint;r.sortingOrder=order;if(solid)go.AddComponent<BoxCollider2D>();
            if(name=="Table"||name=="Sofa"||name=="Wiring desk"||name=="Valve bank"||name=="Entrance")r.enabled=false;
        }
        public GameObject Solid(string name,Vector3 p,Vector3 scale,Color tint,PrimitiveType type=PrimitiveType.Cube,bool collision=true)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.position=p;go.transform.localScale=scale;
            var template=Resources.Load<Material>("VenueSurface")??Resources.Load<Material>("CityGreybox");var material=new Material(template);material.color=tint;materials.Add(material);go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision){var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);}return go;
        }
        void Build2D()
        {
            art.Backdrop(Service?"BuildingService2D":"NeighborRoom2D");

            Flat("Apartment floor",Vector2.zero,new Vector2(Service?12:18,Service?8:12),Service?new Color(.1f,.16f,.18f):new Color(.17f,.20f,.20f),false,-5);
            Flat("North wall",new Vector2(0,Service?4:6),new Vector2(Service?12:18,.3f),Color.black,true);Flat("South wall",new Vector2(0,Service?-4:-6),new Vector2(Service?12:18,.3f),Color.black,true);
            Flat("East wall",new Vector2(Service?6:9,0),new Vector2(.3f,Service?8:12),Color.black,true);Flat("West wall",new Vector2(Service?-6:-9,0),new Vector2(.3f,Service?8:12),Color.black,true);
            Flat("Entrance",Door,new Vector2(.6f,1.5f),new Color(.25f,.45f,.48f));
            Flat(Service?"Wiring desk":"Table",Table,new Vector2(2,1),Service?new Color(.25f,.30f,.33f):new Color(.32f,.23f,.16f),true);Flat(Service?"Valve bank":"Sofa",Sofa,new Vector2(2,1),Service?new Color(.22f,.35f,.36f):new Color(.23f,.30f,.24f),true);
            if(!Service)BuildTenant2D();
            else {Flat("Protected routing switch",new Vector2(-2,1),Vector2.one,new Color(.2f,.5f,.5f),true);Flat("Group source controller",new Vector2(2,1),Vector2.one,new Color(.5f,.25f,.2f),true);}
            Actor=new GameObject("Hugo");Actor.tag="Player";Actor.transform.position=new Vector3(-4,0,0);var renderer=Actor.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=new Color(.6f,.7f,.65f);renderer.sortingOrder=4;Actor.transform.localScale=new Vector3(.5f,.8f,1);
            renderer.sprite=art.Person();renderer.color=Color.white;Actor.transform.localScale=Vector3.one;
            var body=Actor.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;var box=Actor.AddComponent<BoxCollider2D>();box.size=new Vector2(.5f,.6f);box.offset=new Vector2(0,-.5f);Actor.AddComponent<PlayerController2D>();
            var camera=new GameObject("Room camera");camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-10);var view=camera.AddComponent<Camera>();view.orthographic=true;view.orthographicSize=Service?4.8f:6.8f;camera.AddComponent<AudioListener>();
        }
        void BuildTenant2D()
        {
            concealed=art.Person();revealed=art.Person(false,true);
            tenant2D=new GameObject("Neighbor beneath the hat");tenant2D.transform.position=Tenant;var renderer=tenant2D.AddComponent<SpriteRenderer>();renderer.sprite=GameManager.Instance.State.neighborHatRevealed?revealed:concealed;renderer.sortingOrder=4;
            hat=new GameObject("Neighbor removable hat");hat.transform.SetParent(tenant2D.transform,false);hat.transform.localPosition=new Vector3(0,.70f,0);var cap=hat.AddComponent<SpriteRenderer>();cap.sprite=art.Hat();cap.sortingOrder=5;hat.SetActive(!GameManager.Instance.State.neighborHatRevealed);
            if(GameManager.Instance.State.neighborDefeated)tenant2D.transform.rotation=Quaternion.Euler(0,0,80);
        }
        void Build3D()
        {
            VenueRoom.LightRoom();
            foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.type==LightType.Directional)l.intensity*=.35f;
            RenderSettings.ambientLight=new Color(.10f,.115f,.125f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.035f,.045f,.05f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.055f;
            Solid("Stained ceiling",new Vector3(0,4.1f,0),new Vector3(18,.2f,12),new Color(.09f,.10f,.09f));
            for(int i=0;i<12;i++)Solid("Floorboard seam",new Vector3(0,.011f,-5.5f+i),new Vector3(18,.012f,.015f),new Color(.045f,.035f,.03f),PrimitiveType.Cube,false);
            for(int i=0;i<6;i++){Solid("Wall panel",new Vector3(-7.5f+i*3,1.7f,5.8f),new Vector3(2.8f,3.2f,.08f),new Color(.13f,.15f,.13f),PrimitiveType.Cube,false);}
            Solid("Overturned partition",new Vector3(-1,.7f,-2.5f),new Vector3(3,1.4f,.5f),new Color(.19f,.14f,.10f));
            var lamp=new GameObject("Single sickly bulb");lamp.transform.position=new Vector3(2,3.4f,0);var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.72f,.64f,.40f);light.intensity=2.5f;light.range=9;light.shadows=LightShadows.Soft;
            Solid("Same apartment floor",new Vector3(0,-.2f,0),new Vector3(18,.4f,12),new Color(.16f,.12f,.11f));
            Solid("North wall",new Vector3(0,2,6),new Vector3(18,4,.25f),new Color(.22f,.18f,.17f));Solid("South wall",new Vector3(0,2,-6),new Vector3(18,4,.25f),new Color(.22f,.18f,.17f));
            Solid("East wall",new Vector3(9,2,0),new Vector3(.25f,4,12),new Color(.22f,.18f,.17f));Solid("West wall",new Vector3(-9,2,0),new Vector3(.25f,4,12),new Color(.22f,.18f,.17f));
            Solid("Table",new Vector3(Table.x,.6f,Table.y),new Vector3(2,1.2f,1),new Color(.32f,.23f,.16f));Solid("Sofa",new Vector3(Sofa.x,.45f,Sofa.y),new Vector3(2,.9f,1),new Color(.23f,.30f,.24f));
            Actor=new GameObject("Hugo");Actor.tag="Player";Actor.layer=8;Actor.transform.position=new Vector3(-4,0,0);
            var visual=Solid("Hugo body",Actor.transform.position+Vector3.up*.9f,new Vector3(.55f,.85f,.55f),new Color(.4f,.55f,.56f),PrimitiveType.Capsule,false);visual.transform.SetParent(Actor.transform,true);visual.layer=8;
            visual.GetComponent<Renderer>().enabled=false;
            var motor=Actor.AddComponent<ThirdPersonMotor>();motor.SetViewYaw(90);var camera=new GameObject("Encounter camera");camera.tag="MainCamera";motor.view=camera.AddComponent<Camera>();motor.view.nearClipPlane=.1f;motor.view.fieldOfView=60;camera.AddComponent<AudioListener>();
            var combat=new GameObject("Mutated neighbor encounter").AddComponent<NeighborCombat>();combat.room=this;combat.actor=motor;
        }
        void PresentPerception()
        {
            if(Settings==null)return;int tier=Settings.Tier(GameManager.Instance.Sanity);if(tier==PerceptionTier)return;PerceptionTier=tier;if(altered!=null)Destroy(altered);altered=new GameObject("Perceived structures tier "+tier);
            if(tier<1)return;
            perceptionSound.pitch=1-tier*.07f;perceptionSound.PlayOneShot(Resources.Load<AudioClip>("Audio/DreamCrack"),tier*.15f);
            for(int i=0;i<tier*3;i++)
            {
                if(Arena){var rib=Solid("Perceived biological rib",new Vector3(-4+i*.9f,1.7f,3.6f),new Vector3(.16f,1.8f+.2f*tier,.25f),new Color(.35f,.12f,.14f),PrimitiveType.Capsule,false);rib.transform.SetParent(altered.transform,true);}
                else {var go=new GameObject("Wall seam");go.transform.SetParent(altered.transform);go.transform.position=new Vector3(-4+i*.9f,3.5f,0);go.transform.localScale=new Vector3(.13f,.3f*tier,1);var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=new Color(.35f,.12f,.14f);r.sortingOrder=3;}
            }
        }
        string InteractionId()
        {
            if(Actor==null||Arena)return null;Vector2 p=Actor.transform.position;
            if(Vector2.Distance(p,Door)<1.2f)return Service?"return-service":"return-corridor";
            if(Service){if(Vector2.Distance(p,new Vector2(-2,1))<1.4f)return "routing";if(Vector2.Distance(p,new Vector2(2,1))<1.4f)return "source-core";if(Vector2.Distance(p,Table)<1.7f)return "city-fuse";}
            else {if(Vector2.Distance(p,Tenant)<1.5f&&!GameManager.Instance.State.neighborDefeated)return "neighbor-talk";if(Vector2.Distance(p,Table)<1.7f)return "neighbor-desk";}
            return null;
        }
        public bool UseNearest()
        {
            if(StoryJournal.WorldBlocked||CognitiveRupture.IsActive||GameManager.Instance.HasEnded)return false;
            string id=InteractionId();if(id==null)return false;
            if(id=="return-service"){RooftopAccess.Instance.EnterFromStreet();return true;}
            if(id=="return-corridor"){DeathBySeventhsDawn.World.RoomDoor2D.TravelTo("Corridor",new Vector2(-4.7f,-.55f));return true;}
            if(id=="neighbor-talk")return NeighborSlice.Instance.BeginDialogue();
            if(id=="neighbor-desk"){bool access=GameManager.Instance.State.neighborTrusted||GameManager.Instance.State.neighborDefeated;NeighborSlice.Instance.Show(access?"neighbor-order":"desk-locked",access?"neighbor-order":null);return true;}
            NeighborSlice.Instance.Show(id,id=="city-fuse"?id:null);return true;
        }
        void Update()
        {
            if(revealStarted>=0)
            {
                if(MainMenuUI.WorldBlocked||PauseMenuUI.WorldBlocked){revealStarted+=Time.unscaledDeltaTime;return;}
                float p=RevealProgress;if(hat!=null){hat.transform.localPosition=new Vector3(p*.5f,.70f+p*.8f,0);hat.transform.localRotation=Quaternion.Euler(0,0,p*-45);}
                if(p>.44f)tenant2D.GetComponent<SpriteRenderer>().sprite=revealed;
                if(p>.5f&&!exposureApplied)
                {exposureApplied=true;if(newExposure)GameManager.Instance.LoseSanity(20);GameManager.Instance.State.neighborHatRevealed=true;ProgressionSave.Write(GameManager.Instance.State);}
                if(p>=1)
                {
                    HatRevealActive=false;revealStarted=-1;
                    CognitiveRupture.BeginSpecialEncounter("neighbor","NeighborArena3D",false);
                }
                return;
            }
            if(tenant2D!=null&&!GameManager.Instance.State.neighborDefeated)tenant2D.transform.rotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*17)*(1+PerceptionTier));
            if(Arena||Actor==null||StoryJournal.WorldBlocked||CognitiveRupture.IsActive)return;
            string id=InteractionId();if(id==null)return;
            WorldPromptUI.Offer("slice-room","E "+NeighborSlice.Text(id).title,90);
            if(Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)UseNearest();
        }
        void OnGUI()
        {
            if(!HatRevealActive||revealStarted<0)return;
            var line=NeighborSlice.Text(RevealProgress<.5f?"hat-reveal":newExposure?"contamination":"contamination-known");
            NarrativeCard.Caption(line.title,line.body);
        }
        void OnDestroy(){if(Arena)RenderSettings.fog=false;HatRevealActive=false;if(GameManager.Instance!=null)GameManager.Instance.Changed-=PresentPerception;foreach(var m in materials)Destroy(m);if(sprite!=null)Destroy(sprite);if(pixel!=null)Destroy(pixel);if(tenantSprite!=null)Destroy(tenantSprite);if(tenantTexture!=null)Destroy(tenantTexture); }
    }
}
