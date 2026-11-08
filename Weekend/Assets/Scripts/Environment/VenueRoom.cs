using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Player;

namespace DeathBySeventhsDawn.Interaction
{
    // Enterable venue sets. Final chapter puzzles and the Birdcaller's boss fight remain separate work.
    public sealed class VenueRoom : MonoBehaviour
    {
        public static bool IsVenue(string scene)=>scene=="SushiInterior3D"||scene=="CircusInterior3D";
        public bool Circus=>SceneManager.GetActiveScene().name=="CircusInterior3D";
        public int Tier {get;private set;}=-1;
        public ThirdPersonMotor Actor {get;private set;}
        readonly List<Material> materials=new List<Material>();GameObject perception;AudioSource sound;float entered;
        GameObject Part(string name,Vector3 p,Vector3 size,Color color,PrimitiveType type=PrimitiveType.Cube,bool solid=true,Transform parent=null)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.position=p;go.transform.localScale=size;if(parent!=null)go.transform.SetParent(parent,true);
            var m=new Material(Resources.Load<Material>("VenueSurface")??Resources.Load<Material>("CityGreybox"));m.color=color;materials.Add(m);go.GetComponent<Renderer>().sharedMaterial=m;if(!solid){go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());}return go;
        }
        public static void LightRoom()
        {
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.27f,.29f,.32f);
            var key=new GameObject("Warm oblique key").AddComponent<Light>();key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(48,-25,0);key.color=new Color(1,.80f,.62f);key.intensity=1.3f;key.shadows=LightShadows.Soft;
            var fill=new GameObject("Cold reflected fill").AddComponent<Light>();fill.type=LightType.Directional;fill.transform.rotation=Quaternion.Euler(-20,145,0);fill.color=new Color(.49f,.67f,.79f);fill.intensity=.35f;
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.065f,.075f,.085f);RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.022f;
        }
        void Start()
        {
            entered=Time.unscaledTime;
            LightRoom();
            var wood=new Color(.30f,.22f,.17f);var cream=new Color(.66f,.58f,.42f);var wine=new Color(.28f,.10f,.14f);
            Part("Venue floor",new Vector3(0,-.2f,0),new Vector3(14,.4f,18),wood);
            Part("North boundary",new Vector3(0,2.5f,9),new Vector3(14,5,.3f),Circus?wine:cream);
            Part("West boundary",new Vector3(-7,2.5f,0),new Vector3(.3f,5,18),Circus?wine:cream);Part("East boundary",new Vector3(7,2.5f,0),new Vector3(.3f,5,18),Circus?wine:cream);
            Part("South boundary",new Vector3(0,2.5f,-9),new Vector3(14,5,.3f),wine);
            for(int i=0;i<9;i++)Part("Floor joint",new Vector3(0,.01f,-8+i*2),new Vector3(14,.012f,.035f),new Color(.14f,.13f,.12f),PrimitiveType.Cube,false);
            for(int i=0;i<14;i++)Part("Long floor plank",new Vector3(-6.5f+i,.015f,0),new Vector3(.96f,.025f,18),new Color(.26f+i%3*.018f,.19f+i%2*.012f,.14f),PrimitiveType.Cube,false);
            if(Circus)
            {
                Part("Curtained stage",new Vector3(0,.35f,5.5f),new Vector3(10,.7f,5),new Color(.32f,.22f,.19f));
                for(int i=0;i<12;i++)Part("Curtain pleat",new Vector3(-5.5f+i,2.8f,8.5f),new Vector3(.8f,4.4f,.3f),i%2==0?wine:new Color(.39f,.16f,.18f),PrimitiveType.Cube,false);
                for(int row=0;row<3;row++)for(int side=-1;side<=1;side+=2)
                {Part("Audience bench",new Vector3(side*3,.45f,-3+row*2.1f),new Vector3(3,.35f,.7f),wood);Part("Bench back",new Vector3(side*3,.9f,-3.35f+row*2.1f),new Vector3(3,.7f,.12f),wood);}
                for(int side=-1;side<=1;side+=2)Part("Tent mast",new Vector3(side*5.8f,2.8f,1),new Vector3(.18f,5.6f,.18f),cream);
            }
            else
            {
                Part("Sushi counter",new Vector3(0,.75f,6),new Vector3(11,1.5f,1.5f),wood);Part("Counter top",new Vector3(0,1.55f,6),new Vector3(11.3f,.12f,1.8f),cream);
                for(int row=0;row<3;row++)for(int side=-1;side<=1;side+=2)
                {
                    var p=new Vector3(side*3.8f,.75f,-3+row*2.6f);Part("Dining table",p,new Vector3(2.2f,.2f,1.5f),wood);Part("Table support",p-Vector3.up*.4f,new Vector3(.3f,.7f,.3f),wood);
                    Part("Porcelain plate",p+Vector3.up*.15f,new Vector3(.55f,.035f,.55f),cream,PrimitiveType.Cylinder,false);
                    Part("Paper lantern",p+Vector3.up*2.7f,new Vector3(.6f,.8f,.6f),new Color(.68f,.43f,.27f),PrimitiveType.Capsule,false);
                }
            }
            var actor=new GameObject("Hugo");actor.tag="Player";actor.layer=8;actor.transform.position=new Vector3(0,0,-6);Actor=actor.AddComponent<ThirdPersonMotor>();Actor.SetViewYaw(0);
            var body=Part("Hugo silhouette",actor.transform.position+Vector3.up*.9f,new Vector3(.5f,.8f,.5f),new Color(.32f,.41f,.40f),PrimitiveType.Capsule,false);body.transform.SetParent(actor.transform,true);body.layer=8;
            body.GetComponent<Renderer>().enabled=false;
            var camera=new GameObject("Venue camera");camera.tag="MainCamera";Actor.view=camera.AddComponent<Camera>();Actor.view.nearClipPlane=.1f;Actor.view.fieldOfView=64;camera.AddComponent<AudioListener>();
            Part("Exit door",new Vector3(0,1.1f,-8.6f),new Vector3(1.5f,2.2f,.1f),new Color(.24f,.38f,.34f),PrimitiveType.Cube,false);
            sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.volume=.23f;var mix=Resources.Load<UnityEngine.Audio.AudioMixer>("ApartmentMix");if(mix!=null){var groups=mix.FindMatchingGroups("Master");if(groups.Length>0)sound.outputAudioMixerGroup=groups[0];}
            GameManager.Instance.Changed+=Refresh;Refresh();JourneyCheckpoint.RestoreActor(actor);ProgressionSave.Write(GameManager.Instance.State);
        }
        void Refresh()
        {
            int tier=NeighborRoom.Settings.Tier(GameManager.Instance.Sanity);if(Tier==tier)return;bool newlyTorn=Tier==0&&tier>0;Tier=tier;if(perception!=null)Destroy(perception);perception=new GameObject("Venue perception tier "+tier);
            if(newlyTorn)CognitiveRupture.RevealInPlace();if(tier==0)return;sound.PlayOneShot(Resources.Load<AudioClip>("Audio/DreamCrack"),.4f);
            var pale=new Color(.70f,.65f,.50f);var tissue=new Color(.38f,.10f,.17f);
            if(Circus)
            {
                // Bone cages hold living bird silhouettes. No boss AI or victory state is implied here.
                for(int cage=0;cage<tier*2;cage++)
                {
                    float x=(cage%2==0?-1:1)*(1.1f+cage/2*.95f);Vector3 center=new Vector3(x,1.9f,6);
                    for(int bar=0;bar<8;bar++){float a=bar*Mathf.PI/4;Part("Pale rib cage",center+new Vector3(Mathf.Cos(a)*.43f,0,Mathf.Sin(a)*.43f),new Vector3(.045f,1.2f,.045f),pale,PrimitiveType.Capsule,false,perception.transform);}
                    for(int ring=0;ring<2;ring++)for(int segment=0;segment<12;segment++)
                    {float a=segment*Mathf.PI/6;var bone=Part("Bone cage hoop",center+new Vector3(Mathf.Cos(a)*.43f,ring==0?-.5f:.5f,Mathf.Sin(a)*.43f),new Vector3(.24f,.04f,.04f),pale,PrimitiveType.Cube,false,perception.transform);bone.transform.rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg+90,0);}
                    var rib=Part("Rib growing from keeper",new Vector3(x*.5f,1.7f,6),new Vector3(Mathf.Abs(x),.075f,.08f),pale,PrimitiveType.Cube,false,perception.transform);rib.transform.rotation=Quaternion.Euler(0,0,x>0?10:-10);
                    Part("Bone cage floor",center-Vector3.up*.6f,new Vector3(.5f,.035f,.5f),pale,PrimitiveType.Cylinder,false,perception.transform);
                    var bird=Part("Living caged bird",center,new Vector3(.20f,.14f,.28f),new Color(.13f,.18f,.18f),PrimitiveType.Sphere,false,perception.transform);bird.AddComponent<CagedBirdMotion>();
                    Part("Bird wing",center+Vector3.right*.16f,new Vector3(.23f,.03f,.15f),new Color(.22f,.26f,.24f),PrimitiveType.Cube,false,bird.transform);
                }
                Part("Draped keeper silhouette",new Vector3(0,1.4f,6),new Vector3(.85f,1.2f,.65f),tissue,PrimitiveType.Capsule,false,perception.transform);
                Part("Keeper head",new Vector3(0,2.65f,6),new Vector3(.43f,.55f,.43f),new Color(.53f,.43f,.34f),PrimitiveType.Sphere,false,perception.transform);
            }
            else
            {
                for(int i=0;i<tier*2;i++)
                {
                    Vector3 center=new Vector3(i%2==0?-3.8f:3.8f,.95f,-3+(i/2)*2.6f);
                    Part("Living table mouth",center,new Vector3(1.9f,.17f,1.1f),tissue,PrimitiveType.Sphere,false,perception.transform);
                    for(int tooth=0;tooth<10;tooth++){float a=tooth*Mathf.PI/5;Part("Porcelain-like tooth",center+new Vector3(Mathf.Cos(a)*.75f,.12f,Mathf.Sin(a)*.38f),new Vector3(.09f,.16f,.07f),pale,PrimitiveType.Cube,false,perception.transform);}
                }
            }
            if(tier>=2)for(int i=0;i<tier*4;i++)Part("Exposed venue rib",new Vector3(-6+i*12f/(tier*4),2.7f,8.4f),new Vector3(.12f,3.6f,.3f),Circus?pale:tissue,PrimitiveType.Capsule,false,perception.transform);
        }
        public bool Exit()=>Vector3.Distance(Actor.transform.position,new Vector3(0,0,-8))<2&&CognitiveRupture.ReturnFromSpecial();
        void Update()
        {
            if(Actor==null||ThirdPersonMotor.Blocked)return;
            if(Vector3.Distance(Actor.transform.position,new Vector3(0,0,-8))<2){WorldPromptUI.Offer("venue-exit",NeighborSlice.Text("venue-exit").body,90);if(Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)Exit();}
            if(perception!=null)perception.transform.localScale=new Vector3(1,1+Mathf.Sin(Time.time*1.7f)*Tier*.004f,1);
        }
        void OnGUI()
        {
            if(Actor==null||MainMenuUI.WorldBlocked||CognitiveRupture.IsActive)return;
            if(Time.unscaledTime-entered<8){var line=NeighborSlice.Text(Circus?"circus-venue":"sushi-venue");NarrativeCard.Caption(line.title,line.body);}
        }
        void OnDestroy(){if(GameManager.Instance!=null)GameManager.Instance.Changed-=Refresh;foreach(var m in materials)Destroy(m);}
    }
    public sealed class CagedBirdMotion : MonoBehaviour
    {Vector3 origin;void Start(){origin=transform.localPosition;}void Update(){transform.localPosition=origin+Vector3.up*Mathf.Sin(Time.time*9+origin.x)*.04f;transform.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*3)*12,Mathf.Sin(Time.time*17)*8);if(transform.childCount>0)transform.GetChild(0).localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time*23)*45);}}
}
