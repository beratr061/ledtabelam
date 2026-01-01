namespace LEDTabelam;

/// <summary>
/// Uygulama genelinde kullanılan sabit değerler
/// </summary>
public static class Constants
{
    /// <summary>
    /// Animasyon render aralığı (milisaniye) - ~30 FPS
    /// </summary>
    public const double AnimationRenderIntervalMs = 33.33;

    /// <summary>
    /// Varsayılan animasyon frame rate
    /// </summary>
    public const int DefaultFrameRate = 30;

    /// <summary>
    /// Minimum panel genişliği (piksel)
    /// </summary>
    public const int MinPanelWidth = 16;

    /// <summary>
    /// Maksimum panel genişliği (piksel)
    /// </summary>
    public const int MaxPanelWidth = 1024;

    /// <summary>
    /// Minimum panel yüksekliği (piksel)
    /// </summary>
    public const int MinPanelHeight = 8;

    /// <summary>
    /// Maksimum panel yüksekliği (piksel)
    /// </summary>
    public const int MaxPanelHeight = 256;

    /// <summary>
    /// Minimum piksel boyutu
    /// </summary>
    public const int MinPixelSize = 1;

    /// <summary>
    /// Maksimum piksel boyutu
    /// </summary>
    public const int MaxPixelSize = 32;

    /// <summary>
    /// Minimum parlaklık yüzdesi
    /// </summary>
    public const int MinBrightness = 0;

    /// <summary>
    /// Maksimum parlaklık yüzdesi
    /// </summary>
    public const int MaxBrightness = 100;

    /// <summary>
    /// Minimum program süresi (saniye)
    /// </summary>
    public const int MinProgramDurationSeconds = 1;

    /// <summary>
    /// Maksimum program süresi (saniye)
    /// </summary>
    public const int MaxProgramDurationSeconds = 60;

    /// <summary>
    /// Minimum geçiş süresi (milisaniye)
    /// </summary>
    public const int MinTransitionDurationMs = 200;

    /// <summary>
    /// Maksimum geçiş süresi (milisaniye)
    /// </summary>
    public const int MaxTransitionDurationMs = 1000;

    /// <summary>
    /// Minimum kaydırma hızı
    /// </summary>
    public const int MinScrollSpeed = 0;

    /// <summary>
    /// Maksimum kaydırma hızı
    /// </summary>
    public const int MaxScrollSpeed = 100;

    /// <summary>
    /// Varsayılan kaydırma hızı
    /// </summary>
    public const int DefaultScrollSpeed = 30;

    /// <summary>
    /// Minimum slot numarası
    /// </summary>
    public const int MinSlotNumber = 1;

    /// <summary>
    /// Maksimum slot numarası
    /// </summary>
    public const int MaxSlotNumber = 999;

    /// <summary>
    /// Minimum pitch oranı
    /// </summary>
    public const double MinPitchRatio = 0.3;

    /// <summary>
    /// Maksimum pitch oranı
    /// </summary>
    public const double MaxPitchRatio = 0.95;

    /// <summary>
    /// Varsayılan pitch oranı
    /// </summary>
    public const double DefaultPitchRatio = 0.7;

    /// <summary>
    /// Glow minimum yarıçapı
    /// </summary>
    public const float GlowMinRadius = 2f;

    /// <summary>
    /// Glow maksimum yarıçapı
    /// </summary>
    public const float GlowMaxRadius = 10f;

    /// <summary>
    /// Yanmayan LED parlaklık farkı
    /// </summary>
    public const byte OffLedIntensityOffset = 15;

    /// <summary>
    /// Glow alpha değeri (%30)
    /// </summary>
    public const byte GlowAlpha = 77;

    /// <summary>
    /// Maksimum metin uzunluğu
    /// </summary>
    public const int MaxTextLength = 1000;

    /// <summary>
    /// Maksimum profil adı uzunluğu
    /// </summary>
    public const int MaxProfileNameLength = 100;

    /// <summary>
    /// Maksimum program adı uzunluğu
    /// </summary>
    public const int MaxProgramNameLength = 50;

    /// <summary>
    /// Log dosyası saklama süresi (gün)
    /// </summary>
    public const int LogRetentionDays = 7;

    /// <summary>
    /// Amber LED rengi
    /// </summary>
    public const uint AmberLedColor = 0xFFFFB000;

    /// <summary>
    /// Kırmızı LED rengi
    /// </summary>
    public const uint RedLedColor = 0xFFFF0000;

    /// <summary>
    /// Yeşil LED rengi
    /// </summary>
    public const uint GreenLedColor = 0xFF00FF00;

    /// <summary>
    /// Uygulama veri klasörü adı
    /// </summary>
    public const string AppDataFolderName = "LEDTabelam";

    /// <summary>
    /// Profil dosya uzantısı
    /// </summary>
    public const string ProfileFileExtension = ".json";

    /// <summary>
    /// Font dosya uzantısı
    /// </summary>
    public const string FontFileExtension = ".fnt";
}
