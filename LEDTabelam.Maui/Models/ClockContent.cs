using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Saat içerik öğesi
/// </summary>
public partial class ClockContent : ContentItem
{
    [ObservableProperty]
    private string _format = "HH:mm:ss";

    [ObservableProperty]
    private string _fontName = "Default";

    [ObservableProperty]
    private Color _foregroundColor = Color.FromRgb(255, 176, 0); // Amber

    [ObservableProperty]
    private bool _showSeconds = true;

    [ObservableProperty]
    private bool _is24Hour = true;

    public ClockContent()
    {
        ContentType = ContentType.Clock;
        Name = "Saat";
    }
}
