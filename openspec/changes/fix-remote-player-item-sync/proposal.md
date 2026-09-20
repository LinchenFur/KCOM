# Proposal

## Why

实机双机测试显示双方能完成同图初始化，但远端玩家代理不可见，拾取或生成的物品在另一端不跟随。当前物品 SPWN 使用本地实体名，而后续 PHYS 使用 UUID；远端实体没有对应 UUID 缓存，导致后续位置命令被静默丢弃。

## What Changes

- 使用稳定同步名创建远端物品，并在接收端复用已存在的实体。
- UUID 缓存查找失败时回退到实体 targetname，支持动态生成实体。
- 代理位置命令增加 FindByName 回退，减少远端头部/手部实体因查找 API 返回空表而不移动的情况。
- 修正门/触发器输出在实体移动后重新生成 UUID 导致的失配。
- 增加游戏同步心跳和退出清理，避免“WebSocket 仍连接但游戏脚本已经停止”无法判断，以及退出玩家代理残留。
- 保持现有协议字段和同图、已初始化过滤规则不变。

## Impact

- `AlyxGamemode/AlyxGamemode.cs`
- `WorkshopAddon/game/hlvr_addons/kiwimp_alyx/scripts/vscripts/kcom_interval.lua`
- `tests/LanguageSupport/Program.cs`
