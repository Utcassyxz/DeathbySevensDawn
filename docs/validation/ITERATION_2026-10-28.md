# 2026-10-28 迭代验收

同步基线：740a56b。Unity 6000.6.7f1，正常 DX11 编辑器。

## 结果

PASS: seven-day boundary, sleep, death, aging, knowledge, journal, ending and save persistence. Original save restored.

PASS: Window E/Esc, 5-second deferred notices, final sleep time, J read/save, second-night telescope, retained Days/read state, and resumed movement. Original save restored.

PASS: player moved 3.153327 units under held D input. 8 physical door triggers switched scenes and placed player at expected arrivals. Days remained 5 across all room switches, then reset to 1.

人工查看：四幕梦境、卧室醒来、日记更新提示、未读计数、窗外观察日记、第二夜望远镜插图日记、时间提示。中文文字完整显示。

测试时曾发生 Unity 热导入内部崩溃；重启后编译及运行通过，现有弃用警告保留。回归中先等待传送位置同步再发送按键。测试截图、个人进度、Library 和日志不上传。
