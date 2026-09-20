# 验证记录

## 自动化与构建

- `dotnet run --project tests/LanguageSupport/LanguageSupport.csproj -c Release`：348 项检查通过。
- 新增检查：RESC 快照解析、负数拒绝、共享池首个快照、后续差值更新、独立模式不回写，以及 Lua 资源事件和回写命令存在。
- `dotnet build KCOM.sln -c Release --no-restore`：0 错误，1 个 Open.NAT 兼容性警告。
- `openspec validate shared-resource-inventory --strict`、`git diff --check`：通过；CodeGraph 已同步。

## 功能范围

- 默认设置为“独立资源库存”，保持原有行为。
- 打开“选项”→“服务器：共享资源库存”后，服务器维护弹药弹匣/霰弹/树脂池并向同地图 Ready 玩家回写。
- 同步单位：手枪弹匣数、冲锋枪弹匣数、霰弹颗数、树脂数；generic pistol 未纳入共享池。
- 资源回写会转换为游戏资源命令需要的数量：手枪弹匣×10、冲锋枪弹匣×30、霰弹按颗数、树脂按点数。

## 尚未验收

- 自动化测试没有进入 Alyx 实体事件，因此没有证明真实游戏事件字段、`hlvr_setresources` 在当前游戏版本中的实际行为。
- 未验证多人同时拾取同一资源的竞争顺序，也未验证跨地图或服务器重启后的库存策略；共享池只在服务器生命周期内存在。
- 门、按钮、物品位置同步协议没有改动。
