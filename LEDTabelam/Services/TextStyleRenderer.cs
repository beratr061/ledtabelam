using System;
using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Metin stil render servisi implementasyonu (arkaplan ve stroke)
/// Requirements: 22.1, 22.2, 22.3, 22.4, 22.5, 22.6, 22.7
/// </summary>
public class TextStyleRenderer : ITextStyleRenderer
{
    /// <inheritdoc/>
    public SKBitmap ApplyBackground(SKBitmap textBitmap, TextStyle style)
    {
        if (textBitmap == null)
            throw new ArgumentNullException(nameof(textBitmap));

        if (style == null || !style.HasBackground)
            return textBitmap;

        // Sonuç bitmap'i oluştur (aynı boyutta)
        var result = new SKBitmap(textBitmap.Width, textBitmap.Height);
        using var canvas = new SKCanvas(result);

        // Arkaplan rengini çiz
        var bgColor = new SKColor(
            style.BackgroundColor.R,
            style.BackgroundColor.G,
            style.BackgroundColor.B,
            style.BackgroundColor.A);

        // Metin piksellerinin arkasına dolgu rengi çiz
        // Önce tüm aktif piksellerin bounding box'ını bul
        var bounds = FindTextBounds(textBitmap);
        
        if (bounds.HasValue)
        {
            using var bgPaint = new SKPaint
            {
                Color = bgColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = false
            };

            // Arkaplan dikdörtgenini çiz
            canvas.DrawRect(bounds.Value, bgPaint);
        }

        // Metin bitmap'ini üzerine çiz
        canvas.DrawBitmap(textBitmap, 0, 0);

        return result;
    }

    /// <inheritdoc/>
    public SKBitmap ApplyStroke(SKBitmap textBitmap, TextStyle style)
    {
        if (textBitmap == null)
            throw new ArgumentNullException(nameof(textBitmap));

        if (style == null || !style.HasStroke)
            return textBitmap;

        // Stroke kalınlığını 1-3 arasında sınırla
        int strokeWidth = Math.Clamp(style.StrokeWidth, 1, 3);

        // Genişletilmiş boyutları hesapla
        var (expandedWidth, expandedHeight) = CalculateStrokeExpandedBounds(
            textBitmap.Width, textBitmap.Height, strokeWidth);

        // Sonuç bitmap'i oluştur (genişletilmiş boyutta)
        var result = new SKBitmap(expandedWidth, expandedHeight);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        var strokeColor = new SKColor(
            style.StrokeColor.R,
            style.StrokeColor.G,
            style.StrokeColor.B,
            style.StrokeColor.A);

        // Stroke için paint
        using var strokePaint = new SKPaint
        {
            Color = strokeColor,
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };

        // Offset (stroke genişliği kadar kaydır)
        int offset = strokeWidth;

        // Her aktif piksel için stroke çiz (dilate/expand işlemi)
        for (int y = 0; y < textBitmap.Height; y++)
        {
            for (int x = 0; x < textBitmap.Width; x++)
            {
                var pixel = textBitmap.GetPixel(x, y);
                
                // Aktif piksel mi kontrol et (alpha > 0 ve renk siyah değil)
                if (IsActivePixel(pixel))
                {
                    // Stroke genişliği kadar çevresine piksel çiz
                    for (int dy = -strokeWidth; dy <= strokeWidth; dy++)
                    {
                        for (int dx = -strokeWidth; dx <= strokeWidth; dx++)
                        {
                            int newX = x + offset + dx;
                            int newY = y + offset + dy;
                            
                            if (newX >= 0 && newX < expandedWidth && 
                                newY >= 0 && newY < expandedHeight)
                            {
                                canvas.DrawPoint(newX, newY, strokePaint);
                            }
                        }
                    }
                }
            }
        }

        // Orijinal metin bitmap'ini ortaya çiz (stroke'un üzerine)
        canvas.DrawBitmap(textBitmap, offset, offset);

        return result;
    }

    /// <inheritdoc/>
    public SKBitmap ApplyStyles(SKBitmap textBitmap, TextStyle style)
    {
        if (textBitmap == null)
            throw new ArgumentNullException(nameof(textBitmap));

        if (style == null)
            return textBitmap;

        var result = textBitmap;

        // Önce stroke uygula (boyut genişler)
        if (style.HasStroke)
        {
            result = ApplyStroke(result, style);
        }

        // Sonra arkaplan uygula
        if (style.HasBackground)
        {
            result = ApplyBackground(result, style);
        }

        return result;
    }

    /// <inheritdoc/>
    public (int width, int height) CalculateStrokeExpandedBounds(int originalWidth, int originalHeight, int strokeWidth)
    {
        // Stroke her yönde strokeWidth kadar genişletir
        int expansion = strokeWidth * 2;
        return (originalWidth + expansion, originalHeight + expansion);
    }

    /// <summary>
    /// Bitmap'teki metin sınırlarını bulur
    /// </summary>
    private SKRect? FindTextBounds(SKBitmap bitmap)
    {
        int minX = bitmap.Width;
        int minY = bitmap.Height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (IsActivePixel(pixel))
                {
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        if (maxX < 0 || maxY < 0)
            return null;

        return new SKRect(minX, minY, maxX + 1, maxY + 1);
    }

    /// <summary>
    /// Pikselin aktif olup olmadığını kontrol eder
    /// </summary>
    private bool IsActivePixel(SKColor pixel)
    {
        return pixel.Alpha > 0 && (pixel.Red > 0 || pixel.Green > 0 || pixel.Blue > 0);
    }
}
