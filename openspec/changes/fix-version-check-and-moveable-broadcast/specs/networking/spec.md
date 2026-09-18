# Networking Specification

## MODIFIED Requirements

### Requirement: Client version comparison reports both mismatch directions

客户端 SHALL 分别处理服务端版本高于或低于客户端版本的情况。

#### Scenario: Server is newer

- **WHEN** `response.version` 大于 `Response.internalVersion`
- **THEN** 客户端 SHALL 提示客户端需要更新

#### Scenario: Server is older

- **WHEN** `response.version` 小于 `Response.internalVersion`
- **THEN** 客户端 SHALL 提示服务端需要更新

### Requirement: Grace timeout broadcasts to every other client

移动实体 grace 超时 SHALL 向连接列表中所有非拥有者客户端发送 `kcom_grace` 命令。

#### Scenario: Three or more clients are connected

- **WHEN** 移动实体追踪超时
- **THEN** 每一个连接 ID 不等于 `ClientID` 的客户端 SHALL 收到一次相同的 `kcom_grace` 命令
- **AND** 拥有该实体的客户端 SHALL NOT 收到该命令
