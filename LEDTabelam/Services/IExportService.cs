using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Dışa aktarma servisi interface'i - PNG, GIF, WebP export
/// Requirements: 7.1, 7.5, 7.6
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Bitmap'i PNG formatında dışa aktarır
    /// </summary>
    /// <param name="bitmap">Dışa aktarılacak bitmap</param>
    /// <param name="filePath">Hedef dosya yolu</param>
    /// <param name="useZoom">Zoom seviyesini uygula (true) veya gerçek çözünürlük (false)</param>
    /// <param name="zoomLevel">Zoom seviyesi (useZoom true ise, %50-400)</param>
    /// <returns>Başarılı ise true</returns>
    Task<bool> ExportPngAsync(SKBitmap bitmap, string filePath, bool useZoom = false, int zoomLevel = 100);

    /// <summary>
    /// Frame listesini GIF formatında dışa aktarır
    /// </summary>
    /// <param name="frames">Animasyon frame'leri</param>
    /// <param name="filePath">Hedef dosya yolu</param>
    /// <param name="fps">Frame per second (1-60)</param>
    /// <returns>Başarılı ise true</returns>
    Task<bool> ExportGifAsync(IReadOnlyList<SKBitmap> frames, string filePath, int fps = 30);

    /// <summary>
    /// Frame listesini WebP formatında dışa aktarır
    /// </summary>
    /// <param name="frames">Animasyon frame'leri</param>
    /// <param name="filePath">Hedef dosya yolu</param>
    /// <param name="fps">Frame per second (1-60)</param>
    /// <returns>Başarılı ise true</returns>
    Task<bool> ExportWebPAsync(IReadOnlyList<SKBitmap> frames, string filePath, int fps = 30);

    /// <summary>
    /// Bitmap'i belirtilen formatta dışa aktarır
    /// </summary>
    /// <param name="bitmap">Dışa aktarılacak bitmap</param>
    /// <param name="filePath">Hedef dosya yolu</param>
    /// <param name="format">Çıktı formatı</param>
    /// <param name="quality">Kalite (0-100, JPEG/WebP için)</param>
    /// <returns>Başarılı ise true</returns>
    Task<bool> ExportAsync(SKBitmap bitmap, string filePath, ExportFormat format, int quality = 100);

    /// <summary>
    /// Desteklenen export formatlarını döndürür
    /// </summary>
    IReadOnlyList<ExportFormat> SupportedFormats { get; }
}

/// <summary>
/// Dışa aktarma formatları
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// PNG formatı (kayıpsız)
    /// </summary>
    Png,

    /// <summary>
    /// JPEG formatı (kayıplı)
    /// </summary>
    Jpeg,

    /// <summary>
    /// GIF formatı (animasyonlu)
    /// </summary>
    Gif,

    /// <summary>
    /// WebP formatı (animasyonlu, kayıplı/kayıpsız)
    /// </summary>
    WebP
}
