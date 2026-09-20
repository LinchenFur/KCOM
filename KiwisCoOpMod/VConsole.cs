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
using KiwisCoOpModCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Websocket.Client;

namespace KiwisCoOpMod
{
    public class VConsole
    {
        private WebsocketClient? ws;
        private TcpClient? client;
        private NetworkStream? stream;
        private readonly object writeLock = new();
        private StreamWatcher? watcher;
        private readonly List<byte[]> commandQueue = new();
        private CancellationTokenSource? bootstrapCancellation;

        public bool Connect(WebsocketClient ws)
        {
            this.ws = ws;
            return Connect();
        }

        public bool Connect()
        {
            Disconnect();
            try
            {
                client = new TcpClient();
                if (!client.ConnectAsync("127.0.0.1", Settings.Default.VconsolePort).Wait(2000))
                {
                    ActivityLog.Write("VCONSOLE", Settings.Default.ClientUsername, "", "connect_failed", "127.0.0.1:" + Settings.Default.VconsolePort);
                    Disconnect();
                    return false;
                }
                ActivityLog.Write("VCONSOLE", Settings.Default.ClientUsername, "", "connected", "127.0.0.1:" + Settings.Default.VconsolePort);
                stream = client.GetStream();
                stream.WriteTimeout = 2000;
                watcher = new StreamWatcher(stream);
                watcher.MessageAvailable += MessageAvailable;
                watcher.SetWorking(true);
                WriteWindowFocus();
                lock (writeLock)
                {
                    foreach (byte[] command in commandQueue) stream.Write(command);
                    commandQueue.Clear();
                }
                return true;
            }
            catch (Exception e) when (e is SocketException or IOException or AggregateException)
            {
                Debug.WriteLine(e);
                Disconnect();
                return false;
            }
        }

        // Started only after WebSocket authentication. A new session also handles
        // connecting to a map that was already running before KCOM was opened.
        public void StartBootstrapProbe()
        {
            bootstrapCancellation?.Cancel();
            bootstrapCancellation?.Dispose();
            bootstrapCancellation = new CancellationTokenSource();
            var token = bootstrapCancellation.Token;
            var connectedStream = stream;
            var connectedWatcher = watcher;
            if (connectedStream == null) return;
            string session = Guid.NewGuid().ToString("N");
            string command = "sv_cheats 1;script KCOM_BOOTSTRAP_SESSION=\"" + session + "\";script_execute kcom_bootstrap";
            byte[] frame = VConsoleProtocol.Command(command, Convert.ToByte(Settings.Default.VconsoleProtocol));
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested && connectedWatcher?.Completion.IsCompleted == false)
                    {
                        lock (writeLock)
                        {
                            if (token.IsCancellationRequested || stream != connectedStream) return;
                            connectedStream.Write(frame);
                        }
                        await Task.Delay(2000, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { }
                catch (Exception e) when (e is IOException or ObjectDisposedException)
                {
                    Debug.WriteLine(e);
                }
            }, token);
        }

        private void MessageAvailable(object sender, MessageAvailableEventArgs e)
        {
            if (sender != watcher || ws == null || !ws.IsStarted || e.MessageType != "PRNT") return;
            foreach (string line in VConsoleProtocol.PrintLines(e.Data))
            {
                ActivityLog.Write("VCONSOLE", Settings.Default.ClientUsername, "", "game_print", line);
                Response input = new("print", line);
                input.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                ws.Send(JsonConvert.SerializeObject(input));
            }
        }

        public void Disconnect()
        {
            bootstrapCancellation?.Cancel();
            watcher?.SetWorking(false);
            // Close before taking the send lock: unblock any pending network write.
            client?.Close();
            lock (writeLock)
            {
                stream = null;
                client = null;
            }
        }

        public void WriteRaw(byte[] data)
        {
            lock (writeLock)
            {
                if (stream != null) stream.Write(data);
            }
        }

        public void WriteWindowFocus(bool focused = true)
        {
            WriteRaw(VConsoleProtocol.Focus(focused, Convert.ToByte(Settings.Default.VconsoleProtocol)));
        }

        public void WriteCommand(string command, bool urgent = false)
        {
            ActivityLog.Write("VCONSOLE", Settings.Default.ClientUsername, "", "send_command", command);
            byte[] frame = VConsoleProtocol.Command(command, Convert.ToByte(Settings.Default.VconsoleProtocol));
            lock (writeLock)
            {
                if (stream != null) stream.Write(frame);
                else if (urgent) commandQueue.Add(frame);
            }
        }
    }
}
