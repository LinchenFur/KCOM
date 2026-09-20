# Sync Lifecycle Hardening Specification

## ADDED Requirements

### Requirement: Player entity indexes remain stable while connected

服务端 SHALL 为新连接分配当前未占用的 0–15 索引；移除玩家 SHALL 正确返回移除结果，且不得改变其他在线玩家索引。

#### Scenario: Reconnect after a middle player leaves

- **WHEN** 索引 1 的玩家离开而索引 0 和 2 仍在线
- **THEN** 下一个玩家 SHALL 复用空闲索引 1，索引 0 和 2 SHALL 保持不变

### Requirement: Cross-map synchronization is isolated

服务端 SHALL 只向初始化阶段为 Ready 且地图与发送者一致的玩家转发游戏同步事件。

#### Scenario: Recipient is still loading

- **WHEN** 发送者已经产生位置或实体事件而接收者尚未完成当前地图初始化
- **THEN** 接收者 SHALL 不收到该事件

### Requirement: Incompatible protocol versions stop initialization

客户端 SHALL 在认证响应的版本缺失或不等于内部协议版本时停止本次初始化。

#### Scenario: Version mismatch

- **WHEN** 服务器返回不兼容版本
- **THEN** 客户端 SHALL 显示更新提示，且 SHALL NOT 发送地图加载命令或 bootstrap 探测
