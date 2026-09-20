# Kiwi's Co-Op Mod for Half-Life: Alyx

<a href="https://github.com/LinchenFur/KCOM"><img align="left" width="256" src="https://i.imgur.com/qIIjxCs.png"></a>

[![Releases](https://img.shields.io/github/v/tag/LinchenFur/KCOM?label=release)](https://github.com/LinchenFur/KCOM/releases)

[![Downloads](https://img.shields.io/github/downloads/LinchenFur/KCOM/total)](https://github.com/LinchenFur/KCOM/releases)

[![Workshop Subscribers](https://img.shields.io/steam/subscriptions/2739356543?label=workshop%20subscribers)](https://steamcommunity.com/sharedfiles/filedetails/?id=2739356543)

[![License](https://img.shields.io/badge/license-mit-green.svg)](https://github.com/LinchenFur/KCOM/blob/main/LICENSE.md)

[![Contributors](https://img.shields.io/github/contributors/LinchenFur/KCOM)](https://github.com/LinchenFur/KCOM/graphs/contributors)

--------

### Kiwi's Co-Op Mod for Half-Life: Alyx (KCOM) is a cooperative experience for Half-Life: Alyx.

## Authors and Maintainers

- Original author: [KiwifruitDev / original KCOM project](https://github.com/KiwifruitDev/KCOM)
- Current maintainer: [LinchenFur / current KCOM project](https://github.com/LinchenFur/KCOM)

## Features

- Simultaneous gameplay with up to 16 players
- VR hands are synced across clients
- Physics objects are synced across clients
- Trigger outputs are synced across clients
- Custom addon (gamemode/plugin) support
- Lua scripting support
- Support for Workshop maps
- Discord rich presence
- Public addon contents

## Installation

- Subscribe to the [Workshop Addon](https://steamcommunity.com/sharedfiles/filedetails/?id=2739356543) on Steam
- Download the [latest release](https://github.com/LinchenFur/KCOM/releases) from GitHub
- Extract the contents of the zip file to a safe place (e.g. a new folder on your desktop)
- Set the following launch options for Half-Life: Alyx in Steam: `-console -vconsole`
- Launch the game
- Open KCOM using `KiwisCoOpMod.exe`
- Follow instructions within the KCOM application
- KCOM is ready!

## 游戏语言支持（本分支）

- 新版本不再依赖英文 `has joined the game` 日志，启动参数使用 `-console -vconsole`，无需强制 `-language english`。游戏语言由你在 Steam 中自行选择；KCOM 界面仍为简体中文。
- **所有参与联机的电脑都需要更新桌面程序与 addon 脚本，不要混用原版客户端。** 本分支协议版本为 1，旧版本为 0；版本不匹配时请停止测试并先更新。 将 `WorkshopAddon/game/hlvr_addons/kiwimp_alyx` 中的文件合并到 Alyx 的同名目录，尤其是新增的 `scripts/vscripts/kcom_bootstrap.lua`。仅安装原版 Workshop 包没有这个脚本。更新时保留编译地图、存档和用户配置。
- 认证后程序每两秒探测本地玩家是否就绪；收到 `KRDY KCOM` 才沿用 INIT/IENT 初始化流程。进入新地图或建立新连接会重新检测，不依赖本地化提示。
- VConsole 命令/输出使用 UTF-8，长度按字节计算；坐标按点号小数格式同步。协议关键字、地图名和实体名不翻译。
- VConsole 连接失败依然意味着游戏同步不可用，不代表仅界面问题；先确认游戏已运行并关闭独立 VConsole 窗口，再启动 KCOM。

### 同步生命周期加固

### 游戏报文安全校验

KCOM 现在会在处理 VConsole 报文前检查参数数量、`KCOM` 尾标记、浮点数/整数格式、NaN/Infinity、负资源和危险控制字符。截断或异常报文会被丢弃，不会继续访问缺失字段或发送同步命令。旧版第三方 addon 如果发送不带 `KCOM` 尾标记的 `SPWN`，需要更新 addon 脚本。

### 第三方模组兼容 API（v1）

第三方 Alyx addon 可以在 `kcom_interval.lua` 加载后注册自己的同步事件：

```lua
KCOM_RegisterCompatibility("my_weapon", {
    events = {"fire", "reload"},
    entities = {"my_weapon_entity"},
    resources = {"energy"},
    handlers = {
        fire = function(payload)
            -- 只处理数据；不要把 payload 直接当控制台代码执行
        end,
    },
})
KCOM_EmitCompatibility("my_weapon", "fire", "shot_1")
```

- `namespace`、事件名和资源名只允许字母、数字、`_`、`-`，长度最多 64。
- `payload` 最多 1024 字节，不能包含换行、分号、引号、反斜杠或控制字符。
- 服务器只转发给同地图且初始化完成的玩家，不回传给发送者。
- API 只传递事件数据，不自动实现武器弹道、动画、换弹、模型生成或 Workshop 下载；其它玩家仍需安装对应 addon。

### 弹药与树脂库存

- 默认每名玩家独立保存弹药和树脂。服务器主机可在“选项”菜单打开“服务器：共享资源库存”，让手枪弹匣、冲锋枪弹匣、霰弹和树脂由服务器维护共享池。
- 共享池按玩家资源快照的变化量更新，并回写给当前地图且已完成初始化的玩家；回写不会重复计数。generic pistol 和跨服务器持久化暂不纳入。
- 资源同步仍需真实 Alyx 多人测试；自动化检查不等于游戏内资源事件验证。


- 在线玩家使用稳定的 0–15 实体索引；中间玩家退出时只回收自己的索引，不重新编号其他玩家。
- 位置、实体、触发器和交互事件只转发给已经完成当前初始化、且地图一致的玩家，避免换图或加载中串线。
- 客户端与服务器协议版本不一致（包括版本缺失）时停止本次初始化，不发送地图命令或脚本探测。

### 初始化与地图识别修复（Issue #2）

- 先连接游戏 VConsole，再向服务器认证，避免服务器快速回应时丢失首次换图命令。失败后先启动游戏、关闭独立 VConsole，再在 KCOM 点击“停止”→“启动”。
- “已请求切换地图”只表示命令已发送；收到合法地图名和匹配 API 版本的游戏回报后，才显示“联机初始化完成；已识别地图”。
- 脚本实体存在但初始化未完成时，每 10 秒重试并提示检查 addon；重复 INIT/IENT/MAPN 不重复初始化，断线或新一轮初始化使旧延迟任务失效。
- 地图栏只填地图名（例如 `mp_kiwitest`），不要填 `.vmap` / `.vpk` 文件名或控制台命令。校验通过不代表地图资源已安装或一定能加载。

### 验证范围

运行 `dotnet run --project tests/LanguageSupport/LanguageSupport.csproj -c Release` 可测试多语言编码、长报文/分段读取、EOF/取消、回环 TCP/WebSocket 转发、Lua 就绪逻辑及不同区域的初始化/坐标同步，以及快速认证、断线/停止、重复/过期初始化和非法地图输入。

这些自动化测试不等于完整游戏内验证。中文游戏内仍需验证：初始化提示与地图识别、换图后重新初始化、重新连接，以及两台机器间头手/物体同步。用户此前已反馈本地模组可以运行；本次修复的换图、重连及两台电脑联机专项实机验收仍待完成，也不保证所有地图和游戏字体均支持任意字符。

## Connecting to a Server
- Follow the [instructions](#installation) to install KCOM
- Click on the "Client" tab in the KCOM application
- Check the "Enabled" box to allow your client to connect
- Set the IP address to the *public* IP address of the remote server
- Set the port to the port of the remote server
- *(Optional)* If provided, set the password to the password of the remote server
- Set a username for your client
- Click "Connect"
- KCOM will connect to the remote server and start playing!

## Hosting a Server
- Follow the [instructions](#installation) to install KCOM
- *(Optional)* Follow the [client instructions](#connecting-to-a-server) to create a listen server
	- Note: Non-listen servers are not fully developed yet, please follow the above instructions for now
- Click on the "Server" tab in the KCOM application
- Check the "Enabled" box to host a server
- *(Optional)* Set a password for the server, make sure to provide it to peers
- Type in a map name to load initially (use `mp_kiwitest` for testing)
- Ensure that the port is forwarded, try UPnP mapping via "File" > "Forward Port via UPnP"
- Click on the "Start" button to start the server
- KCOM is now hosting a server! You can now provide your *public* IP address to peers

## Client Commands
- Type "/help" into the chat box as a client to view a list of commands
- Default commands:
	- `/echo <message>` - Echo a message
	- `/ping` - Check your ping
	- `/help` - This help menu
	- `/list` - List all players on the server.

## Server Commands
- As a server host, click on the "Chat" button in KCOM until it says "Server" to enter server operator mode
- Type "help" to view a list of server operator commands
- Default commands:
	- `echo <message>` - Echo a message
	- `persistent_set <key> <value>` - Set a persistent Lua value
	- `persistent_get <key>` - Get a persistent Lua value
	- `persistent_remove <key>` - Remove a persistent Lua value
	- `persistent_get_all` - List all persistent Lua values
	- `persistent_clear` - Clear all persistent Lua values
	- `script_refresh <script>` - Refresh a Lua script
	- `script_refresh_all` - Refresh all Lua scripts
	- `kick <username>` - Kick a player from the server
	- `ban <username>` - Ban a player from the server
	- `ipban <username>` - Ban a player from the server by IP
	- `unban <username>` - Remove a player's ban from the server
	- `lua <code>` - Run Lua code
	- `tp <username> (<username>/<x> <y> <z>)` - Teleport a player to a location
	- `tpall (<username>/<x> <y> <z>)` - Teleport all players to a location.

## Lua Scripting

### Requirements
- A code editor (such as [Visual Studio Code](https://code.visualstudio.com/))

### Instructions
- Follow the [instructions](#installation) to install KCOM
- Follow the [server instructions](#hosting-a-server) to host a server
- Open the `scripts` folder in the KCOM installation directory using a code editor

### Configuration
- Open `base/_config.lua` in the `scripts` folder using a code editor
- Follow instructions within the file to configure base Lua settings

### Plugins
- Copy `base/basic.lua` to a new folder (or the root `scripts` folder) under a different name to create a new "plugin" script
- Edit the script to your liking, pay attention to what is being "handled" as handling events will cancel out internal KCOM events and other scripts in alphabetical order

### Lua Documentation
Coming soon!

For Lua help, use [GitHub Issues](https://github.com/LinchenFur/KCOM/issues).

### Script Redistribution
You are free to modify and redistribute KCOM's default ("base") Lua scripts without permission, however when it comes to others' scripts, please provide credit to the original author(s) and link to the original source.

Submit script improvements and compatibility suggestions through [GitHub](https://github.com/LinchenFur/KCOM).

## Debugging/Modding

### Requirements
- [Visual Studio 2022 or later](https://visualstudio.microsoft.com/downloads/)
	- Select ".NET desktop development" during installation
- [7-Zip](https://www.7-zip.org/download.html)
- [Git](https://git-scm.com/downloads)
- Half-Life: Alyx - Workshop Tools
	- You can find this under the DLC section of Half-Life: Alyx within your Steam library

### Instructions
- Clone the repository within Visual Studio
- Build all projects for KCOM (x64) by pressing `Ctrl+Shift+B` or by clicking "Build" > "Build Solution"
	- Note: You may need to build the `KiwisCoOpModCore` project first, this can be done by right-clicking the project and selecting "Build"
- In Visual Studio, set `KiwisCoOpMod` as the startup project by right-clicking the project and selecting "Set as Startup Project"
- Click on the green "Start" button to start debugging
- Visual Studio will automatically launch KCOM with debugging enabled

### Symlinks
For addon development, you may want to "symlink" folders from this repository to your game:
- Open a command prompt as an administrator
- Type `cd` into the command prompt and press space
- Copy Half-Life: Alyx's `game/hlvr_addons` directory path and paste it into the command prompt, then press enter
	- Note: You can create these folders relative to the `Half-Life Alyx` folder if they don't exist
	- Note: You may need to "wrap" the path in quotes if it contains spaces
- Type `mkdir kiwimp_alyx` into the command prompt and press enter
- Type `cd kiwimp_alyx` into the command prompt and press enter
- Type `mklink /J scripts` and press space
- Find the repository's directory, this is likely `C:\Users\{username}\source\repos\KCOM`
- Copy the KCOM repository directory path and paste it into the command prompt
- Type `\WorkshopAddon\game\hlvr_addons\kiwimp_alyx\scripts` at the end of the command prompt to complete the path and press enter
- Type `cd` into the command prompt and press space
- Copy Half-Life: Alyx's `content/hlvr_addons` directory path and paste it into the command prompt, then press enter
	- Note: You can create these folders relative to the `Half-Life Alyx` folder if they don't exist
	- Note: You may need to "wrap" the path in quotes if it contains spaces
- Type `mklink /J kiwimp_alyx` and press space
- Copy the KCOM repository directory path and paste it into the command prompt
- Type `\WorkshopAddon\content\hlvr_addons\kiwimp_alyx` at the end of the command prompt to complete the path and press enter
- Launch Half-Life: Alyx with Workshop tools enabled 
	- Type `-console -vconsole` into the launch options for Half-Life: Alyx in Steam before launching
- Open VConsole by pressing the tilde (`~`) key
- Type `addon_enable kiwimp_alyx` into VConsole and press enter
- Changes within the Workshop tools will now be reflected to the KCOM repository and vice versa


## Help & Support

For support, open a ticket in [GitHub Issues](https://github.com/LinchenFur/KCOM/issues).
