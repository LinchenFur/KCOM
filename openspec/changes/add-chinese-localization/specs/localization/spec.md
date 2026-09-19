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

### Requirement: Game initialization is language independent

KCOM SHALL 通过固定协议标记而不是本地化入场提示初始化联机，且不强制修改游戏语言。

#### Scenario: Player ready in any game language

- **WHEN** 玩家在非英文或英文游戏中进入地图并完成认证
- **THEN** 有效本地玩家 SHALL 触发 KRDY/INIT/IENT 初始化链
- **AND** 自然语言日志 SHALL NOT 触发初始化
- **AND** 换图和新客户端会话 SHALL 可再次初始化而不累计计时器

### Requirement: Console transport preserves Unicode and framing

KCOM SHALL 按 UTF-8 字节长度收发控制台文本，并按完整报文处理流。

#### Scenario: Long multilingual output crosses TCP reads

- **WHEN** 中文、日文或俄文 PRNT 报文超过 255 字节并跨越多次读取
- **THEN** 文本 SHALL 完整保留，紧随的 KCOM 标记 SHALL 被独立识别
- **AND** EOF、截断或非法长度 SHALL 终止读取而不是忙循环

#### Scenario: Numeric synchronization in comma-decimal locales

- **WHEN** 主机区域使用逗号作为小数分隔符
- **THEN** 坐标和角度 SHALL 仍按点号解析及输出
