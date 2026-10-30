# 公寓原型：主动收集与线索推理

本轮把看见、理解、收录和推理拆开。开门看到 401 贴传单仍是剧情演出，但演出不会给玩家一份已经抄好的传单。玩家走近纸张按 E 查看，可以离开，也可以按 R 抄录。新闻、木板、望远镜、窗外变化和低 SAN 字迹同样需要主动收录。

## 试玩

- 卧室书桌：E 查看自己的观测手记，R 收录。
- 走廊 401 门旁：等邻居离开，靠近传单按 E，R 抄录。第二天可以查看门上的木板。
- 阳台：E 查看望远镜，R 收录物件，T 透过镜头观察。第二夜目击后，可以按 R 画下轮廓，也可以直接离开。目击造成的原有属性影响只触发一次，收录素描不会再次发奖。即便没有画下来，序章仍可继续。
- 第二天的窗户：查看后 R 记录变化。第二天电视新闻：看完后 R 摘录；只消耗原有的一个时段。
- SAN <= 35：隐藏线索保持原有出现条件，但阅读后要按 R 才收录。
- J 打开笔记，点击「线索连线」或按 B 切换。选两张卡片，选择一种关系，检查依据，最后确认写下推论。
- 纯键盘：方向键移到卡片，空格选择；Tab 换关系；Enter 先检查、再确认；Backspace 清空选择；Delete 擦除待证线；J/Esc 合上。

## 六条手工设计的推论

| 依据 | 关系 | 推论 |
|---|---|---|
| 我的观测手记 + 阳台望远镜 | 习惯与身份 | 我爱好天文 |
| 轮廓素描 + 阳台望远镜 | 观察工具 | 轮廓来自一次望远镜观测 |
| 上面两项推论 | 交叉印证 | 我主动寻找了那片天空 |
| 401 传单 + SKT-079 新闻 | 时间关联 | 警告早于新闻；还要求亲眼经历第一天贴传单 |
| 401 传单 + 封门木板 | 行为动机 | 他在照着自己的警告行动 |
| 封门木板 + 隐藏字迹 | 交叉印证 | 封门也许是为了不再看见 |

推论不等于全知真相。新闻和传单都谈到天空，不能据此判定邻居了解超新星，更不能认定超新星就是古神。素描记录主角的记忆，不能替代独立目击或仪器数据。推论正文写明证据边界。

其他组合可以保存为虚线「待证」，不会奖励数值，也不会被标为事实。待证线可擦去；已写下的推论保留原始依据并可继续参与下一层推理。预览不会写入存档，确认才会保存。反向重复、自我连接、未收录证据和无效关系均拒绝。

## 数据与兼容

- 10 个证据定义、6 个推理规则位于 `Weekend/Assets/Resources/Investigation.json`。展示内容和稳定 ID 与运行逻辑分开；这是一组明确设计的规则，不是自动生成任意推论的 AI。
- 已收录证据沿用 journal；连线保存到 investigationLinks。observedKnowledgeIds 独立记住目击属性影响，避免为了得到 SAN 代价而自动写笔记。
- 老存档记录不删、不重发奖励；缺少新字段时补空列表。新轮回保留收集与推理，调试面板的完整初始状态重置会清空它们。
- 开场梦境和章节纪事仍属于剧情记事，不充当未经收集的证据。
- 本轮保留正常像素画面、家具布局和碰撞。新界面沿用现有笔记的 IMGUI，不迁移整个 UI 框架。

## 技能与素材

安装并阅读了 [game-ui-ux](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/disciplines/game-ui-ux) 与 [dialogue-systems](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/disciplines/dialogue-systems)：应用了缩放与键盘流程、条件图、数据和状态分离、重复进入不重复写状态等原则。也查阅了现有 Unity UI 技能。

轮廓素描通过内置 imagegen 生成，最终资源为 `Weekend/Assets/Resources/Story/AncientOutlineSketch.png`。写实目击图仍用于现场事件，素描用于笔记。

生成提示词：

> Create a game evidence asset: a hurried amateur astronomer's graphite pencil sketch on warm cream unlined notebook paper, landscape 4:3. An immense ambiguous eye-shaped contour behind thin clouds, dark hollow center, a few distorted star positions marked as tiny crosses, very faint city rooftop silhouette below. A frightened human's incomplete observational sketch, shaky retraced lines and rubbed out pencil, restrained and intimate, mostly empty paper, monochrome graphite. NO words, no letters, no labels, no handwriting text, no border, no hands, no pencils, no photorealistic scene rendering, no colored ink. Flat scan of the paper filling the image. This is the collectible sketch recalled after viewing an ancient being through a telescope.

## 验收入口

Unity → Prototype → Test investigation collection and deductions。测试隔离并恢复主存档、备份与临时文件，覆盖主动收集、六条推论、待证操作、键盘确认、旧档、存档重读、轮回与完整重置。
