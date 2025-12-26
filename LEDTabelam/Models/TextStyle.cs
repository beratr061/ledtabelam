using Avalonia.Media;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Metin stil ayarları (arkaplan ve stroke)
/// Requirements: 22.1, 22.2, 22.4, 22.5, 22.6
/// </summary>
public class TextStyle : ReactiveObject
{
    private bool _hasBackground = false;
    private Color _backgroundColor = Colors.Black;
    private bool _hasStroke = false;
    private int _strokeWidth = 1;
    private Color _strokeColor = Colors.Black;

    /// <summary>
    /// Metin arkaplanı aktif mi
    /// </summary>
    public bool HasBackground
    {
        get => _hasBackground;
        set => this.RaiseAndSetIfChanged(ref _hasBackground, value);
    }

    /// <summary>
    /// Arkaplan rengi
    /// </summary>
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => this.RaiseAndSetIfChanged(ref _backgroundColor, value);
    }

    /// <summary>
    /// Stroke (kontur) aktif mi
    /// </summary>
    public bool HasStroke
    {
        get => _hasStroke;
        set => this.RaiseAndSetIfChanged(ref _hasStroke, value);
    }

    /// <summary>
    /// Stroke kalınlığı (1-3 piksel)
    /// </summary>
    public int StrokeWidth
    {
        get => _strokeWidth;
        set => this.RaiseAndSetIfChanged(ref _strokeWidth, value);
    }

    /// <summary>
    /// Stroke rengi
    /// </summary>
    public Color StrokeColor
    {
        get => _strokeColor;
        set => this.RaiseAndSetIfChanged(ref _strokeColor, value);
    }
}
