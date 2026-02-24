using System.Threading.Channels;
using DebugViewCS.Core.Models;
using DebugViewCS.Core.Storage;

namespace DebugViewCS.Core.Capture;

/// <summary>
/// 管理所有日志源，合并消息流并写入 LogStore
/// </summary>
public sealed class LogSourceManager : IDisposable
{
    private readonly LogStore _store;
    private readonly List<ILogSource> _sources = [];
    private readonly Channel<LogEntry> _channel;
    private CancellationTokenSource? _cts;
    private Task? _consumerTask;
    private readonly List<Task> _producerTasks = [];

    public LogSourceManager(LogStore store)
    {
        _store = store;
        _channel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>
    /// 注册一个日志源
    /// </summary>
    public void AddSource(ILogSource source)
    {
        _sources.Add(source);
    }

    /// <summary>
    /// 启动所有日志源的读取
    /// </summary>
    public void Start()
    {
        if (_cts != null) return;

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        // 为每个数据源启动一个生产者任务
        foreach (var source in _sources)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    await foreach (var entry in source.ReadAsync(ct))
                    {
                        await _channel.Writer.WriteAsync(entry, ct);
                    }
                }
                catch (OperationCanceledException) { }
            }, ct);
            _producerTasks.Add(task);
        }

        // 启动消费者：从 channel 读取并写入 LogStore
        _consumerTask = Task.Run(async () =>
        {
            var batch = new List<LogEntry>(64);
            try
            {
                await foreach (var entry in _channel.Reader.ReadAllAsync(ct))
                {
                    batch.Add(entry);

                    // 批量读取：尝试一次性读取 channel 中所有待处理的
                    while (batch.Count < 256 && _channel.Reader.TryRead(out var extra))
                    {
                        batch.Add(extra);
                    }

                    _store.AddRange(batch);
                    batch.Clear();
                }
            }
            catch (OperationCanceledException) { }
        }, ct);
    }

    /// <summary>
    /// 停止所有日志源
    /// </summary>
    public async Task StopAsync()
    {
        if (_cts == null) return;

        _cts.Cancel();
        _channel.Writer.TryComplete();

        try
        {
            await Task.WhenAll(_producerTasks);
        }
        catch (OperationCanceledException) { }

        if (_consumerTask != null)
        {
            try { await _consumerTask; }
            catch (OperationCanceledException) { }
        }

        _producerTasks.Clear();
        _cts.Dispose();
        _cts = null;
    }

    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

    public void Dispose()
    {
        _cts?.Cancel();
        _channel.Writer.TryComplete();
        foreach (var source in _sources)
            source.Dispose();
        _cts?.Dispose();
    }
}
