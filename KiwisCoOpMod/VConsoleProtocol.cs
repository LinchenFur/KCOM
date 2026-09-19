using System.Buffers.Binary;
using System.Text;

namespace KiwisCoOpMod
{
    internal static class VConsoleProtocol
    {
        internal const int HeaderLength = 12;

        internal static byte[] Command(string command, byte protocol)
        {
            if (command.Contains('\0'))
                throw new ArgumentException("控制台命令不能包含 NUL。", nameof(command));
            byte[] text = Encoding.UTF8.GetBytes(command);
            if (text.Length > ushort.MaxValue - HeaderLength - 1)
                throw new ArgumentException("控制台命令超过 65535 字节报文上限。", nameof(command));
            byte[] frame = Frame("CMND", protocol, text.Length + 1);
            text.CopyTo(frame, HeaderLength);
            return frame;
        }

        internal static byte[] Focus(bool focused, byte protocol)
        {
            byte[] frame = Frame("VFCS", protocol, 1);
            frame[HeaderLength] = (byte)(focused ? 1 : 0);
            return frame;
        }

        private static byte[] Frame(string type, byte protocol, int payloadLength)
        {
            byte[] frame = new byte[HeaderLength + payloadLength];
            Encoding.ASCII.GetBytes(type).CopyTo(frame, 0);
            frame[5] = protocol;
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(8, 2), (ushort)frame.Length);
            return frame;
        }

        internal static IEnumerable<string> PrintLines(byte[] data)
        {
            // StreamWatcher preserves the old API: Data begins at frame offset 10.
            const int textOffset = 30; // PRNT text begins at frame offset 40.
            if (data.Length <= textOffset)
                return Array.Empty<string>();
            int end = Array.IndexOf(data, (byte)0, textOffset);
            if (end < 0) end = data.Length;
            return Encoding.UTF8.GetString(data, textOffset, end - textOffset)
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
