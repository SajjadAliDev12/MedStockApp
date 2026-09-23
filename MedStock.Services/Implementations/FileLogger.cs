using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace MedStock.Services.Implementations
{
    /// <summary>
    /// Minimal file logger with no external packages. Fire-and-forget queue
    /// drained by a single background thread. Never throws.
    /// </summary>
    public static class FileLogger
    {
        private sealed class LogEntry
        {
            public string Level = "";
            public string Message = "";
            public Exception? Exception;
        }

        private static readonly BlockingCollection<LogEntry> _queue =
            new BlockingCollection<LogEntry>(new ConcurrentQueue<LogEntry>());

        private static readonly Thread _worker;

        static FileLogger()
        {
            _worker = new Thread(Drain)
            {
                IsBackground = true,
                Name = "MedStock-FileLogger"
            };
            _worker.Start();
        }

        public static void Info(string message, Exception? ex = null)
        {
            Enqueue("INFO", message, ex);
        }

        public static void Warn(string message, Exception? ex = null)
        {
            Enqueue("WARN", message, ex);
        }

        public static void Error(string message, Exception? ex = null)
        {
            Enqueue("ERROR", message, ex);
        }

        private static void Enqueue(string level, string? message, Exception? ex)
        {
            try
            {
                _queue.Add(new LogEntry
                {
                    Level = level,
                    Message = message ?? "",
                    Exception = ex
                });
            }
            catch
            {
                // Never throw from logging.
            }
        }

        private static void Drain()
        {
            foreach (var entry in _queue.GetConsumingEnumerable())
            {
                try
                {
                    WriteEntry(entry);
                }
                catch
                {
                    // Swallow: logging must never crash the app.
                }
            }
        }

        private static void WriteEntry(LogEntry entry)
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MedStockPro", "logs");
            Directory.CreateDirectory(dir);

            var file = Path.Combine(dir, $"medstock-{DateTime.Now:yyyyMMdd}.log");
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{entry.Level}] {entry.Message}";
            if (entry.Exception != null)
                line += " | " + entry.Exception.ToString();

            File.AppendAllText(file, line + Environment.NewLine);
        }
    }
}
