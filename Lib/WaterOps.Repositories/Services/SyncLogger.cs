// =============================================
// SyncLogger.cs – Append-only local JSON logger
// =============================================
using System.Text.Json;
using WaterOps.Repositories.Helpers;

namespace WaterOps.Repositories.Services;

/// <summary>
/// Appends newline-delimited JSON entries to sync-log.json in the app's local data folder.
/// Thread-safe via a static lock. Never throws – logging must never crash the app.
/// </summary>
internal static class SyncLogger
{
    private static readonly string _logPath = Path.Combine(PathHelper.BasePath, "sync-log.json");
    private static readonly object _lock = new();

    internal static void Info(string message) => Write("INFO", message);
    internal static void Warn(string message) => Write("WARN", message);
    internal static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private static void Write(string level, string message, Exception? ex = null)
    {
        try
        {
            var entry = new SyncLogEntry(
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

    private record SyncLogEntry(
        DateTimeOffset Timestamp,
        string Level,
        string Message,
        string? ErrorType,
        string? Error
    );
}
