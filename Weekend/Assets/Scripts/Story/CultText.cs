// 职责：邻居对白和锡纸物品描述的 JSON 字段。
using System;
namespace DeathBySeventhsDawn.Story
{
    [Serializable]
    public sealed class CultText
    {
        public string sound, note, refusal, itemTitle, itemBody; public string[] dayOne, door;
    }
}
