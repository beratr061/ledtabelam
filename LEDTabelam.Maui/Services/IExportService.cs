using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Frame üretici delegate
/// </summary>
public delegate SKBitmap? FrameGenerator(int frameIndex);

/// <summary>
/// Dışa aktarma formatları
/// </summary>
public enum ExportFormat
{
    Png,
    Jpeg,
    Gif,
    WebP
}

/// <summary>
/// Dışa aktarma servisi interface'i
/// </summary>
public interface IExportService
{
    Task<bool> ExportPngAsync(SKBitmap bitmap, string filePath, bool useZoom = false, int zoomLevel = 100);
    Task<bool> ExportGifAsync(IReadOnlyList<SKBitmap> frames, string filePath, int fps = 30);
    Task<bool> ExportGifStreamingAsync(FrameGenerator frameGenerator, int totalFrames, string filePath, int fps = 30, Action<int>? progress = null);
    Task<bool> ExportWebPAsync(IReadOnlyList<SKBitmap> frames, string filePath, int fps = 30);
    Task<bool> ExportAsync(SKBitmap bitmap, string filePath, ExportFormat format, int quality = 100);
    IReadOnlyList<ExportFormat> SupportedFormats { get; }
}
