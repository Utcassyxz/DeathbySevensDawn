using UnityEngine;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
namespace DeathBySeventhsDawn.Interaction
{
    public enum NeighborAttack { Punch, Kick, BrainCast }
    public sealed class NeighborCombat : MonoBehaviour
    {
        public NeighborRoom room;public Player.ThirdPersonMotor actor;
        public float Health {get;private set;}
        public bool WindingUp=>windup>0;
        public bool EyeActive=>eyeUntil>Time.unscaledTime;
        GameObject tenant,eye;Transform brain,arm,kickLeg;
        float windup,nextAttack,recovery,nextStrike,eyeUntil,eyeReady,pendingStrike=-1,enemyStagger,meleeImpact;
        float defaultFixed=.02f;AudioSource pulse;
        public NeighborAttack AttackKind {get;private set;}
        public bool BrainInFlight=>projectile!=null&&!brainHit;
        public bool IsSnared=>actor!=null&&actor.MovementLocked;
        GameObject projectile;LineRenderer tether;Vector3 brainOrigin,brainTarget;float flight,nextBrain;bool brainHit;int meleeCount;
        static ProgressionState S=>GameManager.Instance.State;
        void Start()
        {
            if(S.neighborCombatTuning==0&&S.neighborHealth>=0)S.neighborHealth=Mathf.Clamp01(S.neighborHealth/100f)*NeighborRoom.Settings.neighborHealth;
            S.neighborCombatTuning=1;
            Health=S.neighborDefeated?0:S.neighborHealth>=0?S.neighborHealth:NeighborRoom.Settings.neighborHealth;
            S.neighborHealth=Health;eyeReady=Time.unscaledTime+Mathf.Max(0,(float)System.TimeSpan.FromTicks(System.Math.Max(0,S.eyeReadyUtcTicks-System.DateTime.UtcNow.Ticks)).TotalSeconds);
            tenant=new GameObject("Recognizably human neighbor");tenant.transform.position=new Vector3(1,0,0);
            Part("Clothed torso",new Vector3(0,1,0),new Vector3(.57f,.82f,.34f),new Color(.16f,.19f,.17f),PrimitiveType.Cube);
            Part("Left leg",new Vector3(-.17f,.35f,0),new Vector3(.2f,.7f,.23f),Color.gray);kickLeg=Part("Right leg",new Vector3(.17f,.35f,0),new Vector3(.2f,.7f,.23f),Color.gray).transform;
            Part("Lower face and jaw",new Vector3(0,1.63f,0),new Vector3(.31f,.38f,.28f),new Color(.62f,.46f,.38f),PrimitiveType.Sphere);
            Part("Remaining skull",new Vector3(.14f,1.8f,.05f),new Vector3(.19f,.23f,.3f),new Color(.67f,.55f,.46f),PrimitiveType.Sphere);
            brain=Part("Exposed upper brain",new Vector3(-.07f,1.82f,0),new Vector3(.28f,.18f,.25f),new Color(.55f,.16f,.21f),PrimitiveType.Sphere).transform;
            for(int i=0;i<4;i++)Part("Brain lobe",new Vector3(-.15f+(i%2)*.12f,1.87f,(i/2)*.1f-.06f),new Vector3(.13f,.08f,.12f),new Color(.69f,.24f,.29f),PrimitiveType.Sphere);
            for(int fold=0;fold<6;fold++)Part("Brain lobe",new Vector3(-.15f+(fold%3)*.09f,1.89f,(fold/3)*.1f-.05f),new Vector3(.065f,.055f,.09f),new Color(.76f,.36f,.37f),PrimitiveType.Sphere);
            Part("Broken skull edge",new Vector3(-.20f,1.77f,0),new Vector3(.035f,.07f,.29f),new Color(.72f,.62f,.45f));
            Part("Temple blood",new Vector3(-.16f,1.69f,-.155f),new Vector3(.022f,.20f,.018f),new Color(.36f,.06f,.11f));
            Part("Shirt collar left",new Vector3(-.10f,1.39f,-.18f),new Vector3(.13f,.09f,.055f),new Color(.39f,.40f,.32f));
            Part("Shirt collar right",new Vector3(.10f,1.39f,-.18f),new Vector3(.13f,.09f,.055f),new Color(.39f,.40f,.32f));
            for(int button=0;button<3;button++)Part("Coat button",new Vector3(0,1.22f-button*.16f,-.205f),new Vector3(.035f,.035f,.02f),new Color(.58f,.51f,.35f),PrimitiveType.Sphere);
            Part("Eye left",new Vector3(-.1f,1.66f,-.16f),new Vector3(.055f,.018f,.018f),Color.black,PrimitiveType.Sphere);Part("Eye right",new Vector3(.1f,1.66f,-.16f),new Vector3(.055f,.018f,.018f),Color.black,PrimitiveType.Sphere);
            Part("Nose",new Vector3(0,1.63f,-.17f),new Vector3(.07f,.10f,.08f),new Color(.48f,.35f,.28f),PrimitiveType.Capsule);
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
            if(actor.FirstPerson&&actor.view!=null){eye.transform.SetParent(actor.view.transform,false);eye.transform.localPosition=new Vector3(-.35f,-.12f,.64f);eye.transform.localRotation=Quaternion.identity;eye.transform.localScale=new Vector3(.15f,.12f,.12f);pupil.transform.localScale=new Vector3(.17f,.7f,.17f);pupil.transform.localPosition=new Vector3(0,0,-.5f);}
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
            if(Player.ThirdPersonMotor.Blocked||IsSnared||actor.Staggered||Health<=0||!S.inventory.Contains(NeighborSlice.Crowbar)||Time.unscaledTime<nextStrike)return false;
            nextStrike=Time.unscaledTime+.65f;pendingStrike=Time.unscaledTime+.18f;actor.PulseHands();return true;
        }
        void ResolveStrike()
        {
            pendingStrike=-1;
            if(IsSnared||actor.Staggered||Vector3.Distance(actor.transform.position,tenant.transform.position)>2.4f)return;
            var toward=tenant.transform.position-actor.transform.position;toward.y=0;
            if(Vector3.Dot(actor.transform.forward,toward.normalized)<.45f||Physics.Linecast(actor.transform.position+Vector3.up,tenant.transform.position+Vector3.up,~(1<<8),QueryTriggerInteraction.Ignore))return;
            bool exposed=recovery>0||EyeActive;Health=Mathf.Max(0,Health-(exposed?32:12));
            if(exposed){enemyStagger=.45f;windup=0;var push=toward.normalized*.5f;if(!Physics.SphereCast(tenant.transform.position+Vector3.up,.35f,push.normalized,out _,push.magnitude,~(1<<8)))tenant.transform.position+=push;}
            S.neighborHealth=Health;ProgressionSave.Write(S);pulse.PlayOneShot(Resources.Load<AudioClip>("Audio/WoodHammer"),.7f);if(Health<=0)Win();
        }
        public bool UsePreparedTool()
        {
            if(Player.ThirdPersonMotor.Blocked||IsSnared||Health<=0||!S.inventory.Contains(NeighborSlice.Tool)||Vector3.Distance(actor.transform.position,tenant.transform.position)>6)return false;
            actor.PulseHands();S.inventory.Remove(NeighborSlice.Tool);ProgressionSave.Write(S);
            var flash=room.Solid("Contained fictional ember burst",tenant.transform.position+Vector3.up,new Vector3(1.2f,2,1.2f),new Color(.9f,.25f,.05f),PrimitiveType.Capsule,false);Destroy(flash,1.2f);
            pulse.pitch=1.1f;pulse.PlayOneShot(Resources.Load<AudioClip>("Audio/DreamCrack"),.65f);Health=Mathf.Max(0,Health-110);S.neighborHealth=Health;recovery=3;windup=0;ProgressionSave.Write(S);if(Health<=0)Win();return true;
        }
        void Win(){RestoreTime();S.neighborDefeated=true;S.neighborHostile=false;ProgressionSave.Write(S);NeighborSlice.Instance.Victory();}
        void RestoreTime(){Time.timeScale=1;Time.fixedDeltaTime=defaultFixed;actor?.ReleaseBind();if(projectile!=null)Destroy(projectile);}
        public bool BeginAttack(NeighborAttack kind)
        {
            if(Health<=0||windup>0||recovery>0||projectile!=null&&!brainHit||Player.ThirdPersonMotor.Blocked)return false;
            float distance=Vector3.Distance(actor.transform.position,tenant.transform.position);
            if(distance>(kind==NeighborAttack.BrainCast?6:2.6f)||Physics.Linecast(tenant.transform.position+Vector3.up,actor.transform.position+Vector3.up,~(1<<8)))return false;
            AttackKind=kind;windup=kind==NeighborAttack.BrainCast?NeighborRoom.Settings.brainWindup:NeighborRoom.Settings.attackWindup;
            brainTarget=actor.transform.position+Vector3.up*.9f;return true;
        }
        void Hit(float damage)
        {
            actor.ReceiveImpact(actor.transform.position-tenant.transform.position,AttackKind==NeighborAttack.Kick?5:3);
            if(GameManager.Instance.Vitality<=damage)NeighborSlice.Instance.BeginNeighborDeath();
            else {GameManager.Instance.Damage(damage);ProgressionSave.Write(S);}
        }
        void LaunchBrain()
        {
            brainOrigin=tenant.transform.position+Vector3.up*1.82f;flight=0;brainHit=false;nextBrain=Time.time+NeighborRoom.Settings.brainCooldown;
            projectile=room.Solid("Cast brain and neural tether",brainOrigin,new Vector3(.45f,.28f,.4f),new Color(.61f,.20f,.27f),PrimitiveType.Sphere,false);
            for(int i=0;i<6;i++){var lobe=room.Solid("Cast brain fold",brainOrigin+new Vector3((i%3-1)*.11f,.1f,(i/3-.5f)*.17f),new Vector3(.13f,.11f,.16f),new Color(.76f,.34f,.38f),PrimitiveType.Sphere,false);lobe.transform.SetParent(projectile.transform,true);}
            tether=projectile.AddComponent<LineRenderer>();tether.sharedMaterial=brain.GetComponent<Renderer>().sharedMaterial;tether.startWidth=.07f;tether.endWidth=.04f;tether.positionCount=24;tether.useWorldSpace=true;
            for(int i=0;i<tenant.transform.childCount;i++){var part=tenant.transform.GetChild(i);if(part.name=="Exposed upper brain"||part.name=="Brain lobe")part.gameObject.SetActive(false);}
            pulse.PlayOneShot(Resources.Load<AudioClip>("Audio/DreamMudStep"),.5f);
        }
        void UpdateBrain()
        {
            if(projectile==null)return;
            if(!brainHit)
            {
                flight+=Time.deltaTime/NeighborRoom.Settings.brainFlight;
                projectile.transform.position=Vector3.Lerp(brainOrigin,brainTarget,Mathf.Clamp01(flight))+Vector3.up*Mathf.Sin(Mathf.Clamp01(flight)*Mathf.PI)*.65f;
                if(flight>=1)
                {
                    bool clear=!Physics.Linecast(brainOrigin,actor.transform.position+Vector3.up*.9f,~(1<<8));
                    if(clear&&Vector3.Distance(projectile.transform.position,actor.transform.position+Vector3.up*.9f)<1.05f)
                    {brainHit=true;actor.Bind(NeighborRoom.Settings.brainBindDuration);Hit(8);recovery=0;nextAttack=Time.time;}
                    else {RetractBrain();return;}
                }
            }
            else
            {
                if(!actor.MovementLocked){RetractBrain();return;}
                projectile.transform.position=actor.transform.position+Vector3.up;
            }
            for(int i=0;i<24;i++)
            {
                float t=i/23f;Vector3 p=Vector3.Lerp(tenant.transform.position+Vector3.up*1.8f,projectile.transform.position,t);
                if(brainHit&&t>.55f){float angle=(t-.55f)*Mathf.PI*14;p=actor.transform.position+new Vector3(Mathf.Cos(angle)*.45f,.45f+(t-.55f)*2.2f,Mathf.Sin(angle)*.45f);}
                else p+=Vector3.up*Mathf.Sin(t*Mathf.PI*3+Time.time*5)*.08f;
                tether.SetPosition(i,p);
            }
        }
        void RetractBrain()
        {
            if(projectile!=null)Destroy(projectile);projectile=null;actor.ReleaseBind();recovery=1.1f;
            for(int i=0;i<tenant.transform.childCount;i++){var part=tenant.transform.GetChild(i);if(part.name=="Exposed upper brain"||part.name=="Brain lobe")part.gameObject.SetActive(true);}
        }
        void Update()
        {
            if(eyeUntil>0&&Time.unscaledTime>=eyeUntil){eyeUntil=0;RestoreTime();}
            if(Health<=0||Player.ThirdPersonMotor.Blocked)return;
            var k=Keyboard.current;if(k!=null){if(k.vKey.wasPressedThisFrame)ActivateEye();if(k.gKey.wasPressedThisFrame)UsePreparedTool();}
            if(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)Strike();
            if(pendingStrike>=0&&Time.unscaledTime>=pendingStrike)ResolveStrike();
            if(Health<=0)return;
            if(projectile!=null){UpdateBrain();if(!brainHit&&projectile!=null)return;}
            if(enemyStagger>0){enemyStagger-=Time.deltaTime;return;}
            float distance=Vector3.Distance(tenant.transform.position,actor.transform.position);
            var toward=actor.transform.position-tenant.transform.position;toward.y=0;if(toward.sqrMagnitude>.01f)tenant.transform.rotation=Quaternion.Slerp(tenant.transform.rotation,Quaternion.LookRotation(-toward),Time.deltaTime*6);
            meleeImpact=Mathf.Max(0,meleeImpact-Time.deltaTime);
            arm.localPosition=new Vector3(-.45f,1,WindingUp&&AttackKind==NeighborAttack.Punch?.18f:meleeImpact>0?-.7f:0);
            arm.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*29)*7,0,WindingUp?-75:Mathf.Sin(Time.time*17)*(5+room.PerceptionTier*2));
            kickLeg.localRotation=Quaternion.Euler(WindingUp&&AttackKind==NeighborAttack.Kick?-65:recovery>.7f&&AttackKind==NeighborAttack.Kick?65:0,0,0);
            brain.localScale=new Vector3(.28f,.18f+Mathf.Sin(Time.time*13)*.014f,.25f);
            if(recovery>0){recovery-=Time.deltaTime;return;}
            if(windup>0)
            {
                windup-=Time.deltaTime;if(windup<=0)
                {
                    if(AttackKind==NeighborAttack.BrainCast)LaunchBrain();
                    else {meleeImpact=.28f;if(distance<(AttackKind==NeighborAttack.Kick?2.5f:2))Hit(NeighborRoom.Settings.neighborDamage);meleeCount++;}
                    recovery=1.1f;nextAttack=Time.time+.4f;
                }return;
            }
            if(projectile==null&&Time.time>nextAttack&&Time.time>nextBrain&&(distance>2.6f||meleeCount>1)&&distance<6&&BeginAttack(NeighborAttack.BrainCast))return;
            if(distance<2.2f&&Time.time>nextAttack&&BeginAttack(meleeCount%2==0?NeighborAttack.Punch:NeighborAttack.Kick))return;
            if(distance>1.5f)
            {
                float best=float.PositiveInfinity;Vector3 step=Vector3.zero;
                foreach(float angle in new[]{0f,45f,-45f,90f,-90f})
                {
                    var direction=Quaternion.Euler(0,angle,0)*toward.normalized;var delta=direction*(Health<NeighborRoom.Settings.neighborHealth*.5f?3.2f:2.6f)*Time.deltaTime;
                    if(Physics.SphereCast(tenant.transform.position+Vector3.up,.35f,direction,out _,delta.magnitude+.2f,~(1<<8)))continue;
                    float score=(tenant.transform.position+delta-actor.transform.position).sqrMagnitude;
                    if(score<best){best=score;step=delta;}
                }
                tenant.transform.position+=step;
            }
        }
        void OnGUI()
        {
            if(CognitiveRupture.IsActive||Health<=0)return;
            var matrix=GUI.matrix;var color=GUI.color;float scale=Mathf.Min(Screen.width/1200f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            try
            {
                var text=new GUIStyle(GUI.skin.label){font=GameTypography.Current,fontSize=20,wordWrap=true};text.normal.textColor=new Color(.87f,.84f,.74f);
                if(GameManager.Instance.HasAbility("shoulder_eye"))GUI.Label(new Rect(w*.08f,h-100,w*.84f,80),string.Format(NeighborSlice.Text(EyeActive?"eye-active":Time.unscaledTime<eyeReady?"eye-cooldown":"eye-ready").body,EyeActive?Mathf.Max(0,eyeUntil-Time.unscaledTime):Time.unscaledTime<eyeReady?Mathf.Max(0,eyeReady-Time.unscaledTime):NeighborRoom.Settings.eyeDuration,NeighborRoom.Settings.eyeCooldown,NeighborRoom.Settings.eyeSanityCost),text);
                string cue=IsSnared?"brain-bound":WindingUp?(AttackKind==NeighborAttack.BrainCast?"brain-warning":AttackKind==NeighborAttack.Kick?"kick-warning":"attack-warning"):"combat-controls";
                GUI.color=new Color(.025f,.04f,.045f,.76f);GUI.DrawTexture(new Rect(w*.075f,14,w*.67f,100),Texture2D.whiteTexture);GUI.color=color;
                GUI.Label(new Rect(w*.08f,20,w*.66f,90),string.Format(NeighborSlice.Text(cue=="combat-controls"&&!S.inventory.Contains(NeighborSlice.Tool)?"combat-basic":cue).body,actor.BoundSeconds)+"\n"+Mathf.CeilToInt(Health)+" / "+NeighborRoom.Settings.neighborHealth,text);
            }
            finally{GUI.matrix=matrix;GUI.color=color;}
        }
        void OnDisable(){RestoreTime();}
    }
}
