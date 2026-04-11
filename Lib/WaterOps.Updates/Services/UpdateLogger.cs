// =============================================
// UpdateLogger.cs – Append-only local JSON logger
// =============================================
using System.Text.Json;

namespace WaterOps.Updates.Services;

/// <summary>
/// Appends newline-delimited JSON entries to update-log.json in the app's local data folder.
/// Thread-safe via a static lock. Never throws – logging must never crash the app.
/// </summary>
internal static class UpdateLogger
{
    private static readonly string _logPath = Path.Combine(BasePath, "update-log.json");
    private static readonly object _lock = new();

    private static string BasePath
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WaterOS"
            );
            Directory.CreateDirectory(path);
            return path;
        }
    }

    internal static void Info(string message) => Write("INFO", message);
    internal static void Warn(string message) => Write("WARN", message);
    internal static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private static void Write(string level, string message, Exception? ex = null)
    {
        try
        {
            var entry = new UpdateLogEntry(
                DateTimeOffset.UtcNow,
                level,
                message,
                ex?.GetType().Name,
                ex?.Message
            );
            lock (_lock)
                File.AppendAllText(_logPath, JsonSerializer.Serialize(entry) + Environment.NewLine);
        }
        catch { /* logging must never crash the app */ }
    }

    private record UpdateLogEntry(
        DateTimeOffset Timestamp,
        string Level,
        string Message,
        string? ErrorType,
        string? Error
    );
}
