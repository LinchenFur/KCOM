# Localization Specification

## ADDED Requirements

### Requirement: Main UI is Chinese

KCOM 主窗口 SHALL 使用简体中文显示面向用户的菜单、按钮、标签、状态栏和帮助文字。

#### Scenario: User opens the main window

- **WHEN** KCOM 启动并显示主窗口
- **THEN** 用户可见控件 SHALL 显示中文文本
- **AND** VConsole 命令、地图名和插件标识 SHALL 保持原样

### Requirement: Runtime user messages are Chinese

客户端、服务器和内置 Lua gamemode 面向玩家的常用提示 SHALL 使用简体中文。

#### Scenario: Client connects or receives a status

- **WHEN** 客户端连接、断开、认证失败或收到服务器状态
- **THEN** 面向用户的提示 SHALL 使用中文
- **AND** JSON type、命令名和协议字段 SHALL 不变

#### Scenario: Campaign checkpoint runs

- **WHEN** campaign checkpoint、等待玩家或重生提示显示
- **THEN** 游戏内提示 SHALL 使用中文
- **AND** 传输命令 SHALL 保持可执行格式
