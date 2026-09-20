# Design

## Decisions

- `AlyxGlobalData.AddPlayer` 扫描 0–15 的第一个空闲 `Player.Index`，不压缩现有玩家编号；这样断线不会改变其他玩家实体名称。
- 增加按连接、阶段和地图检查的接收资格函数。广播前要求目标 `InitializationStage.Ready` 且 `IndexedClient.Map` 与发送者当前地图一致。
- 认证版本必须等于 `Response.internalVersion`；不匹配只记录错误并结束本次 authenticated 分支，不发送换图命令或 bootstrap 探测。
- 保留现有 FIRE、按钮和门相关协议，避免把本次生命周期修复扩大成交互协议重写。

## Non-Goals

不改变实体同步报文格式，不增加新地图机制，不宣称实机多人验收完成。
