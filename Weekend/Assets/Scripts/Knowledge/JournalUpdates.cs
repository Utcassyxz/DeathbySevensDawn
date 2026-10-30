// 职责：日记新增与已读通知；避免 UI 重复提示同一条目。
using System;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
namespace DeathBySeventhsDawn.Knowledge
{
    // 已读状态属于永久日记；提示事件只用于当前运行，不影响知识奖励。
    public static class JournalUpdates
    {
        public static event Action<JournalEntry> Added;
        public static int UnreadCount
        {
            get
            {
                var game = GameManager.Instance;
                return game != null && game.State.journal != null ? game.State.journal.FindAll(e => e != null && !e.isRead).Count : 0;
            }
        }
        public static void NotifyAdded(JournalEntry entry)
        {
            if (entry != null)
                Added?.Invoke(entry);
        }
        public static bool MarkRead(string id)
        {
            var game = GameManager.Instance;
            var entry = game != null ? game.State.journal?.Find(e => e != null && e.id == id) : null;
            if (entry == null || entry.isRead)
                return false;
            entry.isRead = true;
            ProgressionSave.Write(game.State);
            return true;
        }
    }
}
