using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Metin içerik öğesi
/// </summary>
public partial class TextContent : ContentItem
{
    [ObservableProperty]
    private string _text = "";

    [ObservableProperty]
    private string _fontName = "Default";

    [ObservableProperty]
    private int _fontSize = 16;

    [ObservableProperty]
    private Color _foregroundColor = Color.FromRgb(255, 176, 0); // Amber

    [ObservableProperty]
    private Color _backgroundColor = Colors.Transparent;

    [ObservableProperty]
    private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Center;

    [ObservableProperty]
    private VerticalAlignment _verticalAlignment = VerticalAlignment.Center;

    [ObservableProperty]
    private bool _isBold = false;

    [ObservableProperty]
    private bool _isItalic = false;

    [ObservableProperty]
    private bool _isUnderline = false;

    [ObservableProperty]
    private bool _isRightToLeft = false;

    [ObservableProperty]
    private bool _isScrolling = false;

    [ObservableProperty]
    private int _scrollSpeed = 20;

    public TextContent()
    {
        ContentType = ContentType.Text;
        Name = "Metin Yazı";
    }
}
