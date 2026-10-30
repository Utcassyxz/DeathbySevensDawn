# 固定按键 / Controls

此表是原型的按键约定。后续新增功能遵守此表；更改按键时必须同步更新本表与界面提示。

| 按键 | 唯一职责 / Purpose |
|---|---|
| WASD | 移动 / Move |
| E | 与面前对象互动；对话/梦境继续；赠物后拾取 / Interact, advance dialogue, take offered item |
| R | 将正在查看的线索主动收录 / Record the inspected evidence |
| Esc | 关闭当前页面、退出对话；游戏中暂停；开场跳过 / Back, pause in world, skip opening |
| J | 打开日记 / Open journal |
| B | 打开线索板 / Open evidence board |
| I | 打开背包 / Open inventory |
| T | 望远镜检查页中开始观测 / Observe through telescope |
| 方向键 | 日记翻页、线索板焦点 / Journal pages, board focus (不移动角色) |
| Space | 在线索板选中焦点条目 / Select focused board record |
| Enter | 主菜单开始；在线索板提交待证连线或推论 / Start game; commit board connection |
| Tab | 线索板切换筛选 / Cycle board filter |
| Delete | 删除选中的待证线 / Erase tentative link |
| Backspace | 清空线索板选择 / Clear board selection |
| ~，首次输入 1997 | 开发面板 / Developer console |

J、B、I 只负责打开，统一用 Esc 关闭。E 不再兼任关闭；Space、Enter 不再兼任对话继续。鼠标按钮与键盘操作共享同一行为。

## 邪教线前置

第一轮第一天：房内敲木声 → 出门看见 401 钉板 → 走近住户按 E，完成警告对话。

第二天：先到纸条前按 E、R 主动收录，再走到 401 门前按 E 敲门。缺少第一天对话或第二天纸条，门不会赠物。近景对话结束后，小三角从门缝落下；按 E 拾取后才进背包。Esc 离开不会自动拾取，可在第二天再次敲门取回。

背包按稳定物品 ID 保存，锡纸三角不消耗、不重复领取。当前作为关键道具跨轮回保留；「新游戏／回到初始状态」会清空背包与路线前置。这里只建立结局入口条件，没有实现完整邪教结局。
