using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Renkli metin segmenti - aynı yazıda farklı renklerde harfler için
/// </summary>
public partial class ColoredTextSegment : ObservableObject
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private Color _color = Color.FromRgb(255, 176, 0);

    public ColoredTextSegment() { }

    public ColoredTextSegment(string text, Color color)
    {
        _text = text ?? string.Empty;
        _color = color;
    }

    public ColoredTextSegment Clone()
    {
        return new ColoredTextSegment(Text, Color);
    }
}
