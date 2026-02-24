using DebugViewCS.Core.Models;

namespace DebugViewCS.Core.Storage;

/// <summary>
/// 线程安全的环形缓冲区日志存储
/// </summary>
public sealed class LogStore
{
    private readonly LogEntry[] _buffer;
    private readonly object _lock = new();
    private long _totalCount;
    private int _head; // 下一个写入位置，也是最旧数据位置（满时）

    /// <summary>
    /// 新消息到达事件
    /// </summary>
    public event Action<IReadOnlyList<LogEntry>>? EntriesAdded;

    public LogStore(int capacity = 100_000)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _buffer = new LogEntry[capacity];
    }

    public int Capacity => _buffer.Length;

    /// <summary>
    /// 已写入的总条目数（包括已被覆盖的）
    /// </summary>
    public long TotalCount
    {
        get { lock (_lock) return _totalCount; }
    }

    /// <summary>
    /// 当前缓冲区中可读的条目数
    /// </summary>
    public int Count
    {
        get { lock (_lock) return (int)Math.Min(_totalCount, _buffer.Length); }
    }

    /// <summary>
    /// 追加单条日志
    /// </summary>
    public void Add(LogEntry entry)
    {
        lock (_lock)
        {
            _buffer[_head] = entry;
            _head = (_head + 1) % _buffer.Length;
            _totalCount++;
        }
        EntriesAdded?.Invoke([entry]);
    }

    /// <summary>
    /// 批量追加日志
    /// </summary>
    public void AddRange(IReadOnlyList<LogEntry> entries)
    {
        if (entries.Count == 0) return;

        lock (_lock)
        {
            foreach (var entry in entries)
            {
                _buffer[_head] = entry;
                _head = (_head + 1) % _buffer.Length;
                _totalCount++;
            }
        }
        EntriesAdded?.Invoke(entries);
    }

    /// <summary>
    /// 获取所有当前缓冲区中的条目（按时间顺序）
    /// </summary>
    public List<LogEntry> GetAll()
    {
        lock (_lock)
        {
            int count = Count;
            var result = new List<LogEntry>(count);

            if (_totalCount <= _buffer.Length)
            {
                // 未满，从 0 到 _head
                for (int i = 0; i < count; i++)
                    result.Add(_buffer[i]);
            }
            else
            {
                // 已满，从 _head 开始绕一圈
                for (int i = 0; i < _buffer.Length; i++)
                    result.Add(_buffer[(_head + i) % _buffer.Length]);
            }

            return result;
        }
    }

    /// <summary>
    /// 清空所有日志
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer);
            _head = 0;
            _totalCount = 0;
        }
    }

    /// <summary>
    /// 收缩缓存区，保留最新的 maxCount 条记录
    /// </summary>
    public void ShrinkTo(int maxCount)
    {
        lock (_lock)
        {
            int currentCount = Count;
            if (currentCount <= maxCount) return;

            var newest = new LogEntry[maxCount];
            if (_totalCount <= _buffer.Length)
            {
                Array.Copy(_buffer, currentCount - maxCount, newest, 0, maxCount);
            }
            else
            {
                for (int i = 0; i < maxCount; i++)
                {
                    int index = (_head - maxCount + i) % _buffer.Length;
                    if (index < 0) index += _buffer.Length;
                    newest[i] = _buffer[index];
                }
            }

            Array.Clear(_buffer);
            Array.Copy(newest, _buffer, maxCount);
            
            _totalCount = maxCount;
            _head = maxCount % _buffer.Length;
        }
    }
}
