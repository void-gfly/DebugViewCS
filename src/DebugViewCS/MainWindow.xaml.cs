using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DebugViewCS.ViewModels;
using Wpf.Ui.Controls;

namespace DebugViewCS;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private ScrollViewer? _scrollViewer;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = (MainViewModel)DataContext;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 查找 ListView 内部的 ScrollViewer 用于自动滚动
        _scrollViewer = FindVisualChild<ScrollViewer>(LogListView);

        // 监听自动滚动
        _viewModel.LogEntries.CollectionChanged += (_, args) =>
        {
            if (_viewModel.AutoScroll && _scrollViewer != null && args.NewItems?.Count > 0)
            {
                _scrollViewer.ScrollToEnd();
            }
        };

        // 默认启动捕获
        _viewModel.ToggleCaptureCommand.Execute(null);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.Dispose();
    }

    private void ClearOptions_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
        }
    }

    private void OpenFilterSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new Views.FilterSettingsWindow
        {
            Owner = this
        };
        settingsWindow.ShowDialog();

        // 当设置窗口关闭时强制刷新界面，确保已存在的日志项能立刻响应新的高亮颜色变更
        LogListView.Items.Refresh();
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
                return found;

            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }
}