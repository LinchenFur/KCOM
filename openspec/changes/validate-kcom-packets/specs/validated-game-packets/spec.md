# Validated Game Packets Specification

## Purpose

为游戏到 KCOM 的同步报文提供一致的结构和数值边界检查，确保截断、空参数、非法数字和危险尾部不会进入实体同步处理，同时保持合法报文的现有行为。

## ADDED Requirements

### Requirement: Packets are structurally validated before dispatch

KCOM SHALL 在访问报文字段前验证已知报文的类型、参数数量和 `KCOM` 尾标记；无效报文 SHALL 被丢弃。

#### Scenario: Truncated packet

- **WHEN** 报文缺少坐标、实体名或尾标记
- **THEN** KCOM SHALL 不访问缺失字段、不发送同步命令且不抛出未处理异常

### Requirement: Numeric packet fields are bounded and invariant

坐标、角度、生命值、API 版本和资源字段 SHALL 使用固定文化解析并拒绝非法、NaN、Infinity 或负资源值。

#### Scenario: Invalid numeric value

- **WHEN** 报文包含字母、NaN、Infinity 或负资源数量
- **THEN** KCOM SHALL 丢弃该报文且 SHALL NOT 修改地图、资源池或实体状态

### Requirement: Valid packets retain existing behavior

合法的 HEAD、HAND、PHYS、MAPN、RESC、FIRE 和 SPWN 报文 SHALL 继续进入原有同步逻辑。

#### Scenario: Valid packet

- **WHEN** 报文参数完整、数值有效且尾标记为 `KCOM`
- **THEN** KCOM SHALL 按现有协议处理该报文
