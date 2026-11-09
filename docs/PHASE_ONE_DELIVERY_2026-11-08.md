# 第一阶段交付记录 · 2026-11-08

以用户完整规格为基线实施，版本0.3.0。主要功能代码已提交并推送，原GitHub/Unity工程已同步，Windows非开发运行包已生成并校验。自动化场景验收不代替完整人工游玩，尚未签收的内容见下文。

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
- 带图形联合回归：249项PASS（153历史+96正式）。最后空间/灯光收尾后正式专项98项再通过，覆盖真实传单读取、Day2回档外观、45分钟短休、2D/3D位置一致与原有正式规则。不要把重叠的多次回归相加称为独立检查数。
- 三种相机渲染：1280×800、1920×1080、1024×768；相机PNG不含IMGUI，因此不声称已验证完整UI。临时PNG留在本机outputs，不提交到仓库。
- 最后房间演出、光位和实体桌椅修改后再次98项PASS，Unity退出码0。
- Windows x64 Build Finished, Result: Success；最终BuildOptions.None，退出码0。ZIP含244项、展开161111754字节，每项CRC校验通过，排除了开发备份目录。

首次构建在Unity初始化阶段退出；后续开发构建和最终非开发构建均成功。没有使用旧版ZIP作为本轮交付。

## 受阻事项与运行告警

独立程序已完成Mono、输入和Direct3D初始化，出现实际游戏窗口。开发预览首次启动产生的Windows防火墙权限窗口仍遮挡画面，未代操作任何系统安全权限；用户需亲自关闭后才能继续原生窗口的UI检查。最终运行包不启用开发调试连接，原Unity存档没有被这次独立预览覆盖。

Player日志没有记录游戏脚本异常；存在URP景深/Panini后处理着色器被剥离或不支持的警告，及D3D12信息队列查询失败。不能把这些警告描述为已经消除，也不能因此宣称完整原生画面验收通过。场景相机画面与自动规则验收已分别通过。

## 未签收项目

中文和英文分别从新游戏完整手动走到Day7；第一天15–20分钟/第二天20–25分钟实际时长；完整夜戏录像；耳机及扬声器试听。双语自动连续路线已通过，但这些人工项目不能用日期夹具或相机PNG代替。当前程序化人物和环境仍属原型美术，不能称为最终美术完成版。寿司/马戏团完整章节和新Boss按本阶段规格冻结。

## 构建与版本

Windows x64，产品Death by Sevens Dawn First Stage，0.3.0，独立产品存档目录；不打包临时视觉桥、调试存档或Library。

- 实现提交：3a88346，已推送origin/feature/seven-day-city。
- 原Unity工程：C:/Users/Owner/Documents/GitHub/DeathbySevensDawn/Weekend，已快进同步实现提交；未知文件保留，未更改玩家原旅程。
- 包：仓库外../outputs/DeathbySevensDawn-StageOne-playable.zip，解压后运行SevenDay.exe，保持同目录Data等文件完整。
- SHA256：80AF594806273C29AA7C4C9859179212BDB10036BFA9EA8013A07049467385B0。
- 文案、验证摘要、三分辨率截图：../outputs/StageOne-2026-11-08/。
- 最终非开发构建脚本与本交付记录另作交付确认提交；GitHub源码以该分支最新提交为准。
