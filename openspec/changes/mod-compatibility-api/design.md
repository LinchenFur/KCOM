# Design

## Context

现有同步通过固定 `PacketType` 和 `kcom_interval.lua` 事件输出工作；插件生命周期可扩展桌面端，但游戏内 addon 没有第三方模组注册入口。

## Goals / Non-Goals

**Goals:**

- 让第三方 addon 在初始化后注册自定义同步实体和资源字段。
- 提供受限的事件传输，不允许插件借 API 向远端注入任意控制台命令。
- 保持原版协议和旧 addon 的兼容性。

**Non-Goals:**

- 不自动推断自定义武器的弹道、动画或脚本状态。
- 不自动订阅或下载第三方 Workshop 内容。
- 不把任意字符串变成服务器命令。

## Decisions

- Lua API 使用全局 `KCOM_RegisterCompatibility`，注册 `namespace`、事件名、实体 class 和资源字段；命名空间/名称只允许 ASCII 字母、数字、下划线、连字符。
- 游戏输出 `XEVT <namespace> <event> <payload> KCOM`；服务器验证字段、长度（每字段 64、payload 1024 字节）后转发为 `kcom_compat_event <namespace> <event> <payload>`，客户端 addon 再交给注册回调。
- 仅把事件当数据转发；payload 不允许分号、换行或 NUL，客户端回调自行处理，不执行 payload。
- 自定义实体注册只扩展扫描白名单；模型、生成命令和资源仍由第三方 addon 自己提供。
- API 版本为 1，注册错误输出 `KCOM_COMPAT_ERROR`，不会阻止基础 KCOM 初始化。

## Risks / Trade-offs

- 自定义模组必须在 KCOM interval 脚本之后注册或重复注册，KCOM 才能收到其事件；文档提供初始化顺序。
- 客户端缺少第三方 addon 时只会丢弃扩展事件，并保留 KCOM 基础同步。
- 服务器不验证 payload 语义，只做格式和安全限制。

## Migration Plan

旧模组无需修改。第三方模组逐步调用兼容 API 并为所有玩家安装相同 addon；关闭兼容事件即可回滚。
