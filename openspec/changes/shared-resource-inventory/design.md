# Design

## Decisions

- 协议使用 `RESC <energygun_magazines> <rapidfire_magazines> <shotgun_shells> <resin> KCOM`，数值为非负整数；不把未能由 `hlvr_setresources` 设置的 generic pistol 纳入共享池。
- addon 每次资源计数变化只发一次快照。服务器记录每个玩家的上次快照；共享模式按差值更新池，然后广播 `kcom_setresources` 绝对值。
- 第一次有效快照建立共享池；新玩家加入共享池后直接收到池值，不把自己的本地初始库存合并进去。
- addon 记录最近一次服务器回写值，回写后不再生成反向快照，避免无限回声。
- 共享池只在服务器生命周期内有效；服务器重启重新由第一个已初始化玩家建立。默认设置为独立库存，兼容当前行为。

## Non-Goals

不同步 generic pistol，不改世界实体拾取/生成协议，不处理跨地图或存档持久化，不宣称实机资源测试完成。
