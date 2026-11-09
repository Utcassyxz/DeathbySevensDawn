# 第一阶段交付记录 · 2026-11-08

以用户完整规格为基线实施。自动化场景验收不代替完整人工游玩；此文随最终构建更新。

## 修改范围

核心：TimeManager、GameManager、LoopManager、CrisisRules、WorldEndSequence、NightDreamSequence、ProgressionState/Save、SaveValidation、ApartmentEventDirector。

交互：NeighborSlice/Room/Combat、ThirdPersonMotor、AquariumEntity、CultNeighbor、MorningRoutine、DayOneSupernovaEvent、StoryJournal、Investigation/Board、HUD/EndingCollection/WorldPrompt、CityWorld/PocketSceneArt/StreetDepth。

数据：Investigation.json、StoryText.json、StoryTextEnglish.json；新增4个原创音效/收音机片段。所有Assets文件配meta并保留既有GUID。编辑器验收和Windows构建入口同步。

完整行为、日期、三条路线及时钟规则见SLICE_CURRENT.md。可编辑文案只在两份StoryText JSON；没有新增任务清单。

## 对白样例

雨果给401改备注：邻居。没有写危险人物。母亲发来晚饭照片，他放大看一会儿，只回复“菜色不错”。“需要帮忙吗”在脑子里练习过，听起来却像在问推销员。这些段落表现他拆逻辑和回避亲密，没有让所有角色解释世界观。

## 验证记录

- 新规则专项上一轮：93项PASS，真实标题新游戏输入加场景夹具；含教学跨日兜底、夜戏两种抓捕、实际E逃出、Day7三种干预、短循环、鱼、锡纸、迁移及双语查找。
- 联合无图形回归：246项PASS（153项明确隔离的历史契约 + 93项正式规则）。此前Unity初始化原生崩溃保留日志；重建工作副本缓存后成功，不更换Unity版本或创建第二工程。
- 中英文连续路线：50项PASS。每种语言均从真实标题新游戏，经完整第一日教学、第二日深夜追逐/正确401出口、连续第三至第七日，到未觉醒净化正式终局；没有调试跳剧情、日期赋值、注入线索或赠送恩赐。仅导航和等待被加速，不能当作手动计时或全场景碰撞路线。
- 推理静态合同：15条配方、双语标题、节点与无环、传单门槛PASS。
- 原四条广告：中英文与cb74790逐字一致PASS。
- 最终联合回归、Windows构建和原工程同步：进行中，完成后补实际结果。
- 带图形联合回归：249项PASS（153历史+96正式）。最后空间/灯光收尾后正式专项98项再通过，覆盖真实传单读取、Day2回档外观、45分钟短休、2D/3D位置一致与原有正式规则。不要把重叠的多次回归相加称为独立检查数。
- 三种相机渲染：1280×800、1920×1080、1024×768；相机PNG不含IMGUI，因此不声称已验证完整UI。临时PNG留在本机outputs，不提交到仓库。

首次Windows构建在Mono初始化时退出，未执行构建方法。无图形重试构建中。该失败保留在work/stage-one-windows-build.log，不能用旧版ZIP冒充新包。

## 未签收项目

中文和英文分别从新游戏完整手动走到Day7；第一天15–20分钟/第二天20–25分钟实际时长；完整夜戏录像；耳机及扬声器试听。双语自动连续路线已通过，但这些人工项目不能用日期夹具或相机PNG代替。

## 构建与版本

构建目标：Windows x64，产品Death by Sevens Dawn First Stage，0.3.0，独立产品存档目录；不打包临时视觉桥、调试存档或Library。最终构建结果和push哈希待更新。
