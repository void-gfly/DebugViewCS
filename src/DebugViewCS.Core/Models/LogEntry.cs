namespace DebugViewCS.Core.Models;

/// <summary>
/// 日志条目数据模型
/// </summary>
public sealed record LogEntry
{
    public long Id { get; init; }
    public DateTime Timestamp { get; init; }
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
