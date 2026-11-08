using UnityEngine;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
namespace DeathBySeventhsDawn.Interaction
{
    public sealed class NeighborCombat : MonoBehaviour
    {
        public NeighborRoom room;public Player.ThirdPersonMotor actor;
        public float Health {get;private set;}
        public bool WindingUp=>windup>0;
        public bool EyeActive=>eyeUntil>Time.unscaledTime;
        GameObject tenant,eye;Transform brain,arm;
        float windup,nextAttack,recovery,nextStrike,eyeUntil,eyeReady;
        float defaultFixed=.02f;AudioSource pulse;
        static ProgressionState S=>GameManager.Instance.State;
        void Start()
        {
            Health=S.neighborDefeated?0:S.neighborHealth>=0?S.neighborHealth:NeighborRoom.Settings.neighborHealth;
            S.neighborHealth=Health;eyeReady=Time.unscaledTime+Mathf.Max(0,(float)System.TimeSpan.FromTicks(System.Math.Max(0,S.eyeReadyUtcTicks-System.DateTime.UtcNow.Ticks)).TotalSeconds);
            tenant=new GameObject("Recognizably human neighbor");tenant.transform.position=new Vector3(1,0,0);
            Part("Clothed torso",new Vector3(0,1,0),new Vector3(.65f,.425f,.4f),new Color(.25f,.28f,.25f),PrimitiveType.Capsule);
            Part("Left leg",new Vector3(-.17f,.35f,0),new Vector3(.2f,.7f,.23f),Color.gray);Part("Right leg",new Vector3(.17f,.35f,0),new Vector3(.2f,.7f,.23f),Color.gray);
            Part("Lower face and jaw",new Vector3(0,1.63f,0),new Vector3(.4f,.33f,.34f),new Color(.62f,.46f,.38f),PrimitiveType.Sphere);
            Part("Remaining skull",new Vector3(.14f,1.8f,.05f),new Vector3(.19f,.23f,.3f),new Color(.67f,.55f,.46f),PrimitiveType.Sphere);
            brain=Part("Exposed upper brain",new Vector3(-.07f,1.82f,0),new Vector3(.28f,.18f,.25f),new Color(.55f,.16f,.21f),PrimitiveType.Sphere).transform;
            for(int i=0;i<4;i++)Part("Brain lobe",new Vector3(-.15f+(i%2)*.12f,1.87f,(i/2)*.1f-.06f),new Vector3(.13f,.08f,.12f),new Color(.69f,.24f,.29f),PrimitiveType.Sphere);
            Part("Eye left",new Vector3(-.1f,1.66f,-.16f),new Vector3(.06f,.05f,.03f),Color.black,PrimitiveType.Sphere);Part("Eye right",new Vector3(.1f,1.66f,-.16f),new Vector3(.06f,.05f,.03f),Color.black,PrimitiveType.Sphere);
            Part("Mouth",new Vector3(0,1.55f,-.17f),new Vector3(.12f,.025f,.025f),new Color(.25f,.08f,.06f));
            arm=Part("Contracting arm",new Vector3(-.45f,1,0),new Vector3(.17f,.35f,.18f),new Color(.6f,.44f,.36f),PrimitiveType.Capsule).transform;
            Part("Human right arm",new Vector3(.45f,1,0),new Vector3(.17f,.35f,.18f),new Color(.6f,.44f,.36f),PrimitiveType.Capsule);
            pulse=gameObject.AddComponent<AudioSource>();pulse.playOnAwake=false;pulse.volume=.25f;
            var mixer=Resources.Load<UnityEngine.Audio.AudioMixer>("ApartmentMix");if(mixer!=null){var groups=mixer.FindMatchingGroups("Master");if(groups.Length>0)pulse.outputAudioMixerGroup=groups[0];}
            if(GameManager.Instance.HasAbility("shoulder_eye"))BuildEye();
        }
        GameObject Part(string name,Vector3 p,Vector3 size,Color tint,PrimitiveType primitive=PrimitiveType.Cube)
        {var go=room.Solid(name,tenant.transform.position+p,size,tint,primitive,false);go.transform.SetParent(tenant.transform,true);return go;}
        void BuildEye()
        {
            eye=room.Solid("Unnatural shoulder eye",actor.transform.TransformPoint(new Vector3(.38f,1.4f,0)),new Vector3(.22f,.17f,.17f),new Color(.77f,.68f,.48f),PrimitiveType.Sphere,false);eye.transform.SetParent(actor.transform,true);eye.layer=8;
            var pupil=room.Solid("Shoulder eye slit",eye.transform.position+Vector3.back*.09f,new Vector3(.035f,.12f,.035f),Color.black,PrimitiveType.Sphere,false);pupil.transform.SetParent(eye.transform,true);pupil.layer=8;
        }
        public bool ActivateEye()
        {
            if(Player.ThirdPersonMotor.Blocked||!GameManager.Instance.HasAbility("shoulder_eye")||Time.unscaledTime<eyeReady||GameManager.Instance.Sanity<=NeighborRoom.Settings.eyeSanityCost)return false;
            if(eye==null)BuildEye();var settings=NeighborRoom.Settings;eyeUntil=Time.unscaledTime+settings.eyeDuration;eyeReady=Time.unscaledTime+settings.eyeCooldown;S.eyeReadyUtcTicks=System.DateTime.UtcNow.AddSeconds(settings.eyeCooldown).Ticks;
            GameManager.Instance.LoseSanity(settings.eyeSanityCost);Time.timeScale=settings.eyeTimeScale;Time.fixedDeltaTime=defaultFixed*Time.timeScale;
            pulse.clip=Resources.Load<AudioClip>("Audio/DreamMudStep");pulse.pitch=.45f;pulse.Play();return true;
        }
        public bool Strike()
        {
            if(Player.ThirdPersonMotor.Blocked||Health<=0||!S.inventory.Contains(NeighborSlice.Crowbar)||Time.unscaledTime<nextStrike||Vector3.Distance(actor.transform.position,tenant.transform.position)>2.2f)return false;
            if(Physics.Linecast(actor.transform.position+Vector3.up,tenant.transform.position+Vector3.up,~(1<<8),QueryTriggerInteraction.Ignore))return false;
            nextStrike=Time.unscaledTime+.7f;
            if(recovery<=0&&!EyeActive)return false;
            Health=Mathf.Max(0,Health-34);S.neighborHealth=Health;ProgressionSave.Write(S);pulse.pitch=1;pulse.PlayOneShot(Resources.Load<AudioClip>("Audio/WoodHammer"),.6f);if(Health<=0)Win();return true;
        }
        public bool UsePreparedTool()
        {
            if(Player.ThirdPersonMotor.Blocked||Health<=0||!S.inventory.Contains(NeighborSlice.Tool)||Vector3.Distance(actor.transform.position,tenant.transform.position)>6)return false;
            S.inventory.Remove(NeighborSlice.Tool);ProgressionSave.Write(S);
            var flash=room.Solid("Contained fictional ember burst",tenant.transform.position+Vector3.up,new Vector3(1.2f,2,1.2f),new Color(.9f,.25f,.05f),PrimitiveType.Capsule,false);Destroy(flash,1.2f);
            pulse.pitch=1.1f;pulse.PlayOneShot(Resources.Load<AudioClip>("Audio/DreamCrack"),.65f);Health=0;S.neighborHealth=0;Win();return true;
        }
        void Win(){RestoreTime();S.neighborDefeated=true;S.neighborHostile=false;ProgressionSave.Write(S);NeighborSlice.Instance.Victory();}
        void RestoreTime(){Time.timeScale=1;Time.fixedDeltaTime=defaultFixed;}
        void Update()
        {
            if(eyeUntil>0&&Time.unscaledTime>=eyeUntil){eyeUntil=0;RestoreTime();}
            if(Health<=0||Player.ThirdPersonMotor.Blocked)return;
            var k=Keyboard.current;if(k!=null){if(k.vKey.wasPressedThisFrame)ActivateEye();if(k.qKey.wasPressedThisFrame)Strike();if(k.gKey.wasPressedThisFrame)UsePreparedTool();}
            if(Health<=0)return;
            float distance=Vector3.Distance(tenant.transform.position,actor.transform.position);
            var toward=actor.transform.position-tenant.transform.position;toward.y=0;if(toward.sqrMagnitude>.01f)tenant.transform.rotation=Quaternion.Slerp(tenant.transform.rotation,Quaternion.LookRotation(-toward),Time.deltaTime*6);
            arm.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*29)*7,0,WindingUp?-75:Mathf.Sin(Time.time*17)*(5+room.PerceptionTier*2));
            brain.localScale=new Vector3(.28f,.18f+Mathf.Sin(Time.time*13)*.014f,.25f);
            if(recovery>0){recovery-=Time.deltaTime;return;}
            if(windup>0)
            {
                windup-=Time.deltaTime;if(windup<=0){if(distance<2){float hit=NeighborRoom.Settings.neighborDamage;if(GameManager.Instance.Vitality<=hit)NeighborSlice.Instance.BeginNeighborDeath();else {GameManager.Instance.Damage(hit);ProgressionSave.Write(S);}}recovery=1.1f;nextAttack=Time.time+.4f;}return;
            }
            if(distance<2.2f&&Time.time>nextAttack)windup=NeighborRoom.Settings.attackWindup;
            else if(distance>1.5f)
            {
                var delta=toward.normalized*1.9f*Time.deltaTime;
                if(!Physics.SphereCast(tenant.transform.position+Vector3.up,.35f,toward.normalized,out _,delta.magnitude+.2f,~(1<<8)))tenant.transform.position+=delta;
            }
        }
        void OnGUI()
        {
            if(CognitiveRupture.IsActive||Health<=0)return;var text=new GUIStyle(GUI.skin.label){font=GameTypography.Current,fontSize=20,wordWrap=true};
            if(GameManager.Instance.HasAbility("shoulder_eye"))GUI.Label(new Rect(Screen.width*.08f,Screen.height-100,Screen.width*.84f,80),string.Format(NeighborSlice.Text(EyeActive?"eye-active":Time.unscaledTime<eyeReady?"eye-cooldown":"eye-ready").body,EyeActive?Mathf.Max(0,eyeUntil-Time.unscaledTime):Time.unscaledTime<eyeReady?Mathf.Max(0,eyeReady-Time.unscaledTime):NeighborRoom.Settings.eyeDuration,NeighborRoom.Settings.eyeCooldown,NeighborRoom.Settings.eyeSanityCost),text);
            GUI.Label(new Rect(Screen.width*.08f,18,Screen.width*.84f,90),NeighborSlice.Text(WindingUp?"attack-warning":"combat-controls").body+"\n"+Mathf.CeilToInt(Health)+" / "+NeighborRoom.Settings.neighborHealth,text);
        }
        void OnDisable(){RestoreTime();}
    }
}
