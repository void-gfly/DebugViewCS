using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DebugViewCS.Core.Settings;

namespace DebugViewCS.ViewModels;

public partial class FilterSettingsViewModel : ObservableObject
{
    public FilterSettings Settings => SettingsManager.Settings;

    // 枚举供ComboBox绑定
    public IEnumerable<FilterMode> AvailableFilterModes => [FilterMode.Include, FilterMode.Exclude];

    [ObservableProperty]
    private string _newProcessName = string.Empty;

    [ObservableProperty]
    private string _selectedProcessFilter = string.Empty;

    [ObservableProperty]
    private ColorHighlightRule? _selectedColorRule;

    [RelayCommand]
    private void AddProcess()
    {
        var processName = NewProcessName.Trim();
        if (!string.IsNullOrWhiteSpace(processName) && !Settings.ProcessFilters.Contains(processName, StringComparer.OrdinalIgnoreCase))
        {
            Settings.ProcessFilters.Add(processName);
            NewProcessName = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveProcess()
    {
        if (!string.IsNullOrEmpty(SelectedProcessFilter))
        {
            Settings.ProcessFilters.Remove(SelectedProcessFilter);
        }
    }

    [RelayCommand]
    private void AddColorRule()
    {
        // 自动添加一条默认的颜色规则
        Settings.ColorFilters.Add(new ColorHighlightRule("NewText", "#FFFF4343"));
    }

    [RelayCommand]
    private void RemoveColorRule()
    {
        if (SelectedColorRule != null)
        {
            Settings.ColorFilters.Remove(SelectedColorRule);
        }
    }

    [RelayCommand]
    private void MoveUpColorRule()
    {
        if (SelectedColorRule != null)
        {
            int index = Settings.ColorFilters.IndexOf(SelectedColorRule);
            if (index > 0)
            {
                Settings.ColorFilters.Move(index, index - 1);
            }
        }
    }

    [RelayCommand]
    private void MoveDownColorRule()
    {
        if (SelectedColorRule != null)
        {
            int index = Settings.ColorFilters.IndexOf(SelectedColorRule);
            if (index >= 0 && index < Settings.ColorFilters.Count - 1)
            {
                Settings.ColorFilters.Move(index, index + 1);
            }
        }
    }
}
