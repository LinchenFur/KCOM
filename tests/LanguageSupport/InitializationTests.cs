using System.Net;
using System.Net.Sockets;
using Fleck;
using KiwisCoOpMod;
using KiwisCoOpModCore;
using Newtonsoft.Json;

internal static partial class Program
{
    static async Task WaitFor(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }

    static void TestMapNames()
    {
        foreach (string? bad in new string?[] { null, "", "  ", "../map", "map.vmap", "map.vpk", "map;quit", "map\nquit", "map\\name", "\"map\"", "a//b", "/map", "map/", "a b", "a\0b", new string('a', 241) })
        {
            Check(!Map.TryNormalize(bad, out _), "reject unsafe map name");
            Reject(() => Map.LoadCommand(bad!), "unsafe map command built");
        }
        foreach (string good in new[] { "mp_kiwitest", "a3_hotel_lobby_basement", "workshop_examples/test-map" })
        {
            Check(Map.TryNormalize("  " + good + "  ", out var normalized) && normalized == good, "normalize map");
            string command = Map.LoadCommand(good);
            Check(command.EndsWith("addon_play " + good + ";addon_tools_map " + good), "map command uses normalized name");
            Check(command.StartsWith("addon_enable 2739356543;addon_enable kiwimp_alyx;"), "enable before load");
        }
    }

    static async Task TestClientStartup()
    {
        var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        int serverPort = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        Settings.Default.ClientPort = serverPort;
        string serverMap = "mp_kiwitest";
        int authentications = 0;
        int? serverVersion = Response.internalVersion;
        IWebSocketConnection? lastSocket = null;
        FleckLog.Level = LogLevel.Error;
        using var server = new WebSocketServer("ws://127.0.0.1:" + serverPort);
        server.Start(socket => socket.OnMessage = json =>
        {
            var request = JsonConvert.DeserializeObject<Response>(json);
            if (request?.type != "client") return;
            Interlocked.Increment(ref authentications);
            lastSocket = socket;
            socket.Send(new Response("authenticated") { map = serverMap, version = serverVersion }.ToString());
        });
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int consolePort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop(); // deliberately not available during first Start
        Settings.Default.VconsolePort = consolePort;
        var ui = new UserInterface();
        var client = new ClientProgram(ui);
        // Reproduces the former race: an immediate server response while the UI is
        // displaying the console-connect message, before TCP Connect is attempted.
        ui.OnLog = text => { if (text.Contains("正在通过端口")) Thread.Sleep(200); };
        try
        {
            client.Start(new());
            await Task.Delay(250);
            Check(authentications == 0, "no authentication without game console");
            Check(ui.Messages.Any(x => x.Contains("游戏客户端未启动")), "clear console failure diagnostic");
            listener.Start();
            Settings.Default.VconsolePort = ((IPEndPoint)listener.LocalEndpoint).Port;
            client.Start(new());
            using var game = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(5));
            using var wire = game.GetStream();
            Check((await ReadFrame(wire)).SequenceEqual(VConsoleProtocol.Focus(true, 211)), "focus before authentication map command");
            Check(CommandText(await ReadFrame(wire)) == Map.LoadCommand(serverMap), "immediate authentication retains first map command");
            Check(CommandText(await ReadFrame(wire)).Contains("script_execute kcom_bootstrap"), "probe starts after map request");
            await WaitFor(() => Volatile.Read(ref authentications) == 1);
            Check(ui.Messages.Any(x => x.Contains("尚未确认加载成功")), "map request is not reported as success");
            lastSocket!.Close();
            await DrainToEnd(wire);
            await WaitFor(() => ui.Messages.Any(x => x.Contains("已断开连接")));
            Check(true, "server disconnect closes console/probe");
            client.Close();

            // Stop/start a session on an already-running game, same ClientProgram object.
            client.Start(new());
            using var game2 = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(5));
            using var wire2 = game2.GetStream();
            await ReadFrame(wire2);
            Check(CommandText(await ReadFrame(wire2)) == Map.LoadCommand(serverMap), "restart reloads map after prior disconnect");
            await ReadFrame(wire2);
            client.Close();
            await DrainToEnd(wire2);
            Check(true, "manual stop closes console/probe");

            serverVersion = 0;
            serverMap = "mp_kiwitest";
            ui.Messages.Clear();
            client.Start(new());
            using var game3 = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(5));
            using var wire3 = game3.GetStream();
            await ReadFrame(wire3);
            await WaitFor(() => ui.Messages.Any(x => x.Contains("已停止本次初始化")));
            await Task.Delay(100);
            Check(!wire3.DataAvailable, "version mismatch sends no map command or probe");
            client.Close();

            serverVersion = Response.internalVersion;
            serverMap = "mp_kiwitest;quit";
            ui.Messages.Clear();
            client.Start(new());
            using var game4 = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(5));
            using var wire4 = game4.GetStream();
            await ReadFrame(wire4);
            await WaitFor(() => ui.Messages.Any(x => x.Contains("地图名无效")));
            await Task.Delay(100);
            Check(!wire4.DataAvailable, "malformed authenticated map sends no command or probe");
        }
        finally { client.Close(); listener.Stop(); }
    }

    static async Task DrainToEnd(NetworkStream wire)
    {
        byte[] data = new byte[8192];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (await wire.ReadAsync(data, timeout.Token) != 0) { }
    }
}

namespace KiwisCoOpMod
{
    // Only the UI and Lua plugin host are replaced. ClientProgram, VConsole, TCP,
    // WebSocket, gamemode and game bootstrap Lua under test are production code.
    public sealed class UserInterface
    {
        public readonly System.Collections.Concurrent.ConcurrentQueue<string> Messages = new();
        public Action<string>? OnLog;
        public void Invoke(Action callback) => callback();
        public void BeginInvoke(Action callback) => callback();
        public void LogToOutput(Channel channel, params object[] text)
        {
            string message = string.Join(" ", text);
            Messages.Enqueue(message);
            OnLog?.Invoke(message);
        }
    }
    public sealed class LuaEnvironment
    {
        public static LuaEnvironment instance = new();
        public void Handle(PluginHandleType type, params object[] args) { }
    }
}
