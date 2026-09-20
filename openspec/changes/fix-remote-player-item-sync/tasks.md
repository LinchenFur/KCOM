# Tasks

## Implementation

- [x] 使用稳定同步键发送 SPWN，并在接收端实现幂等生成/定位。
- [x] 为 UUID 命令和非 UUID 代理移动增加实体名称回退。
- [x] 使用缓存稳定键转发 FIRE，修复门/触发器移动后 UUID 变化。
- [x] 增加 ALIV 心跳和断开连接时的远端代理清理。
- [x] 增加玩家 HEAD 和 SPWN 的路由回归检查。

## Verification

- [x] 运行语言/传输回归检查。
- [x] Release 编译通过。
- [ ] 双机 Alyx 实机确认玩家可见、拾取物同步、门/物理物品继续同步。
