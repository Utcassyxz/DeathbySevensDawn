# 卧室窗户与 Days

当前天数的唯一来源：`TimeManager.Instance.Days`，从 1 开始，跨房间保留。

现有 `AdvanceTime()` 推进时段，夜晚结束后 Days 加一；`ResetToDayOne()` 把 Days 和时段恢复为第一天早晨。窗户只读取该值，不负责推进它。未来事件、NPC 与环境变化应读取同一个 Days，不另存当前天数。

卧室靠近北侧窗户，按 E 查看，按 E / Esc 返回。查看时停止角色移动。出生点不自动处于互动范围。

资源：
- `Weekend/Assets/ScriptableObjects/WindowViews/BedroomWindowViews.asset`
- `Weekend/Assets/Art/WindowViews/Day01.png` 至 `Day07.png`

序列第 0 格对应 Days 1，第 6 格对应 Days 7。超过显示范围时夹到首尾图片；这只保护图片显示，不修改全局 Days。占位图可单独替换，生成器保留已有 PNG。

Unity 菜单 `Prototype > Test bedroom window and Days` 验证打开、七日映射、关闭与控制恢复；`Verify current layout` 验证移动与八条房门连接。测试会暂时推进天数，结束后退出 Play，不修改场景中保存的初始 Days。
