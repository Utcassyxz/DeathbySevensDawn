# 七日轮回系统：实现与测试

已直接修改项目：`C:\Users\hongy\source\DeathbySevensDawn\Weekend`。

## 修改文件

以下路径均相对于 Unity 项目目录。

| 文件 | 用途 |
|---|---|
| `Assets/Scripts/Core/GameManager.cs` | 永久属性、知识等级、体力、衰老、理智计算与成长事件 |
| `Assets/Scripts/Time/TimeManager.cs` | 保留原四时段 API，限制 Day 1–7，第七夜结束发出事件 |
| `Assets/Scripts/Time/LoopManager.cs` | 接收第七夜事件，重置时间、衰老、保存并返回 LivingRoom |
| `Assets/Scripts/Knowledge/KnowledgeData.cs` | 保留原知识字段，增加知识类别及单项效果覆盖 |
| `Assets/Scripts/Knowledge/KnowledgeManager.cs` | 保留原字符串解锁 API，增加分类/资源解锁、去重和永久保存 |
| `Assets/Scripts/Progression/ProgressionSettings.cs` | 可配置参数和能力阈值的数据定义 |
| `Assets/Scripts/Progression/ProgressionState.cs` | 带版本号的永久进度数据 |
| `Assets/Scripts/Progression/ProgressionSave.cs` | JSON 保存、临时文件替换、备份与读取校验 |
| `Assets/Scripts/Progression/ProgressionBootstrap.cs` | 进入 Play 自动创建常驻管理器及 UI，无须改场景 |
| `Assets/Scripts/Progression/ProgressionHUD.cs` | 状态显示、可展开测试按钮、理智色调分层 |
| `Assets/Resources/SevenDayProgression.asset` | 已创建的默认参数，Inspector 可直接调整 |
| `Assets/Editor/VerifySevenDayProgression.cs` | 自动验证入口及创建参数资源的菜单 |

新增文件及文件夹配有 `.meta`。未修改已有场景、移动脚本、房门脚本、窗户交互或构建场景列表。

## 在 Unity 中测试

1. 用 Unity 6000.6.7f1 打开 `Weekend`，等待导入和编译。
2. 打开 `Assets/Scenes/Apartment/LivingRoom.unity`，点击 Play。左上角自动显示全部六类状态及知识等级。
3. 展开 `Prototype test controls`。每次 `Advance time` 推进一个时段，四个时段为一天。从第 1 天早晨累计推进 27 次抵达第 7 天夜晚，第 28 次自动轮回：Day 回到 1，Loop Count 加 1，最大及当前体力从 100 变成 95，并加载 LivingRoom。
4. 点击 `Discover Ordinary / Forbidden / Cosmic`。默认分别获得知识 5 / 15 / 30，变异 0 / 8 / 20，额外理智负担 0 / 4 / 12。测试按钮每次生成不同知识 ID，因此可重复点击；正式内容相同 ID 只能奖励一次。
5. 变异达到 10、25、40、60、80 时解锁并显示对应能力标记。理智降至 65、35、10 时切换 Unstable、Fractured、Revelation，并逐渐增加轻微紫色色调。
6. 点击 `Damage 25`；体力归零时自动轮回。也可点击 `Die / restart loop` 直接测试。
7. 轮回后确认知识、变异、知识 ID 及理智负担保持；停止 Play 后再进入，确认这些永久数据与轮回次数恢复。
8. 在公寓各房间测试 WASD、房门和卧室窗户。场景切换应保持同一天及全部永久属性。

时间目前由行动调用 `AdvanceTime()` 推进，测试按钮提供最小入口；尚未增加现实时间倒计时。

## 默认规则与调参

选择 `Assets/Resources/SevenDayProgression.asset` 调整参数。

- 最大体力 = max(最低体力 10，初始体力 100 − 轮回次数 × 每轮衰老 5)。轮回恢复到新的体力上限。衰老目前由上限降低体现，未添加角色外观老化素材。
- 知识等级 = floor(累计知识 / 每级知识 20)。知识作为永久成长值，不会因升级被消费。
- 理智 = clamp(100 − 累计额外理智负担 − 知识 × 0.1 − 变异 × 0.35，0，100)。轮回不会洗掉理智代价。
- 普通知识默认无直接变异或额外理智代价，但累计知识仍通过全局系数降低理智。可修改三类效果和全局系数。
- 能力名称与变异阈值可配置。NPC、人类路线和知识门槛接口可供现有或后续玩法调用。
- 若参数资源丢失，运行时回退到默认参数；可用菜单 `Prototype > Create progression settings` 重新创建。

## 内容与玩法接入

```csharp
// 正式知识：创建 Knowledge Data 资源，设置唯一 id、类别或单项效果。
KnowledgeManager.Instance.Unlock(knowledgeData);

// 保留了原调用方式，也支持类别。
KnowledgeManager.Instance.Unlock("basement-seal", KnowledgeTier.Forbidden);
bool knowsSeal = KnowledgeManager.Instance.Has("basement-seal");

// 知识是主要升级门槛；能力与路线状态可直接查询。
bool canReadSeal = GameManager.Instance.MeetsKnowledgeRequirement(40);
bool nightVisionUnlocked = GameManager.Instance.HasAbility("NightVision");
bool humanRouteOpen = GameManager.Instance.CanUseHumanRoute(60);

// 战斗/行动系统可调用这些入口。
GameManager.Instance.Damage(20);
GameManager.Instance.Heal(10);
TimeManager.Instance.AdvanceTime();
```

事件：`Changed`、`AbilityUnlocked(string)`、`MutationChanged(float)`、`SanityLayerChanged(SanityLayer)`、`Died`、`LoopRestarted`。订阅方应在禁用时取消订阅，进场时先查询当前状态；存档恢复时不重放历史解锁事件。

夜视、再生等目前实现为能力解锁状态和事件，尚未制作各能力具体战斗/渲染效果。NPC 反应及路线接口、理智剧情接口已预留，尚未创作 NPC 剧情内容。零生命死亡触发 `Died`，第七夜灾难通过 `SeventhDayEnded` 触发轮回。

## 持久化

原项目未发现存档系统，新增 `Application.persistentDataPath/seven-day-progress-v1.json`。按项目当前公司名和产品名，通常在 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Weekend`。保存知识 ID、知识、变异、理智负担和轮回次数，每次获得知识或轮回立即写入；保留上一版 `.bak`。

当前日期、时段、当前体力不写入永久档；重新启动从 Day 1、当前年龄对应的满体力开始，不额外增加轮回次数。当前版本未做完整场景任务存档。需要从零测试时，退出 Play 后备份并移走该 JSON 文件。

## 验证结果与限制

已使用本机 Unity 配套 C# 编译器及 Unity/UI/Input System 程序集，对项目所有运行时脚本（启用新输入系统）及新增编辑器验证脚本进行编译，均无编译错误。

尝试直接启动原项目及隔离副本进行 Unity 批处理验证，但本机许可证连接及 Burst 子进程通信出现问题，未完成 Play Mode 自动验证。因此不能声称实际运行与 UI 视觉已验证通过，请按上述步骤验收。

自动验证入口为 `VerifySevenDayProgression.Run`（供 Unity `-executeMethod` 使用），覆盖七日边界、死亡衰老、知识去重、阈值事件、门槛查询和磁盘保存；它会临时隔离并恢复原存档，完成后退出该 Unity 批处理进程，不应在日常编辑器里手动调用。
