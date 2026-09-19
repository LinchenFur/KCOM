# Addon Initialization Specification

## ADDED Requirements

### Requirement: Client starts only with a ready console connection

客户端 SHALL 在 VConsole 连接成功后才进行 WebSocket 认证，失败 SHALL 明确报告且允许停止/启动重试。

#### Scenario: Fast authentication

- **WHEN** 服务器立即返回认证结果
- **THEN** addon 启用和换图命令 SHALL 写入已连接的控制台

#### Scenario: Console unavailable or session ends

- **WHEN** VConsole 未运行或者 WebSocket 已断线
- **THEN** 客户端 SHALL 不继续无人接收的初始化探测

### Requirement: Initialization success requires a verified map report

服务端 SHALL 仅在当前初始化收到合法且 API 匹配的 MAPN 后报告成功。

#### Scenario: Missing script with existing entities

- **WHEN** 初始化实体存在但脚本未完成
- **THEN** 探测 SHALL 节流重试并报告未完成，不得认定成功

#### Scenario: Duplicate and stale initialization

- **WHEN** 重复 INIT/IENT 或旧延迟任务在重连后完成
- **THEN** 服务端 SHALL 不重复挂载计时器或向新会话应用旧任务

#### Scenario: Malformed or mismatched map report

- **WHEN** MAPN 缺字段、版本不符或者地图名非法
- **THEN** 服务端 SHALL 保持原地图，不发送成功提示

### Requirement: Map names are validated before command construction

地图输入 SHALL 经共享规则验证后才能拼接到控制台命令。

#### Scenario: Invalid input

- **WHEN** 输入空地图、扩展名、分号、引号或路径穿越
- **THEN** 程序 SHALL 拒绝输入并提示，不得发送额外控制台命令
