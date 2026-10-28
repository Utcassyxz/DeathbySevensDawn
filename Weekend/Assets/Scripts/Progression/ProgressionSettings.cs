using System;
using UnityEngine;
namespace DeathBySeventhsDawn.Progression
{
    public enum KnowledgeTier { Ordinary, Forbidden, Cosmic }
    [Serializable] public class KnowledgeEffect
    {
        public float knowledge, mutation, sanityCost;
        public KnowledgeEffect(float k, float m, float s) { knowledge=k; mutation=m; sanityCost=s; }
    }
    [Serializable] public class AbilityThreshold
    {
        public string id; public float mutation;
        public AbilityThreshold(string name, float value) { id=name; mutation=value; }
    }
    [CreateAssetMenu(menuName="Death by Seventh's Dawn/Progression Settings")]
    public class ProgressionSettings : ScriptableObject
    {
        public float initialVitality=100, agingPerLoop=5, minimumVitality=10;
        public float maximumSanity=100, mutationSanityCost=.35f, knowledgeSanityCost=.1f;
        public float knowledgePerLevel=20, unstableSanity=65, fracturedSanity=35, revelationSanity=10;
        public KnowledgeEffect ordinary=new KnowledgeEffect(5,0,0);
        public KnowledgeEffect forbidden=new KnowledgeEffect(15,8,4);
        public KnowledgeEffect cosmic=new KnowledgeEffect(30,20,12);
        public AbilityThreshold[] abilities={new AbilityThreshold("NightVision",10),new AbilityThreshold("AberrantHearing",25),new AbilityThreshold("Regeneration",40),new AbilityThreshold("SpatialPerception",60),new AbilityThreshold("NonHumanForm",80)};
        public KnowledgeEffect Effect(KnowledgeTier tier) { return tier==KnowledgeTier.Cosmic?cosmic:tier==KnowledgeTier.Forbidden?forbidden:ordinary; }
    }
}