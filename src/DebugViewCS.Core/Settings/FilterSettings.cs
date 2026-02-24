using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DebugViewCS.Core.Settings;

public enum FilterMode
{
    Include,
    Exclude
}

public partial class ColorHighlightRule : ObservableObject
{
    [ObservableProperty]
    private string _matchText = string.Empty;

    [ObservableProperty]
    private string _colorHex = "#FFFF0000"; // Default to Red

    public ColorHighlightRule() { }

    public ColorHighlightRule(string matchText, string colorHex)
    {
        MatchText = matchText;
        ColorHex = colorHex;
    }
}

public partial class FilterSettings : ObservableObject
{
    /// <summary>
    /// 是否启用过滤模式
    /// </summary>
    [ObservableProperty]
    private bool _isFilterEnabled = false;

    /// <summary>
    /// 进程过滤模式：包含(Include) 或 排除(Exclude)
    /// </summary>
    [ObservableProperty]
    private FilterMode _processFilterMode = FilterMode.Include;

    /// <summary>
    /// 需要过滤的进程名列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _processFilters = new();

    /// <summary>
    /// 文本高亮颜色规则列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ColorHighlightRule> _colorFilters = new();

    public FilterSettings()
    {
    }
}
