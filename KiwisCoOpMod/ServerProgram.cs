/*
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
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using KiwisCoOpModCore;
using Fleck;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Drawing;
using System.IO;

namespace KiwisCoOpMod
{
    public class ServerProgram
    {
        public static readonly ServerProgram instance = new();
        public WebSocketServer? wss;
        public List<IndexedClient> connections = new() { };
        public Type? gamemodeType;
        public List<Type> plugins = new();
        public Channel channel = new("SV", "服务器", Color.Olive);
        public int tickrate = 66;
        public bool executeThink = false;
        public void Tick()
        {
            PluginHandler.Handle(plugins, PluginHandleType.Server_PreGamemode_Think, tickrate, connections, Map.map);
            LuaEnvironment.instance.Handle(PluginHandleType.Server_PreGamemode_Think, tickrate, connections, Map.map);
            if (GamemodeHandler.Handle(gamemodeType, GamemodeHandleType.Think, tickrate, connections, Map.map) == HandleState.Continue)
            {
                PluginHandler.Handle(plugins, PluginHandleType.Server_PostGamemode_Think, tickrate, connections, Map.map);
                LuaEnvironment.instance.Handle(PluginHandleType.Server_PostGamemode_Think, tickrate, connections, Map.map);
            }
        }
        public void Start(Type type, List<Type> plugins)
        {
            if (wss == null)
            {
                if (!Map.TryNormalize(Settings.Default.ServerMap, out string requestedMap))
                {
                    Program.userInterface.Invoke(() => Program.userInterface.LogToOutput(channel, Map.InvalidNameMessage));
                    return;
                }
                PluginHandler.Handle(plugins, PluginHandleType.Server_PreGamemode_PreStart);
                LuaEnvironment.instance.Handle(PluginHandleType.Server_PreGamemode_PreStart);
                if (GamemodeHandler.Handle(type, GamemodeHandleType.PreStart) == HandleState.Continue)
                {
                    Map.map = requestedMap;
                    this.plugins = plugins;

                    PluginHandler.Handle(plugins, PluginHandleType.Server_PostGamemode_PreStart, type, plugins, Map.map);
                    LuaEnvironment.instance.Handle(PluginHandleType.Server_PostGamemode_PreStart, type, plugins, Map.map);

                    gamemodeType = type;
                    wss = new WebSocketServer("ws://[::]:" + Settings.Default.ServerPort);
                    wss.Start(socket =>
                    {
                        //socket.OnOpen = () => OnOpen(socket);
                        socket.OnClose = () => OnClose(socket);
                        socket.OnMessage = message => OnMessage(message, socket);
                    });
                    if (gamemodeType != null)
                    {
                        Program.userInterface.Invoke(() => Program.userInterface.LogToOutput(channel, "正在启动游戏模式：" + gamemodeType.Name + "；地图：" + Map.map + "；端口：" + Settings.Default.ServerPort));
                        PluginHandler.Handle(plugins, PluginHandleType.Server_PreGamemode_PostStart, gamemodeType, plugins, Map.map);
                        LuaEnvironment.instance.Handle(PluginHandleType.Server_PreGamemode_PostStart, gamemodeType, plugins, Map.map);
                        GamemodeHandler.Handle(gamemodeType, GamemodeHandleType.PostStart, gamemodeType, plugins);
                        PluginHandler.Handle(plugins, PluginHandleType.Server_PostGamemode_PostStart, gamemodeType, plugins, Map.map);
                        LuaEnvironment.instance.Handle(PluginHandleType.Server_PostGamemode_PostStart, gamemodeType, plugins, Map.map);
                    }
                    executeThink = true;
                    // Tick/thinking
                    Task.Run(() =>
                    {
                        while (executeThink)
                        {
                            Tick();
                            Thread.Sleep(tickrate);
                        }
                    });
                }
            }
        }
        public void Close()
        {
            executeThink = false;
            if (wss != null)
            {
                if (gamemodeType != null)
                {
                    PluginHandler.Handle(plugins, PluginHandleType.Server_PreGamemode_PreClose, gamemodeType, plugins, Map.map);
                    LuaEnvironment.instance.Handle(PluginHandleType.Server_PreGamemode_PreClose, gamemodeType, plugins, Map.map);
                    if (GamemodeHandler.Handle(gamemodeType, GamemodeHandleType.PreClose, wss) == HandleState.Continue)
                    {
                        PluginHandler.Handle(plugins, PluginHandleType.Server_PreGamemode_PostClose, gamemodeType, plugins, Map.map);
                        LuaEnvironment.instance.Handle(PluginHandleType.Server_PreGamemode_PostClose, gamemodeType, plugins, Map.map);
                        GamemodeHandler.Handle(gamemodeType, GamemodeHandleType.PostClose, gamemodeType, plugins, Map.map);
                        PluginHandler.Handle(plugins, PluginHandleType.Server_PostGamemode_PostClose, gamemodeType, plugins, Map.map);
                        LuaEnvironment.instance.Handle(PluginHandleType.Server_PostGamemode_PostClose, gamemodeType, plugins, Map.map);
                    }
                }
                Program.userInterface.Invoke(() => Program.userInterface.LogToOutput(channel, "正在关闭服务器，端口：" + Settings.Default.ServerPort));
                wss.Dispose();
                wss = null;
            }
        }
        public void ChangeMap(string map)
        {
            if (!Map.TryNormalize(map, out string requestedMap))
            {
                Program.userInterface.Invoke(() => Program.userInterface.LogToOutput(channel, Map.InvalidNameMessage));
                return;
            }
            Map.map = requestedMap;
            Response output2 = new("command", Map.LoadCommand(requestedMap));
            foreach (IndexedClient con in connections)
            {
                con.Session.Send(JsonConvert.SerializeObject(output2));
            }
        }
        public void GlobalVConsole(string command)
        {
            Response output2 = new("command", command);
            foreach (IndexedClient con in connections)
            {
                con.Session.Send(JsonConvert.SerializeObject(output2));
            }
        }
        public void OnOpen(IWebSocketConnection socket)
        {
            foreach (IndexedClient client in connections)
            {
                if (client.Session.ConnectionInfo.Id == socket.ConnectionInfo.Id)
                {
                    Response outputDisconnect = new("status")
                    {
                        data = client.Username + " 已连接"
                    };
                    if (gamemodeType != null)
                    {
                        PluginHandler.Handle(plugins, PluginHandleType.Server_PreGamemode_ClientOpen, connections, socket, client.Username);
                        LuaEnvironment.instance.Handle(PluginHandleType.Server_PreGamemode_ClientOpen, connections, socket, client.Username);
                        GamemodeHandler.Handle(gamemodeType, GamemodeHandleType.ClientOpen, connections, socket, client.Username);
                        PluginHandler.Handle(plugins, PluginHandleType.Server_PostGamemode_ClientOpen, connections, socket, client.Username);
                        LuaEnvironment.instance.Handle(PluginHandleType.Server_PostGamemode_ClientOpen, connections, socket, client.Username);
                    }
                    connections.ForEach(c => c.Session.Send(JsonConvert.SerializeObject(outputDisconnect)));
                    Program.userInterface.Invoke(() => Program.userInterface.LogToOutput(channel, client.Username + " 已连接"));
                    break;
                }
            }
        }
        public void OnClose(IWebSocketConnection socket)
        {
            foreach(IndexedClient client in connections.ToList())
            {
                if(client.Session.ConnectionInfo.Id == socket.ConnectionInfo.Id)
                {
                    Response outputDisconnect = new("status")
                    {
                        data = client.Username + " 已断开连接"
                    };
                    if (gamemodeType != null)
                    {
                        PluginHandler.Handle(plugins, PluginHandleType.Server_PreGamemode_ClientClose, connections, socket, client.Username);
                        LuaEnvironment.instance.Handle(PluginHandleType.Server_PreGamemode_ClientClose, connections, socket, client.Username);
                        GamemodeHandler.Handle(gamemodeType, GamemodeHandleType.ClientClose, connections, socket, client.Username);
                        PluginHandler.Handle(plugins, PluginHandleType.Server_PostGamemode_ClientClose, connections, socket, client.Username);
                        LuaEnvironment.instance.Handle(PluginHandleType.Server_PostGamemode_ClientClose, connections, socket, client.Username);
                    }
                    connections.ForEach(c => c.Session.Send(JsonConvert.SerializeObject(outputDisconnect)));
                    Program.userInterface.Invoke(() => Program.userInterface.LogToOutput(channel, client.Username + " 已断开连接"));
                    connections.Remove(client);
                    break;
                }
            }
        }

        public void Command(List<string> command)
        {
            if (gamemodeType != null)
            {
                PluginHandler.Handle(plugins, PluginHandleType.Server_PreGamemode_Command, command, connections, gamemodeType);
                LuaEnvironment.instance.Handle(PluginHandleType.Server_PreGamemode_Command, command, connections, gamemodeType);
                if (GamemodeHandler.Handle(gamemodeType, GamemodeHandleType.Command, command, connections, gamemodeType) != HandleState.Handled)
                {
                    PluginHandler.Handle(plugins, PluginHandleType.Server_PostGamemode_Command, command, connections, gamemodeType);
                    LuaEnvironment.instance.Handle(PluginHandleType.Server_PostGamemode_Command, command, connections, gamemodeType);
                }
            }
        }
        public void OnMessage(string message, IWebSocketConnection socket)
        {
            Response ? response = JsonConvert.DeserializeObject<Response>(message);
            if (gamemodeType != null && response != null && response.type != null)
            {
                PluginHandler.Handle(plugins, PluginHandleType.Server_PreGamemode_PreResponse, response, connections, socket, Map.map);
                LuaEnvironment.instance.Handle(PluginHandleType.Server_PreGamemode_PreResponse, response, connections, socket, Map.map);
                if (GamemodeHandler.Handle(gamemodeType, GamemodeHandleType.PreResponse, response, connections, socket, Map.map) == HandleState.Continue)
                {
                    PluginHandler.Handle(plugins, PluginHandleType.Server_PostGamemode_PreResponse, response, connections, socket, Map.map);
                    LuaEnvironment.instance.Handle(PluginHandleType.Server_PostGamemode_PreResponse, response, connections, socket, Map.map);
                    switch (response.type.ToLower())
                    {
                        case "client":
                            if (response.clientUsername != null && response.password != null)
                            {
                                if (Settings.Default.ServerPassword != "" && response.password != Settings.Default.ServerPassword)
                                {
                                    Response output2 = new("status", "服务器密码无效！连接即将关闭……");
                                    socket.Send(JsonConvert.SerializeObject(output2));
                                }
                                else
                                {
                                    foreach (IndexedClient indexed1 in connections)
                                    {
                                        if (indexed1.Username == response.clientUsername)
                                        {
                                            Response output2 = new("status", "该客户端已在其他位置重新连接，连接即将关闭……");
                                            socket.Send(JsonConvert.SerializeObject(output2));
                                            socket.Close();
                                            connections.Remove(indexed1);
                                            break;
                                        }
                                    }
                                    if (response.clientUsername != null)
                                    {
                                        if (response.clientUsername.Length > 32 || Regex.Match(response.clientUsername, @"[\;""]").Success)
                                        {
                                            Response outputRegex = new("status", "用户名无效，连接即将关闭……");
                                            socket.Send(JsonConvert.SerializeObject(outputRegex));
                                            socket.Close();
                                        }
                                        else
                                        {
                                            Response output2 = new("authenticated")
                                            {
                                                clientUsername = response.clientUsername,
                                                map = Map.map,
                                            };
                                            IndexedClient client = new(socket, response.clientUsername, Map.map);
                                            connections.Add(client);
                                            socket.Send(JsonConvert.SerializeObject(output2));
                                            OnOpen(socket);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Response output2 = new("status", "用户名无效！连接即将关闭……");
                                socket.Send(JsonConvert.SerializeObject(output2));
                                socket.Close();
                            }
                            break;
                        case "chat":
                            IndexedClient? indexedClient = connections.Find(c => c.Session.ConnectionInfo.Id == socket.ConnectionInfo.Id);
                            if (indexedClient != null)
                            {
                                if (response.data != null)
                                {
                                    if (response.data.Length == 0) return;
                                    Program.userInterface.Invoke(() => Program.userInterface.LogToOutput(channel, indexedClient.Username + ": " + response.data));
                                    if (response.data.StartsWith("/"))
                                    {
                                        string command = response.data.Split(" ").ToArray().First().Replace("/", "").ToLower();
                                        string[] args = response.data.Split(" ").Skip(1).ToArray();
                                        Response output3 = new("status", response.data)
                                        {
                                            clientUsername = indexedClient.Username,
                                            data = "未知命令“" + command + "”"
                                        };
                                        switch (command)
                                        {
                                            case "vc":
                                                if (!Settings.Default.ServerDisableUserVconsoleInput)
                                                {
                                                    output3 = new Response("command", response.data)
                                                    {
                                                        data = string.Join(" ", args),
                                                        urgent = false
                                                    };
                                                }
                                                else
                                                {
                                                    output3 = new Response("status", response.data)
                                                    {
                                                        data = "此服务器已禁用 VConsole 输入。"
                                                    };
                                                }
                                                break;
                                        }
                                        socket.Send(JsonConvert.SerializeObject(output3));
                                    }
                                    else
                                    {
                                        Response output4 = new("chat", response.data)
                                        {
                                            remoteClientUsername = indexedClient.Username
                                        };
                                        connections.ForEach(c => c.Session.Send(JsonConvert.SerializeObject(output4)));
                                    }
                                }
                            }
                            break;
                        case "print":
                            IndexedClient? indexed2 = connections.Find(c => c.Session.ConnectionInfo.Id == socket.ConnectionInfo.Id);
                            if (indexed2 != null)
                            {
                                if (response.data != null)
                                {
                                    if (indexed2 != null && indexed2 != null)
                                    {
                                        Response output5 = new("vconsole", response.data)
                                        {
                                            clientUsername = indexed2.Username
                                        };
                                        socket.Send(JsonConvert.SerializeObject(output5));
                                    }
                                }
                            }
                            break;
                        case "lua_chat_handled":
                            // Workaround for plugins to "handle" chat commands.
                            IndexedClient? indexedClient0 = connections.Find(c => c.Session.ConnectionInfo.Id == socket.ConnectionInfo.Id);
                            if (indexedClient0 != null)
                            {
                                if (response.data != null)
                                {
                                    if (response.data.Length == 0) return;
                                    Program.userInterface.Invoke(() => Program.userInterface.LogToOutput(channel, indexedClient0.Username + ": " + response.data));
                                }
                            }
                            break;
                        default:
                            //IndexedClient? indexed = connections.Find(c => c.Session.ConnectionInfo.Id == socket.ConnectionInfo.Id);
                            //if (indexed != null) connections.Remove(indexed);
                            //socket.Close();
                            break;
                    }
                    PluginHandler.Handle(plugins, PluginHandleType.Server_PreGamemode_PostResponse, response, connections, socket, Map.map);
                    LuaEnvironment.instance.Handle(PluginHandleType.Server_PreGamemode_PostResponse, response, connections, socket, Map.map);
                    GamemodeHandler.Handle(gamemodeType, GamemodeHandleType.PostResponse, response, connections, socket, Map.map);
                    PluginHandler.Handle(plugins, PluginHandleType.Server_PostGamemode_PostResponse, response, connections, socket, Map.map);
                    LuaEnvironment.instance.Handle(PluginHandleType.Server_PostGamemode_PostResponse, response, connections, socket, Map.map);
                }
            }
        }
    }
}
