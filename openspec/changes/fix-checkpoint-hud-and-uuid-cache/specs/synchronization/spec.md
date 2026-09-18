# Synchronization Specification

## MODIFIED Requirements

### Requirement: Checkpoint HUD state uses one consistent key

系统 SHALL 使用 `checkpoint_using_hud` 记录 checkpoint HUD 是否正在显示。

#### Scenario: HUD timeout is reached

- **WHEN** checkpoint HUD 已显示超过 5 秒
- **THEN** 系统 SHALL 根据 `checkpoint_using_hud` 进入清理分支
- **AND** 系统 SHALL 向玩家发送清空 HUD 的命令
- **AND** 系统 SHALL 将 `checkpoint_using_hud` 设置为 `false`

### Requirement: Entity cache de-duplication supports UUID keys

实体同步 SHALL 遍历 `KCOM_ENTCACHE` 的实际键值，而不能只遍历连续整数索引。

#### Scenario: UUID mode is enabled

- **WHEN** `KCOM_USE_UUIDS` 为 `true`
- **AND** 实体已按 UUID 字符串键写入 `KCOM_ENTCACHE`
- **AND** `KCOM_EntitySyncSpecific` 再次处理同一个实体
- **THEN** 系统 SHALL 识别该实体已经缓存
- **AND** 系统 SHALL NOT 重复注册其输出回调

#### Scenario: Non-UUID mode remains enabled

- **WHEN** `KCOM_USE_UUIDS` 为 `false`
- **AND** 实体按连续数组索引写入 `KCOM_ENTCACHE`
- **THEN** 系统 SHALL 继续识别已缓存实体
- **AND** 现有非 UUID 同步行为 SHALL 保持不变
