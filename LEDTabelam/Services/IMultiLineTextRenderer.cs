using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Çoklu satır metin render servisi interface'i
/// Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6
/// </summary>
public interface IMultiLineTextRenderer
{
    /// <summary>
    /// Çoklu satır metni render eder
    /// </summary>
    /// <param name="font">Kullanılacak font</param>
    /// <param name="text">Render edilecek metin (satırlar \n ile ayrılmış)</param>
    /// <param name="color">Metin rengi</param>
    /// <param name="lineSpacing">Satır arası boşluk (piksel)</param>
    /// <returns>Render edilmiş bitmap</returns>
    SKBitmap RenderMultiLineText(BitmapFont font, string text, SKColor color, int lineSpacing);

    /// <summary>
    /// Çoklu satır metnin toplam yüksekliğini hesaplar
    /// </summary>
    /// <param name="font">Kullanılacak font</param>
    /// <param name="lineCount">Satır sayısı</param>
    /// <param name="lineSpacing">Satır arası boşluk (piksel)</param>
    /// <returns>Toplam yükseklik (piksel)</returns>
    int CalculateMultiLineHeight(BitmapFont font, int lineCount, int lineSpacing);

    /// <summary>
    /// Metnin satır sayısını döndürür
    /// </summary>
    /// <param name="text">Metin</param>
    /// <returns>Satır sayısı</returns>
    int GetLineCount(string text);

    /// <summary>
    /// Metnin display yüksekliğini aşıp aşmadığını kontrol eder
    /// </summary>
    /// <param name="font">Kullanılacak font</param>
    /// <param name="text">Metin</param>
    /// <param name="lineSpacing">Satır arası boşluk</param>
    /// <param name="displayHeight">Display yüksekliği</param>
    /// <returns>Aşıyorsa true</returns>
    bool ExceedsDisplayHeight(BitmapFont font, string text, int lineSpacing, int displayHeight);

    /// <summary>
    /// Çoklu satır render sonucu
    /// </summary>
    MultiLineRenderResult RenderMultiLineTextWithInfo(BitmapFont font, string text, SKColor color, int lineSpacing, int displayHeight);
}

/// <summary>
/// Çoklu satır render sonucu
/// </summary>
public class MultiLineRenderResult
{
    /// <summary>
    /// Render edilmiş bitmap
    /// </summary>
    public SKBitmap? Bitmap { get; set; }

    /// <summary>
    /// Toplam yükseklik (piksel)
    /// </summary>
    public int TotalHeight { get; set; }

    /// <summary>
    /// Satır sayısı
    /// </summary>
    public int LineCount { get; set; }

    /// <summary>
    /// Display yüksekliğini aşıyor mu
    /// </summary>
    public bool ExceedsDisplayHeight { get; set; }

    /// <summary>
    /// Uyarı mesajı (varsa)
    /// </summary>
    public string? WarningMessage { get; set; }
}
