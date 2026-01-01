using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Metin stil ayarları (arkaplan ve stroke)
/// </summary>
public partial class TextStyle : ObservableObject
{
    [ObservableProperty]
    private bool _hasBackground = false;

    [ObservableProperty]
    private Color _backgroundColor = Colors.Black;

    [ObservableProperty]
    private bool _hasStroke = false;

    [ObservableProperty]
    private int _strokeWidth = 1;

    [ObservableProperty]
    private Color _strokeColor = Colors.Black;
}
