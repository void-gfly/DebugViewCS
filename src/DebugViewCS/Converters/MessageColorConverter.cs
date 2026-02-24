using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DebugViewCS.Core.Settings;

namespace DebugViewCS.Converters;

public class MessageColorConverter : IValueConverter
{
    private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(240, 246, 252)); // #FFF0F6FC

    public MessageColorConverter()
    {
        DefaultBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string message && !string.IsNullOrEmpty(message))
        {
            var rules = SettingsManager.Settings.ColorFilters;
            foreach (var rule in rules)
            {
                var matchText = rule.MatchText?.Trim();
                if (!string.IsNullOrEmpty(matchText) && message.Contains(matchText, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(rule.ColorHex);
                        var brush = new SolidColorBrush(color);
                        brush.Freeze(); // 冻结提升性能
                        return brush;
                    }
                    catch
                    {
                        // Ignore parse error
                    }
                }
            }
        }
        
        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
