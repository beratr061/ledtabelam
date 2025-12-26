using System;
using System.Collections.Generic;
using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// LED render servisi implementasyonu
/// Requirements: 6.1, 6.2, 6.5, 6.10, 6.11, 6.12, 6.13, 6.14, 6.15, 19.1, 19.2, 19.3, 19.4, 19.5, 19.6
/// </summary>
public class LedRenderer : ILedRenderer
{
    /// <inheritdoc/>
    public SKBitmap RenderDisplay(bool[,] pixelMatrix, DisplaySettings settings)
    {
        int matrixWidth = pixelMatrix.GetLength(0);
        int matrixHeight = pixelMatrix.GetLength(1);

        // Piksel boyutu - pitch'e göre ayarlanmış
        int pixelSize = settings.PixelSize;
        double ledRatio = settings.Pitch.GetLedDiameterRatio();
        if (settings.Pitch == PixelPitch.Custom)
            ledRatio = settings.CustomPitchRatio;
        
        int ledDiameter = (int)(pixelSize * ledRatio);

        // Bitmap boyutları
        int bitmapWidth = matrixWidth * pixelSize;
        int bitmapHeight = matrixHeight * pixelSize;

        var bitmap = new SKBitmap(bitmapWidth, bitmapHeight);
        using var canvas = new SKCanvas(bitmap);

        // Arka plan rengi
        SKColor backgroundColor = GetBackgroundColor(settings);
        canvas.Clear(backgroundColor);

        // LED rengi
        SKColor ledColor = GetLedColor(settings);
        
        // Parlaklık uygula
        ledColor = ApplyBrightness(ledColor, settings.Brightness);

        // LED'leri çiz
        using var paint = new SKPaint
        {
            IsAntialias = false, // Nearest Neighbor için
            Style = SKPaintStyle.Fill,
            Color = ledColor
        };

        for (int x = 0; x < matrixWidth; x++)
        {
            for (int y = 0; y < matrixHeight; y++)
            {
                bool isLit = pixelMatrix[x, y];
                
                // Ters renk modunda aktif/pasif durumu tersle
                if (settings.InvertColors)
                {
                    isLit = !isLit;
                }

                if (isLit)
                {
                    DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, paint);
                }
            }
        }

        return bitmap;
    }


    /// <inheritdoc/>
    public SKBitmap RenderDisplay(SKColor[,] pixelMatrix, DisplaySettings settings)
    {
        int matrixWidth = pixelMatrix.GetLength(0);
        int matrixHeight = pixelMatrix.GetLength(1);

        // Piksel boyutu - pitch'e göre ayarlanmış
        int pixelSize = settings.PixelSize;
        double ledRatio = settings.Pitch.GetLedDiameterRatio();
        if (settings.Pitch == PixelPitch.Custom)
            ledRatio = settings.CustomPitchRatio;
        
        int ledDiameter = (int)(pixelSize * ledRatio);

        // Bitmap boyutları
        int bitmapWidth = matrixWidth * pixelSize;
        int bitmapHeight = matrixHeight * pixelSize;

        var bitmap = new SKBitmap(bitmapWidth, bitmapHeight);
        using var canvas = new SKCanvas(bitmap);

        // Arka plan rengi
        SKColor backgroundColor = GetBackgroundColor(settings);
        canvas.Clear(backgroundColor);

        using var paint = new SKPaint
        {
            IsAntialias = false,
            Style = SKPaintStyle.Fill
        };

        for (int x = 0; x < matrixWidth; x++)
        {
            for (int y = 0; y < matrixHeight; y++)
            {
                SKColor pixelColor = pixelMatrix[x, y];
                
                // Ters renk modunda
                if (settings.InvertColors)
                {
                    // Piksel aktif mi kontrol et (alpha > 0 ve renk siyah değil)
                    bool isLit = pixelColor.Alpha > 0 && 
                                 (pixelColor.Red > 0 || pixelColor.Green > 0 || pixelColor.Blue > 0);
                    
                    if (!isLit)
                    {
                        // Pasif pikseli aktif yap
                        pixelColor = GetLedColor(settings);
                    }
                    else
                    {
                        // Aktif pikseli pasif yap (çizme)
                        continue;
                    }
                }
                else if (pixelColor.Alpha == 0 || 
                         (pixelColor.Red == 0 && pixelColor.Green == 0 && pixelColor.Blue == 0))
                {
                    continue; // Pasif piksel
                }

                // Parlaklık uygula
                SKColor finalColor = ApplyBrightness(pixelColor, settings.Brightness);
                paint.Color = finalColor;

                DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, paint);
            }
        }

        return bitmap;
    }

    /// <summary>
    /// Tek bir LED pikseli çizer
    /// </summary>
    private void DrawLedPixel(SKCanvas canvas, int x, int y, int pixelSize, int ledDiameter, 
                              PixelShape shape, SKPaint paint)
    {
        float centerX = x * pixelSize + pixelSize / 2f;
        float centerY = y * pixelSize + pixelSize / 2f;
        float radius = ledDiameter / 2f;

        if (shape == PixelShape.Round)
        {
            canvas.DrawCircle(centerX, centerY, radius, paint);
        }
        else // Square
        {
            float left = centerX - radius;
            float top = centerY - radius;
            canvas.DrawRect(left, top, ledDiameter, ledDiameter, paint);
        }
    }


    /// <inheritdoc/>
    public SKBitmap RenderWithGlow(SKBitmap source, DisplaySettings settings)
    {
        // Glow yarıçapı parlaklığa göre 2-10 piksel
        float glowRadius = 2 + (settings.Brightness / 100f) * 8;
        
        // Sonuç bitmap'i oluştur
        var result = new SKBitmap(source.Width, source.Height);
        using var canvas = new SKCanvas(result);

        // Arka plan rengi
        SKColor backgroundColor = GetBackgroundColor(settings);
        canvas.Clear(backgroundColor);

        // Glow efekti için blur filtresi
        using var glowFilter = SKImageFilter.CreateBlur(glowRadius, glowRadius);
        
        // Glow katmanı için paint (%30 alpha)
        using var glowPaint = new SKPaint
        {
            ImageFilter = glowFilter,
            Color = new SKColor(255, 255, 255, 77) // %30 alpha (255 * 0.3 ≈ 77)
        };

        // Önce glow katmanını çiz
        canvas.DrawBitmap(source, 0, 0, glowPaint);

        // Sonra orijinal görüntüyü üzerine çiz
        canvas.DrawBitmap(source, 0, 0);

        return result;
    }


    /// <inheritdoc/>
    public void DrawGridOverlay(SKCanvas canvas, DisplaySettings settings)
    {
        int pixelSize = settings.PixelSize;
        double ledRatio = settings.Pitch.GetLedDiameterRatio();
        if (settings.Pitch == PixelPitch.Custom)
            ledRatio = settings.CustomPitchRatio;
        
        int ledDiameter = (int)(pixelSize * ledRatio);
        int gridWidth = (pixelSize - ledDiameter) / 2;

        // Grid çizgisi için siyah paint
        using var gridPaint = new SKPaint
        {
            IsAntialias = false,
            Style = SKPaintStyle.Fill,
            Color = SKColors.Black
        };

        // Canvas boyutlarını al
        var canvasInfo = canvas.DeviceClipBounds;
        int canvasWidth = (int)canvasInfo.Width;
        int canvasHeight = (int)canvasInfo.Height;

        int matrixWidth = canvasWidth / pixelSize;
        int matrixHeight = canvasHeight / pixelSize;

        // Dikey grid çizgileri
        for (int x = 0; x <= matrixWidth; x++)
        {
            float lineX = x * pixelSize - gridWidth / 2f;
            canvas.DrawRect(lineX, 0, gridWidth, canvasHeight, gridPaint);
        }

        // Yatay grid çizgileri
        for (int y = 0; y <= matrixHeight; y++)
        {
            float lineY = y * pixelSize - gridWidth / 2f;
            canvas.DrawRect(0, lineY, canvasWidth, gridWidth, gridPaint);
        }
    }


    /// <inheritdoc/>
    public SKColor GetLedColor(DisplaySettings settings)
    {
        return settings.ColorType switch
        {
            LedColorType.Amber => new SKColor(255, 176, 0),      // #FFB000
            LedColorType.Red => new SKColor(255, 0, 0),          // #FF0000
            LedColorType.Green => new SKColor(0, 255, 0),        // #00FF00
            LedColorType.OneROneGOneB => new SKColor(255, 255, 255), // Beyaz (basit RGB karışımı)
            LedColorType.FullRGB => new SKColor(
                settings.CustomColor.R,
                settings.CustomColor.G,
                settings.CustomColor.B),
            _ => new SKColor(255, 176, 0) // Varsayılan Amber
        };
    }

    /// <summary>
    /// 1R1G1B modunda renk karışımı hesaplar
    /// Her piksel için basit RGB karışımı (R, G veya B kanallarından biri aktif)
    /// </summary>
    public SKColor GetOneROneGOneBColor(int x, int y)
    {
        // Basit pattern: x + y mod 3 ile R, G, B döngüsü
        int channel = (x + y) % 3;
        return channel switch
        {
            0 => new SKColor(255, 0, 0),   // Red
            1 => new SKColor(0, 255, 0),   // Green
            2 => new SKColor(0, 0, 255),   // Blue
            _ => new SKColor(255, 255, 255)
        };
    }


    /// <inheritdoc/>
    public SKColor GetBackgroundColor(DisplaySettings settings)
    {
        // Arka plan karartma: %0 = #000000, %100 = #0a0a0a
        // BackgroundDarkness değeri arttıkça arka plan daha açık olur
        byte intensity = (byte)(settings.BackgroundDarkness * 10 / 100); // 0-10 arası
        return new SKColor(intensity, intensity, intensity);
    }

    /// <summary>
    /// Parlaklık değerini renge uygular
    /// </summary>
    /// <param name="color">Orijinal renk</param>
    /// <param name="brightness">Parlaklık yüzdesi (0-100)</param>
    /// <returns>Parlaklık uygulanmış renk</returns>
    private SKColor ApplyBrightness(SKColor color, int brightness)
    {
        if (brightness >= 100)
            return color;

        if (brightness <= 0)
            return SKColors.Black;

        float factor = brightness / 100f;
        
        return new SKColor(
            (byte)(color.Red * factor),
            (byte)(color.Green * factor),
            (byte)(color.Blue * factor),
            color.Alpha
        );
    }


    /// <inheritdoc/>
    public void ApplyAgingEffect(bool[,] pixelMatrix, int agingPercent, int? seed = null)
    {
        if (agingPercent <= 0 || agingPercent > 5)
            return;

        int width = pixelMatrix.GetLength(0);
        int height = pixelMatrix.GetLength(1);
        int totalPixels = width * height;
        int deadPixelCount = (int)(totalPixels * agingPercent / 100.0);

        var random = seed.HasValue ? new Random(seed.Value) : new Random();

        // Rastgele pikselleri "öldür"
        int killed = 0;
        while (killed < deadPixelCount)
        {
            int x = random.Next(width);
            int y = random.Next(height);

            // Sadece aktif pikselleri öldür
            if (pixelMatrix[x, y])
            {
                pixelMatrix[x, y] = false;
                killed++;
            }
        }
    }

    /// <inheritdoc/>
    public void ApplyAgingEffect(SKColor[,] pixelMatrix, int agingPercent, int? seed = null)
    {
        if (agingPercent <= 0 || agingPercent > 5)
            return;

        int width = pixelMatrix.GetLength(0);
        int height = pixelMatrix.GetLength(1);
        int totalPixels = width * height;
        int affectedPixelCount = (int)(totalPixels * agingPercent / 100.0);

        var random = seed.HasValue ? new Random(seed.Value) : new Random();

        // Rastgele pikselleri etkile (ölü veya sönük)
        int affected = 0;
        while (affected < affectedPixelCount)
        {
            int x = random.Next(width);
            int y = random.Next(height);

            SKColor currentColor = pixelMatrix[x, y];
            
            // Sadece aktif pikselleri etkile
            if (currentColor.Alpha > 0 && 
                (currentColor.Red > 0 || currentColor.Green > 0 || currentColor.Blue > 0))
            {
                // %50 şansla tamamen öldür, %50 şansla sönükleştir
                if (random.Next(2) == 0)
                {
                    // Tamamen öldür
                    pixelMatrix[x, y] = SKColors.Transparent;
                }
                else
                {
                    // Sönükleştir (%30-50 parlaklık)
                    float dimFactor = 0.3f + (float)random.NextDouble() * 0.2f;
                    pixelMatrix[x, y] = new SKColor(
                        (byte)(currentColor.Red * dimFactor),
                        (byte)(currentColor.Green * dimFactor),
                        (byte)(currentColor.Blue * dimFactor),
                        currentColor.Alpha
                    );
                }
                affected++;
            }
        }
    }

    /// <inheritdoc/>
    public double GetPitchRatio(PixelPitch pitch, double customRatio = 0.7)
    {
        if (pitch == PixelPitch.Custom)
            return Math.Clamp(customRatio, 0.3, 0.95);

        return pitch.GetLedDiameterRatio();
    }
}
