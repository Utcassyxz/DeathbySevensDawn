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
        public float Stamina { get; private set; } = 100;
        public bool Crouching { get; private set; }
        public float Noise { get; private set; }
        public bool Moving { get; private set; }
        float yaw, pitch = 23, fall, hitAt = -10;
        CharacterController controller;
        void Awake() { controller=GetComponent<CharacterController>(); controller.height=1.8f; controller.center=Vector3.up*.9f; controller.radius=.32f; controller.minMoveDistance=0; controller.skinWidth=.035f; }
        public static bool Blocked => StoryJournal.WorldBlocked || CognitiveRupture.IsActive || City.CityWorld.Reading || GameManager.Instance.HasEnded || Time.timeScale==0;
        void Update()
        {
            if(Blocked) { Noise=0;Moving=false;return; }
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
            if(Moving)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),12*Time.deltaTime);
            fall=controller.isGrounded?-2:Mathf.Max(-35,fall-22*Time.deltaTime);
            controller.Move((direction*speed+Vector3.up*fall)*Time.deltaTime);
            Noise=Moving?(sprint?10:Crouching?1:4):0;
            var mouse=Mouse.current;
            if(mouse!=null&&mouse.rightButton.isPressed){var delta=mouse.delta.ReadValue();yaw+=delta.x*.15f;pitch=Mathf.Clamp(pitch-delta.y*.12f,8,65);}
            if(k.qKey.wasPressedThisFrame) Defend();
        }
        public void SetViewYaw(float value){yaw=value;transform.rotation=Quaternion.Euler(0,value,0);}
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
            var target=transform.position+Vector3.up*1.35f;
            var offset=Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-4.7f);
            float length=offset.magnitude;
            if(Physics.SphereCast(target,.18f,offset.normalized,out var hit,length,~(1<<8),QueryTriggerInteraction.Ignore))length=Mathf.Max(.3f,hit.distance-.15f);
            view.transform.position=target+offset.normalized*length;view.transform.LookAt(target);
        }
    }
}
