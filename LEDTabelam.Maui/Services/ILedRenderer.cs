using LEDTabelam.Maui.Models;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// LED render servisi interface'i
/// </summary>
public interface ILedRenderer
{
    /// <summary>
    /// Piksel matrisini LED görüntüsüne render eder
    /// </summary>
    SKBitmap RenderDisplay(bool[,] pixelMatrix, DisplaySettings settings);

    /// <summary>
    /// Piksel matrisini renkli LED görüntüsüne render eder (RGB modları için)
    /// </summary>
    SKBitmap RenderDisplay(SKColor[,] pixelMatrix, DisplaySettings settings);

    /// <summary>
    /// Kaynak görüntüye glow efekti uygular
    /// </summary>
    SKBitmap RenderWithGlow(SKBitmap source, DisplaySettings settings);

    /// <summary>
    /// Canvas üzerine grid overlay çizer
    /// </summary>
    void DrawGridOverlay(SKCanvas canvas, DisplaySettings settings);

    /// <summary>
    /// Piksel matrisine aging (eskime) efekti uygular
    /// </summary>
    void ApplyAgingEffect(bool[,] pixelMatrix, int agingPercent, int? seed = null);

    /// <summary>
    /// Renk matrisine aging efekti uygular
    /// </summary>
    void ApplyAgingEffect(SKColor[,] pixelMatrix, int agingPercent, int? seed = null);

    /// <summary>
    /// LED rengini ayarlara göre hesaplar
    /// </summary>
    SKColor GetLedColor(DisplaySettings settings);

    /// <summary>
    /// Arka plan rengini ayarlara göre hesaplar
    /// </summary>
    SKColor GetBackgroundColor(DisplaySettings settings);

    /// <summary>
    /// Pitch değerine göre piksel aralık oranını döndürür
    /// </summary>
    double GetPitchRatio(PixelPitch pitch, double customRatio = 0.7);

    /// <summary>
    /// Piksel matrisine çerçeve çizer
    /// </summary>
    void DrawBorder(bool[,] pixelMatrix, BorderSettings border, int x, int y, int width, int height);

    /// <summary>
    /// Renk matrisine çerçeve çizer
    /// </summary>
    void DrawBorder(SKColor[,] colorMatrix, BorderSettings border, int x, int y, int width, int height);
}
