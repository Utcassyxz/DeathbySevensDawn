using System;
using DeathBySeventhsDawn.Progression;
namespace DeathBySeventhsDawn.Knowledge
{
    // 日记正文直接保存到永久档，轮回不会丢失；旧档缺少此列表时自动补齐。
    [Serializable] public class JournalEntry
    {
        public string id, title, body, imageResource;
        public KnowledgeTier tier;
        public int day, loop;
        public bool isKnowledge=true;
        public bool IsDream => !isKnowledge && id=="opening-dream";
        public bool isRead; // 旧存档缺省为未读，打开对应条目后保存。
    }
}