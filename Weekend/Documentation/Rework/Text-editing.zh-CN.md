# 文案集中编辑说明

中文实际运行源：`Weekend/Assets/Resources/StoryText.json`。
英文实际运行源：`Weekend/Assets/Resources/StoryTextEnglish.json`。

这是游戏实际读取的文件，不是交接Prompt。Unity 导入保存后的文件，重新进入 Play 即可看到改字。

- `neighborSlice.entries`：第二天醒来、偷听、柜子、拾取、邻居对话、战斗、检修间提示。
- `city.entries`：城市地点、NPC、现场物件。`days` 不再作为玩家头顶的攻略HUD显示。
- `investigation`：线索与推论的可见标题、正文。
- `ui`：按钮、操作提示、日记/推理板/设置等集中界面文字。`copy.*` 是本轮从脚本迁入的文字；`fragment.*` 是保留条件组合的文字片段。
- 其余章节：原有小鱼、泡泡、广播、梦境、结局等文本。

只改 title/body/text 等文字值，不改 id/key、字段名、数组层级或花括号占位符。`{0}` 表示运行时插入的名称或数字，保留它。文字里的换行写成 `\n`，引号写成 `\"`。JSON 末项不加逗号。

原四则广告全文保持不变。它们与既有伏笔的来源已经归档；修改广告会影响之前的叙事证据。

代码中的场景名、线索ID、存档关系键、资源路径不是玩家文案，不应随文案改名。调试命令的语法也保持原样。
