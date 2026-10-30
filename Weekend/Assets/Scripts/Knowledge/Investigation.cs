using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Knowledge
{
    [Serializable] public sealed class EvidenceDefinition
    {
        public string id, title, body, source, image;
    }
    [Serializable] public sealed class DeductionDefinition
    {
        public string id, left, right, relation, title, body;
    }
    [Serializable] public sealed class InvestigationLink
    {
        public string left, right, relation, conclusion;
    }
    [Serializable] public sealed class InvestigationCatalog
    {
        public EvidenceDefinition[] evidence;
        public DeductionDefinition[] deductions;
    }
    // The catalog describes possibilities. Only explicitly collected entries are visible to the player.
    public static class Investigation
    {
        static InvestigationCatalog catalog;
        public static InvestigationCatalog Catalog => catalog ?? (catalog=JsonUtility.FromJson<InvestigationCatalog>(Resources.Load<TextAsset>("Investigation").text));
        public static readonly string[] Relations={"习惯与身份","观察工具","时间关联","行为动机","交叉印证"};
        static ProgressionState State => GameManager.Instance.State;
        public static void Normalize(ProgressionState state)
        {
            if(state.journal==null)state.journal=new List<JournalEntry>();
            if(state.observedKnowledgeIds==null)state.observedKnowledgeIds=new List<string>();
            if(state.investigationLinks==null)state.investigationLinks=new List<InvestigationLink>();
        }
        public static EvidenceDefinition Evidence(string id)=>Array.Find(Catalog.evidence,e=>e.id==id);
        public static bool Has(string id)=>State.journal.Exists(e=>e!=null&&e.id==id);
        public static bool IsNode(string id)=>Evidence(id)!=null||Array.Exists(Catalog.deductions,d=>d.id==id);
        public static List<JournalEntry> Nodes()=>State.journal.Where(e=>e!=null&&IsNode(e.id)).ToList();
        public static bool Collect(string id)
        {
            var definition=Evidence(id);
            if(definition==null||Has(id)||GameManager.Instance.HasEnded)return false;
            var entry=new JournalEntry{id=id,title=definition.title,body=definition.body,imageResource=definition.image,isKnowledge=true,day=TimeManager.Instance.Days,loop=GameManager.Instance.LoopCount};
            // Collecting evidence never adds a second cosmic exposure reward.
            State.journal.Add(entry);
            if(!State.knowledgeIds.Contains(id))State.knowledgeIds.Add(id);
            ProgressionSave.Write(State);JournalUpdates.NotifyAdded(entry);return true;
        }
        public static DeductionDefinition Match(string left,string right,string relation)
        {
            if(left==right||!Has(left)||!Has(right))return null;
            return Array.Find(Catalog.deductions,d=>(d.id!="deduction-warning"||State.neighborFlyersSeen)&&d.relation==relation&&((d.left==left&&d.right==right)||(d.left==right&&d.right==left)));
        }
        public static DeductionDefinition Match(string left,string right)
        {
            if(left==right||!Has(left)||!Has(right))return null;
            return Array.Find(Catalog.deductions,d=>(d.id!="deduction-warning"||State.neighborFlyersSeen)&&((d.left==left&&d.right==right)||(d.left==right&&d.right==left)));
        }
        static bool SamePair(InvestigationLink link,string a,string b)=>(link.left==a&&link.right==b)||(link.left==b&&link.right==a);
        public static InvestigationLink FindLink(string a,string b,string relation)=>State.investigationLinks.Find(l=>l!=null&&l.relation==relation&&SamePair(l,a,b));
        public static InvestigationLink FindLink(string a,string b)
        {
            var links=State.investigationLinks;
            return links.Find(l=>l!=null&&!string.IsNullOrEmpty(l.conclusion)&&SamePair(l,a,b))??links.Find(l=>l!=null&&SamePair(l,a,b));
        }
        public static bool Connect(string left,string right,string relation,bool writeConclusion)
        {
            if(GameManager.Instance.HasEnded||left==right||!IsNode(left)||!IsNode(right)||!Has(left)||!Has(right)||!Relations.Contains(relation))return false;
            var match=Match(left,right,relation);
            if(writeConclusion&&match==null)return false;
            var link=FindLink(left,right,relation);
            if(link!=null&&(!writeConclusion||!string.IsNullOrEmpty(link.conclusion)))return false;
            if(link==null){link=new InvestigationLink{left=left,right=right,relation=relation};State.investigationLinks.Add(link);}
            JournalEntry added=null;
            if(writeConclusion)
            {
                link.conclusion=match.id;
                if(!Has(match.id))
                {
                    added=new JournalEntry{id=match.id,title=match.title,body=match.body+"\n\n依据："+Title(left)+" ＋ "+Title(right)+"\n关系："+relation,isKnowledge=true,day=TimeManager.Instance.Days,loop=GameManager.Instance.LoopCount};
                    State.journal.Add(added);
                }
            }
            ProgressionSave.Write(State);if(added!=null)JournalUpdates.NotifyAdded(added);return true;
        }
        public static bool Connect(string left,string right,bool writeConclusion)
        {
            if(GameManager.Instance.HasEnded||left==right||!IsNode(left)||!IsNode(right)||!Has(left)||!Has(right))return false;
            var match=writeConclusion?Match(left,right):null;
            if(writeConclusion&&match==null)return false;
            var link=FindLink(left,right);
            if(link!=null&&(!writeConclusion||!string.IsNullOrEmpty(link.conclusion)))return false;
            if(link==null)
            {
                link=new InvestigationLink{left=left,right=right,relation=Relations[0]};
                State.investigationLinks.Add(link);
            }
            JournalEntry added=null;
            if(writeConclusion)
            {
                link.relation=match.relation;
                link.conclusion=match.id;
                // Old saves may contain extra tentative lines for this pair; keep only the one being concluded.
                for(int i=State.investigationLinks.Count-1;i>=0;i--)
                {
                    var other=State.investigationLinks[i];
                    if(other!=null&&other!=link&&SamePair(other,left,right)&&string.IsNullOrEmpty(other.conclusion))State.investigationLinks.RemoveAt(i);
                }
                if(!Has(match.id))
                {
                    added=new JournalEntry{id=match.id,title=match.title,body=match.body+"\n\n依据："+Title(left)+" ＋ "+Title(right)+"\n关系："+match.relation,isKnowledge=true,day=TimeManager.Instance.Days,loop=GameManager.Instance.LoopCount};
                    State.journal.Add(added);
                }
            }
            ProgressionSave.Write(State);if(added!=null)JournalUpdates.NotifyAdded(added);return true;
        }
        public static bool RemoveTentative(string left,string right,string relation)
        {
            var link=FindLink(left,right,relation);if(link==null||!string.IsNullOrEmpty(link.conclusion))return false;
            State.investigationLinks.Remove(link);ProgressionSave.Write(State);return true;
        }
        public static bool RemoveTentative(string left,string right)
        {
            bool removed=false;
            for(int i=State.investigationLinks.Count-1;i>=0;i--)
            {
                var link=State.investigationLinks[i];
                if(link!=null&&SamePair(link,left,right)&&string.IsNullOrEmpty(link.conclusion)){State.investigationLinks.RemoveAt(i);removed=true;}
            }
            if(removed)ProgressionSave.Write(State);
            return removed;
        }
        public static string Title(string id)=>State.journal.Find(e=>e!=null&&e.id==id)?.title??"未收录";
        public static IReadOnlyList<InvestigationLink> Links=>State.investigationLinks;
    }
}
