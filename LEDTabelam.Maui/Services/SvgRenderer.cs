using System;
using System.IO;
using SkiaSharp;
using Svg.Skia;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// SVG ve bitmap grafik render servisi implementasyonu
/// </summary>
public class SvgRenderer : ISvgRenderer
{
    public SKBitmap RenderSvg(string svgPath, int targetHeight, SKColor tintColor)
    {
        if (string.IsNullOrEmpty(svgPath))
            throw new ArgumentNullException(nameof(svgPath));

        if (!File.Exists(svgPath))
            throw new FileNotFoundException("SVG dosyası bulunamadı", svgPath);

        var svgContent = File.ReadAllText(svgPath);
        return RenderSvgFromContent(svgContent, targetHeight, tintColor);
    }

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

        float scale = targetHeight / bounds.Height;
        int targetWidth = (int)Math.Ceiling(bounds.Width * scale);

        var bitmap = new SKBitmap(targetWidth, targetHeight);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint { IsAntialias = false };

        canvas.Scale(scale);
        canvas.DrawPicture(svg.Picture, paint);

        return ApplyTintColor(bitmap, tintColor);
    }

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

    public SKBitmap ApplyThreshold(SKBitmap source, int threshold)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        threshold = Math.Clamp(threshold, 0, 100);
        int thresholdValue = (int)(threshold * 2.55);

        var result = new SKBitmap(source.Width, source.Height);

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                int brightness = (int)(0.299 * pixel.Red + 0.587 * pixel.Green + 0.114 * pixel.Blue);

                if (pixel.Alpha < 128)
                    result.SetPixel(x, y, SKColors.Black);
                else if (brightness >= thresholdValue)
                    result.SetPixel(x, y, SKColors.White);
                else
                    result.SetPixel(x, y, SKColors.Black);
            }
        }

        return result;
    }

    public SKBitmap ScaleToHeight(SKBitmap source, int targetHeight)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (targetHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetHeight), "Hedef yükseklik pozitif olmalıdır");

        if (source.Width <= 0 || source.Height <= 0)
            throw new ArgumentException("Kaynak bitmap boyutları geçersiz", nameof(source));

        float scale = (float)targetHeight / source.Height;
        int targetWidth = Math.Max(1, (int)Math.Ceiling(source.Width * scale));

        var result = new SKBitmap(targetWidth, targetHeight);
        using var canvas = new SKCanvas(result);

        using var paint = new SKPaint { IsAntialias = false };

        var destRect = new SKRect(0, 0, targetWidth, targetHeight);
        canvas.DrawBitmap(source, destRect, paint);

        return result;
    }

    private SKBitmap ApplyTintColor(SKBitmap source, SKColor tintColor)
    {
        var result = new SKBitmap(source.Width, source.Height);

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);

                if (pixel.Alpha < 128)
                {
                    result.SetPixel(x, y, SKColors.Transparent);
                    continue;
                }

                int brightness = (int)(0.299 * pixel.Red + 0.587 * pixel.Green + 0.114 * pixel.Blue);

                if (brightness > 128)
                    result.SetPixel(x, y, tintColor);
                else
                    result.SetPixel(x, y, SKColors.Black);
            }
        }

        return result;
    }
}
