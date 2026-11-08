# DeathbySevensDawn 第二日—第三日开发报告

基线 cb74790；工程 Weekend；Unity 6000.6.7f1。本轮交付是可运行的邻居与救楼纵向切片，七日城市框架沿用上轮实现，不代表全部七日已经达到成品质量。

## A．已完成内容与文件

- 三个已注册场景：NeighborRoom2D、NeighborArena3D、BuildingService2D。日常调查保持2D；敌对邻居遭遇经CognitiveRupture切入第三人称3D，战斗结束返回原2D房间。普通街道不再自动触发3D。
- NeighborSlice.cs、NeighborSliceData.cs、NeighborDialogue.json：真实物品、撬门、分支对话、招募关系、现场操作、邻居死亡演出。保留原敲门/锡纸路线。
- NeighborRoom.cs：房间对应布局、调查家具、地下设施、2D邻居与理智呈现。NeighborCombat.cs：可预判攻击、收招反击、单次准备工具、肩眼慢时与冷却。PerceptualFilterSettings.cs及资源保存70/40/20阈值和战斗配置。
- CultNeighbor.cs扩展已有背包；Investigation.json新增证据与多级推论。StoryText两个语言文件及默认语言逻辑已更新，新偏好默认英语，已有中文选择保留。
- CrisisRules.cs、CityWorld.cs、WorldEndSequence.cs：击败邻居不能直接救楼；旧城市终端不能绕过源头调查。实际完成地下两项操作才解除第三夜爆楼。
- GameManager.cs、ProgressionState.cs、ProgressionSave.cs：v7存档、兼容旧版本、当世物品与关系重置、死亡原因、冷却与肩眼继承。泡泡恩赐依然必须实际获得。
- AquariumEntity.cs修复第四天欢迎事件在地下设施被提前消耗的问题；新房间属于离家场景，回家后仍可触发人形小鱼。
- EndingCollection.cs和UI记录邻居杀死、楼宇获救、楼宇毁灭；获救不是强制终局。
- NeighborDebugCommands.cs扩展原PIN控制台，进入PIN即克隆旅程并使用独立存档/收藏。退出隔离恢复原旅程。
- BuildNeighborSlice.cs及NeighborSliceAcceptance.cs负责场景维护、构建与验收；既有死亡/城市回归同步维护。

审计详见Day2-3-Audit.md，未来章节详见Future-Sushi-Birdcaller.md。原四条Day1广告内容保留；没有用新写文案替换原伏笔。

## B．正常游玩步骤

1. 完成原第一日引导：邻居、冰箱、新闻、沙发、日记、望远镜记录和推理板等流程，睡到第二日。基础操作：E交互、R主动记录、B推理板、I背包、J日记、Esc返回。
2. 客厅厨房低柜处E调查并取撬棍，R记录维修说明。附近取食用油并记录；浴室取封存维修介质并记录，再记录管井异常。物品每世只能领取一次。
3. 推理板连接“油+介质”得到封存材料结论，再与维修说明连接得到工具结论。在背包选择准备工具并确认，消耗两种材料，获得一件虚构封装工具；制作和使用均不消耗理智。
4. 401门前选择撬门确认，或保留原敲门路线。入室后先观察仍可辨识为人的早期异变邻居。平静询问→听取解释→R记录分配令；招募有接受、拒绝与调查，掌握组织分配推论后可欺骗。攻击性选项触发认知撕裂与3D战斗。
5. 首世战斗：携带已准备工具，接近到有效距离按G使用一次。撬棍Q只能在收招窗口有效反击。轮回肩眼路线：真正获得泡泡恩赐并返生，带回邻居死亡知识且异变达到20后解锁；V开启可见肩眼，慢时3秒，冷却12秒，消耗8点SAN，再用Q反击。当前生命、距离、遮挡与窗口仍影响战斗。
6. 胜利返回2D，在桌旁E调查、R记录分配令。击败邻居只是解除眼前威胁，独立供能源仍工作。
7. 推理板：分配令+原三角教播报→组织分配；该结论+管井异常→源头位置。楼梯一层左侧服务门进入地下设施，在接线桌E/R，再将源头结论+接线图→楼宇预防方案。
8. 第三日午夜前，先操作左侧线路隔离，再操作右侧源核锁定，各选正确行动。只有第二步完成才获得楼宇获救记录；单做第一步、只杀邻居或只推理均不够。
9. 不阻止则第三日夜末爆楼。邻居先杀死玩家则显示专属死因。未获泡泡恩赐的死亡不会免费轮回；若要带回知识，应先完成第二夜屋顶泡泡相遇并获得恩赐。获救后回家继续第三夜至第四日的小鱼流程；鱼理解、当世关系和变形沿用原条件。

第五日为既有独立城市危机，并非再次爆楼。本轮集中完善第二—三日，未新增完整第五日长篇内容。

## C．控制台与快捷验收

按~打开现有控制台，输入1997。PIN开启隔离旅程，旧按钮也在隔离中生效。命令框输入后执行；session end恢复开启隔离前的旅程。不要把调试自动补齐的证据当作正常游玩奖励。

已验收命令：scene list；scene goto <场景名>；day set 3；time set 3（0早晨/1下午/2傍晚/3夜）；sanity set 15；mutation unlock shoulder_eye；item give crowbar；evidence unlock prep-oil；flags reset；session begin；session end。

16个场景均逐一验证加载：SampleScene、Bedroom、LivingRoom、Bathroom、Balcony、Corridor、Stairwell、Rooftop、CityStreet、CityService、CityBroadcast、CityObservatory、CityEncounter3D、NeighborRoom2D、NeighborArena3D、BuildingService2D。

事件命令格式event trigger <名称>：

| 名称 | 用途 |
|---|---|
| neighbor_peace | 和平对话起点 |
| neighbor_known | 已知分配令后的记忆/欺骗分支 |
| neighbor_encounter | 撬门后敌对入室；接近邻居交互进入撕裂 |
| neighbor_prepared | 携带单次工具的3D战斗，G使用 |
| neighbor_eye | 肩眼战斗，V慢时、Q反击 |
| neighbor_victory | 在邻居战斗场景检查胜利返回 |
| neighbor_death | 邻居死因演出/独立收藏 |
| building_saved | 给出调查条件，但仍需实际完成两处设施操作 |
| apartment_explosion | 爆楼预览，免费调试保护不会杀死正式旅程 |

## D．分支与结果

- 平静接触、接受、拒绝、调查、已知情报欺骗分别保留关系状态；接受得到邀请，不立即结束游戏。
- 敌对接触支持准备工具胜利、肩眼辅助反击胜利、邻居致死。肩眼不是首世必需能力。
- 救楼成功与战斗胜负独立；源头双操作未完成仍会爆楼。邻居提前致死、楼宇毁灭与楼宇获救分别记录。
- 真正死亡仍由原死亡系统处理；无恩赐终局，有恩赐按原知识继承规则返生。当世邻居、物品、鱼身体和关系重置。

## E．验证与证据

验收方式为Unity Play Mode自动交互和状态断言，不把它描述成已经人工从第一日完整通关。覆盖真实拾取/消耗、背包、证据连线、和平/敌对分支、两种战斗胜利、实际致死、两处设施操作、第三夜死亡、第四日小鱼可达性、存档迁移、双语、全部控制台场景与事件。

最终结果、截图及构建结果见本报告末尾的交付验证记录。截图是实际相机渲染；包含2D邻居房间、3D早期异变邻居，以及同一地点SAN100/15的结构差异。音效接入现有ApartmentMix。

测试单独隔离保存路径并恢复语言、输入设备、存档和收藏。Unity曾在启动原生缓存映射时崩溃，隔离临时ScriptMapper后重跑；此类启动失败没有当作测试通过。

## F．未完成、占位与后续

- 目前角色、血肉、家具及眼睛使用程序化低细节模型/像素图。认知撕裂与低SAN的结构/运动/声音机制已接通，成品级血肉美术、骨骼动画、镜头剪辑、专业音效与恐怖强度调优尚未完成。
- 接受组织后的长期支线只保存关系与邀请，尚无完整后续组织章节。
- 寿司店及唤鸟人仅有规格。唤鸟人吞鸟、苍白肋骨笼、活鸟与占有欲来自用户设定；候选攻击阶段不是已实现Boss。
- 既有七日城市框架仍需逐章扩写、美术替换及人工全流程游玩。不能将这一切片称为完整七日成品。
- 未添加新音乐作者声明；缺少逐曲权属证据时不擅自使用Harry Sun署名。未添加AI签名。
- 未向远程推送；本地分阶段提交和可运行包供检查。

## 交付验证记录

- 最终Unity编译及Play Mode：通过243项，包含139项本轮检查、37项死亡轮回检查、67项城市回归；进程退出码0。结果位于Validation/Day2-3/acceptance.txt及unity-playmode.log。
- Unity Editor搜索索引启动曾记录ArgumentOutOfRangeException，堆栈位于UnityEditor.Search.SearchDatabase；未阻断场景或断言，但属于尚存的编辑器环境问题。原生ScriptMapper启动崩溃通过隔离临时缓存后重试解决，未修改游戏存档。
- Windows64 Development构建成功。独立产品名DeathbySevensDawn Day2-3 Review；可运行文件SevenDay.exe。10秒无图形启动检查完成Mono、物理与输入初始化，未出现游戏异常；随后仅关闭本次启动的测试进程。这不是独立程序的人工画面通关测试。
- ZIP：359条目、解压数据228295086字节；逐项解压CRC全部通过，无BackUp/DontShip目录。ZIP SHA256：278833458CA3C6DB2098FC386114EE2479826988D93B681C2B23061270D56CD3。
- 静态检查：原中英文dayOneBroadcast与cb74790深比较一致；新增对话ID唯一、跳转目标存在、两种语言均有对应文本；生成脚本语法通过。
- 已修复最终检查发现的Unity自动生成空格；交付差异检查无空白错误。源码与项目文档变化仅在Weekend内。
- 建议下一步：先人工按B节完整跑通首世和平/准备战斗及返生肩眼三条路径，再替换邻居和撕裂过渡的最终美术与动画，随后扩展第五日城市危机。
