# Design

## Context

报文由 VConsole 的 PRNT 行转成空格分隔的 KCOM token，再由 AlyxGamemode 按 `PacketType` 分支处理。当前 `Packet.IsValid()` 只拒绝未知类型。

## Goals / Non-Goals

**Goals:**

- 在访问 `packet.args` 或调用 `float.Parse` 前完成完整校验。
- 保持当前有效报文的 token 顺序和同步结果。
- 让无效报文静默丢弃，不使玩家同步线程退出。

**Non-Goals:**

- 不为未知第三方报文自动推断参数类型。
- 不改变扩展 API 的事件格式。
- 不将所有输入错误显示给用户，避免日志刷屏。

## Decisions

- `Packet` 负责 token 化、尾标记、参数数量和数值验证；业务分支只处理已通过验证的包。
- 坐标、角度和生命值使用 `InvariantCulture` 浮点解析，并拒绝 NaN/Infinity；资源和 API 版本使用非负整数。
- 对当前只保留兼容性的索引类报文，至少要求存在 `KCOM` 尾标记和一个有效参数；对实际访问参数的报文使用精确参数数量。
- SPWN 输出补上 `KCOM`，使其与其它协议行一致。

## Risks / Trade-offs

- 旧版 addon 若发送没有尾标记的 SPWN 将被拒绝；当前仓库 addon 同步修正，旧 addon 需要更新。
- 未知第三方报文仍会被拒绝，第三方应使用兼容 API。
