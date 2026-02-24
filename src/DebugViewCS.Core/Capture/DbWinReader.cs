using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;
using DebugViewCS.Core.Models;
using DebugViewCS.Core.Utils;

namespace DebugViewCS.Core.Capture;

/// <summary>
/// OutputDebugString 消息捕获器
/// 通过 DBWIN_BUFFER 共享内存 + 事件同步机制读取调试消息
/// </summary>
public sealed class DbWinReader : ILogSource
{
    private const int BufferSize = 4096;
    private const int PidOffset = 0;
    private const int PidSize = 4;
    private const int MessageOffset = 4;
    private const int MessageSize = BufferSize - MessageOffset;

    private readonly bool _global;
    private readonly ProcessResolver _processResolver;
    private long _lineCounter;

    private MemoryMappedFile? _mmf;
    private EventWaitHandle? _bufferReady;
    private EventWaitHandle? _dataReady;

    public DbWinReader(bool global, ProcessResolver processResolver)
    {
        _global = global;
        _processResolver = processResolver;
    }

    public string Name => _global ? "Global OutputDebugString" : "Local OutputDebugString";

    private string Prefix => _global ? @"Global\" : "";

    public async IAsyncEnumerable<LogEntry> ReadAsync([EnumeratorCancellation] CancellationToken ct)
    {
        // 创建共享内存和同步事件
        _mmf = MemoryMappedFile.CreateOrOpen($"{Prefix}DBWIN_BUFFER", BufferSize);
        _bufferReady = new EventWaitHandle(false, EventResetMode.AutoReset, $"{Prefix}DBWIN_BUFFER_READY");
        _dataReady = new EventWaitHandle(false, EventResetMode.AutoReset, $"{Prefix}DBWIN_DATA_READY");

        using var accessor = _mmf.CreateViewAccessor(0, BufferSize, MemoryMappedFileAccess.Read);
        var msgBuffer = new byte[MessageSize];

        while (!ct.IsCancellationRequested)
        {
            // 通知发送方缓冲区就绪
            _bufferReady.Set();

            // 等待数据到达（100ms 超时，避免阻塞 cancellation）
            if (!_dataReady.WaitOne(100))
            {
                await Task.Yield();
                continue;
            }

            // 读取 PID
            int pid = accessor.ReadInt32(PidOffset);

            // 读取消息
            accessor.ReadArray(MessageOffset, msgBuffer, 0, MessageSize);

            // 找到 null 终止符
            int nullIndex = Array.IndexOf(msgBuffer, (byte)0);
            int length = nullIndex >= 0 ? nullIndex : MessageSize;

            string message = Encoding.UTF8.GetString(msgBuffer, 0, length);

            // 去除尾部换行
            message = message.TrimEnd('\r', '\n');

            if (string.IsNullOrEmpty(message))
                continue;

            string processName = _processResolver.Resolve(pid);

            var entry = new LogEntry
            {
                Id = Interlocked.Increment(ref _lineCounter),
                Timestamp = DateTime.Now,
                ProcessId = pid,
                ProcessName = processName,
                Message = message
            };

            yield return entry;
        }
    }

    public void Dispose()
    {
        _bufferReady?.Dispose();
        _dataReady?.Dispose();
        _mmf?.Dispose();
    }
}
