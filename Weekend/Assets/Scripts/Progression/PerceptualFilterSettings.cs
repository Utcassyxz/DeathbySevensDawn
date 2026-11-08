using UnityEngine;
namespace DeathBySeventhsDawn.Interaction
{
    [CreateAssetMenu(menuName="Death by Seventh's Dawn/Perceptual filter")]
    public sealed class PerceptualFilterSettings : ScriptableObject
    {
        public float normalThreshold=70,anomalyThreshold=40,collapseThreshold=20;
        public float eyeDuration=3,eyeCooldown=12,eyeSanityCost=8,eyeTimeScale=.3f;
        public float neighborHealth=100,neighborDamage=18,attackWindup=.85f;
        void OnValidate()
        {
            normalThreshold=Mathf.Clamp(normalThreshold,3,100);anomalyThreshold=Mathf.Clamp(anomalyThreshold,2,normalThreshold-1);collapseThreshold=Mathf.Clamp(collapseThreshold,1,anomalyThreshold-1);
            eyeDuration=Mathf.Clamp(eyeDuration,.2f,10);eyeCooldown=Mathf.Max(eyeCooldown,eyeDuration+.5f);eyeSanityCost=Mathf.Clamp(eyeSanityCost,1,30);eyeTimeScale=Mathf.Clamp(eyeTimeScale,.1f,.9f);
            neighborHealth=Mathf.Max(1,neighborHealth);neighborDamage=Mathf.Max(1,neighborDamage);attackWindup=Mathf.Clamp(attackWindup,.1f,3);
        }
        public int Tier(float sanity)=>sanity>=normalThreshold?0:sanity>=anomalyThreshold?1:sanity>=collapseThreshold?2:3;
    }
}
