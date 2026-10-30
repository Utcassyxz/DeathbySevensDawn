# 像素日常、隐藏线索与写实真相 · 2026-10-28

## 本轮实现

正常探索继续使用现有 2D 像素场景。SAN 降低不会给房间换画风；移除了旧 HUD 的全屏紫色理智染色。

SAN <= SevenDayProgression.fracturedSanity（当前 35）时，可以辨认卧室书桌附近、401 门旁及阳台栏杆的痕迹。靠近按 E 阅读，自动记入日记。重复阅读不重复记录或奖励。恢复理智会隐藏痕迹，已记住的内容保留；“回到初始状态”清空记录。

第二天傍晚/深夜使用望远镜，切入写实的古神插画，按 E / Esc 移开视线，接回已有序章标题。其他时间仍是像素场景。旧古神日记中的默认占位图路径会迁移到新图。

本轮实际接入已有古神事件。Boss 战及其专属插画属于后续内容，没有伪造一场 Boss 战。

## 配乐

- ApartmentHome：36 秒、80 BPM、3/4 拍的原创柔和拨弦与钟琴主题。
- BehindTheFilter：相同长度与节拍，保留主题轮廓并加入失谐音程，低 SAN 时平滑切入。
- Revelation：写实插画出现时的 5 秒低音事件音。

所有音源通过 ApartmentMix 的 Master 组输出。循环从同一 DSP 时间启动，跨场景延续；阅读时降低配乐音量，重置时恢复初始音乐状态。音频由 tools/audio/compose_apartment.py 合成，无第三方采样或既有游戏旋律。峰值/RMS 和再生成方式见 Assets/Resources/Audio/SCORE.md；RMS 不等于 LUFS，主观混音仍需在实际耳机和扬声器上复核。

## 参考学习与边界

查阅了 [UNDERTALE 官方介绍](https://undertale.com/about/) 对幽默、角色对白和选择的说明，以及 [The Coffin of Andy and Leyley 官方商店介绍](https://store.steampowered.com/app/2378900/The_Coffin_of_Andy_and_Leyley/) 的探索、交谈和轻量谜题定位。这是对官方公开资料的研究，不声称已经通关或完整分析原作。

本项目据此采用的设计判断：让熟悉的公寓和主角口吻承载线索，让玩家主动靠近、阅读和记忆，再在关键事件用写实插画打破视觉预期。具体故事、插画和旋律为本项目制作。

已安装并使用 [audio-design 技能](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/disciplines/audio-design)，采用其同步分层、平滑切换、阅读压低背景音乐的做法。

## 快速试玩

1. 调试面板 ~，首次输入 1997；从初始状态连续获得三次宇宙知识，SAN 变为 34。
2. 卧室书桌附近、401 门旁、阳台左侧栏杆可出现可读痕迹；靠近按 E。
3. 正常第一轮：第二天推进到傍晚，阳台望远镜按 E，观看写实插画。
4. 调试面板“回到初始状态”清掉本次进度；正常死亡轮回保留已发现线索。

验收入口：Prototype > Test perception clues and music。测试会备份并恢复个人进度。
