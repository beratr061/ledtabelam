using Avalonia.Media;
using ReactiveUI;
using AvaloniaHAlign = Avalonia.Layout.HorizontalAlignment;
using AvaloniaVAlign = Avalonia.Layout.VerticalAlignment;

namespace LEDTabelam.Models;

/// <summary>
/// Tabela bölge (zone) tanımı
/// Requirements: 17.1, 17.2, 17.3
/// 
/// Her zone kendi animasyon state'ini tutar:
/// - ScrollSpeed: Zone'a özel kayan yazı hızı
/// - CurrentOffset: Zone'un mevcut scroll pozisyonu (DeltaTime ile hesaplanır)
/// </summary>
public class Zone : ReactiveObject
{
    private int _index;
    private double _widthPercent = 100;
    private ZoneContentType _contentType = ZoneContentType.Text;
    private string _content = string.Empty;
    private Models.HorizontalAlignment _hAlign = Models.HorizontalAlignment.Center;
    private Models.VerticalAlignment _vAlign = Models.VerticalAlignment.Center;
    private bool _isScrolling = false;
    private int _scrollSpeed = 20;
    private Color _textColor = Color.FromRgb(255, 176, 0); // Varsayılan Amber
    private int _letterSpacing = 1;
    private int _lineSpacing = 2;
    
    // Zone-specific animasyon state'i
    private double _currentOffset;
    private double _accumulatedOffset;
    
    // Çerçeve ayarları
    private BorderSettings _border = new();

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
    public Models.HorizontalAlignment HAlign
    {
        get => _hAlign;
        set => this.RaiseAndSetIfChanged(ref _hAlign, value);
    }

    /// <summary>
    /// Dikey hizalama
    /// </summary>
    public Models.VerticalAlignment VAlign
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
    /// Her zone kendi hızında bağımsız olarak kayar
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

    /// <summary>
    /// Harf arası boşluk (piksel, 1-10)
    /// </summary>
    public int LetterSpacing
    {
        get => _letterSpacing;
        set => this.RaiseAndSetIfChanged(ref _letterSpacing, value);
    }

    /// <summary>
    /// Satır arası boşluk (piksel, 0-10)
    /// </summary>
    public int LineSpacing
    {
        get => _lineSpacing;
        set => this.RaiseAndSetIfChanged(ref _lineSpacing, value);
    }

    /// <summary>
    /// Zone çerçeve ayarları
    /// </summary>
    public BorderSettings Border
    {
        get => _border;
        set => this.RaiseAndSetIfChanged(ref _border, value ?? new BorderSettings());
    }
    
    /// <summary>
    /// Zone'un mevcut scroll offset'i (piksel)
    /// DeltaTime * ScrollSpeed ile hesaplanır
    /// </summary>
    public double CurrentOffset
    {
        get => _currentOffset;
        private set => this.RaiseAndSetIfChanged(ref _currentOffset, value);
    }
    
    /// <summary>
    /// Tam piksel offset değeri (render için)
    /// </summary>
    public int PixelOffset => (int)_currentOffset;

    /// <summary>
    /// AnimationService'den gelen tick ile offset'i günceller
    /// Her zone kendi hızıyla bağımsız olarak ilerler
    /// </summary>
    /// <param name="deltaTime">Son frame'den bu yana geçen süre (saniye)</param>
    public void UpdateOffset(double deltaTime)
    {
        if (!IsScrolling) return;
        
        // Offset += DeltaTime * ZoneSpeed
        _accumulatedOffset += deltaTime * ScrollSpeed;
        CurrentOffset = _accumulatedOffset;
    }
    
    /// <summary>
    /// Offset'i sıfırlar (animasyon durdurulduğunda)
    /// </summary>
    public void ResetOffset()
    {
        _accumulatedOffset = 0;
        CurrentOffset = 0;
    }
    
    /// <summary>
    /// Offset'i belirli bir değere ayarlar
    /// </summary>
    public void SetOffset(double offset)
    {
        _accumulatedOffset = offset;
        CurrentOffset = offset;
    }
}
