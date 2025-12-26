using System;
using System.IO;
using SkiaSharp;
using Svg.Skia;

namespace LEDTabelam.Services;

/// <summary>
/// SVG ve bitmap grafik render servisi implementasyonu
/// Requirements: 16.1, 16.2, 16.3, 16.4, 16.5, 16.6, 16.7
/// </summary>
public class SvgRenderer : ISvgRenderer
{
    /// <inheritdoc/>
    public SKBitmap RenderSvg(string svgPath, int targetHeight, SKColor tintColor)
    {
        if (string.IsNullOrEmpty(svgPath))
            throw new ArgumentNullException(nameof(svgPath));

        if (!File.Exists(svgPath))
            throw new FileNotFoundException("SVG dosyası bulunamadı", svgPath);

        var svgContent = File.ReadAllText(svgPath);
        return RenderSvgFromContent(svgContent, targetHeight, tintColor);
    }

    /// <inheritdoc/>
    public SKBitmap RenderSvgFromContent(string svgContent, int targetHeight, SKColor tintColor)
    {
        if (string.IsNullOrEmpty(svgContent))
            throw new ArgumentNullException(nameof(svgContent));

        if (targetHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetHeight), "Hedef yükseklik pozitif olmalıdır");

        using var svg = new SKSvg();
        svg.FromSvg(svgContent);

        if (svg.Picture == null)
            throw new InvalidOperationException("SVG parse edilemedi");

        var bounds = svg.Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidOperationException("SVG boyutları geçersiz");

        // En-boy oranını koruyarak ölçekleme
        float scale = targetHeight / bounds.Height;
        int targetWidth = (int)Math.Ceiling(bounds.Width * scale);

        var bitmap = new SKBitmap(targetWidth, targetHeight);
        using var canvas = new SKCanvas(bitmap);
        
        // Arka planı temizle (şeffaf)
        canvas.Clear(SKColors.Transparent);

        // Hard-edge rendering için antialiasing kapalı
        using var paint = new SKPaint
        {
            IsAntialias = false
        };

        // Ölçekleme uygula
        canvas.Scale(scale);
        
        // SVG'yi çiz
        canvas.DrawPicture(svg.Picture, paint);

        // Renk boyama uygula (siyah/beyaz SVG'ler için)
        return ApplyTintColor(bitmap, tintColor);
    }


    /// <inheritdoc/>
    public SKBitmap RenderBitmap(string imagePath, int threshold = 50)
    {
        if (string.IsNullOrEmpty(imagePath))
            throw new ArgumentNullException(nameof(imagePath));

        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Görüntü dosyası bulunamadı", imagePath);

        using var stream = File.OpenRead(imagePath);
        var source = SKBitmap.Decode(stream);
        
        if (source == null)
            throw new InvalidOperationException("Görüntü dosyası okunamadı");

        return ApplyThreshold(source, threshold);
    }

    /// <inheritdoc/>
    public SKBitmap ApplyThreshold(SKBitmap source, int threshold)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // Threshold değerini 0-100 aralığında sınırla
        threshold = Math.Clamp(threshold, 0, 100);

        // Threshold değerini 0-255 aralığına dönüştür
        int thresholdValue = (int)(threshold * 2.55);

        var result = new SKBitmap(source.Width, source.Height);

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                
                // Pikselin parlaklığını hesapla (grayscale)
                // Standart luminance formülü: 0.299*R + 0.587*G + 0.114*B
                int brightness = (int)(0.299 * pixel.Red + 0.587 * pixel.Green + 0.114 * pixel.Blue);

                // Alpha değerini de dikkate al
                if (pixel.Alpha < 128)
                {
                    // Şeffaf pikseller "off" olarak kabul edilir
                    result.SetPixel(x, y, SKColors.Black);
                }
                else if (brightness >= thresholdValue)
                {
                    // Parlaklık >= threshold: "on" (beyaz)
                    result.SetPixel(x, y, SKColors.White);
                }
                else
                {
                    // Parlaklık < threshold: "off" (siyah)
                    result.SetPixel(x, y, SKColors.Black);
                }
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public SKBitmap ScaleToHeight(SKBitmap source, int targetHeight)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (targetHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetHeight), "Hedef yükseklik pozitif olmalıdır");

        // En-boy oranını koruyarak ölçekleme
        float scale = (float)targetHeight / source.Height;
        int targetWidth = (int)Math.Ceiling(source.Width * scale);

        var result = new SKBitmap(targetWidth, targetHeight);
        using var canvas = new SKCanvas(result);

        // Hard-edge rendering için Nearest Neighbor kullan
        using var paint = new SKPaint
        {
            IsAntialias = false
        };

        var destRect = new SKRect(0, 0, targetWidth, targetHeight);
        canvas.DrawBitmap(source, destRect, paint);

        return result;
    }

    /// <summary>
    /// Bitmap'e renk boyama uygular (siyah/beyaz görüntüler için)
    /// Beyaz pikseller belirtilen renge, siyah pikseller siyah kalır
    /// </summary>
    private SKBitmap ApplyTintColor(SKBitmap source, SKColor tintColor)
    {
        var result = new SKBitmap(source.Width, source.Height);

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);

                // Şeffaf pikselleri koru
                if (pixel.Alpha < 128)
                {
                    result.SetPixel(x, y, SKColors.Transparent);
                    continue;
                }

                // Pikselin parlaklığını hesapla
                int brightness = (int)(0.299 * pixel.Red + 0.587 * pixel.Green + 0.114 * pixel.Blue);

                if (brightness > 128)
                {
                    // Açık renkli pikselleri tint rengine boya
                    result.SetPixel(x, y, tintColor);
                }
                else
                {
                    // Koyu pikselleri siyah yap
                    result.SetPixel(x, y, SKColors.Black);
                }
            }
        }

        return result;
    }
}
