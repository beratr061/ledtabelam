using LEDTabelam.Maui.Models;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// İçerik yönetimi servisi interface'i
/// İçerik oluşturma, güncelleme ve render işlemlerini yönetir
/// </summary>
public interface IContentManager
{
    /// <summary>
    /// Yeni metin içeriği oluşturur
    /// </summary>
    TextContent CreateTextContent();

    /// <summary>
    /// Yeni saat içeriği oluşturur
    /// </summary>
    ClockContent CreateClockContent();

    /// <summary>
    /// Yeni tarih içeriği oluşturur
    /// </summary>
    DateContent CreateDateContent();

    /// <summary>
    /// Yeni geri sayım içeriği oluşturur
    /// </summary>
    CountdownContent CreateCountdownContent();

    /// <summary>
    /// İçeriği günceller ve değişiklik bildirimini tetikler
    /// </summary>
    void UpdateContent(ContentItem content);

    /// <summary>
    /// İçeriği bitmap olarak render eder
    /// </summary>
    SKBitmap RenderContent(ContentItem content, DisplaySettings settings);

    /// <summary>
    /// Metin içeriğini bitmap olarak render eder
    /// </summary>
    SKBitmap RenderTextContent(TextContent content, DisplaySettings settings);

    /// <summary>
    /// Saat içeriğini bitmap olarak render eder
    /// </summary>
    SKBitmap RenderClockContent(ClockContent content, DisplaySettings settings);

    /// <summary>
    /// Tarih içeriğini bitmap olarak render eder
    /// </summary>
    SKBitmap RenderDateContent(DateContent content, DisplaySettings settings);

    /// <summary>
    /// Geri sayım içeriğini bitmap olarak render eder
    /// </summary>
    SKBitmap RenderCountdownContent(CountdownContent content, DisplaySettings settings);

    /// <summary>
    /// İçerik tipine göre varsayılan isim döndürür
    /// </summary>
    string GetDefaultName(ContentType contentType);

    /// <summary>
    /// İçeriği kopyalar
    /// </summary>
    ContentItem CloneContent(ContentItem content);
}
