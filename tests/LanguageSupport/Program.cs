using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using AlyxGamemode;
using Fleck;
using KiwisCoOpMod;
using KiwisCoOpModCore;
using Newtonsoft.Json;
using NLua;

internal static class Program
{
    private static int checks;
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }
    static void Reject(Action action, string message)
    {
        try { action(); } catch (ArgumentException) { checks++; return; }
        throw new Exception(message);
    }
    static byte[] Print(string text)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        byte[] frame = new byte[40 + utf8.Length + 1];
        Encoding.ASCII.GetBytes("PRNT").CopyTo(frame, 0);
        frame[5] = 211;
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(8), checked((ushort)frame.Length));
        utf8.CopyTo(frame, 40);
        return frame;
    }
    static async Task<byte[]> ReadFrame(NetworkStream stream)
    {
        async Task Fill(byte[] bytes, int start = 0)
        {
            while (start < bytes.Length)
            {
                int n = await stream.ReadAsync(bytes.AsMemory(start)).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                if (n == 0) throw new EndOfStreamException();
                start += n;
            }
        }
        byte[] header = new byte[12];
        await Fill(header);
        byte[] frame = new byte[BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(8))];
        header.CopyTo(frame, 0);
        await Fill(frame, 12);
        return frame;
    }
    static string CommandText(byte[] frame) => Encoding.UTF8.GetString(frame, 12, frame.Length - 13);

    static async Task Main()
    {
        Check(new Response("client").version == 1, "new handshake advertises protocol revision");
        string multilingual = "中文 简体 繁體 日本語 한국어 Русский Français العربية 😀";
        string longText = string.Concat(Enumerable.Repeat(multilingual, 20));
        foreach (string text in new[] { "echo INIT KCOM", multilingual, longText })
        {
            byte[] frame = VConsoleProtocol.Command(text, 211);
            Check(frame.Length == Encoding.UTF8.GetByteCount(text) + 13, "UTF-8 byte length");
            Check(BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(8)) == frame.Length, "16-bit frame length");
            Check(CommandText(frame) == text && frame[^1] == 0, "Unicode command round-trip");
        }
        Check(Convert.ToHexString(VConsoleProtocol.Command("echo INIT KCOM", 211)) ==
            "434D4E4400D30000001B00006563686F20494E4954204B434F4D00", "ASCII wire compatibility");
        Check(Convert.ToHexString(VConsoleProtocol.Focus(true, 211)) == "5646435300D30000000D000001", "focus wire compatibility");
        Check(VConsoleProtocol.Command(new string('a', 65522), 211).Length == 65535, "maximum frame");
        Reject(() => VConsoleProtocol.Command(new string('中', 22000), 211), "oversized command accepted");
        Reject(() => VConsoleProtocol.Command("echo\0quit", 211), "NUL command accepted");
        Check(!VConsoleProtocol.PrintLines(new byte[5]).Any(), "short PRNT");
        Check(VConsoleProtocol.PrintLines(Print(multilingual)[10..]).Single() == multilingual, "PRNT UTF-8");
        Check(VConsoleProtocol.PrintLines(Print("中文\r\nKRDY KCOM\nINIT KCOM")[10..]).SequenceEqual(
            new[] { "中文", "KRDY KCOM", "INIT KCOM" }), "separate protocol lines");

        byte[] frames = Print(longText).Concat(Print("KRDY KCOM")).Concat(VConsoleProtocol.Focus(true, 211)).ToArray();
        foreach (int chunk in new[] { 1, 2, 7, 10, 255, 4096 })
        {
            using var source = new FragmentedStream(frames, chunk);
            var watcher = new StreamWatcher(source);
            var received = new List<MessageAvailableEventArgs>();
            watcher.MessageAvailable += (_, e) => received.Add(e);
            watcher.SetWorking(true);
            await watcher.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            Check(received.Count == 3, "fragmented/coalesced frame count");
            Check(VConsoleProtocol.PrintLines(received[0].Data).Single() == longText, "split UTF-8 codepoint");
            Check(VConsoleProtocol.PrintLines(received[1].Data).Single() == "KRDY KCOM", "next frame alignment");
            int reads = source.Reads;
            await Task.Delay(20);
            Check(source.Reads == reads, "EOF busy loop");
        }
        foreach (byte[] broken in new[] { Array.Empty<byte>(), Print("abc")[..6], Print("abc")[..^2], new byte[10] })
        {
            using var source = new FragmentedStream(broken, 3);
            var watcher = new StreamWatcher(source);
            int count = 0;
            watcher.MessageAvailable += (_, _) => count++;
            watcher.SetWorking(true);
            await watcher.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            Check(count == 0, "truncated/invalid frame delivered");
        }
        // Invalid frames terminate instead of reinterpreting arbitrary bytes as a header.
        using (var source = new FragmentedStream(new byte[10].Concat(Print("KRDY KCOM")).ToArray(), 10))
        {
            var watcher = new StreamWatcher(source);
            int count = 0;
            watcher.MessageAvailable += (_, _) => count++;
            watcher.SetWorking(true);
            await watcher.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            Check(count == 0, "invalid frame resynchronization");
        }
        await TestTransport(longText);
        TestLuaBootstrap(multilingual);
        await TestGamemode();
        Console.WriteLine($"PASS: {checks} language support checks (mock game/loopback, not a live Alyx playtest).");
    }

    static async Task TestTransport(string text)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        Settings.Default.VconsolePort = ((IPEndPoint)listener.LocalEndpoint).Port;
        var console = new VConsole();
        // Real loopback WebSocket forwarding, without starting the WinForms UI.
        var portReservation = new TcpListener(IPAddress.Loopback, 0);
        portReservation.Start();
        int port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
        portReservation.Stop();
        FleckLog.Level = LogLevel.Error;
        using var server = new WebSocketServer("ws://127.0.0.1:" + port);
        var forwarded = new System.Collections.Concurrent.ConcurrentQueue<Response>();
        server.Start(socket => socket.OnMessage = json => forwarded.Enqueue(JsonConvert.DeserializeObject<Response>(json)!));
        using var websocket = new Websocket.Client.WebsocketClient(new Uri("ws://127.0.0.1:" + port)) { IsReconnectionEnabled = false };
        await websocket.Start();
        try
        {
            console.WriteCommand("echo queued", urgent: true);
            Check(console.Connect(websocket), "loopback VConsole connect");
            using var peer = await listener.AcceptTcpClientAsync();
            using var wire = peer.GetStream();
            Check((await ReadFrame(wire)).SequenceEqual(VConsoleProtocol.Focus(true, 211)), "actual focus frame");
            Check(CommandText(await ReadFrame(wire)) == "echo queued", "urgent queue preserved");
            console.StartBootstrapProbe();
            string probe = CommandText(await ReadFrame(wire));
            Check(probe.Contains("script_execute kcom_bootstrap") && probe.Contains("KCOM_BOOTSTRAP_SESSION="), "probe sent");
            await Task.WhenAll(Enumerable.Range(0, 15).Select(i => Task.Run(() => console.WriteCommand($"echo {i} {text}"))));
            var commands = new List<string>();
            while (commands.Count < 15)
            {
                string command = CommandText(await ReadFrame(wire));
                if (command.StartsWith("echo ")) commands.Add(command);
            }
            Check(commands.Distinct().Count() == 15 && commands.All(c => c.EndsWith(text)), "concurrent writes not interleaved");
            byte[] gameOutput = Print(text + "\nKRDY KCOM\n").Concat(Print("MAPN mp_kiwitest 4 KCOM")).ToArray();
            for (int offset = 0; offset < gameOutput.Length; offset += 7)
                await wire.WriteAsync(gameOutput.AsMemory(offset, Math.Min(7, gameOutput.Length - offset)));
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                while (forwarded.Count < 3) await Task.Delay(10, timeout.Token);
            Check(forwarded.Select(r => r.data).SequenceEqual(new[] { text, "KRDY KCOM", "MAPN mp_kiwitest 4 KCOM" }), "PRNT TCP -> UTF-8 -> WebSocket -> JSON round-trip");
            console.Disconnect();
            byte[] pending = new byte[1024];
            int remaining;
            do { remaining = await wire.ReadAsync(pending).AsTask().WaitAsync(TimeSpan.FromSeconds(5)); } while (remaining != 0);
            Check(remaining == 0, "disconnect closes stream/probe");
            Check(console.Connect(), "reconnect");
            using var second = await listener.AcceptTcpClientAsync();
            using var secondWire = second.GetStream();
            await ReadFrame(secondWire);
            console.StartBootstrapProbe();
            Check(CommandText(await ReadFrame(secondWire)) != probe, "new bootstrap session after reconnect");
        }
        finally { console.Disconnect(); listener.Stop(); }

        var cancelListener = new TcpListener(IPAddress.Loopback, 0);
        cancelListener.Start();
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)cancelListener.LocalEndpoint).Port);
            using var peer = await cancelListener.AcceptTcpClientAsync();
            var watcher = new StreamWatcher(client.GetStream());
            watcher.SetWorking(true);
            await Task.Delay(25);
            watcher.SetWorking(false);
            await watcher.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            Check(watcher.Completion.IsCompleted, "cancel blocked read");
        }
        finally { cancelListener.Stop(); }
    }

    static void TestLuaBootstrap(string text)
    {
        using var lua = new Lua();
        lua.State.Encoding = Encoding.UTF8;
        lua["sample"] = text;
        Check((string)lua.DoString("return sample")[0] == text, "Lua UTF-8 bridge");
        lua.DoString(@"
            now = 0; validPlayer = false; scriptExists = false; timerExists = false; prints = 0
            Entities = {}
            function Entities:GetLocalPlayer() if validPlayer then return {} end end
            function Entities:FindByName(_, name)
                if name == 'kcom_script' and scriptExists then return {} end
                if name == 'kcom_timer' and timerExists then return {} end
            end
            function IsValidEntity(e) return e ~= nil end
            function Time() return now end
            function print(message) assert(message == 'KRDY KCOM'); prints = prints + 1 end
            KCOM_BOOTSTRAP_SESSION = 'session1'
        ");
        void Run() => lua.DoFile(Path.Combine(AppContext.BaseDirectory, "kcom_bootstrap.lua"));
        int Count() => Convert.ToInt32(lua["prints"]);
        Run(); Check(Count() == 0, "no player means no readiness");
        lua.DoString("validPlayer = true"); Run(); Check(Count() == 1, "first player readiness");
        Run(); Check(Count() == 1, "no duplicate during initialization");
        lua.DoString("now = 11"); Run(); Check(Count() == 2, "retry missing initialization");
        lua.DoString("scriptExists = true; timerExists = true; now = 30"); Run(); Check(Count() == 2, "idle after successful initialization");
        lua.DoString("KCOM_BOOTSTRAP_SESSION = 'session2'"); Run(); Check(Count() == 3, "new session initializes existing map");
        lua.DoString("KCOM_BOOTSTRAP_STARTED_SESSION=nil; KCOM_BOOTSTRAP_RETRY_AT=nil; scriptExists=false; timerExists=false; now=0");
        Run(); Check(Count() == 4, "new map readiness");
    }

    static async Task TestGamemode()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (string locale in new[] { "en-US", "zh-CN", "zh-TW", "ja-JP", "ko-KR", "ru-RU", "de-DE", "fr-FR", "tr-TR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
                Check(new Vector(1.25f, -2.5f, 3.75f).ToString() == "1.25 -2.5 3.75", "vector " + locale);
                Check(new Angle(1.25f, -2.5f, 3.75f).ToString() == "1.25 -2.5 3.75", "angle " + locale);
                Check(new Packet("init").type == PacketType.Initialization, "packet casing " + locale);
                var socket = DispatchProxy.Create<IWebSocketConnection, SocketProxy>();
                var proxy = (SocketProxy)(object)socket;
                var client = new IndexedClient(socket, "玩家", "mp_kiwitest");
                var clients = new List<IndexedClient> { client };
                void Feed(string line) => _ = new AlyxGamemode.AlyxGamemode(GamemodeHandleType.PreResponse,
                    new Response("print", line), clients, socket, "mp_kiwitest");
                foreach (string log in new[] { "Player has joined the game", "玩家已加入游戏", "Игрок присоединился", "玩家：KRDY KCOM", "not KRDY KCOM" }) Feed(log);
                Check(proxy.Sent.Count == 0, "localized/natural logs ignored " + locale);
                Feed("KRDY KCOM");
                Check(proxy.Sent.Any(r => r.data?.Contains("echo INIT KCOM") == true), "readiness " + locale);
                Check(proxy.Sent.Any(r => r.data?.Contains("ent_remove_all kcom_timer") == true), "timer cleanup " + locale);
                Check(AlyxGlobalData.instance.GetPlayer(proxy.ID) != null, "player registered " + locale);
                Feed("INIT KCOM");
                Check(proxy.Sent.Any(r => r.data?.Contains("vscripts kcom_interval") == true), "INIT chain " + locale);
                var peerSocket = DispatchProxy.Create<IWebSocketConnection, SocketProxy>();
                var peerProxy = (SocketProxy)(object)peerSocket;
                var peerClient = new IndexedClient(peerSocket, "观察者", "mp_kiwitest");
                clients.Add(peerClient);
                AlyxGlobalData.instance.AddPlayer(peerClient);
                Feed("TELE 1.25 -2.5 3.75 4.5 5.25 -6.75 KCOM");
                Check(peerProxy.Sent.Any(r => r.data == "kcom_teleportangles 1.25 -2.5 3.75 4.5 5.25 -6.75"), "teleport culture " + locale);
                Feed("PHYS box_" + locale + " 1.25 -2.5 3.75 4.5 5.25 -6.75 KCOM");
                Check(peerProxy.Sent.Any(r => r.data == "kcom_setlocation box_" + locale + " 1.25 -2.5 3.75 4.5 5.25 -6.75"), "physics culture " + locale);
                AlyxGlobalData.instance.RemovePlayer(peerProxy.ID);
                Map.map = "";
                Feed("MAPN mp_kiwitest 4 KCOM");
                Check(Map.map == "mp_kiwitest" && proxy.Sent.Any(r => r.data?.Contains("Detected map: mp_kiwitest") == true), "map detection " + locale);
                if (locale == "zh-CN")
                {
                    Feed("IENT KCOM");
                    await Task.Delay(2800);
                    Check(proxy.Sent.Any(r => r.data?.Contains("OnTimer>kcom_script") == true), "IENT timer chain");
                }
                AlyxGlobalData.instance.RemovePlayer(proxy.ID);
                clients.Clear(); proxy.Sent.Clear(); Feed("KRDY KCOM");
                Check(proxy.Sent.Count == 0, "unauthenticated readiness rejected " + locale);
            }
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}

public class SocketProxy : DispatchProxy
{
    public readonly Guid ID = Guid.NewGuid();
    public readonly System.Collections.Concurrent.ConcurrentBag<Response> Sent = new();
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method!.Name == "get_ConnectionInfo")
        {
            var info = DispatchProxy.Create<IWebSocketConnectionInfo, InfoProxy>();
            ((InfoProxy)(object)info).ID = ID;
            return info;
        }
        if (method.Name == "Send" && args![0] is string json)
        {
            Sent.Add(JsonConvert.DeserializeObject<Response>(json)!);
            return Task.CompletedTask;
        }
        if (method.Name == "get_IsAvailable") return true;
        if (method.ReturnType == typeof(void)) return null;
        if (method.ReturnType == typeof(Task)) return Task.CompletedTask;
        throw new NotSupportedException(method.Name);
    }
}
public class InfoProxy : DispatchProxy
{
    public Guid ID;
    protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
    {
        "get_Id" => ID,
        "get_ClientIpAddress" => "127.0.0.1",
        _ => throw new NotSupportedException(method.Name)
    };
}
internal sealed class FragmentedStream : MemoryStream
{
    private readonly int chunk;
    public int Reads;
    public FragmentedStream(byte[] data, int chunk) : base(data) { this.chunk = chunk; }
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Reads++;
        return base.ReadAsync(buffer[..Math.Min(chunk, buffer.Length)], cancellationToken);
    }
}
namespace KiwisCoOpMod
{
    internal sealed class Settings
    {
        public static Settings Default { get; } = new();
        public int VconsolePort { get; set; }
        public int VconsoleProtocol { get; set; } = 211;
    }
}
