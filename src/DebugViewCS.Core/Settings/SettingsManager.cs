using System.Text.Json;
using System.Text.Json.Serialization;

namespace DebugViewCS.Core.Settings;

public static class SettingsManager
{
    private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "filter_settings.json");

    private static FilterSettings? _currentSettings;

    public static FilterSettings Settings
    {
        get
        {
            if (_currentSettings == null)
            {
                LoadSettings();
            }
            return _currentSettings!;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                _currentSettings = JsonSerializer.Deserialize<FilterSettings>(json, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load filter settings: {ex.Message}");
        }

        if (_currentSettings == null)
        {
            _currentSettings = new FilterSettings();
            // 添加几个常见的颜色文本
            _currentSettings.ColorFilters.Add(new ColorHighlightRule("Error", "#FFFF4343")); // Red
            _currentSettings.ColorFilters.Add(new ColorHighlightRule("Warning", "#FFFFD800")); // Yellow
            _currentSettings.ColorFilters.Add(new ColorHighlightRule("Exception", "#FFFF0000")); // Bright Red
            _currentSettings.ColorFilters.Add(new ColorHighlightRule("Success", "#FF00FF00")); // Green
            SaveSettings();
        }

        // 监听集合变化自动保存
        _currentSettings.ProcessFilters.CollectionChanged += (s, e) => SaveSettings();
        _currentSettings.ColorFilters.CollectionChanged += (s, e) => SaveSettings();
        _currentSettings.PropertyChanged += (s, e) => SaveSettings();
    }

    public static void SaveSettings()
    {
        if (_currentSettings == null) return;
        
        try
        {
            var json = JsonSerializer.Serialize(_currentSettings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save filter settings: {ex.Message}");
        }
    }
}
