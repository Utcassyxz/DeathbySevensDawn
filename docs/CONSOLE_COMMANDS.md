# 《七日》控制台指令手册

适用：0.3.2。由实际命令注册表、线索目录及中英文文案生成。

## 打开与输入

进入游戏后按键盘左上角、数字 1 左边的 波浪号（~） 键直接打开，无需输入旧口令。Enter 执行，Tab 循环补全，↑/↓ 查历史并保留未完成输入，Esc 或 ~ 收起。窗口可滚动；最多保留 200 行输出、100 条输入。指令和 ID 使用英文；动词、场景、物品及事件 ID 不区分大小写，推理节点 ID 请按表原样输入。每次输入一条，不支持分号串联。

首次打开会复制当前旅程到临时调试会话，并开启自由调试。**收起窗口仍停留在临时旅程**；输入 `session end` 才返回进入前的场景、位置、日期、钟点、物品与剧情状态。调试旅程和收藏使用独立存储，不覆盖正常旅程。关闭整个程序后重新启动走正常存档；不要用重启代替现场恢复。

`debug on` 会跳过强制引导并阻止部分致死危机，适合跳场景；`debug off` 恢复正式剧情/时钟规则。收起窗口后时钟才可继续；需要测试真实危机时先关闭自由调试。`scene` 传送、`event` 预设、`flags reset`、`sanity` 需要自由调试开启。`session end` 在两种模式下都有效。

## 全部指令

| 指令 | 作用 |
|---|---|
| `help [command]` | 显示全部指令，或查看某一条的用法。 |
| `clear` | 清空控制台输出。 |
| `history` | 列出本次运行的输入历史。 |
| `status` | 查看场景、时间、体力、理智、知识和能力。 |
| `close` | 关闭窗口，保留临时调试会话。 |
| `session begin|end` | begin 查看隔离会话；end 恢复进入调试前的旅程、位置和时间。 |
| `debug on|off` | on 开启自由调试并跳过强制引导；off 恢复剧情和时钟规则。 |
| `day <1..9999>` | 设置日期，保留当前时段和钟点；不自动补完剧情。 |
| `time <morning|afternoon|evening|night>` | 切换到指定时段的起始时间；也接受 0、1、2、3。 |
| `date <day> <HH:mm>` | 设置日期和钟点；07:00–25:59，00/01 点视为本日深夜。 |
| `next period|day` | period 按规则推进时段；day 直接进入次日 07:00。 |
| `sleep` | 关闭窗口，按现有规则请求睡眠，不替代梦境结算。 |
| `heal [amount]` | 恢复体力；省略数值则回满。不能复活已结束的旅程。 |
| `damage <amount>` | 扣除指定体力，归零会触发真实死亡流程。 |
| `kill` | 将体力扣至零，测试死亡与已获得的回档能力。 |
| `sanity <0..100>` | 设置理智覆盖值，需要 debug on；debug off 暂停覆盖。 |
| `knowledge <ordinary|forbidden|cosmic>` | 获得一次普通、禁忌或宇宙知识奖励，可重复。 |
| `mutation unlock shoulder_eye` | 解锁肩眼。 |
| `scene list|<scene>` | list 列场景；输入场景名传送，需要 debug on。 |
| `item list|give <id>` | list 列物品；give 添加真实物品，不生成任意文本条目。 |
| `evidence list|unlock <id>` | list 列原始线索；unlock 收集指定线索，不直接授予推论。 |
| `deduce <left-id> <right-id>` | 连接两个已有节点，仍检查日期、前提和传单条件。 |
| `flags reset` | 重置邻居测试标记，需要 debug on；不会清空全游戏。 |
| `crisis <building|city|cosmic> <on|off>` | on 保留对应危机，off 标记已解决。正式阶段以 building 的第七日爆破为准。 |
| `event list|<id>` | list 列事件；输入 ID 进入测试预设，需要 debug on，通常关闭窗口。 |
| `replay news` | 重播第一天广播。 |
| `ending <1..3>` | 预览 1–3 号旧结局，不解锁正式收藏。 |
| `skip` | 跳过当前强制引导。 |
| `board` | 打开现有线索开发工具。 |
| `reset confirm` | 必须输入 reset confirm；只重新开始临时旅程，session end 仍可恢复原旅程。 |

## 常用示例

```text
help
help event
status
date 2 23:10
scene Corridor
item give crowbar
evidence unlock prep-oil
sanity 25
event neighbor_eye
debug off
session end
```

以上为独立示例，不是一套连续通关流程。`day` / `date` 直接设置日期，不补齐前几天的剧情；`next period` 使用既有时段结算，`next day` 则直接跳到次日早晨。`sleep` 仍走实际梦境规则。体力归零会测试真实死亡/回档，回档恩赐仍须按剧情获得。已结束旅程不能用 heal 复活；请恢复原旅程或重开临时旅程。

`crisis building off` 将第七日爆破标记为已解除；`on` 撤销该标记。city / cosmic 保留旧系统标记，**不会给当前第一阶段凭空增加旧版第五/第七日危机**。

兼容旧写法：`scene goto Bedroom`、`day set 2`、`time set 0`、`sanity set 100`、`event trigger neighbor_eye`。数值以小数点分隔，禁止 NaN、无穷大和超范围值。

## 场景 ID

```text
SampleScene
Bedroom
LivingRoom
Bathroom
Balcony
Corridor
Stairwell
Rooftop
CityStreet
CityService
CityBroadcast
CityObservatory
CityEncounter3D
NeighborRoom2D
NeighborArena3D
BuildingService2D
SushiInterior3D
CircusInterior3D
```

这些包含开发用场景。直接传送 3D 场景属于测试，不代表普通探索改为 3D；正式探索仍然是 2D。邻居遭遇及店铺会设置正确的返回场景。

## 物品 ID

| ID | 物品 |
|---|---|
| `crowbar` | 撬棍 |
| `cooking-oil` | 食用油 |
| `sealed-maintenance-medium` | 封存维修芯 |
| `sealed-ember-tool` | 单次准备工具 |
| `foil-triangle` | 锡纸三角 |
| `invitation` | 邀请函 |

`item give` 直接加入临时背包，不重复生成已有物品，也不替代打开壁橱、拾取演出或工具合成的正式流程。

## 事件 ID

| ID | 实际预设 |
|---|---|
| `sushi_normal` | 寿司店，理智 100 的正常外观。 |
| `sushi_corrupt` | 寿司店，理智 15 的污染外观。 |
| `circus_normal` | 马戏团，理智 100 的正常外观。 |
| `circus_corrupt` | 马戏团，理智 15 的污染外观。 |
| `neighbor_encounter` | 第二天邻居家 2D 敌对入口；靠近住户按 E 开始遭遇。 |
| `neighbor_peace` | 第二天邻居家和平沟通入口。 |
| `neighbor_known` | 授予分配令及新闻证据，进入已知命令的对话分支。 |
| `neighbor_prepared` | 授予准备推论与单次工具，进入邻居 3D 战斗；G 使用工具。 |
| `neighbor_eye` | 授予肩眼与撬棍，进入邻居 3D 战斗；V 肩眼、Q 闪避、鼠标左键攻击。 |
| `neighbor_death` | 播放邻居致死演出。 |
| `neighbor_victory` | 在 NeighborArena3D 中触发胜利及返回。 |
| `apartment_explosion` | 爆破演出预览；自由调试开启时不发生正式死亡。 |
| `building_saved` | 正式规则下第七天检修间干预预设，授予四段前置推理，仍需操作现场设施。 |

预设会改写临时剧情条件，常会收起控制台；可按 ~ 再打开。它们用于复现单独场景，不保证任意混用后的剧情连续性。

## 原始线索 ID

`evidence list` 可在游戏中查看实时目录。`evidence unlock <id>` 收集一条原始线索，推论需使用 deduce。

| ID | 标题 |
|---|---|
| `my-observation-notes` | 旧观测记录 |
| `apartment-telescope` | 阳台的望远镜 |
| `401-flyers` | 那张传单 |
| `401-barricade` | 401 的木板 |
| `skt079-broadcast` | 新闻摘录 |
| `balcony-telescope-day2-ancient-outline` | 天台目击记录 |
| `bedroom-window-day2-observation` | 对面的窗户 |
| `filter-bedroom-margins` | 桌角小字 |
| `filter-401-underprint` | 墙纸下面的字 |
| `filter-railing-tally` | 七道刻痕 |
| `tank-label` | 换水标签 |
| `tank-eye` | 鳞片下的人脸 |
| `tank-overflow` | 鱼缸原来的位置 |
| `001-name` | 她记得那些抱怨 |
| `notes-gap` | 少了两页 |
| `403-silence` | 403 门下的灰 |
| `cult-door-note` | 401 的纸条 |
| `401-warning` | 邻居的警告 |
| `first-night-silhouette` | 缺了一块的星空 |
| `first-night-recheck` | 一小时后的复查 |
| `fish-help-promise` | 小鱼听见的动静 |
| `circus` | 威尔马戏团 |
| `sushi` | 宫崎寿司 |
| `city-maintenance` | 维修员的交接单 |
| `city-fuse` | 检修间接线图 |
| `city-orders` | 搜捕名单 |
| `city-routing` | 广播员的路由记录 |
| `city-spectrum` | 第六日观测记录 |
| `city-lens` | 阵列维护手册 |
| `prep-oil` | 食用油 |
| `prep-medium` | 封存维修芯 |
| `prep-manual` | 热标记说明 |
| `pipe-pulse` | 管道脉冲 |
| `neighbor-order` | 净化分配令 |
| `neighbor-death-memory` | 住户的攻击 |

## 推理配方

用法：`deduce <左节点> <右节点>`。必须已持有两节点；日期、前置推论及传单限制仍生效。

| 左节点 | 右节点 | 结论 ID | 最早日 |
|---|---|---|---|
| `first-night-silhouette` | `first-night-recheck` | `deduction-growing-shadow` | 1 |
| `balcony-telescope-day2-ancient-outline` | `first-night-silhouette` | `deduction-optical` | 1 |
| `401-warning` | `skt079-broadcast` | `deduction-warning` | 1 |
| `401-warning` | `401-barricade` | `deduction-sealing` | 1 |
| `401-barricade` | `filter-401-underprint` | `deduction-not-looking` | 1 |
| `tank-label` | `tank-eye` | `deduction-tank-resident` | 1 |
| `tank-overflow` | `001-name` | `deduction-waterline` | 1 |
| `city-orders` | `city-routing` | `deduction-city-orders` | 1 |
| `city-spectrum` | `city-lens` | `deduction-observatory` | 1 |
| `prep-oil` | `prep-medium` | `deduction-sealed-materials` | 1 |
| `deduction-sealed-materials` | `prep-manual` | `deduction-sealed-tool` | 1 |
| `neighbor-order` | `skt079-broadcast` | `deduction-group-assignment` | 1 |
| `deduction-group-assignment` | `pipe-pulse` | `deduction-source-location` | 1 |
| `deduction-source-location` | `city-fuse` | `deduction-building-plan` | 1 |
| `deduction-building-plan` | `city-maintenance` | `deduction-day7-plan` | 3 |
