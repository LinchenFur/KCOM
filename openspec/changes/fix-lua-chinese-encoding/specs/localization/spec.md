# Localization Specification

## MODIFIED Requirements

### Requirement: Runtime user messages are Chinese

客户端、服务器和内置 Lua gamemode 面向玩家的常用提示 SHALL 使用简体中文，并正确保留 Unicode 字符。

#### Scenario: Lua status contains Chinese

- **WHEN** 内置 Lua 向服务器或客户端发送包含中文的状态提示
- **THEN** 日志和游戏内提示 SHALL 显示原始中文字符
- **AND** 不得将中文字符替换为 `?`
- **AND** JSON type、命令名和协议字段 SHALL 不变
