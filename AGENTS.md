# 七日：工作边界

唯一 Unity 工程为 Weekend/。不重建工程，不覆盖未知工作，不反复关闭并重启编辑器。

## 文案任务
默认只编辑 Weekend/Assets/Resources/StoryText.json、StoryTextEnglish.json 和 docs/ 中的文案文档。保留字段名、线索 ID、页面结构、条件及中英文对应关系。纯文案润色不修改 C#、场景、Prefab、输入、存档或贴图导入设置。

## 明确授权的工程任务
用户明确要求功能或画面变更时，可以修改相应工程文件。先读 docs/CORE_CODE_MAP.md，了解唯一负责该行为的系统，再改动；不要为消除眼前症状同时改几个系统。

标记 CORE 的脚本有剧情或存档契约。修改必须同步相应验收，不得把测试期望改成当前错误行为来消除失败。不要为旧测试绕过正式流程：场景准备可以显式调用 CompleteSleep，玩家睡眠必须走 SleepUntilNextMorning。

键位固定：Enter 推进剧情，E 使用物件，R 主动记录，J 日记，B 推理，I 背包，Esc 返回；已有专用 F 投入/搬运保持不变。Days 是唯一日期变量。

开始新旅程清空本次剧情，保留结局收藏。获得回档的条件是完成泡泡会面；古神目击、草图、锡纸道具不能替代。

最新功能验收：Prototype → Validate fish dreams and scenery（VerifyNightArc）。该工具备份并恢复旅程、结局收藏、语言和编辑器场景。运行前退出 Play；不要手工清空玩家存档来验证。
