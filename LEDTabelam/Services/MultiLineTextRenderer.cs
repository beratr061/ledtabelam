using System;
using System.Collections.Generic;
using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Çoklu satır metin render servisi implementasyonu
/// Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6
/// </summary>
public class MultiLineTextRenderer : IMultiLineTextRenderer
{
    private readonly IFontLoader _fontLoader;

    public MultiLineTextRenderer(IFontLoader fontLoader)
    {
        _fontLoader = fontLoader ?? throw new ArgumentNullException(nameof(fontLoader));
    }

    /// <inheritdoc/>
    public SKBitmap RenderMultiLineText(BitmapFont font, string text, SKColor color, int lineSpacing)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        if (string.IsNullOrEmpty(text))
        {
            var emptyBitmap = new SKBitmap(1, font.LineHeight);
            emptyBitmap.Erase(SKColors.Transparent);
            return emptyBitmap;
        }

        // Satırları ayır
        var lines = SplitLines(text);
        int lineCount = lines.Count;

        if (lineCount == 0)
        {
            var emptyBitmap = new SKBitmap(1, font.LineHeight);
            emptyBitmap.Erase(SKColors.Transparent);
            return emptyBitmap;
        }

        // Her satırı önce render et ve gerçek yüksekliklerini bul
        var lineBitmaps = new List<SKBitmap>();
        int maxWidth = 0;
        int actualLineHeight = 0;
        
        foreach (var line in lines)
        {
            if (!string.IsNullOrEmpty(line))
            {
                var lineBitmap = RenderSingleLine(font, line, color);
                lineBitmaps.Add(lineBitmap);
                maxWidth = Math.Max(maxWidth, lineBitmap.Width);
                
                // Gerçek karakter yüksekliğini bul (boş olmayan piksel satırları)
                int lineActualHeight = GetActualBitmapHeight(lineBitmap);
                actualLineHeight = Math.Max(actualLineHeight, lineActualHeight);
            }
            else
            {
                lineBitmaps.Add(null!);
            }
        }

        // Eğer gerçek yükseklik bulunamadıysa font LineHeight kullan
        if (actualLineHeight == 0)
            actualLineHeight = font.LineHeight;

        // Minimum genişlik kontrolü
        if (maxWidth <= 0)
            maxWidth = 1;

        // Toplam yüksekliği hesapla (gerçek karakter yüksekliği ile)
        int totalHeight = lineCount * actualLineHeight;
        if (lineCount > 1)
        {
            totalHeight += (lineCount - 1) * lineSpacing;
        }

        // Sonuç bitmap'i oluştur
        var result = new SKBitmap(maxWidth, totalHeight);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Her satırı çiz
        int currentY = 0;
        for (int i = 0; i < lineBitmaps.Count; i++)
        {
            var lineBitmap = lineBitmaps[i];
            if (lineBitmap != null)
            {
                // Bitmap'in üst kısmındaki boşluğu atla
                int topOffset = GetTopOffset(lineBitmap);
                
                // Sadece içerik kısmını çiz
                var srcRect = new SKRect(0, topOffset, lineBitmap.Width, topOffset + actualLineHeight);
                var destRect = new SKRect(0, currentY, lineBitmap.Width, currentY + actualLineHeight);
                
                canvas.DrawBitmap(lineBitmap, srcRect, destRect);
                lineBitmap.Dispose();
            }

            currentY += actualLineHeight;
            if (i < lineBitmaps.Count - 1)
            {
                currentY += lineSpacing;
            }
        }

        return result;
    }

    /// <summary>
    /// Bitmap'in gerçek içerik yüksekliğini bulur (boş satırları hariç tutar)
    /// </summary>
    private int GetActualBitmapHeight(SKBitmap bitmap)
    {
        int topY = -1;
        int bottomY = -1;
        
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha > 0)
                {
                    if (topY == -1) topY = y;
                    bottomY = y;
                    break;
                }
            }
        }
        
        if (topY == -1) return bitmap.Height;
        return bottomY - topY + 1;
    }

    /// <summary>
    /// Bitmap'in üst kısmındaki boşluk miktarını bulur
    /// </summary>
    private int GetTopOffset(SKBitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha > 0)
                {
                    return y;
                }
            }
        }
        return 0;
    }


    /// <inheritdoc/>
    public int CalculateMultiLineHeight(BitmapFont font, int lineCount, int lineSpacing)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        if (lineCount <= 0)
            return 0;

        // Formül: (satır_sayısı * font_satır_yüksekliği) + ((satır_sayısı - 1) * satır_arası_boşluk)
        // Requirements: 14.3 - Satır yüksekliğini font yüksekliğine göre otomatik hesapla
        int totalHeight = lineCount * font.LineHeight;
        
        if (lineCount > 1)
        {
            totalHeight += (lineCount - 1) * lineSpacing;
        }

        return totalHeight;
    }

    /// <inheritdoc/>
    public int GetLineCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return SplitLines(text).Count;
    }

    /// <inheritdoc/>
    public bool ExceedsDisplayHeight(BitmapFont font, string text, int lineSpacing, int displayHeight)
    {
        if (font == null || string.IsNullOrEmpty(text))
            return false;

        int lineCount = GetLineCount(text);
        int totalHeight = CalculateMultiLineHeight(font, lineCount, lineSpacing);

        // Requirements: 14.4 - Satır sayısı display yüksekliğini aştığında uyarı göster
        return totalHeight > displayHeight;
    }

    /// <inheritdoc/>
    public MultiLineRenderResult RenderMultiLineTextWithInfo(BitmapFont font, string text, SKColor color, int lineSpacing, int displayHeight)
    {
        var result = new MultiLineRenderResult();

        if (font == null)
        {
            result.WarningMessage = "Font yüklenmemiş";
            return result;
        }

        if (string.IsNullOrEmpty(text))
        {
            result.Bitmap = new SKBitmap(1, font.LineHeight);
            result.Bitmap.Erase(SKColors.Transparent);
            result.TotalHeight = font.LineHeight;
            result.LineCount = 0;
            return result;
        }

        result.LineCount = GetLineCount(text);
        result.TotalHeight = CalculateMultiLineHeight(font, result.LineCount, lineSpacing);
        result.ExceedsDisplayHeight = result.TotalHeight > displayHeight;

        // Requirements: 14.4 - Yükseklik aşımı uyarısı
        if (result.ExceedsDisplayHeight)
        {
            result.WarningMessage = $"Metin yüksekliği ({result.TotalHeight}px) display yüksekliğini ({displayHeight}px) aşıyor";
        }

        result.Bitmap = RenderMultiLineText(font, text, color, lineSpacing);

        return result;
    }

    /// <summary>
    /// Metni satırlara ayırır
    /// </summary>
    private List<string> SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        // \r\n, \n, \r ile satırları ayır
        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        return new List<string>(lines);
    }

    /// <summary>
    /// Tek satır metnin genişliğini hesaplar
    /// </summary>
    private int CalculateLineWidth(BitmapFont font, string line)
    {
        if (font == null || string.IsNullOrEmpty(line))
            return 0;

        if (_fontLoader is FontLoader fontLoader)
        {
            return fontLoader.CalculateTextWidth(font, line);
        }

        // Fallback: basit hesaplama
        int width = 0;
        foreach (char c in line)
        {
            var fontChar = font.GetCharacter(c);
            if (fontChar != null)
            {
                width += fontChar.XAdvance;
            }
        }
        return width;
    }

    /// <summary>
    /// Tek satır metni render eder
    /// </summary>
    private SKBitmap RenderSingleLine(BitmapFont font, string line, SKColor color)
    {
        return _fontLoader.RenderText(font, line, color);
    }
}
