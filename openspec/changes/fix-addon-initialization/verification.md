# 验证记录

## 自动化与构建

- `dotnet run --project tests/LanguageSupport/LanguageSupport.csproj -c Release`：310 项检查通过。包含实际 TCP/WebSocket 回环、客户端连接顺序、停止/重启、Lua 引导桩、重复/过期初始化和地图输入校验。
- `dotnet build KCOM.sln -c Release --no-restore`：0 错误，12 个既有 Open.NAT 兼容性及可空引用警告。
- `openspec validate fix-addon-initialization --strict` 和 `git diff --check`：通过。
- CodeGraph 已同步本次代码修改。

## 本地部署

- 已更新 Alyx 安装目录下的主程序、Core 和 AlyxGamemode 程序集及两份 addon Lua 脚本，共 8 个文件；逐项 SHA256 与构建/源码一致。
- 更新前备份位于 `G:\SteamLibrary\steamapps\common\Half-Life Alyx\KCOM-backups\20260920-074817-addon-initialization`。
- 未替换地图、存档或用户配置，未启动或关闭游戏进程。

## 尚未验收

- 用户此前反馈本地模组可以运行，但不是本次改动的实机验收。
- 本次没有实际进入 Alyx 验证首次加载、换图或重连，也没有两台电脑联机测试。
- 测试中的游戏实体/计时器为桩；回环通过不能证明 Source 2 实体作用域、计时或地图资源在实机中均正常。
- 不宣称解决全部卡死、地图问题或多人同步问题；门和交互物品同步逻辑未修改。
