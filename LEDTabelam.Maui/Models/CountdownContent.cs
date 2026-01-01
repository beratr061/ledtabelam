using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Geri sayım içerik öğesi
/// </summary>
public partial class CountdownContent : ContentItem
{
    [ObservableProperty]
    private DateTime _targetDateTime = DateTime.Now.AddHours(1);

    [ObservableProperty]
    private string _format = "HH:mm:ss";

    [ObservableProperty]
    private string _fontName = "Default";

    [ObservableProperty]
    private Color _foregroundColor = Color.FromRgb(255, 176, 0); // Amber

    [ObservableProperty]
    private string _completedText = "SÜRE DOLDU";

    public CountdownContent()
    {
        ContentType = ContentType.Countdown;
        Name = "Geri Sayım";
    }
}
