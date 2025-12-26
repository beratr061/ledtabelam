using Avalonia.Media;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Tabela bölge (zone) tanımı
/// Requirements: 17.1, 17.2, 17.3
/// </summary>
public class Zone : ReactiveObject
{
    private int _index;
    private double _widthPercent = 100;
    private ZoneContentType _contentType = ZoneContentType.Text;
    private string _content = string.Empty;
    private HorizontalAlignment _hAlign = HorizontalAlignment.Center;
    private VerticalAlignment _vAlign = VerticalAlignment.Center;
    private bool _isScrolling = false;
    private int _scrollSpeed = 20;
    private Color _textColor = Color.FromRgb(255, 176, 0); // Varsayılan Amber

    /// <summary>
    /// Zone sıra numarası
    /// </summary>
    public int Index
    {
        get => _index;
        set => this.RaiseAndSetIfChanged(ref _index, value);
    }

    /// <summary>
    /// Zone genişliği (yüzde olarak, toplam %100)
    /// </summary>
    public double WidthPercent
    {
        get => _widthPercent;
        set => this.RaiseAndSetIfChanged(ref _widthPercent, value);
    }

    /// <summary>
    /// İçerik tipi (Metin, Resim, Kayan Yazı)
    /// </summary>
    public ZoneContentType ContentType
    {
        get => _contentType;
        set => this.RaiseAndSetIfChanged(ref _contentType, value);
    }

    /// <summary>
    /// Zone içeriği (metin veya dosya yolu)
    /// </summary>
    public string Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    /// <summary>
    /// Yatay hizalama
    /// </summary>
    public HorizontalAlignment HAlign
    {
        get => _hAlign;
        set => this.RaiseAndSetIfChanged(ref _hAlign, value);
    }

    /// <summary>
    /// Dikey hizalama
    /// </summary>
    public VerticalAlignment VAlign
    {
        get => _vAlign;
        set => this.RaiseAndSetIfChanged(ref _vAlign, value);
    }

    /// <summary>
    /// Kayan yazı modu aktif mi
    /// </summary>
    public bool IsScrolling
    {
        get => _isScrolling;
        set => this.RaiseAndSetIfChanged(ref _isScrolling, value);
    }

    /// <summary>
    /// Kayan yazı hızı (piksel/saniye)
    /// </summary>
    public int ScrollSpeed
    {
        get => _scrollSpeed;
        set => this.RaiseAndSetIfChanged(ref _scrollSpeed, value);
    }

    /// <summary>
    /// Zone metin rengi (RGB modunda kullanılır)
    /// </summary>
    public Color TextColor
    {
        get => _textColor;
        set => this.RaiseAndSetIfChanged(ref _textColor, value);
    }
}
