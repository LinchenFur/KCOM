# Proposal

## Why

KCOM 当前客户端界面和内置联机提示主要为英文，对中文用户不友好。需要在不改变网络协议、命令和插件 API 的前提下提供中文界面与常用状态提示。

## What Changes

- 汉化 KCOM 主窗口的菜单、按钮、标签、帮助文本和状态栏。
- 汉化客户端/服务器连接、错误、认证和常用聊天提示。
- 汉化内置 Lua gamemode 的帮助、权限、checkpoint 和重生提示。
- 保留 VConsole 命令、协议字段、地图名、插件 API 标识和日志通道内部名称。

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `localization`：增加中文用户界面和运行时提示。

## Impact

- 主要影响 `KiwisCoOpMod/UserInterface.Designer.cs`、`UserInterface.resx`、`ClientProgram.cs`、`ServerProgram.cs` 和 `base_scripts`。
- 不新增依赖，不改变网络数据结构。

## Follow-up: Game language support

按用户追加要求，修复原项目 Issue #1：不再依赖英文入场日志；使用游戏脚本就绪标记；VConsole 文本使用 UTF-8 并正确处理完整帧长度、分段读取和 EOF；同步数字采用不变区域格式。不更改游戏语言设置，不引入翻译框架。更新桌面程序和本地 addon 脚本时保留地图与用户配置。
