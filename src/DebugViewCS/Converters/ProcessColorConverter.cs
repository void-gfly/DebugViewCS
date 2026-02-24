using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DebugViewCS.Converters;

/// <summary>
/// 根据进程名自动分配独特颜色（HSL 色环均匀取色）
/// </summary>
public sealed class ProcessColorConverter : IValueConverter
{
    private static readonly Dictionary<string, SolidColorBrush> _colorCache = [];
    private static int _colorIndex;

    // 预定义一组在暗色背景上好看的柔和色
    private static readonly Color[] _palette =
    [
        Color.FromRgb(86, 156, 214),   // 蓝
        Color.FromRgb(78, 201, 176),   // 青绿
        Color.FromRgb(184, 215, 163),  // 浅绿
        Color.FromRgb(206, 145, 120),  // 橙棕
        Color.FromRgb(197, 134, 192),  // 紫
        Color.FromRgb(220, 220, 170),  // 米黄
        Color.FromRgb(156, 220, 254),  // 浅蓝
        Color.FromRgb(244, 135, 113),  // 珊瑚
        Color.FromRgb(141, 219, 139),  // 绿
        Color.FromRgb(229, 192, 123),  // 金
        Color.FromRgb(171, 178, 191),  // 灰蓝
        Color.FromRgb(224, 108, 117),  // 红
        Color.FromRgb(97, 175, 239),   // 亮蓝
        Color.FromRgb(209, 154, 102),  // 棕
        Color.FromRgb(152, 195, 121),  // 橄榄绿
        Color.FromRgb(198, 120, 221),  // 洋红
    ];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string processName || string.IsNullOrEmpty(processName))
            return Brushes.Transparent;

        lock (_colorCache)
        {
            if (!_colorCache.TryGetValue(processName, out var brush))
            {
                var color = _palette[_colorIndex % _palette.Length];
                // 降低饱和度作为背景色
                var bgColor = Color.FromArgb(30, color.R, color.G, color.B);
                brush = new SolidColorBrush(bgColor);
                brush.Freeze();
                _colorCache[processName] = brush;
                _colorIndex++;
            }
            return brush;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// 进程名 → 前景色 转换器
/// </summary>
public sealed class ProcessForegroundConverter : IValueConverter
{
    private static readonly Dictionary<string, SolidColorBrush> _colorCache = [];
    private static int _colorIndex;

    private static readonly Color[] _palette =
    [
        Color.FromRgb(86, 156, 214),
        Color.FromRgb(78, 201, 176),
        Color.FromRgb(184, 215, 163),
        Color.FromRgb(206, 145, 120),
        Color.FromRgb(197, 134, 192),
        Color.FromRgb(220, 220, 170),
        Color.FromRgb(156, 220, 254),
        Color.FromRgb(244, 135, 113),
        Color.FromRgb(141, 219, 139),
        Color.FromRgb(229, 192, 123),
        Color.FromRgb(171, 178, 191),
        Color.FromRgb(224, 108, 117),
        Color.FromRgb(97, 175, 239),
        Color.FromRgb(209, 154, 102),
        Color.FromRgb(152, 195, 121),
        Color.FromRgb(198, 120, 221),
    ];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string processName || string.IsNullOrEmpty(processName))
            return Brushes.White;

        lock (_colorCache)
        {
            if (!_colorCache.TryGetValue(processName, out var brush))
            {
                var color = _palette[_colorIndex % _palette.Length];
                brush = new SolidColorBrush(color);
                brush.Freeze();
                _colorCache[processName] = brush;
                _colorIndex++;
            }
            return brush;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
