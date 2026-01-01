namespace LEDTabelam.Maui.Models;

/// <summary>
/// LED renk tipi seçenekleri
/// </summary>
public enum LedColorType
{
    Amber,
    Red,
    Green,
    OneROneGOneB,
    FullRGB
}

/// <summary>
/// LED piksel aralığı (pitch) değerleri
/// </summary>
public enum PixelPitch
{
    P2_5,
    P3,
    P4,
    P5,
    P6,
    P7_62,
    P10,
    Custom
}

/// <summary>
/// PixelPitch için yardımcı extension metodları
/// </summary>
public static class PixelPitchExtensions
{
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

    public static double GetLedDiameterRatio(this PixelPitch pitch)
    {
        return pitch switch
        {
            PixelPitch.P2_5 => 0.90,
            PixelPitch.P3 => 0.85,
            PixelPitch.P4 => 0.80,
            PixelPitch.P5 => 0.75,
            PixelPitch.P6 => 0.70,
            PixelPitch.P7_62 => 0.65,
            PixelPitch.P10 => 0.60,
            PixelPitch.Custom => 0.70,
            _ => 0.70
        };
    }

    public static double GetMinViewingDistanceMeters(this PixelPitch pitch)
    {
        return pitch.GetPitchMm() / 10.0;
    }

    public static int GetPixelsPerSquareMeter(this PixelPitch pitch)
    {
        double p = pitch.GetPitchMm();
        int pixelsPerMeter = (int)(1000.0 / p);
        return pixelsPerMeter * pixelsPerMeter;
    }

    public static int GetResolutionMultiplier(this PixelPitch pitch)
    {
        return pitch switch
        {
            PixelPitch.P2_5 => 4,
            PixelPitch.P3 => 3,
            PixelPitch.P4 => 2,
            PixelPitch.P5 => 2,
            PixelPitch.P6 => 1,
            PixelPitch.P7_62 => 1,
            PixelPitch.P10 => 1,
            PixelPitch.Custom => 1,
            _ => 1
        };
    }
}

/// <summary>
/// LED piksel şekli seçenekleri
/// </summary>
public enum PixelShape
{
    Square,
    Round
}

/// <summary>
/// Zone içerik tipi
/// </summary>
public enum ZoneContentType
{
    Text,
    Image,
    ScrollingText
}

/// <summary>
/// Playlist geçiş efekti tipleri
/// </summary>
public enum TransitionType
{
    None,
    Fade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
    Blink,
    Laser,
    Curtain,
    Dissolve,
    Wipe
}

/// <summary>
/// Yatay hizalama seçenekleri
/// </summary>
public enum HorizontalAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Dikey hizalama seçenekleri
/// </summary>
public enum VerticalAlignment
{
    Top,
    Center,
    Bottom
}

/// <summary>
/// Program geçiş efekti tipleri
/// </summary>
public enum ProgramTransitionType
{
    Direct,
    Fade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown
}

/// <summary>
/// Ara durak animasyon tipleri
/// </summary>
public enum StopAnimationType
{
    Direct,
    Fade,
    SlideUp,
    SlideDown
}

/// <summary>
/// Tabela öğe tipleri
/// </summary>
public enum TabelaItemType
{
    Text,
    Symbol,
    Image,
    Clock,
    Date
}

/// <summary>
/// Kayma yönleri
/// </summary>
public enum ScrollDirection
{
    Left,
    Right,
    Up,
    Down
}
