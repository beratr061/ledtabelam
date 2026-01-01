using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Tabela bölge (zone) tanımı
/// </summary>
public partial class Zone : ObservableObject
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private double _widthPercent = 100;

    [ObservableProperty]
    private ZoneContentType _contentType = ZoneContentType.Text;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private HorizontalAlignment _hAlign = HorizontalAlignment.Center;

    [ObservableProperty]
    private VerticalAlignment _vAlign = VerticalAlignment.Center;

    [ObservableProperty]
    private bool _isScrolling = false;

    [ObservableProperty]
    private int _scrollSpeed = 20;

    [ObservableProperty]
    private Color _textColor = Color.FromRgb(255, 176, 0);

    [ObservableProperty]
    private int _letterSpacing = 1;

    [ObservableProperty]
    private int _lineSpacing = 2;

    [ObservableProperty]
    private BorderSettings _border = new();

    [ObservableProperty]
    private double _currentOffset;

    private double _accumulatedOffset;

    public int PixelOffset => (int)CurrentOffset;

    public void UpdateOffset(double deltaTime)
    {
        if (!IsScrolling) return;
        _accumulatedOffset += deltaTime * ScrollSpeed;
        CurrentOffset = _accumulatedOffset;
    }

    public void ResetOffset()
    {
        _accumulatedOffset = 0;
        CurrentOffset = 0;
    }

    public void SetOffset(double offset)
    {
        _accumulatedOffset = offset;
        CurrentOffset = offset;
    }
}
