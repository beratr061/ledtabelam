using System;
using LEDTabelam.Maui.Models;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// LED render servisi implementasyonu
/// </summary>
public class LedRenderer : ILedRenderer, IDisposable
{
    private readonly SKPaint _ledPaint = new SKPaint
    {
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _offLedPaint = new SKPaint
    {
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _gridPaint = new SKPaint
    {
        IsAntialias = false,
        Style = SKPaintStyle.Fill,
        Color = SKColors.Black
    };

    private readonly SKPaint _glowPaint = new SKPaint
    {
        Color = new SKColor(255, 255, 255, 77)
    };

    private SKImageFilter? _cachedGlowFilter;
    private float _cachedGlowRadius = -1;
    private bool _disposed;

    public SKBitmap RenderDisplay(bool[,] pixelMatrix, DisplaySettings settings)
    {
        int matrixWidth = pixelMatrix.GetLength(0);
        int matrixHeight = pixelMatrix.GetLength(1);

        if (matrixWidth == 0 || matrixHeight == 0)
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);

        int pixelSize = Math.Max(1, settings.PixelSize);
        double ledRatio = settings.Pitch.GetLedDiameterRatio();
        if (settings.Pitch == PixelPitch.Custom)
            ledRatio = settings.CustomPitchRatio;

        int ledDiameter = Math.Max(1, (int)(pixelSize * ledRatio));
        int bitmapWidth = Math.Max(1, matrixWidth * pixelSize);
        int bitmapHeight = Math.Max(1, matrixHeight * pixelSize);

        var bitmap = new SKBitmap(bitmapWidth, bitmapHeight, SKColorType.Rgba8888, SKAlphaType.Premul);

        if (bitmap.Handle == IntPtr.Zero)
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);

        using var canvas = new SKCanvas(bitmap);

        SKColor backgroundColor = GetBackgroundColor(settings);
        canvas.Clear(backgroundColor);

        SKColor ledColor = GetLedColor(settings);
        ledColor = ApplyBrightness(ledColor, settings.Brightness);
        _ledPaint.Color = ledColor;

        SKColor offLedColor = GetOffLedColor(settings);
        _offLedPaint.Color = offLedColor;

        for (int x = 0; x < matrixWidth; x++)
        {
            for (int y = 0; y < matrixHeight; y++)
            {
                bool isLit = pixelMatrix[x, y];
                if (settings.InvertColors)
                    isLit = !isLit;

                if (isLit)
                    DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, _ledPaint);
                else
                    DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, _offLedPaint);
            }
        }

        return bitmap;
    }

    public SKBitmap RenderDisplay(SKColor[,] pixelMatrix, DisplaySettings settings)
    {
        int matrixWidth = pixelMatrix.GetLength(0);
        int matrixHeight = pixelMatrix.GetLength(1);

        if (matrixWidth == 0 || matrixHeight == 0)
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);

        int pixelSize = Math.Max(1, settings.PixelSize);
        double ledRatio = settings.Pitch.GetLedDiameterRatio();
        if (settings.Pitch == PixelPitch.Custom)
            ledRatio = settings.CustomPitchRatio;

        int ledDiameter = Math.Max(1, (int)(pixelSize * ledRatio));
        int bitmapWidth = Math.Max(1, matrixWidth * pixelSize);
        int bitmapHeight = Math.Max(1, matrixHeight * pixelSize);

        var bitmap = new SKBitmap(bitmapWidth, bitmapHeight, SKColorType.Rgba8888, SKAlphaType.Premul);

        if (bitmap.Handle == IntPtr.Zero)
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);

        using var canvas = new SKCanvas(bitmap);

        SKColor backgroundColor = GetBackgroundColor(settings);
        canvas.Clear(backgroundColor);

        SKColor offLedColor = GetOffLedColor(settings);
        _offLedPaint.Color = offLedColor;

        for (int x = 0; x < matrixWidth; x++)
        {
            for (int y = 0; y < matrixHeight; y++)
            {
                SKColor pixelColor = pixelMatrix[x, y];

                if (settings.InvertColors)
                {
                    bool isLit = pixelColor.Alpha > 0 &&
                                 (pixelColor.Red > 0 || pixelColor.Green > 0 || pixelColor.Blue > 0);

                    if (!isLit)
                        pixelColor = GetLedColor(settings);
                    else
                    {
                        DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, _offLedPaint);
                        continue;
                    }
                }
                else if (pixelColor.Alpha == 0 ||
                         (pixelColor.Red == 0 && pixelColor.Green == 0 && pixelColor.Blue == 0))
                {
                    DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, _offLedPaint);
                    continue;
                }

                SKColor finalColor = ApplyBrightness(pixelColor, settings.Brightness);
                _ledPaint.Color = finalColor;
                DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, _ledPaint);
            }
        }

        return bitmap;
    }

    private void DrawLedPixel(SKCanvas canvas, int x, int y, int pixelSize, int ledDiameter,
                              PixelShape shape, SKPaint paint)
    {
        float centerX = x * pixelSize + pixelSize / 2f;
        float centerY = y * pixelSize + pixelSize / 2f;
        float radius = ledDiameter / 2f;

        if (shape == PixelShape.Round)
            canvas.DrawCircle(centerX, centerY, radius, paint);
        else
        {
            float left = centerX - radius;
            float top = centerY - radius;
            canvas.DrawRect(left, top, ledDiameter, ledDiameter, paint);
        }
    }

    public SKBitmap RenderWithGlow(SKBitmap source, DisplaySettings settings)
    {
        if (source == null || source.Handle == IntPtr.Zero || source.Width <= 0 || source.Height <= 0)
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);

        float glowRadius = 2 + (settings.Brightness / 100f) * 8;

        var result = new SKBitmap(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

        if (result.Handle == IntPtr.Zero)
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);

        using var canvas = new SKCanvas(result);

        SKColor backgroundColor = GetBackgroundColor(settings);
        canvas.Clear(backgroundColor);

        if (_cachedGlowFilter == null || _cachedGlowRadius != glowRadius)
        {
            _cachedGlowFilter?.Dispose();
            _cachedGlowFilter = SKImageFilter.CreateBlur(glowRadius, glowRadius);
            _cachedGlowRadius = glowRadius;
        }

        _glowPaint.ImageFilter = _cachedGlowFilter;
        canvas.DrawBitmap(source, 0, 0, _glowPaint);
        canvas.DrawBitmap(source, 0, 0);
        _glowPaint.ImageFilter = null;

        return result;
    }

    public void DrawGridOverlay(SKCanvas canvas, DisplaySettings settings)
    {
        int pixelSize = settings.PixelSize;
        double ledRatio = settings.Pitch.GetLedDiameterRatio();
        if (settings.Pitch == PixelPitch.Custom)
            ledRatio = settings.CustomPitchRatio;

        int ledDiameter = (int)(pixelSize * ledRatio);
        int gridWidth = (pixelSize - ledDiameter) / 2;

        var canvasInfo = canvas.DeviceClipBounds;
        int canvasWidth = (int)canvasInfo.Width;
        int canvasHeight = (int)canvasInfo.Height;

        int matrixWidth = canvasWidth / pixelSize;
        int matrixHeight = canvasHeight / pixelSize;

        for (int x = 0; x <= matrixWidth; x++)
        {
            float lineX = x * pixelSize - gridWidth / 2f;
            canvas.DrawRect(lineX, 0, gridWidth, canvasHeight, _gridPaint);
        }

        for (int y = 0; y <= matrixHeight; y++)
        {
            float lineY = y * pixelSize - gridWidth / 2f;
            canvas.DrawRect(0, lineY, canvasWidth, gridWidth, _gridPaint);
        }
    }

    public SKColor GetLedColor(DisplaySettings settings)
    {
        return settings.ColorType switch
        {
            LedColorType.Amber => new SKColor(255, 176, 0),
            LedColorType.Red => new SKColor(255, 0, 0),
            LedColorType.Green => new SKColor(0, 255, 0),
            LedColorType.OneROneGOneB => new SKColor(255, 255, 255),
            LedColorType.FullRGB => new SKColor(
                (byte)(settings.CustomColor.Red * 255),
                (byte)(settings.CustomColor.Green * 255),
                (byte)(settings.CustomColor.Blue * 255)),
            _ => new SKColor(255, 176, 0)
        };
    }

    public SKColor GetBackgroundColor(DisplaySettings settings)
    {
        byte intensity = (byte)(settings.BackgroundDarkness * 10 / 100);
        return new SKColor(intensity, intensity, intensity);
    }

    public SKColor GetOffLedColor(DisplaySettings settings)
    {
        byte baseIntensity = (byte)(settings.BackgroundDarkness * 10 / 100);
        byte offIntensity = (byte)Math.Min(255, baseIntensity + 15);
        return new SKColor(offIntensity, offIntensity, offIntensity);
    }

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

    public void ApplyAgingEffect(bool[,] pixelMatrix, int agingPercent, int? seed = null)
    {
        if (agingPercent <= 0 || agingPercent > 5)
            return;

        int width = pixelMatrix.GetLength(0);
        int height = pixelMatrix.GetLength(1);
        int totalPixels = width * height;
        int deadPixelCount = (int)(totalPixels * agingPercent / 100.0);

        var random = seed.HasValue ? new Random(seed.Value) : new Random();

        int killed = 0;
        while (killed < deadPixelCount)
        {
            int x = random.Next(width);
            int y = random.Next(height);

            if (pixelMatrix[x, y])
            {
                pixelMatrix[x, y] = false;
                killed++;
            }
        }
    }

    public void ApplyAgingEffect(SKColor[,] pixelMatrix, int agingPercent, int? seed = null)
    {
        if (agingPercent <= 0 || agingPercent > 5)
            return;

        int width = pixelMatrix.GetLength(0);
        int height = pixelMatrix.GetLength(1);
        int totalPixels = width * height;
        int affectedPixelCount = (int)(totalPixels * agingPercent / 100.0);

        var random = seed.HasValue ? new Random(seed.Value) : new Random();

        int affected = 0;
        while (affected < affectedPixelCount)
        {
            int x = random.Next(width);
            int y = random.Next(height);

            SKColor currentColor = pixelMatrix[x, y];

            if (currentColor.Alpha > 0 &&
                (currentColor.Red > 0 || currentColor.Green > 0 || currentColor.Blue > 0))
            {
                if (random.Next(2) == 0)
                    pixelMatrix[x, y] = SKColors.Transparent;
                else
                {
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

    public double GetPitchRatio(PixelPitch pitch, double customRatio = 0.7)
    {
        if (pitch == PixelPitch.Custom)
            return Math.Clamp(customRatio, 0.3, 0.95);

        return pitch.GetLedDiameterRatio();
    }

    public void DrawBorder(bool[,] pixelMatrix, BorderSettings border, int x, int y, int width, int height)
    {
        if (!border.IsEnabled || width <= 0 || height <= 0) return;

        int matrixWidth = pixelMatrix.GetLength(0);
        int matrixHeight = pixelMatrix.GetLength(1);

        for (int line = 0; line < border.HorizontalLines; line++)
        {
            int lineY = y + line;
            if (lineY >= 0 && lineY < matrixHeight)
            {
                for (int px = x; px < x + width && px < matrixWidth; px++)
                {
                    if (px >= 0) pixelMatrix[px, lineY] = true;
                }
            }
        }

        for (int line = 0; line < border.HorizontalLines; line++)
        {
            int lineY = y + height - 1 - line;
            if (lineY >= 0 && lineY < matrixHeight)
            {
                for (int px = x; px < x + width && px < matrixWidth; px++)
                {
                    if (px >= 0) pixelMatrix[px, lineY] = true;
                }
            }
        }

        for (int line = 0; line < border.VerticalLines; line++)
        {
            int lineX = x + line;
            if (lineX >= 0 && lineX < matrixWidth)
            {
                for (int py = y; py < y + height && py < matrixHeight; py++)
                {
                    if (py >= 0) pixelMatrix[lineX, py] = true;
                }
            }
        }

        for (int line = 0; line < border.VerticalLines; line++)
        {
            int lineX = x + width - 1 - line;
            if (lineX >= 0 && lineX < matrixWidth)
            {
                for (int py = y; py < y + height && py < matrixHeight; py++)
                {
                    if (py >= 0) pixelMatrix[lineX, py] = true;
                }
            }
        }
    }

    public void DrawBorder(SKColor[,] colorMatrix, BorderSettings border, int x, int y, int width, int height)
    {
        if (!border.IsEnabled || width <= 0 || height <= 0) return;

        int matrixWidth = colorMatrix.GetLength(0);
        int matrixHeight = colorMatrix.GetLength(1);

        var borderColor = new SKColor(
            (byte)(border.Color.Red * 255),
            (byte)(border.Color.Green * 255),
            (byte)(border.Color.Blue * 255),
            (byte)(border.Color.Alpha * 255));

        for (int line = 0; line < border.HorizontalLines; line++)
        {
            int lineY = y + line;
            if (lineY >= 0 && lineY < matrixHeight)
            {
                for (int px = x; px < x + width && px < matrixWidth; px++)
                {
                    if (px >= 0) colorMatrix[px, lineY] = borderColor;
                }
            }
        }

        for (int line = 0; line < border.HorizontalLines; line++)
        {
            int lineY = y + height - 1 - line;
            if (lineY >= 0 && lineY < matrixHeight)
            {
                for (int px = x; px < x + width && px < matrixWidth; px++)
                {
                    if (px >= 0) colorMatrix[px, lineY] = borderColor;
                }
            }
        }

        for (int line = 0; line < border.VerticalLines; line++)
        {
            int lineX = x + line;
            if (lineX >= 0 && lineX < matrixWidth)
            {
                for (int py = y; py < y + height && py < matrixHeight; py++)
                {
                    if (py >= 0) colorMatrix[lineX, py] = borderColor;
                }
            }
        }

        for (int line = 0; line < border.VerticalLines; line++)
        {
            int lineX = x + width - 1 - line;
            if (lineX >= 0 && lineX < matrixWidth)
            {
                for (int py = y; py < y + height && py < matrixHeight; py++)
                {
                    if (py >= 0) colorMatrix[lineX, py] = borderColor;
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        _ledPaint.Dispose();
        _offLedPaint.Dispose();
        _gridPaint.Dispose();
        _glowPaint.Dispose();
        _cachedGlowFilter?.Dispose();
        _cachedGlowFilter = null;

        GC.SuppressFinalize(this);
    }
}
