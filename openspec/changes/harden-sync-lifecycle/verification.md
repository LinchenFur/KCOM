# 验证记录

## 自动化与构建

- `dotnet run --project tests/LanguageSupport/LanguageSupport.csproj -c Release`：342 项检查通过。
- 新增检查：中间玩家退出后索引复用且不重新编号其他玩家；加载中或不同地图玩家不接收位置同步；认证版本不匹配时不发送地图命令或 bootstrap 探测。
- `dotnet build KCOM.sln -c Release --no-restore`：0 错误，12 个既有警告。
- `openspec validate harden-sync-lifecycle --strict`、`git diff --check`：通过；CodeGraph 已同步。

## 实机范围

- 代码修复不依赖头显，但当前 Alyx 实机启动仍被 `VRInitError_Init_HmdNotFound (108)` 阻塞，任务不宣称实机通过。
- 不修改门、按钮、物品同步协议；现有 `FIRE` 事件链路保留，只增加接收方生命周期和地图一致性过滤。
- 部署待当前 Alyx/KCOM 进程退出后进行；不强制关闭用户进程。部署时只替换构建产物，不动地图、存档和配置。
