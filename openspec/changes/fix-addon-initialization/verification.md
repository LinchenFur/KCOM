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

## 实机启动尝试（待解除阻塞）

- 已通过本机 Steam 启动 Alyx，附加参数仅为 `-console -vconsole`，未添加强制英文参数。
- 实际游戏错误窗口显示：`VRInitError_Init_HmdNotFound: Hmd Not Found (108)`。SteamVR 未检测到头显，游戏没有进入地图。
- 已尝试启动安装目录中的 KCOM；进程存在，但自动化工具未取得可操作窗口，尚未执行客户端连接或换图。
- 此次尝试不能计作首次加载、换图、重连或多人测试通过；任务 6 保持未完成。
- 继续条件：用户连接头显，确认 SteamVR 能识别并进入 Alyx；双机验收另需第二台运行相同版本模组的电脑。
- 环境就绪后依次检查：有效地图回报后才提示成功、换图后重新初始化、停止/启动能重新连接、双机头手及物体同步。记录实际日志，不以文件存在或回环检查替代结果。
