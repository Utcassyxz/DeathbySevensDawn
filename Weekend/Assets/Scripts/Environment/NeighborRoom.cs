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
        public const float Width=12,Depth=8;
        public static readonly Vector2 Door=new Vector2(-5,0),Table=new Vector2(0,1.8f),Sofa=new Vector2(3,2.2f),Tenant=new Vector2(1,0);
        public bool Arena=>SceneManager.GetActiveScene().name=="NeighborArena3D";
        public bool Service=>SceneManager.GetActiveScene().name=="BuildingService2D";
        public GameObject Actor {get;private set;}
        public int PerceptionTier {get;private set;}=-1;
        GameObject altered;AudioSource perceptionSound;
        Sprite sprite,tenantSprite;Texture2D pixel,tenantTexture;GameObject tenant2D;
        readonly System.Collections.Generic.List<Material> materials=new System.Collections.Generic.List<Material>();
        public static PerceptualFilterSettings Settings=>Resources.Load<PerceptualFilterSettings>("PerceptualFilter");
        void Start()
        {
            pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();sprite=Sprite.Create(pixel,new Rect(0,0,1,1),Vector2.one*.5f,1);
            if(Arena)Build3D();else Build2D();
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
        }
        public GameObject Solid(string name,Vector3 p,Vector3 scale,Color tint,PrimitiveType type=PrimitiveType.Cube,bool collision=true)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.position=p;go.transform.localScale=scale;
            var template=Resources.Load<Material>("CityGreybox");var material=new Material(template);material.color=tint;materials.Add(material);go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision){var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);}return go;
        }
        void Build2D()
        {
            Flat("Apartment floor",Vector2.zero,new Vector2(12,8),Service?new Color(.1f,.16f,.18f):new Color(.17f,.20f,.20f),false,-5);
            Flat("North wall",new Vector2(0,4),new Vector2(12,.3f),Color.black,true);Flat("South wall",new Vector2(0,-4),new Vector2(12,.3f),Color.black,true);
            Flat("East wall",new Vector2(6,0),new Vector2(.3f,8),Color.black,true);Flat("West wall",new Vector2(-6,0),new Vector2(.3f,8),Color.black,true);
            Flat("Entrance",Door,new Vector2(.6f,1.5f),new Color(.25f,.45f,.48f));
            Flat(Service?"Wiring desk":"Table",Table,new Vector2(2,1),Service?new Color(.25f,.30f,.33f):new Color(.32f,.23f,.16f),true);Flat(Service?"Valve bank":"Sofa",Sofa,new Vector2(2,1),Service?new Color(.22f,.35f,.36f):new Color(.23f,.30f,.24f),true);
            if(!Service)BuildTenant2D();
            else {Flat("Protected routing switch",new Vector2(-2,1),Vector2.one,new Color(.2f,.5f,.5f),true);Flat("Group source controller",new Vector2(2,1),Vector2.one,new Color(.5f,.25f,.2f),true);}
            Actor=new GameObject("Hugo");Actor.tag="Player";Actor.transform.position=new Vector3(-4,0,0);var renderer=Actor.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=new Color(.6f,.7f,.65f);renderer.sortingOrder=4;Actor.transform.localScale=new Vector3(.5f,.8f,1);
            var body=Actor.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;Actor.AddComponent<BoxCollider2D>();Actor.AddComponent<PlayerController2D>();
            var camera=new GameObject("Room camera");camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-10);var view=camera.AddComponent<Camera>();view.orthographic=true;view.orthographicSize=4.8f;camera.AddComponent<AudioListener>();
        }
        void BuildTenant2D()
        {
            tenantTexture=new Texture2D(16,28,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
            for(int y=0;y<28;y++)for(int x=0;x<16;x++)
            {
                Color c=Color.clear;
                if(y>=2&&y<11&&((x>=4&&x<=6)||(x>=9&&x<=11)))c=new Color(.15f,.20f,.22f);
                if(y>=11&&y<20&&x>=4&&x<=11)c=new Color(.28f,.32f,.28f);
                if(y>=10&&y<19&&((x>=2&&x<=3)||(x>=12&&x<=13)))c=new Color(.62f,.45f,.36f);
                if(y>=20&&y<=26&&x>=5&&x<=11)c=new Color(.66f,.49f,.40f);
                if(y>=25&&x<=8)c=Color.clear;
                if(y>=24&&y<=25&&x>=5&&x<=8)c=new Color(.63f,.16f,.24f);
                if(y==22&&(x==6||x==10))c=Color.black;
                tenantTexture.SetPixel(x,y,c);
            }
            tenantTexture.Apply();tenantSprite=Sprite.Create(tenantTexture,new Rect(0,0,16,28),new Vector2(.5f,.25f),20);
            tenant2D=new GameObject("Early human mutation: exposed scalp");tenant2D.transform.position=Tenant;var renderer=tenant2D.AddComponent<SpriteRenderer>();renderer.sprite=tenantSprite;renderer.sortingOrder=4;
            if(GameManager.Instance.State.neighborDefeated)tenant2D.transform.rotation=Quaternion.Euler(0,0,80);
        }
        void Build3D()
        {
            Solid("Same apartment floor",new Vector3(0,-.2f,0),new Vector3(12,.4f,8),new Color(.16f,.12f,.11f));
            Solid("North wall",new Vector3(0,1.5f,4),new Vector3(12,3,.25f),new Color(.22f,.18f,.17f));Solid("South wall",new Vector3(0,1.5f,-4),new Vector3(12,3,.25f),new Color(.22f,.18f,.17f));
            Solid("East wall",new Vector3(6,1.5f,0),new Vector3(.25f,3,8),new Color(.22f,.18f,.17f));Solid("West wall",new Vector3(-6,1.5f,0),new Vector3(.25f,3,8),new Color(.22f,.18f,.17f));
            Solid("Table",new Vector3(Table.x,.6f,Table.y),new Vector3(2,1.2f,1),new Color(.32f,.23f,.16f));Solid("Sofa",new Vector3(Sofa.x,.45f,Sofa.y),new Vector3(2,.9f,1),new Color(.23f,.30f,.24f));
            Actor=new GameObject("Hugo");Actor.tag="Player";Actor.layer=8;Actor.transform.position=new Vector3(-4,0,0);
            var visual=Solid("Hugo body",Actor.transform.position+Vector3.up*.9f,new Vector3(.55f,.85f,.55f),new Color(.4f,.55f,.56f),PrimitiveType.Capsule,false);visual.transform.SetParent(Actor.transform,true);visual.layer=8;
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
            if(id=="return-corridor"){SceneManager.LoadScene("Corridor");return true;}
            if(id=="neighbor-talk")return NeighborSlice.Instance.BeginDialogue();
            if(id=="neighbor-desk"){bool access=GameManager.Instance.State.neighborTrusted||GameManager.Instance.State.neighborDefeated;NeighborSlice.Instance.Show(access?"neighbor-order":"desk-locked",access?"neighbor-order":null);return true;}
            NeighborSlice.Instance.Show(id,id=="city-fuse"?id:null);return true;
        }
        void Update()
        {
            if(tenant2D!=null&&!GameManager.Instance.State.neighborDefeated)tenant2D.transform.rotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*17)*(1+PerceptionTier));
            if(Arena||Actor==null||StoryJournal.WorldBlocked||CognitiveRupture.IsActive)return;
            string id=InteractionId();if(id==null)return;
            WorldPromptUI.Offer("slice-room","E "+NeighborSlice.Text(id).title,90);
            if(Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)UseNearest();
        }
        void OnDestroy(){if(GameManager.Instance!=null)GameManager.Instance.Changed-=PresentPerception;foreach(var m in materials)Destroy(m);if(sprite!=null)Destroy(sprite);if(pixel!=null)Destroy(pixel);if(tenantSprite!=null)Destroy(tenantSprite);if(tenantTexture!=null)Destroy(tenantTexture); }
    }
}
