# 核心代码边界

写作协作者（包括 DeepSeek）进行台词任务时，只修改 StoryText.json 与 StoryTextEnglish.json 的文本值；不要随手“修复”下面的控制器。明确的工程任务可以修改，但需同步验证。

| 系统 | 唯一职责与禁止破坏的条件 |
|---|---|
| Time/TimeManager.cs | Days 与四时段；睡眠与跨午夜等待进入梦境。CompleteSleep 仅供梦结束和测试场景准备。 |
| Progression/NightDreamSequence.cs | 实际古神观测当晚的泡泡梦、首次介绍与后续轮回首次睡眠自动商店、身体碎片过渡、未建立理解时的替身杀人演出；Enter 推进，结束只执行一次。 |
| Core/GameManager.cs、Time/LoopManager.cs | ancientGodSeen 只是目击，yogDreamSeen 才提供回档；未觉醒不自动回档。 |
| Progression/RadiationRules.cs | 3 天人脸、4 天说话、7 天替身；锡纸仅在前两天有效，晚投消耗但不逆转。当前原型不增加新的辐射数值面板。 |
| Environment/AquariumEntity.cs | 24 像素各阶段精灵、女性鱼的永久理解解锁、后续轮回坐姿 NPC、每日首次剧情与重复闲聊、主动收集调查证词、近身查看、手动投入并消耗一个锡纸；同伴不生成雨果替身，不代替睡眠结算。 |
| Environment/BalconyTimeView.cs | 负责阳台粗像素建筑层、SevenDayExterior 七日俯视外景、统一平面尺度与安全到达位置。地面、门与碰撞同步缩放；不要另加控制器重复缩放。不能与 SceneBackdrop 抢相机清屏色。 |
| Environment/TimeAtmosphere.cs | 世界配色和天台天空。动态远景、鱼缸、灯光各自由其所有者着色。 |
| Environment/InteriorLighting.cs、CorridorDetail.cs | 客厅不再创建灯具或遗留局部光效，其余房间保留灯具；鱼缸没有凭空的夜间光斑。走廊装饰不能覆盖门口和通路。 |
| UI/StoryJournal.cs | 日记与主动记录；观测使用双帧像素图，复查时轮廓前移；不要整体放大星空。初始梦不自动成为线索。 |
| UI/PrototypeButtons.cs、AudioVisual/ApartmentAudio.cs | 所有按钮共用按压反馈；音轨由状态平滑切换。不要在 OnGUI 每帧重新播放声音。 |
| Progression/EndingCollection.cs、UI/EndingCollectionUI.cs | 独立保存结局收藏，重开旅程不会擦掉；未知结局不泄漏名称。 |

## 验证

- VerifyNightArc：覆盖普通夜晚无重复梦、首次观测当晚泡泡赋能（包含较晚观测用例）、两次观测、每日鱼形、锡纸防护、两种第七夜结算、结局永久保留及场景日夜。
- VerifyPresentation：旧的新手流程与布局回归。无图形编辑器中的虚拟键盘可能受焦点影响；流程 API 与原工程按键试玩需要分开报告。
- 使用已有 Unity 窗口进行最终画面复核；独立验证副本不代替画面检查。

## 素材

旧版图片曾使用内置 image_gen 制作并保存至 Resources/Story：CosmicOccultation、BubbleContact、EndingReplacement、EndingWorld、FishMetamorphosis。提示词与图像用途见 ART_AUDIO_2026-10-31.md。最新对话立绘与失真结局插图见 COMPANION_WORLD_2026-11-01.md，旧写实结局图已退出运行时。音乐为本项目合成原创音轨，生成源位于 Weekend/Tools/。

本轮最新规则与素材见 DREAM_PIXEL_REVISION_2026-10-31.md；最新专项为 VerifyRevision。StoryDebugActions 只用于开发，结局预览不得写入真实结局收藏。开场三脚由 StoryJournal 单独负责，DreamMotion 只提供一个衰减脉冲，不另外计时补震。

2026-11-01 修订以 STAIRWELL_PIXEL_2026-11-01.md 为准。RooftopAccess 单独负责 EXIT → Stairwell → 四楼走廊/五楼天台、层数与望远镜搬运；不允许其他脚本再直接把走廊楼梯传送到天台。楼梯场景由 BuildStairwell 生成，模型为 StairwellLanding.prefab。望远镜搬运改为 E，F 只保留鱼缸投放锡纸。结局三版使用相同和弦序列与旋律，以配器分层区分。

2026-11-01 后续修订以 COMPANION_WORLD_2026-11-01.md 为准。泡泡由实际古神观测当晚触发，首次介绍、后续轮回首次睡眠自动商店分开；鱼的理解选择可成为调查同伴；Save v3 必须迁移 v1/v2；SevenDayExterior 统一所有七日外景。

2026-11-02：最新跨轮回和日常交互规则见 CONTINUITY_2026-11-02.md。StoveInteraction 独占炉火与贴图开关；RoomDoor2D 独占走门传送；Bubble 的全局解锁不能再绑定后续轮回的重复观测；存档迁移必须保留 v3 已获得关系。
