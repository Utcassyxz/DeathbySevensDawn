# 2026-11-03 · Unity 启动崩溃修复

原工程 Weekend 使用 Unity 6000.6.7f1。两次启动在脚本编译成功之后、第二次 MonoManager ReloadAssembly 期间发生 native crash，托管栈指向 Microsoft.Unity.VisualStudio.Editor.VisualStudioEditor.TryGetInstallationForPath，经 CodeEditor.Register 和 VisualStudioIntegration 静态初始化进入。

从 Packages/manifest.json 移除可选 com.unity.ide.visualstudio 2.0.28，并同步 packages-lock.json。Unity 自动更新其余测试依赖深度，未替换版本。修改前两个包文件备份在本机工作区 unity-startup-backup-2026-11-03。未重建工程、删除 Library、迁移 Unity 版本或修改场景/剧情/存档。

验证：从 Unity Hub 打开原 Weekend，编辑器完成启动并加载 Assets/Apartment/LivingRoom.unity；进入 Play 后显示七日主菜单，未复现原 Visual Studio 集成崩溃，未见 C# 编译错误。仅验证启动及主菜单，不声称重新运行全部剧情验收。

限制：Visual Studio 的自动项目同步和编辑器联动暂时停用，普通 C# 编译和游戏运行仍正常。该电脑另有 Windows code integrity policy (Smart App Control / WDAC) 拦截 Burst JIT 缓存 DLL 的警告；本次未更改 Windows 安全设置，警告仍在，当前未阻止场景和主菜单运行。两者是否存在共同环境原因，尚未确认。
