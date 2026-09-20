# 验证记录

- `dotnet run --project tests/LanguageSupport/LanguageSupport.csproj -c Release`：352 项检查通过。
- 新增检查：兼容事件注册、同地图 Ready 转发、发送者不回传、未注册事件拒绝、危险 payload 拒绝，以及 Lua API 静态存在性。
- `dotnet build KCOM.sln -c Release --no-restore`：0 错误，1 个已有 Open.NAT 兼容性警告。
- `openspec validate mod-compatibility-api --strict`、`git diff --check`：通过；CodeGraph 已同步。

## 限制

这不是任意模组自动兼容器。第三方模组必须在每台机器安装相同 addon，并主动调用注册/发送 API；payload 只作为数据交给回调。真实武器开火、弹道、动画和多人竞争仍需具体模组逐项验收。
