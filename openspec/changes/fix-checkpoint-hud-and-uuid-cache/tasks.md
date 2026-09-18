# Tasks

## 1. Lua 修复

- [x] 1.1 将 checkpoint HUD 超时判断改为读取 `checkpoint_using_hud`，并确认该键的设置、读取和清理引用一致
- [x] 1.2 将 `KCOM_EntitySyncSpecific` 的缓存去重遍历改为 `pairs`，并确认 UUID 与非 UUID 两种写入方式仍被覆盖

## 2. 验证

- [x] 2.1 运行静态引用检查，确认不存在 `checkpoint_checkpoint_using_hud` 或 `ipairs(KCOM_ENTCACHE)`
- [x] 2.2 运行最小 Lua 表键行为验证，并运行 `openspec validate fix-checkpoint-hud-and-uuid-cache --strict`；当前环境无 Lua 解释器，因此仅完成目标断言和静态验证
