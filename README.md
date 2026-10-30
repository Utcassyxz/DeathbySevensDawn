# 七日 · Death by Seventh's Dawn

Unity 叙事恐怖原型。唯一的游戏工程是 **Weekend/**；根目录旧版重复 Assets 已移除，历史可从 Git 查看。

## 打开项目

1. Unity Hub → 添加磁盘上的项目 → 选择 `Weekend`。
2. 使用 **6000.6.7f1 (b22767f04f04)**，正式版本以 `Weekend/ProjectSettings/ProjectVersion.txt` 为准。
3. 打开 `Weekend/Assets/Scenes/Apartment/LivingRoom.unity`，Play 后从标题开始。保存当前修改再开始新游戏；新游戏会清空本次旅程。
4. VS Code 打开 Weekend；Unity → Edit → Preferences → External Tools 选择 VS Code，Regenerate project files。

最新画面修订见 `docs/VISUAL_DIALOGUE_2026-10-30.md`：长字幕按显示高度分页，天台使用细化像素资源，场景外为相机纯黑背景，没有全屏蒙版。

## 当前玩法与约束

- WASD/方向键移动，E 互动/继续，Esc 关闭或暂停，J 日记，B 线索，I 背包。梦境使用 Enter。
- 第一日：邻居 → 冰箱新闻 → 沙发 → 书桌日记 → 阳台观测 → R 记录 → B 建立待证并确认连线。
- 教程不用箭头。靠近尚未开放的物件，会读到当前应该先做什么；门仍可通行。
- 第二日从 401 获取锡纸三角。完成教程后，在鱼缸旁 E 打开互动，F 或按钮投入。投入会消耗物品并永久封缸；本旅程的第三、五、六日不会再出现生物。开始新游戏才清除。
- **未触发“见到古神”，进入第七日立即末日**：火焰、异变演出，然后保存坏结局；不等待体力耗尽，也不自动轮回。
- 雨果住四楼，走廊西侧楼梯通五楼天台。完成教程后，阳台望远镜旁 F 收起，天台观测垫旁 E 放置，再 E 观察。第二日起傍晚/深夜的真实观测才解锁“见到古神”；白天、阳台教学、旧草图和调试发知识都不算目击。
- 冰箱剧情结束自动关门，并由第三人称旁白交代雨果吃面包。首轮新闻为四张连续的独立配图界面；看完后，各时段电视均只播无聊节目，不会暗中推进时间。完成教程后可在沙发休息，度过一个时段；床则直接睡到次日。
- 已见古神的旅程保留原有七日轮回/衰老分支。达到衰老体力下限，第七日触发原坏结局。

## 找文件

| 目录 | 内容 |
|---|---|
| Weekend/Assets/Scripts | 按职责分类的运行时代码；各目录 README 说明边界 |
| Weekend/Assets/Editor/Validation | 编辑器回归检查 |
| Weekend/Assets/Editor/Maintenance | 场景和资源制作工具 |
| Weekend/Assets/Resources/StoryText*.json | 唯一中英文剧情文案源 |
| Weekend/Assets/Resources/Investigation.json | 线索图结构、ID、关系和图片 |
| Weekend/Assets/Resources/News | 四张新闻插图；README 记录生成提示与用途 |
| Weekend/Assets/Resources/Story | 剧情图片；真实古神观测在天台触发 |
| Weekend/Assets/Scenes/Apartment | 公寓房间与五楼 Rooftop 天台 |
| Weekend/Assets/Art、Audio、Prefabs 等 | 美术、声音和预制体；Resources 下的声音按运行时路径加载 |
| docs | 当前操作、架构与修改记录 |
| docs/archive | 旧阶段交接与开发日志，不能视为当前规格 |
| docs/validation | 历史验证与截图 |
| tools/audio | 原创声音制作工具 |

## 代码与文案约定

每个 C# 文件顶部有职责注释。复杂方法解释状态和边界，不给每句代码重复加注释。移动 Unity 资源时必须同时移动同名 `.meta`；不改 GUID。

对白、叙述和互动提醒写进中英文 StoryText 文件；不复制到 MonoBehaviour。短句、具体动作、人物有自己的立场。黑色幽默来自生活需要与处境的冲突；不给每个物品强塞笑话。创作参考与实例见 `docs/NARRATIVE_DIRECTION.md`。

## 保存与验证

`ProgressionSave.PathName` 指向本机 persistentDataPath 下的 `seven-day-progress-v1.json`，保留旧文件名和 v1/v2 兼容。新增的 `aquariumSealed`、`ancientGodSeen`、`worldEndEnding`、`breadEaten`、`telescopeCarried`、`telescopeOnRoof` 默认 false，不从旧线索推断天台目击。

本次冰箱、新闻、天台与双语变更见 `docs/NEWS_ROOFTOP_2026-10-30.md`。运行 `Prototype/Validate fridge news and fifth floor`、`Prototype/Validate story revision`、`Prototype/Test tutorial state logic without keyboard`、`Prototype/Validate synced seven day progression`。验证自动备份/恢复本机存档。不要把 Library、Temp、Logs、UserSettings 或本地存档提交到 Git。

## 两台电脑同步

开始工作前关闭 Unity，先 `git status`，确认本地修改已保存，再 `git pull --ff-only`。结束时保存场景并关闭 Unity，执行 `git diff` → `git add .` → `git diff --cached` → `git commit -m "说明改动"` → `git push`。确认 push 成功再换电脑。冲突先解决，不使用强推或硬重置掩盖问题。
