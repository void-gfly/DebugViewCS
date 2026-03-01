using System.IO;
using System.Text.RegularExpressions;
using DebugViewCS.Core.Models;
using DebugViewCS.Core.Settings;

namespace DebugViewCS.Core.Filters;

public enum FilterType
{
    Include,
    Exclude,
    Highlight
}

public enum MatchMode
{
    Simple,
    Regex
}

/// <summary>
/// 单条过滤规则
/// </summary>
public sealed class LogFilter
{
    public string Pattern { get; set; } = string.Empty;
    public FilterType Type { get; set; } = FilterType.Include;
    public MatchMode Mode { get; set; } = MatchMode.Simple;

    private Regex? _compiledRegex;

    public bool IsMatch(string text)
    {
        if (string.IsNullOrEmpty(Pattern))
            return false;

        if (Mode == MatchMode.Regex)
        {
            _compiledRegex ??= new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return _compiledRegex.IsMatch(text);
        }

        return text.Contains(Pattern, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 当 Pattern 变化时清除编译缓存
    /// </summary>
    public void InvalidateCache() => _compiledRegex = null;
}

/// <summary>
/// 过滤引擎 — 判定日志条目是否应该显示
/// </summary>
public static class FilterEngine
{
    /// <summary>
    /// 判断一条日志是否应该被包含在视图中
    /// </summary>
    public static bool ShouldInclude(LogEntry entry, IReadOnlyList<LogFilter> filters)
    {
        if (filters.Count == 0)
            return true;

        bool hasIncludeFilter = false;
        bool matchedInclude = false;

        foreach (var filter in filters)
        {
            string textToMatch = filter.Type switch
            {
                _ => entry.Message
            };

            bool matched = filter.IsMatch(textToMatch);

            switch (filter.Type)
            {
                case FilterType.Exclude:
                    if (matched) return false; // 排除优先
                    break;
                case FilterType.Include:
                    hasIncludeFilter = true;
                    if (matched) matchedInclude = true;
                    break;
                case FilterType.Highlight:
                    // Highlight 不影响包含性判定
                    break;
            }
        }

        // 如果有 Include 过滤器，必须至少匹配一个
        if (hasIncludeFilter && !matchedInclude)
            return false;

        return true;
    }

    /// <summary>
    /// 基于增强过滤规则判断是否显示日志
    /// </summary>
    public static bool MatchesFilter(LogEntry entry)
    {
        var settings = Settings.SettingsManager.Settings;
        if (!settings.IsFilterEnabled)
        {
            return true;
        }

        var processFilters = settings.ProcessFilters;
        
        // 如果没有进程过滤规则，全部显示
        if (processFilters.Count == 0)
        {
            return true;
        }

        bool matchProcess = false;
        foreach (var proc in processFilters)
        {
            if (IsProcessMatch(entry.ProcessName, proc))
            {
                matchProcess = true;
                break;
            }
        }

        if (settings.ProcessFilterMode == Settings.FilterMode.Include)
        {
            // 包含模式：必须匹配哪怕一个
            return matchProcess;
        }
        else
        {
            // 排除模式：匹配到了就不显示
            return !matchProcess;
        }
    }

    private static bool IsProcessMatch(string processName, string filterText)
    {
        var normalizedProcess = NormalizeProcessToken(processName);
        var normalizedFilter = NormalizeProcessToken(filterText);

        if (string.IsNullOrEmpty(normalizedProcess) || string.IsNullOrEmpty(normalizedFilter))
        {
            return false;
        }

        // 优先走标准化后的精确匹配，避免 ".exe" / 路径 / 空白带来的误判
        if (string.Equals(normalizedProcess, normalizedFilter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 保留历史的模糊匹配行为，兼容用户已有过滤配置
        return normalizedProcess.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeProcessToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var normalized = token.Trim().Trim('"');

        // 支持用户输入完整路径
        if (normalized.Contains('\\') || normalized.Contains('/'))
        {
            normalized = Path.GetFileName(normalized);
        }

        if (normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4];
        }

        return normalized.Trim();
    }
}
