# 交互与光照收尾验证 — 2026-10-30

## 范围

本轮只处理上一个中断点留下的两件事，并跑完整回归：

1. 室内夜间灯光白斑：灯材质/局部光晕显示异常。
2. 推理板筛选按钮文字裁切。

没有新增大型系统，没有改场景层级，没有回到旧的中心距离互动逻辑。

## 1. 夜间灯光

### 原问题

旧截图（`home-at-night` 早期版本）里，客厅夜间出现几块明显白色光斑：

- 灯光不跟随设定的暖色/青绿色。
- 加色混合在亮部过曝。
- 自定义 glow 材质在当前 2D 渲染路径下表现不稳定。

### 处理

`Weekend/Assets/Scripts/Environment/InteriorLighting.cs`

- 局部光晕改用内置 `Sprites/Default` 材质，不再使用不稳定的加色自定义材质。
- 使用 `SpriteRenderer.color` 做暖色/青绿色 alpha 混合：
  - 客厅暖灯池：暖橙。
  - 鱼缸：青绿玻璃光 + 下方柜体/地面 spill。
  - 卧室：暖色灯池。
  - 卫生间：冷白微光。
- 白天透明度很低，不产生白斑；傍晚/夜晚才逐渐出现。
- 鱼缸光只出现在玻璃附近和柜体下方，不把整个柜体染亮。
- 保持 `TimeAtmosphere.LitTint`，室内整体仍有暖色可读性；室外夜色不被改暖。

结果见：

- `docs/validation/interaction-polish/home-at-night.png`
- `docs/validation/interaction-polish/sleepy-hugo.png`

### 当前效果

- 夜间没有白色光斑。
- 客厅出现低强度暖光池。
- 鱼缸出现青绿色局部 spill。
- 仍保留“夜晚较暗、只做轮廓和微光”的方向。

## 2. 推理板筛选按钮

### 原问题

筛选按钮宽度 104，左侧还有选中标记 `●  `。英文/较长中文会被裁切。

实际测量（`GUIStyle.CalcSize`，fontSize 16）：

| 文本 | 宽度 |
|---|---:|
| `●  All` | 78.5 |
| `●  Evidence` | 126.5 |
| `●  Deductions` | 145.8 |
| `●  Unconnected` | 160.8 |
| `●  现场记录` | 131.8 |

旧按钮内部可用宽度只有约 92–96，英文 `Deductions` / `Unconnected` 必然裁切。

### 处理

`Weekend/Assets/Scripts/UI/InvestigationBoardUI.cs`

- 筛选按钮从 `104 × 34` 扩大到 `184 × 34`。
- 按钮间距从 `112` 调整为 `192`。
- 保留 `fontSize = 16`、`fixedHeight = 34`、`wordWrap = false`、padding `6/6/2/2`。
- 可用文本宽度约 `172`，大于最长的 `●  Unconnected = 160.8`。
- 最右侧按钮仍在标题栏以下，不与关闭按钮在垂直方向重叠。

结果见：

- `docs/validation/interaction-polish/inference-compatible.png`

## 3. 回归

验证入口：

`Prototype → Validate presentation and two-night story`

测试文件：

`Weekend/Assets/Editor/Validation/VerifyPresentation.cs`

结果：

```text
PASS: 68 checks
```

覆盖：

- 梦境与眼皮渐变
- 单提示优先级
- 字幕等待 Enter / E 不误推进
- 早晨、正午、黄昏、夜晚
- 早餐、沙发午睡
- 第一次日记
- 第一夜一小时/复查/睡眠
- 证据高亮与推论
- Ending 收藏持久化
- 第二天 cult 路线
- 未觉醒第七夜世界结局
- 天台主动觉醒与轮回
- 中英双语
- 字幕边界

`presentation-validation.txt` 为最终结果。

### 运行过程说明

- 有一次运行在“Bulletin follows the TV cue”步骤因输入事件时序失败；清理被占用的临时 Editor 后重跑，完整 68 项 PASS。
- 判定为验收运行时序抖动，不是玩法逻辑失败；最终结果以重跑 PASS 为准。

## 4. 仍需注意

- 本次验证在**当前工程的临时副本**中执行，避免占用用户正开着的原工程 Unity。
- 原工程 Unity 仍需要停止 Play / 重新编译后加载这批改动。
- 未新增“更多线索/互动”、台词重写、双语扩展或主菜单异化动画；这些仍是下一阶段目标。

## 相关文件

- `Weekend/Assets/Scripts/Environment/InteriorLighting.cs`
- `Weekend/Assets/Scripts/Environment/TimeAtmosphere.cs`
- `Weekend/Assets/Scripts/UI/InvestigationBoardUI.cs`
- `Weekend/Assets/Editor/Validation/VerifyPresentation.cs`
- `Weekend/Assets/Resources/SoftRoomGlow.shader`（保留为未使用备用；当前灯光改用内置 Sprite 材质）
