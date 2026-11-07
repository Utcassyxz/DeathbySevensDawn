using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Player;

namespace DeathBySeventhsDawn.City
{
    public enum EnemyMode { Patrol, Chase, Search, Stunned }
    [RequireComponent(typeof(CharacterController))]
    public sealed class CityEnemy : MonoBehaviour
    {
        public EnemyMode Mode { get; private set; }
        public ThirdPersonMotor target;
        public Vector3[] patrol;
        CharacterController body;
        int waypoint;
        float lost, stunUntil, attackAt;
        Vector3 lastKnown;
        void Awake(){body=GetComponent<CharacterController>();body.height=1.8f;body.radius=.35f;body.minMoveDistance=0;body.skinWidth=.035f;body.center=Vector3.up*.9f;}
        public bool ClearPath(Vector3 point)
        {
            var from=transform.position+Vector3.up;
            return !Physics.Linecast(from,point,~(1<<8),QueryTriggerInteraction.Ignore);
        }
        public bool CanSeeTarget()
        {
            if(target==null)return false;
            var delta=target.transform.position-transform.position;float range=target.Crouching?5:11;
            return delta.sqrMagnitude<range*range&&Vector3.Angle(transform.forward,delta)<55&&ClearPath(target.transform.position+Vector3.up*.8f);
        }
        public void Stun(float seconds){Mode=EnemyMode.Stunned;stunUntil=Time.time+seconds;}
        void Update()
        {
            if(target==null||ThirdPersonMotor.Blocked)return;
            if(Mode==EnemyMode.Stunned){if(Time.time<stunUntil)return;Mode=EnemyMode.Search;lost=0;}
            float distance=Vector3.Distance(transform.position,target.transform.position);
            bool detected=CanSeeTarget() || (distance<target.Noise&&ClearPath(target.transform.position+Vector3.up));
            if(detected){Mode=EnemyMode.Chase;lastKnown=target.transform.position;lost=0;}
            else if(Mode==EnemyMode.Chase){lost+=Time.deltaTime;if(lost>1.5f){Mode=EnemyMode.Search;lost=0;}}
            var goal=lastKnown;
            if(Mode==EnemyMode.Patrol){if(patrol==null||patrol.Length==0)return;goal=patrol[waypoint];if(Vector3.Distance(transform.position,goal)<.7f)waypoint=(waypoint+1)%patrol.Length;}
            if(Mode==EnemyMode.Search){lost+=Time.deltaTime;if(lost>7){Mode=EnemyMode.Patrol;return;}}
            var direction=goal-transform.position;direction.y=0;
            if(direction.magnitude>.5f)
            {
                direction.Normalize();
                // Steer around solid cover rather than translating through it.
                if(Physics.SphereCast(transform.position+Vector3.up,.32f,direction,out _,1,~(1<<8)))
                {
                    var side=Quaternion.Euler(0,80,0)*direction;
                    if(Physics.SphereCast(transform.position+Vector3.up,.32f,side,out _,1,~(1<<8)))side=-side;
                    direction=side;
                }
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),7*Time.deltaTime);
                body.Move((direction*(Mode==EnemyMode.Chase?4.7f:2.1f)+Vector3.down*5)*Time.deltaTime);
            }
            if(Mode==EnemyMode.Chase&&distance<1.3f&&Time.time>attackAt&&ClearPath(target.transform.position+Vector3.up))
            {attackAt=Time.time+1.5f;GameManager.Instance.Damage(12);}
        }
    }
}
