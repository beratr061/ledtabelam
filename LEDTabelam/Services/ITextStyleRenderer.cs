using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Metin stil render servisi interface'i (arkaplan ve stroke)
/// Requirements: 22.1, 22.2, 22.3, 22.4, 22.5, 22.6, 22.7
/// </summary>
public interface ITextStyleRenderer
{
    /// <summary>
    /// Metin bitmap'ine arkaplan uygular
    /// </summary>
    /// <param name="textBitmap">Metin bitmap'i</param>
    /// <param name="style">Metin stili</param>
    /// <returns>Arkaplan uygulanmış bitmap</returns>
    SKBitmap ApplyBackground(SKBitmap textBitmap, TextStyle style);

    /// <summary>
    /// Metin bitmap'ine stroke (kontur) uygular
    /// </summary>
    /// <param name="textBitmap">Metin bitmap'i</param>
    /// <param name="style">Metin stili</param>
    /// <returns>Stroke uygulanmış bitmap</returns>
    SKBitmap ApplyStroke(SKBitmap textBitmap, TextStyle style);

    /// <summary>
    /// Metin bitmap'ine tüm stilleri uygular (arkaplan + stroke)
    /// </summary>
    /// <param name="textBitmap">Metin bitmap'i</param>
    /// <param name="style">Metin stili</param>
    /// <returns>Stil uygulanmış bitmap</returns>
    SKBitmap ApplyStyles(SKBitmap textBitmap, TextStyle style);

    /// <summary>
    /// Stroke uygulandığında genişleyecek sınırları hesaplar
    /// </summary>
    /// <param name="originalWidth">Orijinal genişlik</param>
    /// <param name="originalHeight">Orijinal yükseklik</param>
    /// <param name="strokeWidth">Stroke kalınlığı</param>
    /// <returns>Genişletilmiş boyutlar (width, height)</returns>
    (int width, int height) CalculateStrokeExpandedBounds(int originalWidth, int originalHeight, int strokeWidth);
}
