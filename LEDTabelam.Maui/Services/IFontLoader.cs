using System.Collections.Generic;
using System.Threading.Tasks;
using LEDTabelam.Maui.Models;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Bitmap font yükleme ve metin render servisi interface'i
/// </summary>
public interface IFontLoader
{
    /// <summary>
    /// Varsayılan fontları yükler
    /// </summary>
    Task LoadDefaultFontsAsync();

    /// <summary>
    /// BMFont XML formatındaki font dosyasını yükler (.fnt + .png)
    /// </summary>
    Task<BitmapFont> LoadBMFontAsync(string fntPath);

    /// <summary>
    /// JSON formatındaki font dosyasını yükler (.json + .png)
    /// </summary>
    Task<BitmapFont> LoadJsonFontAsync(string jsonPath);

    /// <summary>
    /// Font dosyasının geçerliliğini kontrol eder
    /// </summary>
    bool ValidateFont(BitmapFont font);

    /// <summary>
    /// Metni bitmap font kullanarak SKBitmap'e render eder
    /// </summary>
    SKBitmap RenderText(BitmapFont font, string text, SKColor color, int letterSpacing = 1);

    /// <summary>
    /// Çok renkli metin segmentlerini bitmap font kullanarak SKBitmap'e render eder
    /// </summary>
    SKBitmap RenderColoredText(BitmapFont font, IEnumerable<(string Text, SKColor Color)> segments, int letterSpacing = 1);

    /// <summary>
    /// Metnin toplam genişliğini hesaplar
    /// </summary>
    int CalculateTextWidth(BitmapFont font, string text, int letterSpacing = 1);

    /// <summary>
    /// İsme göre yüklenmiş font döndürür
    /// </summary>
    BitmapFont? GetFont(string fontName);

    /// <summary>
    /// Mevcut fontların listesini döndürür
    /// </summary>
    IReadOnlyList<string> GetAvailableFonts();
}
