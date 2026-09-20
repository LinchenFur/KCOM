# Design

## Decisions

- SPWN 的第二个字段改为发送端实体缓存中的同步键，而不是可能与 UUID 不一致的本地 targetname。
- 接收端新增最小 `kcom_spawn` 命令：优先寻找同名实体，不存在时创建并定位；位置、输出、破坏和生命值命令均允许按 targetname 回退。
- 代理实体仍使用已有 `kcom_setlocation_nonuuid`，只增加单实体查找回退，不引入新的网络消息格式。
- FIRE 使用缓存中的稳定键，不再根据门当前旋转后的坐标重新计算 UUID。
- addon 每隔一段时间发送 `ALIV KCOM`；服务器首次收到后显示同步通道状态。断开连接时服务器发送 `kcom_remove_player`，接收端把代理移出地图。

## Non-Goals

- 不实现完整世界状态快照。
- 不改变资源库存模式或第三方兼容 API。
- 不承诺未被 KCOM 跟踪的第三方实体自动同步。

## Risks

- 远端实体名称冲突时会优先复用第一个同名实体；同步名由发送端缓存生成，正常地图实体应保持唯一。
- 真实 Alyx/VConsole 验证仍需要双机复测。
