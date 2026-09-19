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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// https://stackoverflow.com/a/2784979

namespace KiwisCoOpMod
{
    public delegate void MessageAvailableEventHandler(object sender,
    MessageAvailableEventArgs e);

    public class MessageAvailableEventArgs : EventArgs
    {
        public MessageAvailableEventArgs(string messageType, byte[] data) : base()
        {
            MessageType = messageType;
            Data = data;
        }

        public string MessageType { get; private set; }
        public byte[] Data { get; private set;}
    }
    public class StreamWatcher
    {
        private readonly Stream stream;
        private CancellationTokenSource? cancellation;
        public Task Completion { get; private set; } = Task.CompletedTask;

        public StreamWatcher(Stream stream)
        {
            this.stream = stream;
        }

        public void SetWorking(bool state)
        {
            if (!state)
            {
                cancellation?.Cancel();
                return;
            }
            if (!Completion.IsCompleted) return;
            cancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            // Never run the read loop or its callbacks on the WinForms UI context.
            Completion = Task.Run(() => ReadAsync(token));
        }

        protected void OnMessageAvailable(MessageAvailableEventArgs e)
        {
            MessageAvailable?.Invoke(this, e);
        }

        private async Task<bool> ReadFullyAsync(byte[] buffer, CancellationToken token)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int count = await stream.ReadAsync(buffer.AsMemory(offset), token).ConfigureAwait(false);
                if (count == 0) return false;
                offset += count;
            }
            return true;
        }

        private async Task ReadAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // First 10 header bytes include the full big-endian uint16 length.
                    // Keep the final 2 header bytes in Data for existing event consumers.
                    byte[] header = new byte[10];
                    if (!await ReadFullyAsync(header, token).ConfigureAwait(false)) return;
                    int length = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(8, 2));
                    if (length < VConsoleProtocol.HeaderLength)
                        throw new InvalidDataException("VConsole 报文长度小于报头。");
                    byte[] data = new byte[length - header.Length];
                    if (!await ReadFullyAsync(data, token).ConfigureAwait(false)) return;
                    OnMessageAvailable(new MessageAvailableEventArgs(Encoding.ASCII.GetString(header, 0, 4), data));
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (ObjectDisposedException) { }
            catch (Exception e) when (e is IOException or InvalidDataException)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        public event MessageAvailableEventHandler? MessageAvailable;
    }
}
