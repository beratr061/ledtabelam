using Avalonia.Media;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Renkli metin segmenti - aynı yazıda farklı renklerde harfler için
/// Örnek: "AÇIK" kelimesinde A yeşil, Ç kırmızı, I sarı, K mavi olabilir
/// </summary>
public class ColoredTextSegment : ReactiveObject
{
    private string _text = string.Empty;
    private Color _color = Color.FromRgb(255, 176, 0); // Varsayılan Amber

    /// <summary>
    /// Segment metni (bir veya daha fazla karakter)
    /// </summary>
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value ?? string.Empty);
    }

    /// <summary>
    /// Segment rengi
    /// </summary>
    public Color Color
    {
        get => _color;
        set => this.RaiseAndSetIfChanged(ref _color, value);
    }

    /// <summary>
    /// Varsayılan constructor
    /// </summary>
    public ColoredTextSegment() { }

    /// <summary>
    /// Metin ve renk ile constructor
    /// </summary>
    public ColoredTextSegment(string text, Color color)
    {
        _text = text ?? string.Empty;
        _color = color;
    }

    /// <summary>
    /// Kopyalama
    /// </summary>
    public ColoredTextSegment Clone()
    {
        return new ColoredTextSegment(Text, Color);
    }
}
