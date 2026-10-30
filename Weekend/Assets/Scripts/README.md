# 运行时代码

- `Core/`：GameManager 是状态入口。不要让 UI 直接判定结局。
- `Progression/`：ProgressionState/Save 保存旅程；Settings 放平衡参数；Bootstrap 安装运行时组件。
- `Time/`：TimeManager 统一推进；LoopManager 处理轮回。不要直接改 Days 字段绕过终局检查。
- `Knowledge/`：线索结构、知识奖励、日记与推理。逻辑编号不随翻译修改。
- `Player/`：移动控制。交互阻断来自各剧情系统共享的 WorldBlocked 状态。
- `World/`：房间门和场景切换。保持 arrivalPosition 与场景布局一致。
- `Dialogue/`：数据资产，预留给条件对白。剧情文案请改 StoryText JSON。
- `Tutorial/`：第一天教程。Stage 2 邻居 → 3 冰箱 → 4 新闻 → 5 沙发 → 6 书桌 → 7 观测 → 8 记录 → 9 连线 → 10 自由探索。
- `Narrative/`：邻居演出与锡纸获取。未来天台事件必须显式调用 GameManager.RecordAncientGodEncounter()。
- `Environment/`：房间内的家具、鱼缸、窗户与隐藏痕迹。AquariumEntity.ThrowFoil() 同时消耗物品并保存封缸标志。
- `UI/`：日记、推理图、菜单与反馈；不在绘制函数里推进日期或发放知识。
- `AudioVisual/`：音画表现。WorldEndSequence 是 5.5 秒的单次末日转场；重读已结束的存档不重复播放。
- `Development/`：开发用调试工具。不要在此放正常玩家的剧情流程。
- `Story/`：JSON 数据结构和资源配置。中文 StoryText.json，英文 StoryTextEnglish.json；两种语言键和条目 ID 必须对齐。

命名空间保持稳定。脚本目录描述职责，资源目录遵循 Resources 加载路径，不应随意改名。
