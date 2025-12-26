using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using LEDTabelam.Models;

namespace LEDTabelam.Views;

public partial class ZoneEditor : UserControl
{
    public ZoneEditor()
    {
        InitializeComponent();
    }

    private void OnRedColorClick(object? sender, RoutedEventArgs e)
    {
        SetZoneColor(sender, Color.FromRgb(255, 0, 0));
    }

    private void OnGreenColorClick(object? sender, RoutedEventArgs e)
    {
        SetZoneColor(sender, Color.FromRgb(0, 255, 0));
    }

    private void OnAmberColorClick(object? sender, RoutedEventArgs e)
    {
        SetZoneColor(sender, Color.FromRgb(255, 176, 0));
    }

    private void OnWhiteColorClick(object? sender, RoutedEventArgs e)
    {
        SetZoneColor(sender, Color.FromRgb(255, 255, 255));
    }

    private void SetZoneColor(object? sender, Color color)
    {
        if (sender is Button button && button.Tag is Zone zone)
        {
            zone.TextColor = color;
        }
    }
}
