using System;
using System.IO;

namespace KiwisCoOpModCore
{
    public static class ActivityLog
    {
        private static readonly object Gate = new();
        public static readonly string CurrentFilePath = CreateLogPath();

        private static string CreateLogPath()
        {
            try
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(root)) root = AppContext.BaseDirectory;
                string directory = Path.Combine(root, "KCOM", "logs");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "KCOM-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
                File.AppendAllText(path, "# KCOM activity log\n", System.Text.Encoding.UTF8);
                return path;
            }
            catch
            {
                return Path.Combine(Path.GetTempPath(), "KCOM-activity.log");
            }
        }

        public static void Write(string source, string? actor, string? map, string action, string? detail = null)
        {
            string line = $"{DateTimeOffset.Now:O} [{Clean(source)}] actor={Clean(actor)} map={Clean(map)} action={Clean(action)} detail={Clean(detail)}{Environment.NewLine}";
            try
            {
                lock (Gate) File.AppendAllText(CurrentFilePath, line, System.Text.Encoding.UTF8);
            }
            catch
            {
                // Logging must never stop game synchronization.
            }
        }

        private static string Clean(string? value) => (value ?? "-")
            .Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
    }
}
