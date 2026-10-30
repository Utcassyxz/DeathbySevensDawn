# 固定按键 / Controls

新旅程开局没有日记。傍晚到书桌写完第一篇后解锁 J；望远镜首次观测后解锁 R 调查与 B 连线。电视不提供摘录。设置在主菜单和暂停页进入，Esc 返回。

此表是原型的按键约定。后续新增功能遵守此表；更改按键时必须同步更新本表与界面提示。

| 按键 | 唯一职责 / Purpose |
|---|---|
| WASD | 移动 / Move |
| E | 与面前对象互动（冰箱、沙发、书桌）；对话继续；赠物后拾取 / Interact, advance dialogue, take offered item |
| R | 调查教学解锁后，将正在查看的线索主动收录 /  Record the inspected evidence |
| Esc | 关闭当前页面、退出对话；游戏中暂停；开场跳过 / Back, pause in world, skip opening |
| J | 获得日记后打开 /  Open journal |
| B | 首次观测教学后打开线索板 /  Open evidence board |
| I | 打开背包 / Open inventory |
| T | 望远镜检查页中开始观测 / Observe through telescope |
| 方向键 | 日记翻页、线索板焦点 / Journal pages, board focus (不移动角色) |
| Space | 在线索板选中焦点条目 / Select focused board record |
| Enter | 主菜单开始、梦境继续；在线索板提交待证连线或推论 / Start game, advance opening dream, commit board connection |
| Tab | 线索板切换筛选 / Cycle board filter |
| Delete | 删除选中的待证线 / Erase tentative link |
| Backspace | 清空线索板选择 / Clear board selection |
| ~，首次输入 1997 | 开发面板 / Developer console |

J、B、I 只负责打开，统一用 Esc 关闭。E 不再兼任关闭；Space 不再兼任对话继续；开场梦境按用户指定使用 Enter，对话仍为 E。鼠标按钮与键盘操作共享同一行为。

## 邪教线前置

第一轮第一天：房内敲木声 → 出门看见 401 钉板 → 走近住户按 E，完成警告对话。

第二天：先到纸条前按 E、R 主动收录，再走到 401 门前按 E 敲门。缺少第一天对话或第二天纸条，门不会赠物。近景对话结束后，小三角从门缝落下；按 E 拾取后才进背包。Esc 离开不会自动拾取，可在第二天再次敲门取回。

背包按稳定物品 ID 保存，锡纸三角不消耗、不重复领取。当前作为关键道具跨轮回保留；「新游戏／回到初始状态」会清空背包与路线前置。这里只建立结局入口条件，没有实现完整邪教结局。


## 第一次调查

望远镜页按 T 观察，再按 R 收录。Esc 返回后按 B，选择旧观测记录与望远镜，先留下待证连线，再得出结论。Enter 第一次提交待证，第二次确认支持的推论。
