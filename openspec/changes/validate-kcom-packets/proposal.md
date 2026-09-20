# Proposal

## Why

当前 KCOM 收到游戏 VConsole 报文后只检查报文类型，数组长度、浮点数和整数内容由后续处理代码直接假设。损坏或恶意报文可能触发数组越界和数字解析异常，导致同步处理停止。

## What Changes

- 为 KCOM 报文统一增加结束标记、参数数量、浮点数/整数和非负值校验。
- 统一处理多余空格，拒绝空参数和截断报文。
- 修正 SPWN 报文缺少 `KCOM` 尾标记的问题。
- 为有效和无效报文增加回归检查，不改变有效同步报文的语义。

## Capabilities

### New Capabilities

- `validated-game-packets`：游戏报文的结构和数值边界验证。

### Modified Capabilities

- 无。

## Impact

影响 AlyxGamemode 报文解析和 addon 的 SPWN 输出；不新增依赖，不改变门、物品、资源或第三方兼容 API 的协议语义。
