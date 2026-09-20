# Verification

- `dotnet run --project tests/LanguageSupport/LanguageSupport.csproj -c Release`：390 项检查通过。
- `dotnet build KCOM.sln -c Release --no-restore`：0 错误。
- 日志文件位置会在 KCOM 启动时显示，并由 `ActivityLog.CurrentFilePath` 验证可写入。
