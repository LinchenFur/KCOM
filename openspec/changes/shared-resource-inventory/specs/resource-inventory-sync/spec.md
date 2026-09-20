# Resource Inventory Sync Specification

## ADDED Requirements

### Requirement: Resource changes are reported as snapshots

addon SHALL 在玩家资源快照变化时报告有效的弹药和树脂计数；无效或缺字段快照 SHALL 被服务端忽略。

#### Scenario: Resource snapshot

- **WHEN** 玩家弹药或树脂计数发生变化
- **THEN** addon SHALL 发送包含手枪弹匣、冲锋枪弹匣、霰弹和树脂的 `RESC` 快照

### Requirement: Independent inventory is the default

服务器 SHALL 默认保持每名玩家独立的资源库存，不因收到其他玩家快照而修改本地资源。

#### Scenario: Independent mode

- **WHEN** 服务器共享选项关闭
- **THEN** 资源快照 SHALL NOT 发送共享资源回写命令

### Requirement: Shared inventory is server-authoritative

服务器 SHALL 在共享选项开启时维护一个资源池；首个有效快照建立资源池，之后按玩家快照差值更新，并向已完成当前地图初始化的玩家回写同一绝对值。

#### Scenario: Shared mode

- **WHEN** 玩家拾取、消耗或存取资源导致快照变化
- **THEN** 服务器 SHALL 更新共享池并向同地图 Ready 玩家发送 `kcom_setresources`

### Requirement: Resource write-back does not echo

addon SHALL 识别服务器回写并抑制由回写产生的重复快照。

#### Scenario: Write-back

- **WHEN** 客户端收到共享池的绝对值回写
- **THEN** 客户端 SHALL 更新本地资源且 SHALL NOT 把同一次回写再次作为增量发送
