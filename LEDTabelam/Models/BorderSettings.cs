using Avalonia.Media;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Öğe çerçeve ayarları
/// Görseldeki hat kodu etrafındaki kırmızı çerçeve gibi
/// </summary>
public class BorderSettings : ReactiveObject
{
    private bool _isEnabled = false;
    private int _horizontalLines = 1;  // Yatay çizgi sayısı (üst ve alt)
    private int _verticalLines = 1;    // Dikey çizgi sayısı (sol ve sağ)
    private int _padding = 1;          // İçerik ile çerçeve arası boşluk (piksel)
    private Color _color = Color.FromRgb(255, 0, 0); // Varsayılan kırmızı

    /// <summary>
    /// Çerçeve aktif mi
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    /// <summary>
    /// Yatay çizgi sayısı (üst ve alt kenarlarda)
    /// 1 = tek çizgi, 2 = çift çizgi, vb.
    /// </summary>
    public int HorizontalLines
    {
        get => _horizontalLines;
        set => this.RaiseAndSetIfChanged(ref _horizontalLines, System.Math.Clamp(value, 1, 5));
    }

    /// <summary>
    /// Dikey çizgi sayısı (sol ve sağ kenarlarda)
    /// 1 = tek çizgi, 2 = çift çizgi, vb.
    /// </summary>
    public int VerticalLines
    {
        get => _verticalLines;
        set => this.RaiseAndSetIfChanged(ref _verticalLines, System.Math.Clamp(value, 1, 5));
    }

    /// <summary>
    /// İçerik ile çerçeve arası boşluk (piksel)
    /// </summary>
    public int Padding
    {
        get => _padding;
        set => this.RaiseAndSetIfChanged(ref _padding, System.Math.Clamp(value, 0, 10));
    }

    /// <summary>
    /// Çerçeve rengi
    /// </summary>
    public Color Color
    {
        get => _color;
        set => this.RaiseAndSetIfChanged(ref _color, value);
    }

    /// <summary>
    /// Toplam çerçeve kalınlığı (yatay - üst veya alt)
    /// </summary>
    public int TotalHorizontalThickness => HorizontalLines + Padding;

    /// <summary>
    /// Toplam çerçeve kalınlığı (dikey - sol veya sağ)
    /// </summary>
    public int TotalVerticalThickness => VerticalLines + Padding;

    /// <summary>
    /// Varsayılan ayarlarla yeni bir BorderSettings oluşturur
    /// </summary>
    public static BorderSettings CreateDefault()
    {
        return new BorderSettings
        {
            IsEnabled = false,
            HorizontalLines = 1,
            VerticalLines = 1,
            Padding = 1,
            Color = Color.FromRgb(255, 0, 0)
        };
    }

    /// <summary>
    /// Mevcut ayarların bir kopyasını oluşturur
    /// </summary>
    public BorderSettings Clone()
    {
        return new BorderSettings
        {
            IsEnabled = IsEnabled,
            HorizontalLines = HorizontalLines,
            VerticalLines = VerticalLines,
            Padding = Padding,
            Color = Color
        };
    }
}
