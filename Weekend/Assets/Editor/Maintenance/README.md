# 资源维护

保留了原先制作场景、鱼缸、窗景、音画和教程素材的工具。只有需要重新生成对应资产时才运行；历史验证结果在 docs/archive 与 docs/validation。

BuildRooftop.Build 调用 RooftopArt 生成原创像素底图，创建或重建 Rooftop.unity 并加入 Build Settings。重建会替换天台生成物，手工修改应先备份或纳入制作工具。仅在需要重建时运行；编辑器必须停止播放且当前场景无未保存修改，避免覆盖正在制作的天台。
