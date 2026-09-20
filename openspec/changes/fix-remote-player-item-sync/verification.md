# Verification

- `dotnet run --project tests/LanguageSupport/LanguageSupport.csproj -c Release`：387 项检查通过。
- `dotnet build KCOM.sln -c Release --no-restore`：0 错误，1 个既有 Open.NAT NU1701 警告。
- `git diff --check`：通过。
- 尚未完成双机实机复测。

