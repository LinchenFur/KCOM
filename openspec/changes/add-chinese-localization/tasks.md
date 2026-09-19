# Tasks

## 1. UI

- [x] 1.1 汉化主窗口 Designer 控件、菜单、标签、按钮和状态栏
- [x] 1.2 汉化欢迎页资源和帮助文本，同时保留启动参数与命令原文

## 2. Runtime messages

- [x] 2.1 汉化客户端、服务器和 VConsole 连接提示，不改变协议 type/command
- [x] 2.2 汉化内置 Lua 帮助、checkpoint、重生和常用状态消息

## 3. Verification

- [x] 3.1 运行 OpenSpec 严格校验、CodeGraph 同步和 Release 编译，确认无编译错误
- [x] 3.2 检查协议命令字面量未被翻译，并汇总仍保留英文的非用户文本

## 4. Game language support

- [x] 4.1 用脚本就绪协议替代英文入场日志，支持换图与重连
- [x] 4.2 修复 VConsole UTF-8、完整长度、分段读取及 EOF
- [x] 4.3 固定同步数字格式并更新无需英文启动的使用说明
- [x] 4.4 添加并通过传输、初始化、Lua 和区域格式回归测试
- [x] 4.5 Release 编译、OpenSpec 严格校验、CodeGraph 同步并安装
- [ ] 4.6 中文游戏内实测加载/换图/同步（需游戏运行环境，不以单元测试代替）

### Verification result (2026-09-19)

- `dotnet run --project tests/LanguageSupport/LanguageSupport.csproj -c Release`：174 项检查通过，包括实际回环 TCP → UTF-8 → WebSocket → JSON 转发。
- `dotnet build KCOM.sln -c Release --no-restore`：成功，0 错误；保留原有 Open.NAT/nullable 编译警告，增量编译显示的警告数可能不同。
- 安装前备份桌面文件；7 个部署文件 SHA-256 与编译输出/源脚本一致，原有 interval 脚本与仓库一致；地图、存档、配置未覆盖。
- 实际 Alyx 未运行，4.6 保持未完成。不得将模拟就绪事件、文化区域或回环测试描述为已验证各语言游戏实测。
