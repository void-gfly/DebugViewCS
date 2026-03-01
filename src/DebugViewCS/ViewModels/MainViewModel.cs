using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DebugViewCS.Core.Capture;
using DebugViewCS.Core.Filters;
using DebugViewCS.Core.Models;
using DebugViewCS.Core.Settings;
using DebugViewCS.Core.Storage;
using DebugViewCS.Core.Utils;

namespace DebugViewCS.ViewModels;

public enum LogClearMode
{
    None = 0,
    AutoClear_5000,
    AutoClear_10000,
    KeepMax_5000,
    KeepMax_10000
}

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly LogStore _logStore;
    private readonly LogSourceManager _sourceManager;
    private readonly ProcessResolver _processResolver;
    private readonly DispatcherTimer _refreshTimer;
    private readonly object _pendingLock = new();
    private List<LogEntry> _pendingEntries = [];

    [ObservableProperty]
    private bool _isCapturing;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private long _totalMessages;

    [ObservableProperty]
    private int _visibleMessages;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private string _lastUnfilteredMessage = "最后捕获: 无";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAutoClear5000))]
    [NotifyPropertyChangedFor(nameof(IsAutoClear10000))]
    [NotifyPropertyChangedFor(nameof(IsKeepMax5000))]
    [NotifyPropertyChangedFor(nameof(IsKeepMax10000))]
    private LogClearMode _clearMode = LogClearMode.KeepMax_10000;

    public bool IsAutoClear5000 => ClearMode == LogClearMode.AutoClear_5000;
    public bool IsAutoClear10000 => ClearMode == LogClearMode.AutoClear_10000;
    public bool IsKeepMax5000 => ClearMode == LogClearMode.KeepMax_5000;
    public bool IsKeepMax10000 => ClearMode == LogClearMode.KeepMax_10000;

    /// <summary>
    /// 显示在 ListView 中的日志条目（已过滤）
    /// </summary>
    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    public FilterSettings Settings => SettingsManager.Settings;

    public MainViewModel()
    {
        _processResolver = new ProcessResolver();
        _logStore = new LogStore(100_000);
        _sourceManager = new LogSourceManager(_logStore);

        // 注册 OutputDebugString 数据源
        _sourceManager.AddSource(new DbWinReader(false, _processResolver));

        // 监听新消息
        _logStore.EntriesAdded += OnEntriesAdded;

        // 监听配置变更然后自动重刷UI
        SettingsManager.Settings.ProcessFilters.CollectionChanged += (s, e) => ReapplyFilter();
        SettingsManager.Settings.PropertyChanged += (s, e) => ReapplyFilter();

        // UI 批量刷新定时器 (~60fps)
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _refreshTimer.Tick += OnRefreshTick;
        _refreshTimer.Start();
    }

    private void OnEntriesAdded(IReadOnlyList<LogEntry> entries)
    {
        lock (_pendingLock)
        {
            _pendingEntries.AddRange(entries);
        }
    }

    private void OnRefreshTick(object? sender, EventArgs e)
    {
        List<LogEntry> batch;
        lock (_pendingLock)
        {
            if (_pendingEntries.Count == 0) return;
            batch = _pendingEntries;
            _pendingEntries = new List<LogEntry>(64);
        }

        foreach (var entry in batch)
        {
            if (FilterEngine.MatchesFilter(entry))
            {
                LogEntries.Add(entry);
            }
        }

        if (batch.Count > 0)
        {
            var lastEntry = batch[^1];
            LastUnfilteredMessage = GetCleanMessageText(lastEntry);
        }

        ApplyClearModeLimits();

        TotalMessages = _logStore.TotalCount;
        VisibleMessages = LogEntries.Count;
        StatusText = IsCapturing ? $"捕获中[{TotalMessages}]" : "已暂停";
    }

    private void ApplyClearModeLimits()
    {
        if (ClearMode == LogClearMode.AutoClear_5000 && _logStore.Count >= 5000)
        {
            Clear();
            return;
        }
        else if (ClearMode == LogClearMode.AutoClear_10000 && _logStore.Count >= 10000)
        {
            Clear();
            return;
        }

        int? shrinkLimit = null;
        if (ClearMode == LogClearMode.KeepMax_5000) shrinkLimit = 5000;
        else if (ClearMode == LogClearMode.KeepMax_10000) shrinkLimit = 10000;

        if (shrinkLimit.HasValue && _logStore.Count > shrinkLimit.Value)
        {
            _logStore.ShrinkTo(shrinkLimit.Value);
            
            var all = _logStore.GetAll();
            if (all.Count > 0)
            {
                long minId = all[0].Id;
                while (LogEntries.Count > 0 && LogEntries[0].Id < minId)
                {
                    LogEntries.RemoveAt(0);
                }
            }
            else
            {
                LogEntries.Clear();
            }
        }
    }

    [RelayCommand]
    private void ToggleCapture()
    {
        if (IsCapturing)
        {
            _ = _sourceManager.StopAsync();
            IsCapturing = false;
            StatusText = "已停止";
        }
        else
        {
            _sourceManager.Start();
            IsCapturing = true;
            StatusText = $"捕获中[{TotalMessages}]";
        }
    }

    [RelayCommand]
    private void SetClearMode(string modeStr)
    {
        if (Enum.TryParse<LogClearMode>(modeStr, out var mode))
        {
            if (ClearMode == mode)
                ClearMode = LogClearMode.None;
            else
            {
                ClearMode = mode;
                ApplyClearModeLimits();
                TotalMessages = _logStore.TotalCount;
                VisibleMessages = LogEntries.Count;
            }
        }
    }

    [RelayCommand]
    private void Clear()
    {
        _logStore.Clear();
        LogEntries.Clear();
        TotalMessages = 0;
        VisibleMessages = 0;
        LastUnfilteredMessage = "最后捕获: 无";
    }

    public void ReapplyFilter()
    {
        LogEntries.Clear();
        var all = _logStore.GetAll();
        foreach (var entry in all)
        {
            if (FilterEngine.MatchesFilter(entry))
            {
                LogEntries.Add(entry);
            }
        }
        VisibleMessages = LogEntries.Count;
    }

    [RelayCommand]
    private void CopyFullMessage(LogEntry? entry)
    {
        if (entry == null) return;
        var msg = entry.Message ?? string.Empty;
        var prefix = msg.StartsWith("[") ? "" : " ";
        var text = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss,fff} [{entry.ProcessName}]{prefix}{msg}";
        try
        {
            System.Windows.Clipboard.SetText(text);
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private void CopyMessageText(LogEntry? entry)
    {
        if (entry == null) return;
        var cleanText = GetCleanMessageText(entry);

        try
        {
            System.Windows.Clipboard.SetText(cleanText);
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private void AddProcessToFilter(LogEntry? entry)
    {
        if (entry == null) return;
        var processName = entry.ProcessName.Trim();
        if (string.IsNullOrWhiteSpace(processName)) return;

        var filters = SettingsManager.Settings.ProcessFilters;
        if (!filters.Contains(processName, StringComparer.OrdinalIgnoreCase))
        {
            filters.Add(processName);
            // ReapplyFilter() will be called via CollectionChanged event
        }
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _sourceManager.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string GetCleanMessageText(LogEntry entry)
    {
        var msg = entry.Message ?? string.Empty;
        var cleanText = System.Text.RegularExpressions.Regex.Replace(msg, @"^(?:\[[^\]]*\]|-|\s)+", "");
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            cleanText = msg;
        }
        return cleanText.Replace("\r", "").Replace("\n", " ");
    }
}
