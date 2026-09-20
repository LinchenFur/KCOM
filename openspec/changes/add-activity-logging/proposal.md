# Proposal

## Why

当前窗口日志主要记录状态消息，无法完整回答用户做了什么、VConsole 是否收到游戏输出、服务端是否收到游戏同步报文以及客户端执行了哪些命令。

## What Changes

- 新增按会话文件保存的操作日志。
- 记录 UI 启停、设置、地图、插件和连接操作。
- 记录 VConsole 连接、游戏输出和发往游戏的命令。
- 记录 WebSocket 收发、KCOM 同步报文、无效报文和异常。
- 不记录客户端密码。

## Impact

日志默认保存到 `%LOCALAPPDATA%\\KCOM\\logs`，不会改变同步协议。
