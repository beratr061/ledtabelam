using System;
using Avalonia.Media;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// LED tabela görüntüleme ayarları
/// Requirements: 1.1, 2.1, 5.1, 5.3, 5.5, 5.7, 6.7, 6.15, 14.6, 19.1
/// </summary>
public class DisplaySettings : ReactiveObject
{
    // Panel boyutu (piksel sayısı) - kullanıcının belirlediği çözünürlük
    private int _panelWidth = 160;
    private int _panelHeight = 24;
    
    private LedColorType _colorType = LedColorType.Amber;
    private Color _customColor = Color.FromArgb(255, 255, 176, 0);
    private int _brightness = 100;
    private int _backgroundDarkness = 100;
    private int _pixelSize = 4;
    private PixelPitch _pitch = PixelPitch.P5;
    private double _customPitchRatio = 0.7;
    private PixelShape _shape = PixelShape.Round;
    private int _zoomLevel = 100;
    private bool _invertColors = false;
    private int _agingPercent = 0;
    private int _letterSpacing = 1;

    /// <summary>
    /// Panel genişliği (piksel sayısı)
    /// Bu değer doğrudan LED matris genişliğidir
    /// </summary>
    public int PanelWidth
    {
        get => _panelWidth;
        set => this.RaiseAndSetIfChanged(ref _panelWidth, value);
    }

    /// <summary>
    /// Panel yüksekliği (piksel sayısı)
    /// Bu değer doğrudan LED matris yüksekliğidir
    /// </summary>
    public int PanelHeight
    {
        get => _panelHeight;
        set => this.RaiseAndSetIfChanged(ref _panelHeight, value);
    }

    /// <summary>
    /// LED matris genişliği (piksel) - Pitch çarpanı uygulanmış gerçek çözünürlük
    /// P10 referansında girilen PanelWidth, Pitch'e göre çarpılır
    /// Örnek: PanelWidth=150, P5 seçili → Width=300
    /// </summary>
    public int Width
    {
        get => _panelWidth * Pitch.GetResolutionMultiplier();
        set => PanelWidth = value / Math.Max(1, Pitch.GetResolutionMultiplier());
    }

    /// <summary>
    /// LED matris yüksekliği (piksel) - Pitch çarpanı uygulanmış gerçek çözünürlük
    /// P10 referansında girilen PanelHeight, Pitch'e göre çarpılır
    /// Örnek: PanelHeight=24, P5 seçili → Height=48
    /// </summary>
    public int Height
    {
        get => _panelHeight * Pitch.GetResolutionMultiplier();
        set => PanelHeight = value / Math.Max(1, Pitch.GetResolutionMultiplier());
    }

    /// <summary>
    /// Gerçek çözünürlük genişliği (Width ile aynı, okunabilirlik için alias)
    /// </summary>
    public int ActualWidth => Width;

    /// <summary>
    /// Gerçek çözünürlük yüksekliği (Height ile aynı, okunabilirlik için alias)
    /// </summary>
    public int ActualHeight => Height;

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
    /// Piksel boyutu - önizleme için render boyutu
    /// </summary>
    public int PixelSize
    {
        get => _pixelSize;
        set => this.RaiseAndSetIfChanged(ref _pixelSize, value);
    }

    /// <summary>
    /// LED piksel aralığı (pitch)
    /// Sadece görsel render'ı etkiler (LED'ler arası boşluk oranı)
    /// Çözünürlüğü DEĞİŞTİRMEZ!
    /// </summary>
    public PixelPitch Pitch
    {
        get => _pitch;
        set => this.RaiseAndSetIfChanged(ref _pitch, value);
    }

    /// <summary>
    /// Özel pitch oranı (LED çapı / hücre boyutu)
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
    /// Harf aralığı (0-10 piksel)
    /// </summary>
    public int LetterSpacing
    {
        get => _letterSpacing;
        set => this.RaiseAndSetIfChanged(ref _letterSpacing, value);
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
