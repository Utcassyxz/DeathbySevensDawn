# 七日：工作边界

唯一 Unity 工程为 Weekend/。不重建工程，不覆盖未知工作，不反复关闭并重启编辑器。

## 文案任务
默认只编辑 Weekend/Assets/Resources/StoryText.json、StoryTextEnglish.json 和 docs/ 中的文案文档。保留字段名、线索 ID、页面结构、条件及中英文对应关系。纯文案润色不修改 C#、场景、Prefab、输入、存档或贴图导入设置。

## 明确授权的工程任务
用户明确要求功能或画面变更时，可以修改相应工程文件。先读 docs/CORE_CODE_MAP.md，了解唯一负责该行为的系统，再改动；不要为消除眼前症状同时改几个系统。

标记 CORE 的脚本有剧情或存档契约。修改必须同步相应验收，不得把测试期望改成当前错误行为来消除失败。不要为旧测试绕过正式流程：场景准备可以显式调用 CompleteSleep，玩家睡眠必须走 SleepUntilNextMorning。

键位固定：Enter 推进剧情，E 使用物件，R 主动记录，J 日记，B 推理，I 背包，Esc 返回；专用 F 仅用于投入锡纸；望远镜搬运按最新用户要求使用 E。Days 是唯一日期变量。

开始新旅程清空本次剧情，保留结局收藏。获得回档的条件是完成泡泡会面；古神目击、草图、锡纸道具不能替代。

最新功能验收：Prototype → Validate fish dreams and scenery（VerifyNightArc）。该工具备份并恢复旅程、结局收藏、语言和编辑器场景。运行前退出 Play；不要手工清空玩家存档来验证。

2026-10-31 最新用户修订以 docs/DREAM_PIXEL_REVISION_2026-10-31.md 为准：普通夜晚不播放重复梦，只有第二夜古神目击后见泡泡；锡纸必须在人脸出现前使用；场景鱼用小像素图，对话才展示细致立绘；结局配乐共享一套旋律。新增改动运行 VerifyRevision。

2026-11-01：最新规则见 docs/STAIRWELL_PIXEL_2026-11-01.md。客厅无局部光池；阳台外景留空、粗像素建筑；走廊 EXIT 进入独立 Stairwell，四楼住户/五楼天台；E 望远镜拿取和收起，F 仅保留投锡纸。

2026-11-01 后续最新行为见 docs/COMPANION_WORLD_2026-11-01.md：泡泡是实际观测当晚，首次介绍/后续轮回商店分开；鱼是女性，可通过两次理解成为调查同伴；新增 Save v3，必须兼容旧存档。

2026-11-02 最新规则见 docs/CONTINUITY_2026-11-02.md：鱼关系永久解锁，后续轮回第一天坐在缸旁；泡泡在后续轮回首次睡眠自动会面，不再要求重复目击；每天首谈推进，后续抽独立闲聊；Save v4 迁移 v1/v2/v3。

2026-11-02 本轮走廊与晨间演出规则见 docs/CORRIDOR_MORNING_2026-11-02.md：EXIT改为北墙中门并同步实体墙面碰撞；后续轮回第一天小鱼叫醒雨果并搬走真实餐凳，MorningRoutine编排、AquariumEntity管理模型和家具。新增DoorMorningAcceptance纳入VerifyRevision。

2026-11-02 后续空间/末日规则见 docs/SPATIAL_APOCALYPSE_2026-11-02.md：楼梯间入口位于下侧；小鱼推缸后占原位；世界标签不得穿过模态界面；首次已觉醒午夜必须读完泡泡重逢再回档，未觉醒终局保留。

2026-11-02 最新修订以 docs/DEATH_CYCLE_2026-11-02.md 为准：所有运行场景集中 Weekend/Assets/Apartment；死亡才回档，存活可超过七天；第五天爆楼与第七天危机独立；缺觉超过两天每个午夜扣5SAN；第三夜小鱼变人、第四早叫醒、死亡一起重置，禁止恢复永久人形。Save v5 兼容旧版。当前验收 Prototype/Validate current prototype。
