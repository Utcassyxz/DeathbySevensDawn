# 邻居剧情与战斗修订验收 · 2026-11-09

发布版本：0.3.3。控制台先前独立提交 da22225，后续修改遵循用户新增的强制遭遇、一次性对话及取消阀门剧情要求。

## 实际修改

- 修复正式 hostile 分支只打开 early-threat 而没有调用战斗入口的问题。
- 现有对话图的 11 条有效路线全部汇入失控、摘帽/再次失控、认知滤网撕裂和第一人称遭遇。对话选择仍影响信任、邀请函、证据及后续处置条件。
- 持久保存当前节点与已选选项；核心对话提交后不能退出重开，换场/读档续接当前节点。调试取消有独立入口，正式玩法不借用调试开关。
- 邻居衰退消费 TimeManager 的日期与分钟，并记录本轮最高进展；对话压力加剧失控。未新增第二只剧情时钟。帽子在首次遭遇前保留，首次认知污染扣 20 SAN；再次失控使用不同文案，不重复首次扣除。
- Boss 保留 240 基础体力与现有拳脚/脑触手行为，可通过真实攻击击退；取消追逐中血量耗尽自动回满，以及 E 反复交谈产生停顿的逻辑。徒手为弱攻击，第一人称只显示单侧防御手臂；撬棍仍有明显优势。首次捕获保护、逃生与实际死亡回档仍在。
- 地下设备间改为调查交接档案和已封存的运行设备。正式流程无 routing/source-core 的可执行机关。旧动作只为显式 LegacyValidation 历史夹具保留，正式构建不可开启。
- 第七日已提交的对话分支进入最终遭遇；击退后才掉落控制器。拾取立即入包、关闭现场卡片并移除世界物件，后续通过实际背包入口使用；不会重复拾取。
- 控制器依线索解锁撤回、见证撤回、广播撤回与疏散，展示不同结算。取得控制器之前不能直接完成阶段。接线图、交接单和推论已同步解释为何切断地下供电不能撤回启动。
- 第七日短轮回会一同清除未来取得的控制器与拾取标记，保留原有知识及此前普通物品继承规则。
- Save v7 增量兼容；35 条原始线索 ID、15 条配方以及中英文原广告均核对保留。新增控制器物品后，完整控制台手册同步为 7 种物品。

## 已通过验证

| 范围 | 结果 | 证据 |
|---|---|---|
| 控制台 + 历史规则 + 当前第一阶段 | 371 项通过 | outputs/Neighbor-0.3.3/all-regression.txt；neighbor-console-all-regression.log |
| 最终分支与跨日状态 + 中英文连续七日 | 164 项通过（114 专项 + 50 连续路线） | final-flow-and-continuity.txt；neighbor-final-flow-1.log |
| 最后的短轮回物品修复 | 3 项通过 | short-loop-validation.txt；neighbor-reset-final.log |
| 中英文文本结构、原广告、线索/配方 ID | 通过 | neighbor-final-content-check.py 输出及 JSON 解析 |

371 项全量检查在最后的再次失控文案/短轮回局部修复前完成；之后针对受影响路径运行 164 项与 3 项。没有将不同范围的重复检查相加成唯一用例总数。

验证边界：战斗损伤路径夹具会定期恢复主角体力，证明的是真实命中、有限血量、胜利和返回，不能代替人工难度平衡。连续七日路线加速移动/等待，不注入主线完成标记，不是实体键盘定时手工通关。本轮未完成全七日人工手感、美术与音频签收。

环境记录：工作副本多次遇到 Unity 初始化、脚本重载或 Roslyn 原生异常。失败启动不计入通过；保留独立成功日志和退出码。生成缓存完整备份在 work/Library-before-neighbor-reimport，未关闭用户原 Unity 窗口，也未改动引擎安装。

## 设计参考与待确认事项

使用现有对话指导中的图、持久变量、一次性节点、分支遍历方法；没有为本轮更换叙事框架。

- Yarn Spinner once：https://docs.yarnspinner.dev/write-yarn-scripts/scripting-fundamentals/once
- ink 访问记录及分支合流：https://github.com/inkle/ink/blob/master/Documentation/WritingWithInk.md
- Obsidian / Joshua Sawyer 的选择架构公开简介：https://www.gdcvault.com/play/1015758/Do-(Say)-The-Right-Thing （未声称看完付费视频）
- 《原神》5.2 官方更新说明公开列有特定 Boss 错误脱战/回血的修复：https://genshin.hoyoverse.com/en/news/detail/126992 。本项目据自身机制去掉追逐血量强制刷新；这是设计取舍，不是复用其内部实现。

用户随后明确确认该来源，已安装 unity-csharp-scripting，并运行其经审阅的只读工程检查。发现并清理默认渲染 Profile 的孤立失效/测试子资源；最终严格检查 0 错误、0 警告。详见 unity-project-integrity-2026-11-09.md。

## 最终构建与交付

Windows 64 位非开发构建成功：project-integrity-player-build.log 的 Build Finished, Result: Success，退出码 0。最后的徒手模型接合/形状修正为纯外观调整，已纳入本次构建；未重新跑全部逻辑回归。227 个文件逐项 CRC 校验通过。

压缩包 SHA256：398D493A689D37581AF6C665499FD4D782C4524223AA51F8EEEE8F1006855B73

构建时只对本次进程临时设置 DOTNET_gcServer=0、DOTNET_gcConcurrent=0，未更改系统环境或游戏运行配置。配置依据：https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector 。
