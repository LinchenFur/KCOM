# Verification

- `dotnet run --project tests\LanguageSupport\LanguageSupport.csproj -c Release`
  - 结果：`PASS: 392 language support checks`
- Lua 检查：自动测试确认 `KCOM_ScanDynamicProjectiles`、NPC、投射物类表和 `player_hurt` 反馈命令存在。
- `dotnet build KCOM.sln -c Release --no-restore`
  - 结果：0 errors，保留仓库原有 12 个 warnings
- 需要实机确认：两台客户端在同一地图 Ready 后，打开门/按钮/触发器、拾取物品，并切换共享资源库存，检查第二台客户端和日志中的 `FIRE`、`PHYS`、`RESC`。
