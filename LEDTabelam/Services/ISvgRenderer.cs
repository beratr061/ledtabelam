using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// SVG ve bitmap grafik render servisi interface'i
/// Requirements: 16.1, 16.5
/// </summary>
public interface ISvgRenderer
{
    /// <summary>
    /// SVG dosyasını belirtilen yüksekliğe ölçekleyerek render eder
    /// - Vektör grafikleri matris yüksekliğine kayıpsız ölçekleme
    /// - Hard-edge rendering (IsAntialias = false)
    /// - Siyah/beyaz SVG ikonları seçili LED rengine otomatik boyama
    /// </summary>
    /// <param name="svgPath">SVG dosyasının yolu</param>
    /// <param name="targetHeight">Hedef yükseklik (piksel)</param>
    /// <param name="tintColor">Boyama rengi (siyah/beyaz SVG'ler için)</param>
    /// <returns>Render edilmiş bitmap</returns>
    SKBitmap RenderSvg(string svgPath, int targetHeight, SKColor tintColor);

    /// <summary>
    /// SVG içeriğini string olarak render eder
    /// </summary>
    /// <param name="svgContent">SVG içeriği (XML string)</param>
    /// <param name="targetHeight">Hedef yükseklik (piksel)</param>
    /// <param name="tintColor">Boyama rengi</param>
    /// <returns>Render edilmiş bitmap</returns>
    SKBitmap RenderSvgFromContent(string svgContent, int targetHeight, SKColor tintColor);

    /// <summary>
    /// PNG/JPG görüntüsünü threshold ile binarize ederek render eder
    /// - Threshold değerine göre hangi piksellerin yanacağını belirler
    /// - Parlaklık >= threshold olan pikseller "on", diğerleri "off"
    /// </summary>
    /// <param name="imagePath">Görüntü dosyasının yolu</param>
    /// <param name="threshold">Eşik değeri (0-100, varsayılan %50)</param>
    /// <returns>Binarize edilmiş bitmap (siyah/beyaz)</returns>
    SKBitmap RenderBitmap(string imagePath, int threshold = 50);

    /// <summary>
    /// SKBitmap'i threshold ile binarize eder
    /// </summary>
    /// <param name="source">Kaynak bitmap</param>
    /// <param name="threshold">Eşik değeri (0-100)</param>
    /// <returns>Binarize edilmiş bitmap</returns>
    SKBitmap ApplyThreshold(SKBitmap source, int threshold);

    /// <summary>
    /// Görüntüyü belirtilen yüksekliğe ölçekler (en-boy oranını koruyarak)
    /// </summary>
    /// <param name="source">Kaynak bitmap</param>
    /// <param name="targetHeight">Hedef yükseklik</param>
    /// <returns>Ölçeklenmiş bitmap</returns>
    SKBitmap ScaleToHeight(SKBitmap source, int targetHeight);
}
