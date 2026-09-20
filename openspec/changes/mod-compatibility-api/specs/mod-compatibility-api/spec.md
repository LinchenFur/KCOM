# Mod Compatibility API Specification

## Purpose

为第三方 Half-Life: Alyx addon 提供稳定、受限且可验证的 KCOM 同步扩展入口，使自定义地图、武器和资源能够声明自己的事件，而不要求修改 KCOM 核心协议实现。

## ADDED Requirements

### Requirement: Third-party addons can declare compatibility data

KCOM SHALL 提供版本为 1 的游戏内 API，允许 addon 注册命名空间、扩展事件、实体 class 和资源字段；非法名称 SHALL 被拒绝且不得影响基础初始化。

#### Scenario: Valid registration

- **WHEN** addon 使用合法命名空间和名称注册兼容能力
- **THEN** KCOM SHALL 保存注册信息并允许该 addon 发送对应扩展事件

#### Scenario: Invalid registration

- **WHEN** addon 使用空名称、控制字符或超长名称
- **THEN** KCOM SHALL 拒绝注册并输出诊断，不得执行注册内容

### Requirement: Extension events are validated and forwarded

服务器 SHALL 验证扩展事件的命名空间、事件名和 payload 长度及字符限制，然后只转发给同地图且已完成初始化的玩家。

#### Scenario: Valid weapon event

- **WHEN** 已注册 addon 发送合法开火或资源事件
- **THEN** 同地图 Ready 玩家 SHALL 收到对应兼容事件，发送者 SHALL NOT 收到自己的回传

#### Scenario: Unsafe event

- **WHEN** 事件包含换行、NUL、分号或超出长度限制
- **THEN** 服务器 SHALL 丢弃事件且 SHALL NOT 生成控制台命令

### Requirement: Compatibility API does not replace core synchronization

第三方 API SHALL 独立于原版头手、实体和资源同步；兼容 addon 缺失或注册失败时，基础 KCOM 初始化 SHALL 继续工作。

#### Scenario: Missing third-party addon

- **WHEN** 某客户端没有对应第三方 addon
- **THEN** KCOM SHALL 丢弃该客户端无法处理的兼容事件，但原版同步 SHALL 继续
