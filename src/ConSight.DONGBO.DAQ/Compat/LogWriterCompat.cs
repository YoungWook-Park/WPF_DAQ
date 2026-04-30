// Bi.nsLogWriter 대체 스텁 — 원본 API와 동일한 Write/WriteException 시그니처 제공
// 파일 로그: D:\[LDAQ_REFACTOR]\LOG\app_YYYYMMDD.log
using System.IO;
using System.Text;

namespace Bi.nsLogWriter
{
    public enum LogLevel { High, Medium, Low }

    public class LogWriter
    {
        private static readonly string LogDir = @"D:\[LDAQ_REFACTOR]\LOG";
        private static readonly object _lock = new();

        public void Write(LogLevel level, StringBuilder message) => Append($"[{level}] {message}");
        public void Write(LogLevel level, string message) => Append($"[{level}] {message}");
        public void WriteInformation(string message) => Append($"[INFO] {message}");
        public void WriteWarning(string message) => Append($"[WARN] {message}");
        public void WriteException(Exception ex) => Append($"[ERR] {ex}");
        public void WriteExpException(Exception ex) => Append($"[EXP] {ex}");

        private static void Append(string text)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                string file = Path.Combine(LogDir, $"app_{DateTime.Now:yyyyMMdd}.log");
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {text}{Environment.NewLine}";
                lock (_lock) { File.AppendAllText(file, line); }
            }
            catch { /* 로그 실패는 묵인 */ }
        }
    }
}
