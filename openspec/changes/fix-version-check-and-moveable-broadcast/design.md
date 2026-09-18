# Design

## Context

客户端版本判断位于 WebSocket authenticated 响应处理路径。`Moveable.Track` 目前用 `Find` 只取一个非拥有者连接，无法覆盖多人房间。

## Goals / Non-Goals

**Goals:**

- 用正确的 `<` 分支覆盖服务端旧版本。
- 用一次序列化结果遍历发送给所有其他客户端。

**Non-Goals:**

- 不重构 WebSocket 层。
- 不改变 Response 类型或 `kcom_grace` 命令格式。

## Decisions

- 保留现有消息文本和条件结构，只把重复比较改为 `<`。
- 在 `Track` 中使用 `foreach`，过滤当前 `ClientID` 后逐个发送；这是最小且直接的广播实现。

## Risks / Trade-offs

- `[发送期间连接列表变化]` → 当前服务端已有连接生命周期管理，保持现有列表语义，不引入额外并发抽象。
