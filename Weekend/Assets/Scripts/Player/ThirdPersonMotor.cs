using UnityEngine;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Progression;

namespace DeathBySeventhsDawn.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonMotor : MonoBehaviour
    {
        public Camera view;
        public bool FirstPerson=true;
        public float Stamina { get; private set; } = 100;
        public bool Crouching { get; private set; }
        public float Noise { get; private set; }
        public bool Moving { get; private set; }
        float yaw, pitch, fall, hitAt = -10;
        Transform hands;float handPulse;readonly System.Collections.Generic.List<Material> handMaterials=new System.Collections.Generic.List<Material>();
        float boundUntil,staggerUntil,impactAt=-10; Vector3 recoil;
        public bool Staggered=>Time.unscaledTime<staggerUntil;
        public void ReceiveImpact(Vector3 direction,float strength){direction.y=0;recoil=direction.normalized*strength;staggerUntil=Time.unscaledTime+.35f;impactAt=Time.unscaledTime;}
        public bool MovementLocked=>Time.unscaledTime<boundUntil;
        public float BoundSeconds=>Mathf.Max(0,boundUntil-Time.unscaledTime);
        public void Bind(float seconds){boundUntil=Time.unscaledTime+Mathf.Clamp(seconds,0,5);}
        public void ReleaseBind(){boundUntil=0;}
        CharacterController controller;
        void Awake() { controller=GetComponent<CharacterController>(); controller.height=1.8f; controller.center=Vector3.up*.9f; controller.radius=.32f; controller.minMoveDistance=0; controller.skinWidth=.035f; }
        public static bool Blocked => StoryJournal.WorldBlocked || CognitiveRupture.IsActive || City.CityWorld.Reading || GameManager.Instance.HasEnded || Time.timeScale==0;
        void Update()
        {
            bool focused=Application.isFocused;
            if(FirstPerson){Cursor.lockState=!Blocked&&focused?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=Blocked||!focused;}
            var mouse=Mouse.current;
            if(!Blocked&&mouse!=null&&(FirstPerson||mouse.rightButton.isPressed))
            {var delta=mouse.delta.ReadValue();yaw+=delta.x*.12f;pitch=Mathf.Clamp(pitch-delta.y*.12f,FirstPerson?-75:8,75);}
            if(!Blocked&&recoil.sqrMagnitude>.01f){controller.Move(recoil*Time.deltaTime);recoil=Vector3.MoveTowards(recoil,Vector3.zero,18*Time.deltaTime);}
            if(Blocked||MovementLocked||Staggered) { Noise=0;Moving=false;return; }
            var k=Keyboard.current;if(k==null)return;
            bool crouch=k.leftCtrlKey.isPressed;
            if(crouch!=Crouching)
            { Crouching=crouch; controller.height=Crouching?1.1f:1.8f; controller.center=Vector3.up*controller.height*.5f; }
            var input=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
            input=Vector2.ClampMagnitude(input,1);Moving=input.sqrMagnitude>.01f;
            bool sprint=k.leftShiftKey.isPressed&&!Crouching&&Moving&&Stamina>1;
            Stamina=Mathf.Clamp(Stamina+(sprint?-24:16)*Time.deltaTime,0,100);
            float speed=Crouching?1.65f:sprint?6.2f:3.7f;
            var direction=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);
            if(FirstPerson)transform.rotation=Quaternion.Euler(0,yaw,0);else if(Moving)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),12*Time.deltaTime);
            fall=controller.isGrounded?-2:Mathf.Max(-35,fall-22*Time.deltaTime);
            controller.Move((direction*speed+Vector3.up*fall)*Time.deltaTime);
            Noise=Moving?(sprint?10:Crouching?1:4):0;
            if(mouse!=null&&mouse.leftButton.wasPressedThisFrame) Defend();
        }
        public void SetViewYaw(float value){yaw=value;transform.rotation=Quaternion.Euler(0,value,0);}
        public void PulseHands(){handPulse=Time.unscaledTime;}
        void HandPart(string name,Vector3 position,Vector3 scale,Color color)
        {
            var go=GameObject.CreatePrimitive(name.Contains("shaft")?PrimitiveType.Cylinder:PrimitiveType.Cube);go.name=name;go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(hands,false);go.transform.localPosition=position;go.transform.localScale=scale;var m=new Material(Resources.Load<Material>("VenueSurface"));m.color=color;handMaterials.Add(m);go.GetComponent<Renderer>().sharedMaterial=m;
        }
        void BuildHands()
        {
            hands=new GameObject("Hugo first-person weapon").transform;hands.SetParent(view.transform,false);
            if(GameManager.Instance.State.inventory.Contains(NeighborSlice.Crowbar))
            {HandPart("Crowbar shaft",new Vector3(.30f,-.30f,.72f),new Vector3(.021f,.20f,.021f),new Color(.22f,.20f,.18f));HandPart("Crowbar bent tip",new Vector3(.275f,-.10f,.72f),new Vector3(.065f,.018f,.022f),new Color(.27f,.23f,.20f));HandPart("Crowbar claw",new Vector3(.245f,-.12f,.72f),new Vector3(.018f,.05f,.022f),new Color(.18f,.18f,.17f));}
        }
        public bool Defend()
        {
            if(Blocked||Time.time-hitAt<1||Stamina<25||GameManager.Instance.State.defenseCharges<=0)return false;
            // Consume a charge only when a threat is actually within reach and unobstructed.
            City.CityEnemy nearest=null;float distance=2.3f;
            foreach(var enemy in Object.FindObjectsByType<City.CityEnemy>(FindObjectsSortMode.None))
            {
                float d=Vector3.Distance(transform.position,enemy.transform.position);
                if(d<distance&&Vector3.Dot(transform.forward,(enemy.transform.position-transform.position).normalized)>.2f && enemy.ClearPath(transform.position+Vector3.up)) {nearest=enemy;distance=d;}
            }
            if(nearest==null)return false;
            hitAt=Time.time;Stamina-=25;GameManager.Instance.State.defenseCharges--;nearest.Stun(3);ProgressionSave.Write(GameManager.Instance.State);return true;
        }
        void LateUpdate()
        {
            if(view==null)return;
            if(FirstPerson)
            {
                view.nearClipPlane=.045f;view.transform.position=transform.position+Vector3.up*(Crouching ? .98f : 1.62f);view.transform.rotation=Quaternion.Euler(pitch+Mathf.Max(0,1-(Time.unscaledTime-impactAt)/.35f)*7,yaw,Mathf.Sin((Time.unscaledTime-impactAt)*25)*Mathf.Max(0,1-(Time.unscaledTime-impactAt)/.35f)*3);
                view.fieldOfView=Mathf.Lerp(view.fieldOfView,Noise>=10?80:74,Time.unscaledDeltaTime*8);
                if(hands==null)BuildHands();float phase=Mathf.Clamp01((Time.unscaledTime-handPulse)/.48f);float swing=Mathf.Sin(phase*Mathf.PI);
                hands.localPosition=new Vector3(-swing*.36f,Mathf.Sin(Time.time*14)*.004f*(Moving?1:0),swing*.12f);hands.localRotation=Quaternion.Euler(-swing*45,-swing*35,swing*55);return;
            }
            var target=transform.position+Vector3.up*1.35f;
            var offset=Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-4.7f);
            float length=offset.magnitude;
            if(Physics.SphereCast(target,.18f,offset.normalized,out var hit,length,~(1<<8),QueryTriggerInteraction.Ignore))length=Mathf.Max(.3f,hit.distance-.15f);
            view.transform.position=target+offset.normalized*length;view.transform.LookAt(target);
        }
        void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;ReleaseBind();}
        void OnGUI(){if(!FirstPerson||Blocked)return;var color=GUI.color;GUI.color=new Color(.78f,.76f,.65f,.6f);GUI.DrawTexture(new Rect(Screen.width*.5f-1,Screen.height*.5f-1,2,2),Texture2D.whiteTexture);GUI.color=color;}
        void OnDestroy(){foreach(var m in handMaterials)Destroy(m);}
    }
}
