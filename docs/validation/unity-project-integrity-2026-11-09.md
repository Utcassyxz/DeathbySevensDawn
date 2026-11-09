# 工程引用检查与已确认技能安装 · 2026-11-09

用户明确确认来源后，已安装 gamedev-skills/awesome-gamedev-agent-skills 中的 unity-csharp-scripting 到 Codex 技能目录，下一轮对话自动可用。安装后读取指导并审阅附带检查器：仅标准库、只读工程文件，不执行网络请求或修改工程。

检查器在工作副本和原 Unity 仓库均复现 4 个 missing-script，位于 DefaultVolumeProfile.asset。核查为早期遗留的 OutlineVolumeComponent、TestAnimationCurveVolumeComponent、OasisFogVolumeComponent、TestVolume；另有 5 个无脚本的 Unity 编辑器测试子资源。

9 个条目均未在主 VolumeProfile 的 components 列表或其他 fileID 字段中被引用。已保存清理前备份，只删除这 9 个孤立的序列化子资源。主 Profile 与 19 个实际引用组件保持原文不变；Profile 的 .meta / GUID 不变，所有其余有效子资源保持原文不变。

清理后经过 Unity 重新导入及非开发 Windows 64 位构建，严格资源检查通过：646 个工程文件，0 错误、0 警告。没有新增游戏依赖或改写未知工程设置。该检查不能替代剧情/战斗测试；本轮既有 371、164、3 项逻辑验收继续单独列示。

最终构建：project-integrity-player-build.log，Build Finished, Result: Success，退出码 0。交付包共 227 个文件，全部 CRC 通过。原仓库随提交同步后再次执行同一严格检查，记录保存于 outputs/Neighbor-0.3.3/original-project-integrity-final.json。

安装文件哈希、移除子资源及原文备份分别保存于 installed-skill.json、volume-cleanup.json、DefaultVolumeProfile-before-cleanup.asset。本次仅提交清理后的 Profile 及记录，不将第三方技能代码放入游戏仓库。
