# 验证记录

- `dotnet run --project tests/LanguageSupport/LanguageSupport.csproj -c Release`：367 项检查通过。
- 新增检查：合法 HEAD/HAND/PHYS/MAPN/RESC/SPWN/CMND、截断报文、空格归一化、NaN、Infinity、负资源、危险字符和缺少 KCOM 尾标记。
- `Packet.IsValid()` 现在在业务分支访问字段前校验参数数量和数值；SPWN addon 输出已补齐 `KCOM` 尾标记。
- 自动化检查不等于真实 Alyx VConsole 验证；仍需实机确认自定义实体名称和资源事件。
