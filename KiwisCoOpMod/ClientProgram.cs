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
using System.ComponentModel;
using KiwisCoOpModCore;
using Websocket.Client;
using System.Diagnostics;
using System.Drawing;

namespace KiwisCoOpMod
{
    public class ClientProgram
    {
        private WebsocketClient? ws;
        private readonly UserInterface ui;
        private readonly Channel channel = new("CL", "客户端", Color.Green);
        private readonly Channel chatChannel = new("CHAT", "聊天", Color.Black);
        private readonly Channel statusChannel = new("STATUS", "状态", Color.DeepPink);
        private readonly Channel vConsoleChannel = new("VC", "VConsole", Color.Maroon);
        private readonly VConsole? vConsole = new();
        private List<Type> plugins = new();
        public int version = 0; // Update version if netcode changes.
        public string map = "";
        public ClientProgram(UserInterface ui)
        {
            this.ui = ui;
        }

        public void ConnectVConsole(WebsocketClient ws)
        {
            if (ws != null && vConsole != null)
            {
                ui.Invoke(() => ui.LogToOutput(channel, "正在通过端口连接 VConsole：" + Settings.Default.VconsolePort));
                if (!vConsole.Connect(ws))
                {
                    ui.Invoke(() =>
                    {
                        ui.LogToOutput(channel, "连接 VConsole 失败");
                        DialogResult res = MessageBox.Show("连接 VConsole 时发生错误。\n请确认《半条命：Alyx》已启动。\n是否重新连接？", "错误", MessageBoxButtons.YesNo);
                        if (res == DialogResult.Yes)
                            ConnectVConsole(ws);
                        else
                            ui.Invoke(() => ui.LogToOutput(channel, "已断开 VConsole 连接"));
                    });
                }
            }
        }
        public void Start(List<Type> pluginTypes)
        {
            bool error = false;
            if (ws == null)
            {
                map = "";
                PluginHandler.Handle(pluginTypes, PluginHandleType.Client_PreStart, ui);
                LuaEnvironment.instance.Handle(PluginHandleType.Client_PreStart, ui);
                plugins = pluginTypes;
                ws = new WebsocketClient(new Uri("ws://" + Settings.Default.ClientIpAddress + ":" + Settings.Default.ClientPort))
                {
                    ReconnectTimeout = TimeSpan.FromSeconds(60),
                    IsReconnectionEnabled = false
                };
                ws.Start();
                ws.DisconnectionHappened.Subscribe(info =>
                {
                    ui.Invoke(() => ui.LogToOutput(channel, "已断开连接：" + info.Type.ToString()));
                });
                ws.MessageReceived.Subscribe(msg =>
                {
                    Response? response = JsonConvert.DeserializeObject<Response>(msg.Text);
                    if (response != null && response.type != null)
                    {
                        PluginHandler.Handle(plugins, PluginHandleType.Client_PreResponse, response);
                        LuaEnvironment.instance.Handle(PluginHandleType.Client_PreResponse, response);
                        switch (response.type)
                        {
                            case "authenticated":
                                if (response.version > Response.internalVersion)
                                {
                                    ui.Invoke(() => ui.LogToOutput(channel, "客户端版本较旧！请更新客户端。"));
                                }
                                else if (response.version < Response.internalVersion)
                                {
                                    ui.Invoke(() => ui.LogToOutput(channel, "服务器版本较旧！请联系服务器主机更新。"));
                                }
                                if (vConsole != null && response.map != null)
                                {
                                    if (map != response.map)
                                    {
                                        ui.Invoke(() => ui.LogToOutput(channel, "正在切换地图：" + response.map));
                                        vConsole.WriteCommand("addon_play " + response.map + ";addon_tools_map " + response.map);
                                        map = response.map;
                                    }
                                }
                                break;
                            case "chat":
                                if (response.data != null && response.remoteClientUsername != null)
                                {
                                    ui.Invoke(() => ui.LogToOutput(chatChannel, response.remoteClientUsername + ": " + response.data));
                                }
                                break;
                            case "command":
                                if (vConsole != null && response.data != null)
                                {
                                    vConsole.WriteCommand(response.data, response.urgent);
                                }
                                break;
                            case "status":
                                if (vConsole != null && response.data != null)
                                {
                                    if (ui != null)
                                        ui.Invoke(() => ui.LogToOutput(statusChannel, response.data));
                                }
                                break;
                            case "vconsole":
                                if (vConsole != null && response.data != null && Settings.Default.ClientPrintVconsole)
                                {
                                    if (ui != null)
                                        ui.Invoke(() => ui.LogToOutput(vConsoleChannel, response.data));
                                }
                                break;
                            default:
                                ui.Invoke(() => ui.LogToOutput(channel, "未实现的消息类型：" + response.type + "!"));
                                break;
                        }
                        PluginHandler.Handle(plugins, PluginHandleType.Client_PostResponse, response);
                        LuaEnvironment.instance.Handle(PluginHandleType.Client_PostResponse, response);
                    }
                    else
                    {
                        ui.Invoke(() => ui.LogToOutput(channel, "服务器发送了无效数据！"));
                    }
                });
                ws.ReconnectionHappened.Subscribe(recinfo =>
                {
                    Response input = new("client")
                    {
                        clientUsername = Settings.Default.ClientUsername,
                        password = Settings.Default.ClientPassword,
                        timestamp = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds
                    };
                    ws.Send(JsonConvert.SerializeObject(input));
                    ui.Invoke(() => ui.LogToOutput(channel, "客户端尝试连接到 IP：" + Settings.Default.ClientIpAddress + ":" + Settings.Default.ServerPort));
                });
                ConnectVConsole(ws);
                PluginHandler.Handle(plugins, PluginHandleType.Client_PostStart, ui, ws, error);
                LuaEnvironment.instance.Handle(PluginHandleType.Client_PostStart, ui, ws, error);
            }
        }
        public void Close()
        {
            if (ws != null)
            {
                PluginHandler.Handle(plugins, PluginHandleType.Client_PreClose, ui, ws);
                LuaEnvironment.instance.Handle(PluginHandleType.Client_PreClose, ui, ws);
                ws.Stop(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closed by KCOM.");
                ws.Dispose();
                ws = null;
                ui.Invoke(() => ui.LogToOutput(channel, "客户端已关闭"));
            }
            if (vConsole != null)
            {
                vConsole.Disconnect();
            }
        }
        public void Chat(string text)
        {
            if (ws != null && ws.IsStarted && text.Length > 0)
            {
                Response chatResponse = new("chat", text);
                chatResponse.timestamp = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
                ws.Send(chatResponse.ToString());
            }
        }
    }
}
