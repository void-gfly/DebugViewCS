using System.Diagnostics;
using System.Collections.Concurrent;

namespace DebugViewCS.Core.Utils;

/// <summary>
/// PID → 进程名 解析器，带缓存
/// </summary>
public sealed class ProcessResolver
{
    private readonly ConcurrentDictionary<int, string> _cache = new();

    /// <summary>
    /// 根据 PID 获取进程名称，优先从缓存读取
    /// </summary>
    public string Resolve(int processId)
    {
        return _cache.GetOrAdd(processId, pid =>
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                return process.ProcessName;
            }
            catch
            {
                return $"<{pid}>";
            }
        });
    }

    /// <summary>
    /// 清理不再存活的进程缓存条目
    /// </summary>
    public void Prune()
    {
        foreach (var kvp in _cache)
        {
            try
            {
                Process.GetProcessById(kvp.Key);
            }
            catch
            {
                _cache.TryRemove(kvp.Key, out _);
            }
        }
    }

    public void Clear() => _cache.Clear();
}
