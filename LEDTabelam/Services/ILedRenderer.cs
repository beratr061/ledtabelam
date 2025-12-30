using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// LED render servisi interface'i
/// Requirements: 6.1, 6.2, 6.13
/// </summary>
public interface ILedRenderer
{
    /// <summary>
    /// Piksel matrisini LED görüntüsüne render eder
    /// </summary>
    /// <param name="pixelMatrix">Boolean piksel matrisi (true = aktif LED)</param>
    /// <param name="settings">Görüntüleme ayarları</param>
    /// <returns>Render edilmiş LED görüntüsü</returns>
    SKBitmap RenderDisplay(bool[,] pixelMatrix, DisplaySettings settings);

    /// <summary>
    /// Piksel matrisini renkli LED görüntüsüne render eder (RGB modları için)
    /// </summary>
    /// <param name="pixelMatrix">SKColor piksel matrisi</param>
    /// <param name="settings">Görüntüleme ayarları</param>
    /// <returns>Render edilmiş LED görüntüsü</returns>
    SKBitmap RenderDisplay(SKColor[,] pixelMatrix, DisplaySettings settings);

    /// <summary>
    /// Kaynak görüntüye glow efekti uygular
    /// </summary>
    /// <param name="source">Kaynak bitmap</param>
    /// <param name="settings">Görüntüleme ayarları</param>
    /// <returns>Glow efektli bitmap</returns>
    SKBitmap RenderWithGlow(SKBitmap source, DisplaySettings settings);

    /// <summary>
    /// Canvas üzerine grid overlay çizer
    /// </summary>
    /// <param name="canvas">Hedef canvas</param>
    /// <param name="settings">Görüntüleme ayarları</param>
    void DrawGridOverlay(SKCanvas canvas, DisplaySettings settings);

    /// <summary>
    /// Piksel matrisine aging (eskime) efekti uygular
    /// </summary>
    /// <param name="pixelMatrix">Piksel matrisi (değiştirilecek)</param>
    /// <param name="agingPercent">Eskime yüzdesi (0-5)</param>
    /// <param name="seed">Rastgele seed (tekrarlanabilirlik için)</param>
    void ApplyAgingEffect(bool[,] pixelMatrix, int agingPercent, int? seed = null);

    /// <summary>
    /// Renk matrisine aging efekti uygular
    /// </summary>
    /// <param name="pixelMatrix">Renk matrisi (değiştirilecek)</param>
    /// <param name="agingPercent">Eskime yüzdesi (0-5)</param>
    /// <param name="seed">Rastgele seed (tekrarlanabilirlik için)</param>
    void ApplyAgingEffect(SKColor[,] pixelMatrix, int agingPercent, int? seed = null);

    /// <summary>
    /// LED rengini ayarlara göre hesaplar
    /// </summary>
    /// <param name="settings">Görüntüleme ayarları</param>
    /// <returns>LED rengi</returns>
    SKColor GetLedColor(DisplaySettings settings);

    /// <summary>
    /// Arka plan rengini ayarlara göre hesaplar
    /// </summary>
    /// <param name="settings">Görüntüleme ayarları</param>
    /// <returns>Arka plan rengi</returns>
    SKColor GetBackgroundColor(DisplaySettings settings);

    /// <summary>
    /// Pitch değerine göre piksel aralık oranını döndürür
    /// </summary>
    /// <param name="pitch">Pitch değeri</param>
    /// <param name="customRatio">Özel oran (Custom pitch için)</param>
    /// <returns>LED çapı / merkez mesafesi oranı</returns>
    double GetPitchRatio(PixelPitch pitch, double customRatio = 0.7);

    /// <summary>
    /// Piksel matrisine çerçeve çizer
    /// </summary>
    /// <param name="pixelMatrix">Piksel matrisi (değiştirilecek)</param>
    /// <param name="border">Çerçeve ayarları</param>
    /// <param name="x">Çerçeve başlangıç X koordinatı</param>
    /// <param name="y">Çerçeve başlangıç Y koordinatı</param>
    /// <param name="width">Çerçeve genişliği</param>
    /// <param name="height">Çerçeve yüksekliği</param>
    void DrawBorder(bool[,] pixelMatrix, BorderSettings border, int x, int y, int width, int height);

    /// <summary>
    /// Renk matrisine çerçeve çizer
    /// </summary>
    /// <param name="colorMatrix">Renk matrisi (değiştirilecek)</param>
    /// <param name="border">Çerçeve ayarları</param>
    /// <param name="x">Çerçeve başlangıç X koordinatı</param>
    /// <param name="y">Çerçeve başlangıç Y koordinatı</param>
    /// <param name="width">Çerçeve genişliği</param>
    /// <param name="height">Çerçeve yüksekliği</param>
    void DrawBorder(SKColor[,] colorMatrix, BorderSettings border, int x, int y, int width, int height);
}
