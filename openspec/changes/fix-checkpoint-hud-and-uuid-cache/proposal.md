# Proposal

## Why

main 中 checkpoint HUD 的状态变量名不一致，导致 checkpoint 提示不会按预期清理；实体同步在 UUID 模式下使用数组遍历，可能漏掉已缓存实体并重复注册输出。两个问题都直接影响现有多人同步功能，应先用最小改动修复。

## What Changes

- 修正 checkpoint HUD 清理逻辑使用的状态键。
- 让实体缓存重复检测同时支持 UUID 字符串键和非 UUID 数组键。
- 增加针对两个修复点的静态验证和最小 Lua 行为验证。

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `synchronization`：修正 checkpoint HUD 状态清理和实体缓存去重行为。

## Impact

- 修改 `KiwisCoOpMod/base_scripts/gamemodes/campaign/checkpoints.lua`。
- 修改 `WorkshopAddon/game/hlvr_addons/kiwimp_alyx/scripts/vscripts/kcom_interval.lua`。
- 不改变网络协议、玩家数据格式、命令名称或现有 UUID 生成规则。
