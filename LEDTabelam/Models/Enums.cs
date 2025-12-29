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
/// 
/// ÖNEMLİ: Pitch değeri çözünürlüğü DEĞİŞTİRMEZ!
/// Çözünürlük kullanıcının belirlediği piksel sayısıdır (örn: 128x16)
/// Pitch sadece önizlemede LED'lerin görünümünü etkiler:
/// - Küçük pitch (P5) = LED'ler birbirine daha yakın görünür
/// - Büyük pitch (P10) = LED'ler arası daha fazla boşluk görünür
/// 
/// Requirements: 5.7
/// </summary>
public enum PixelPitch
{
    P2_5,   // 2.5mm - LED'ler çok sıkı
    P3,     // 3mm
    P4,     // 4mm
    P5,     // 5mm - iç mekan standart
    P6,     // 6mm
    P7_62,  // 7.62mm
    P10,    // 10mm - dış mekan standart
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
            PixelPitch.Custom => 10.0,
            _ => 10.0
        };
    }

    /// <summary>
    /// LED çapı / hücre boyutu oranını döndürür (görsel render için)
    /// Bu oran LED'in hücre içinde ne kadar yer kapladığını belirler
    /// 
    /// Küçük pitch = LED'ler daha sıkı = oran daha yüksek (daha az boşluk)
    /// Büyük pitch = LED'ler daha aralıklı = oran daha düşük (daha fazla boşluk)
    /// </summary>
    public static double GetLedDiameterRatio(this PixelPitch pitch)
    {
        return pitch switch
        {
            PixelPitch.P2_5 => 0.90,  // Çok sıkı, neredeyse bitişik
            PixelPitch.P3 => 0.85,
            PixelPitch.P4 => 0.80,
            PixelPitch.P5 => 0.75,    // İç mekan standart
            PixelPitch.P6 => 0.70,
            PixelPitch.P7_62 => 0.65,
            PixelPitch.P10 => 0.60,   // Dış mekan, belirgin boşluklar
            PixelPitch.Custom => 0.70,
            _ => 0.70
        };
    }

    /// <summary>
    /// Pitch için önerilen minimum izleme mesafesini metre cinsinden döndürür
    /// Kural: P × 1 metre (yaklaşık)
    /// </summary>
    public static double GetMinViewingDistanceMeters(this PixelPitch pitch)
    {
        return pitch.GetPitchMm() / 10.0; // mm'yi metreye çevir (yaklaşık)
    }

    /// <summary>
    /// 1 metrekaredeki piksel sayısını döndürür
    /// Formül: (1000 / P) × (1000 / P)
    /// </summary>
    public static int GetPixelsPerSquareMeter(this PixelPitch pitch)
    {
        double p = pitch.GetPitchMm();
        int pixelsPerMeter = (int)(1000.0 / p);
        return pixelsPerMeter * pixelsPerMeter;
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
