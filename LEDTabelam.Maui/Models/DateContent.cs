using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Tarih içerik öğesi
/// </summary>
public partial class DateContent : ContentItem
{
    [ObservableProperty]
    private string _format = "dd.MM.yyyy";

    [ObservableProperty]
    private string _fontName = "Default";

    [ObservableProperty]
    private Color _foregroundColor = Color.FromRgb(255, 176, 0); // Amber

    public DateContent()
    {
        ContentType = ContentType.Date;
        Name = "Tarih";
    }
}
