using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;
using DebugViewCS.Core.Settings;

namespace DebugViewCS.Views;

public partial class FilterSettingsWindow : FluentWindow
{
    public FilterSettingsWindow()
    {
        InitializeComponent();
    }

    private void ColorListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.ListView listView && 
            listView.SelectedItem is ColorHighlightRule rule)
        {
            // Parse existing color
            System.Drawing.Color initialColor = System.Drawing.Color.Red;
            try
            {
                var wpfColor = (Color)ColorConverter.ConvertFromString(rule.ColorHex);
                initialColor = System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
            }
            catch { }

            using var colorDialog = new System.Windows.Forms.ColorDialog
            {
                AllowFullOpen = true,
                Color = initialColor
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = colorDialog.Color;
                rule.ColorHex = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
                SettingsManager.SaveSettings(); // Force save on change
            }
        }
    }
}
