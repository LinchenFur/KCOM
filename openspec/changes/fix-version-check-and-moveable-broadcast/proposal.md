# Proposal

## Why

客户端认证响应中的版本比较条件重复，导致服务端版本较旧时不会显示正确提示；移动实体 grace 超时只通知第一个其他客户端，三人及以上联机时会留下未同步玩家。

## What Changes

- 修正客户端版本比较的第二个分支。
- 让移动实体 grace 命令广播给所有其他客户端。
- 增加静态验证并保持协议和数据格式不变。

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `networking`：修正客户端版本提示和多人实体 grace 广播。

## Impact

- `KiwisCoOpMod/ClientProgram.cs`
- `AlyxGamemode/Moveable.cs`
- 不新增依赖，不改变网络消息格式。
