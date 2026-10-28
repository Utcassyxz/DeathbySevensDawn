# 日记与时间反馈 · 2026-10-28

先同步 main 再在另一台电脑编辑。Unity 工程仍在 Weekend/。

- J 打开日记，优先显示最新未读条目；显示的条目会记录已读，其余条目保持未读。
- 右上提示未读数量，条目列表显示未读标记。新条目出现短暂的“日记已更新”提示。
- 旧存档无需手动迁移：缺少 isRead 的旧条目按未读显示；逐条查阅后保存已读状态。
- 睡觉、看电视等引发的时间变化显示“第 N 天 · 时段”。只读取 Days；不会因提示再次推进时间。
- 提示不阻止走路。读图、日记、梦境、家具和调试界面期间隐藏并保留剩余显示时长，退出后继续显示。
- 两个提示通道各保留最新状态，睡眠不会排队显示跳过的每个时段。

实现入口：Scripts/Progression/GameplayFeedback.cs、Scripts/Knowledge/JournalUpdates.cs；由现有 ProgressionBootstrap 自动安装，场景布局不改动。

验收菜单：Prototype > Validate synced seven day progression；Prototype > Feedback interaction acceptance。测试暂用独立进度并在结束后恢复原主档、备份档与临时档。测试截图和个人存档不随工程上传。
