using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// SVG ve bitmap grafik render servisi interface'i
/// </summary>
public interface ISvgRenderer
{
    /// <summary>
    /// SVG dosyasını belirtilen yüksekliğe ölçekleyerek render eder
    /// </summary>
    SKBitmap RenderSvg(string svgPath, int targetHeight, SKColor tintColor);

    /// <summary>
    /// SVG içeriğini string olarak render eder
    /// </summary>
    SKBitmap RenderSvgFromContent(string svgContent, int targetHeight, SKColor tintColor);

    /// <summary>
    /// PNG/JPG görüntüsünü threshold ile binarize ederek render eder
    /// </summary>
    SKBitmap RenderBitmap(string imagePath, int threshold = 50);

    /// <summary>
    /// SKBitmap'i threshold ile binarize eder
    /// </summary>
    SKBitmap ApplyThreshold(SKBitmap source, int threshold);

    /// <summary>
    /// Görüntüyü belirtilen yüksekliğe ölçekler
    /// </summary>
    SKBitmap ScaleToHeight(SKBitmap source, int targetHeight);
}
