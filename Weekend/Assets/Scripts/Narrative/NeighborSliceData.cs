using System;
using UnityEngine;
namespace DeathBySeventhsDawn.Interaction
{
    [Serializable] public sealed class SliceLine { public string id,title,body; }
    [Serializable] public sealed class NeighborSliceText
    {
        public SliceLine[] entries;
        public SliceLine Find(string id)=>Array.Find(entries,e=>e.id==id);
    }
    [Serializable] public sealed class SliceChoice { public string line,to,condition; }
    [Serializable] public sealed class SliceNode { public string id,line,evidence,effect; public SliceChoice[] choices; }
    [Serializable] public sealed class NeighborDialogue { public SliceNode[] nodes; }
}
