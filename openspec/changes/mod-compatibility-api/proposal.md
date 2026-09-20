# Proposal

## Why

KCOM 当前只对原版 Alyx 实体、武器和资源做了硬编码同步；其它模组即使能加载地图，也无法声明自己的武器、资源和事件同步规则。提供稳定、受限的兼容 API，可以让模组作者自行接入，而不必修改 KCOM 核心。

## What Changes

- 增加 KCOM Lua 兼容 API：模组注册实体类别、资源字段和扩展事件。
- 增加带模组命名空间的扩展事件协议，并只转发给同地图、已完成初始化的玩家。
- 增加服务器端扩展事件白名单和大小限制，拒绝任意控制台命令转发。
- 提供武器/地图兼容示例和 API 文档；原版同步逻辑保持不变。

## Capabilities

### New Capabilities

- `mod-compatibility-api`：第三方模组安全声明和同步扩展接口。

### Modified Capabilities

- 无。

## Impact

影响 addon Lua 脚本、AlyxGamemode 扩展事件解析和 README 文档。不新增外部依赖，不自动下载第三方 Workshop addon，不承诺任意模组无需适配即可兼容。
