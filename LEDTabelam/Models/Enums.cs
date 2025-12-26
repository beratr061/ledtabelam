namespace LEDTabelam.Models;

/// <summary>
/// LED renk tipi seçenekleri
/// Requirements: 2.1
/// </summary>
public enum LedColorType
{
    Amber,      // #FFB000
    Red,        // #FF0000
    Green,      // #00FF00
    OneROneGOneB,  // 1R1G1B - basit RGB karışımı
    FullRGB     // Tam renk desteği
}

/// <summary>
/// LED piksel aralığı (pitch) değerleri
/// Pitch değeri mm cinsinden LED merkezleri arası mesafeyi belirtir
/// Küçük pitch = daha yüksek çözünürlük (aynı fiziksel boyutta daha fazla piksel)
/// Requirements: 5.7
/// </summary>
public enum PixelPitch
{
    P2_5,   // 2.5mm - en yüksek çözünürlük (P10'a göre 4x)
    P3,     // 3mm (P10'a göre ~3.3x)
    P4,     // 4mm (P10'a göre 2.5x)
    P5,     // 5mm (P10'a göre 2x)
    P6,     // 6mm (P10'a göre ~1.67x)
    P7_62,  // 7.62mm (P10'a göre ~1.31x)
    P10,    // 10mm - referans pitch (1x)
    Custom  // Özel değer
}

/// <summary>
/// PixelPitch için yardımcı extension metodları
/// </summary>
public static class PixelPitchExtensions
{
    /// <summary>
    /// Pitch değerinin mm cinsinden karşılığını döndürür
    /// </summary>
    public static double GetPitchMm(this PixelPitch pitch)
    {
        return pitch switch
        {
            PixelPitch.P2_5 => 2.5,
            PixelPitch.P3 => 3.0,
            PixelPitch.P4 => 4.0,
            PixelPitch.P5 => 5.0,
            PixelPitch.P6 => 6.0,
            PixelPitch.P7_62 => 7.62,
            PixelPitch.P10 => 10.0,
            PixelPitch.Custom => 10.0, // Varsayılan
            _ => 10.0
        };
    }

    /// <summary>
    /// P10'a göre çözünürlük çarpanını döndürür
    /// Örnek: P5 için 2.0 (P10'un 2 katı çözünürlük)
    /// </summary>
    public static double GetResolutionMultiplier(this PixelPitch pitch)
    {
        double referencePitch = 10.0; // P10 referans
        double currentPitch = pitch.GetPitchMm();
        return referencePitch / currentPitch;
    }

    /// <summary>
    /// Verilen panel boyutu için gerçek piksel çözünürlüğünü hesaplar
    /// panelPixels: P10 referansındaki piksel sayısı
    /// </summary>
    public static int GetActualResolution(this PixelPitch pitch, int panelPixels)
    {
        return (int)(panelPixels * pitch.GetResolutionMultiplier());
    }

    /// <summary>
    /// LED çapı / merkez mesafesi oranını döndürür (görsel render için)
    /// </summary>
    public static double GetLedDiameterRatio(this PixelPitch pitch)
    {
        // Küçük pitch'lerde LED'ler daha sıkı, oran daha yüksek
        return pitch switch
        {
            PixelPitch.P2_5 => 0.85,
            PixelPitch.P3 => 0.82,
            PixelPitch.P4 => 0.78,
            PixelPitch.P5 => 0.75,
            PixelPitch.P6 => 0.72,
            PixelPitch.P7_62 => 0.68,
            PixelPitch.P10 => 0.65,
            PixelPitch.Custom => 0.70,
            _ => 0.70
        };
    }
}

/// <summary>
/// LED piksel şekli seçenekleri
/// Requirements: 19.1
/// </summary>
public enum PixelShape
{
    Square, // Kare (eski tip)
    Round   // Yuvarlak (SMD/DIP)
}

/// <summary>
/// Zone içerik tipi
/// Requirements: 17.3
/// </summary>
public enum ZoneContentType
{
    Text,           // Sabit metin
    Image,          // Resim/ikon
    ScrollingText   // Kayan yazı
}

/// <summary>
/// Playlist geçiş efekti tipleri
/// Requirements: 15.7
/// </summary>
public enum TransitionType
{
    None,       // Geçiş efekti yok
    Fade,       // Solma efekti
    SlideLeft,  // Sola kayma
    SlideRight  // Sağa kayma
}

/// <summary>
/// Yatay hizalama seçenekleri
/// Requirements: 21.1
/// </summary>
public enum HorizontalAlignment
{
    Left,   // Sol
    Center, // Orta
    Right   // Sağ
}

/// <summary>
/// Dikey hizalama seçenekleri
/// Requirements: 21.2
/// </summary>
public enum VerticalAlignment
{
    Top,    // Üst
    Center, // Orta
    Bottom  // Alt
}
