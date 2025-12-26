using Avalonia.Media;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// LED tabela görüntüleme ayarları
/// Requirements: 1.1, 2.1, 5.1, 5.3, 5.5, 5.7, 6.7, 6.15, 14.6, 19.1
/// </summary>
public class DisplaySettings : ReactiveObject
{
    // Panel boyutu (P10 referansında piksel sayısı)
    private int _panelWidth = 32;
    private int _panelHeight = 16;
    
    // Hesaplanmış çözünürlük (pitch'e göre)
    private int _width = 32;
    private int _height = 16;
    
    private LedColorType _colorType = LedColorType.Amber;
    private Color _customColor = Color.FromArgb(255, 255, 176, 0);
    private int _brightness = 100;
    private int _backgroundDarkness = 100;
    private int _pixelSize = 8;
    private PixelPitch _pitch = PixelPitch.P10;
    private double _customPitchRatio = 0.7;
    private PixelShape _shape = PixelShape.Round;
    private int _zoomLevel = 100;
    private bool _invertColors = false;
    private int _agingPercent = 0;
    private int _lineSpacing = 2;

    /// <summary>
    /// Panel genişliği (P10 referansında piksel sayısı)
    /// Fiziksel panel boyutunu temsil eder
    /// </summary>
    public int PanelWidth
    {
        get => _panelWidth;
        set
        {
            this.RaiseAndSetIfChanged(ref _panelWidth, value);
            UpdateResolution();
        }
    }

    /// <summary>
    /// Panel yüksekliği (P10 referansında piksel sayısı)
    /// Fiziksel panel boyutunu temsil eder
    /// </summary>
    public int PanelHeight
    {
        get => _panelHeight;
        set
        {
            this.RaiseAndSetIfChanged(ref _panelHeight, value);
            UpdateResolution();
        }
    }

    /// <summary>
    /// LED matris genişliği (piksel) - pitch'e göre hesaplanır
    /// </summary>
    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    /// <summary>
    /// LED matris yüksekliği (piksel) - pitch'e göre hesaplanır
    /// </summary>
    public int Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    /// <summary>
    /// LED renk tipi
    /// </summary>
    public LedColorType ColorType
    {
        get => _colorType;
        set => this.RaiseAndSetIfChanged(ref _colorType, value);
    }

    /// <summary>
    /// Özel LED rengi (Full RGB modunda kullanılır)
    /// </summary>
    public Color CustomColor
    {
        get => _customColor;
        set => this.RaiseAndSetIfChanged(ref _customColor, value);
    }

    /// <summary>
    /// Parlaklık seviyesi (%0-100)
    /// </summary>
    public int Brightness
    {
        get => _brightness;
        set => this.RaiseAndSetIfChanged(ref _brightness, value);
    }

    /// <summary>
    /// Arka plan karartma seviyesi (%0-100)
    /// </summary>
    public int BackgroundDarkness
    {
        get => _backgroundDarkness;
        set => this.RaiseAndSetIfChanged(ref _backgroundDarkness, value);
    }

    /// <summary>
    /// Piksel boyutu (1-20 piksel) - önizleme için render boyutu
    /// </summary>
    public int PixelSize
    {
        get => _pixelSize;
        set => this.RaiseAndSetIfChanged(ref _pixelSize, value);
    }

    /// <summary>
    /// LED piksel aralığı (pitch)
    /// Pitch değiştiğinde çözünürlük otomatik güncellenir
    /// </summary>
    public PixelPitch Pitch
    {
        get => _pitch;
        set
        {
            this.RaiseAndSetIfChanged(ref _pitch, value);
            UpdateResolution();
        }
    }

    /// <summary>
    /// Özel pitch oranı (LED çapı / merkez mesafesi)
    /// </summary>
    public double CustomPitchRatio
    {
        get => _customPitchRatio;
        set => this.RaiseAndSetIfChanged(ref _customPitchRatio, value);
    }

    /// <summary>
    /// Piksel şekli (Kare/Yuvarlak)
    /// </summary>
    public PixelShape Shape
    {
        get => _shape;
        set => this.RaiseAndSetIfChanged(ref _shape, value);
    }

    /// <summary>
    /// Zoom seviyesi (%50-400)
    /// </summary>
    public int ZoomLevel
    {
        get => _zoomLevel;
        set => this.RaiseAndSetIfChanged(ref _zoomLevel, value);
    }

    /// <summary>
    /// Ters renk modu (arka plan aydınlık, yazı sönük)
    /// </summary>
    public bool InvertColors
    {
        get => _invertColors;
        set => this.RaiseAndSetIfChanged(ref _invertColors, value);
    }

    /// <summary>
    /// Eskime efekti yüzdesi (%0-5 ölü piksel)
    /// </summary>
    public int AgingPercent
    {
        get => _agingPercent;
        set => this.RaiseAndSetIfChanged(ref _agingPercent, value);
    }

    /// <summary>
    /// Satır arası boşluk (piksel)
    /// </summary>
    public int LineSpacing
    {
        get => _lineSpacing;
        set => this.RaiseAndSetIfChanged(ref _lineSpacing, value);
    }

    /// <summary>
    /// Pitch'e göre çözünürlüğü günceller
    /// </summary>
    private void UpdateResolution()
    {
        Width = Pitch.GetActualResolution(PanelWidth);
        Height = Pitch.GetActualResolution(PanelHeight);
    }

    /// <summary>
    /// Seçili renk tipine göre LED rengini döndürür
    /// </summary>
    public Color GetLedColor()
    {
        return ColorType switch
        {
            LedColorType.Amber => Color.FromRgb(255, 176, 0),
            LedColorType.Red => Color.FromRgb(255, 0, 0),
            LedColorType.Green => Color.FromRgb(0, 255, 0),
            _ => CustomColor
        };
    }
}
