# Design

## Context

main 的 checkpoint 脚本设置和读取了两个不同的 HUD 状态键。实体缓存则在 UUID 模式下使用字符串键写入，但去重逻辑使用 `ipairs`，无法访问这些条目。

## Goals / Non-Goals

**Goals:**

- 修复 HUD 状态键不一致。
- 使用适用于 UUID 字符串键和数组键的遍历方式。
- 保持命令、数据结构和协议兼容。

**Non-Goals:**

- 不重写实体缓存结构。
- 不改变 UUID 算法或模板实体命令。
- 不处理语言兼容、地图支持或其他 Issue。

## Decisions

- 将 checkpoint 清理条件改为已有且实际被设置的 `checkpoint_using_hud`。
- 将实体去重循环改为 `pairs(KCOM_ENTCACHE)`；Lua 的 `pairs` 同时覆盖字符串键和数字键，改动最小。
- 不增加新的辅助函数或抽象，避免扩大同步代码的修改面。

## Risks / Trade-offs

- `[pairs 不保证遍历顺序]` → 去重逻辑只依赖实体索引是否存在，不依赖顺序，因此无影响。
- `[无游戏运行环境]` → 使用 Lua 解释器做最小表键行为验证，并通过静态检查确认唯一关键引用。
