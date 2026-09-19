--[[
    Kiwi's Co-Op Mod for Half-Life: Alyx
    Copyright (c) 2022 KiwifruitDev
    All rights reserved.
    This software is licensed under the MIT License.
    -----------------------------------------------------------------------------
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
    -----------------------------------------------------------------------------
]]--

-- Server configurable variables

lua_config = {}

-- The sub-gamemode name currently loaded.
lua_config.sub_gamemode = "campaign"

-- An introduction message to be sent to a player when they join the server
lua_config.client_introduction_message = {
    "-----------------------------",
    "欢迎来到我的服务器！",
    "这是一个《半条命：Alyx》服务器。",
    "输入 /help 查看命令列表。",
    "当前地图 ~map 上共有 ~playercount 名玩家（~gamemode/~subgamemode）",
    "祝你游戏愉快！",
    "-----------------------------",
}

-- Gamemode types
lua_config.gamemodes = {
    ["AlyxGamemode"] = "《半条命：Alyx》",
    ["CampaignGamemode"] = "战役模式",
    ["CoreGamemode"] = "核心模式",
}

lua_config.sub_gamemodes = {}

-- The output of the /help command
lua_config.client_helptable = {
    "- 命令帮助：-",
    "/echo <message> - 回显消息。",
    "/ping - 查看延迟。",
    "/help - 显示帮助菜单。",
    "/list - 列出服务器上的所有玩家。",
    "/vc - 输入 VConsole 命令。",
    "--------------------",
}

-- The output of the "help" internal server command
lua_config.server_helptable = {
    "- 命令帮助：-",
    "echo <message> - 回显消息。",
    "persistent_set <key> <value> - 设置持久化 Lua 值。",
    "persistent_get <key> - 获取持久化 Lua 值。",
    "persistent_remove <key> - 删除持久化 Lua 值。",
    "persistent_get_all - 列出所有持久化 Lua 值。",
    "persistent_clear - 清除所有持久化 Lua 值。",
    "script_refresh <script> - 刷新 Lua 脚本。",
    "script_refresh_all - 刷新所有 Lua 脚本。",
    "kick <username> - 将玩家踢出服务器。",
    "ban <username> - 封禁玩家。",
    "ipban <username> - 按 IP 封禁玩家。",
    "unban <username> - 解封玩家。",
    "lua <code> - 执行 Lua 代码。",
    "tp <username> (<username>/<x> <y> <z>) - 将玩家传送到指定位置。",
    "tpall (<username>/<x> <y> <z>) - 将所有玩家传送到指定位置。",
    "--------------------",
}