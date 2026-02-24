using DebugViewCS.Core.Models;

namespace DebugViewCS.Core.Capture;

/// <summary>
/// 日志源接口
/// </summary>
public interface ILogSource : IDisposable
{
    /// <summary>
    /// 数据源名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 异步读取日志消息流
    /// </summary>
    IAsyncEnumerable<LogEntry> ReadAsync(CancellationToken ct);
}
