# 公寓验证记录 — 2026-10-27

工程：Weekend，Unity 6000.6.7f1。

## 画面检查

客厅、卧室、浴室、阳台和走廊均在 Unity Game 视图中检查。五个镜头尺寸统一为 5；南侧推拉门保持低门框与轨道剖面。最终走廊门框和壁灯位置在重新打开编辑器后再次确认。

## 步行通路

以 0.1 单位网格和 0.66 × 0.66 的玩家净空，使用实际 Physics2D 碰撞查询，从各场景出生点搜索到每个门触发区。不是仅检查目标场景文件存在。

```text
LivingRoom -> Bedroom: reachable with player clearance
LivingRoom -> Bathroom: reachable with player clearance
LivingRoom -> Corridor: reachable with player clearance
LivingRoom -> Balcony: reachable with player clearance
LivingRoom: camera size=5, reachable cells=5072
Bedroom -> LivingRoom: reachable with player clearance
Bedroom: camera size=5, reachable cells=1617
Bathroom -> LivingRoom: reachable with player clearance
Bathroom: camera size=5, reachable cells=989
Balcony -> LivingRoom: reachable with player clearance
Balcony: camera size=5, reachable cells=2664
Corridor -> LivingRoom: reachable with player clearance
Corridor: camera size=5, reachable cells=3615
PASS: 8 walking routes. No decorative finishing adds colliders.
```

## 运行验证

通过 Prototype → Verify current layout 在正常游戏窗口中执行；持续输入测试使用 Input System 键盘状态，门切换测试把玩家放入各门的物理触发区。

```text
PASS: player moved 3.159412 units under held D input. 8 physical door triggers switched scenes and placed player at expected arrivals.
```

## 已知环境限制

后台批处理没有正常读到持续键盘状态，该模式不作为移动验证通过证据；最终移动与门切换结果来自正常编辑器窗口。Unity 的资源导入后台进程在一次启动中报告过内部崩溃，场景导入、画面检查和上述正常窗口回归仍完成；未修改 Windows 安全策略。本记录不把工具层故障归类为游戏逻辑通过或失败，也不宣称已测试所有后续剧情与玩法。
