using System;
using System.Collections.Generic;
using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// LED render servisi implementasyonu
/// Requirements: 6.1, 6.2, 6.5, 6.10, 6.11, 6.12, 6.13, 6.14, 6.15, 19.1, 19.2, 19.3, 19.4, 19.5, 19.6
/// 
/// Performans optimizasyonları:
/// - Bitmap Reuse: Panel boyutu değişmedikçe aynı bitmap yeniden kullanılır (GC pressure azaltılır)
/// - Paint Caching: SKPaint nesneleri önbelleğe alınır
/// - Thread-safe: Bitmap'ler background thread'de oluşturulabilir
/// </summary>
public class LedRenderer : ILedRenderer, IDisposable
{
    // Paint nesnelerini önbelleğe al - GC baskısını azaltır
    private readonly SKPaint _ledPaint = new SKPaint
    {
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };

    // Yanmayan (off) LED'ler için paint - gerçekçi görünüm
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
        Color = new SKColor(255, 255, 255, 77) // %30 alpha
    };

    // Glow filtresi önbelleği - parlaklık değişmediğinde yeniden kullanılır
    private SKImageFilter? _cachedGlowFilter;
    private float _cachedGlowRadius = -1;

    // Bitmap reuse için render target önbelleği
    private SKBitmap? _renderTarget;
    private int _cachedWidth;
    private int _cachedHeight;
    private readonly object _bitmapLock = new();

    // Glow için ikinci buffer
    private SKBitmap? _glowTarget;
    private int _glowCachedWidth;
    private int _glowCachedHeight;

    private bool _disposed;

    /// <inheritdoc/>
    public SKBitmap RenderDisplay(bool[,] pixelMatrix, DisplaySettings settings)
    {
        int matrixWidth = pixelMatrix.GetLength(0);
        int matrixHeight = pixelMatrix.GetLength(1);

        // Boş matrix kontrolü
        if (matrixWidth == 0 || matrixHeight == 0)
        {
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        }

        // Piksel boyutu - pitch'e göre ayarlanmış
        int pixelSize = Math.Max(1, settings.PixelSize);
        double ledRatio = settings.Pitch.GetLedDiameterRatio();
        if (settings.Pitch == PixelPitch.Custom)
            ledRatio = settings.CustomPitchRatio;
        
        int ledDiameter = Math.Max(1, (int)(pixelSize * ledRatio));

        // Bitmap boyutları
        int bitmapWidth = Math.Max(1, matrixWidth * pixelSize);
        int bitmapHeight = Math.Max(1, matrixHeight * pixelSize);

        // Her seferinde yeni bitmap oluştur (thread safety için)
        var bitmap = new SKBitmap(bitmapWidth, bitmapHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        
        if (bitmap.Handle == IntPtr.Zero)
        {
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        }
        
        using var canvas = new SKCanvas(bitmap);

        // Arka plan rengi
        SKColor backgroundColor = GetBackgroundColor(settings);
        canvas.Clear(backgroundColor);

        // LED rengi
        SKColor ledColor = GetLedColor(settings);
        
        // Parlaklık uygula
        ledColor = ApplyBrightness(ledColor, settings.Brightness);

        // Önbelleğe alınmış paint'i güncelle
        _ledPaint.Color = ledColor;
        
        // Yanmayan LED rengi - arka plandan biraz daha açık, gerçekçi görünüm
        SKColor offLedColor = GetOffLedColor(settings);
        _offLedPaint.Color = offLedColor;

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
                    DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, _ledPaint);
                }
                else
                {
                    // Yanmayan pikseli de çiz - gerçekçi LED panel görünümü
                    DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, _offLedPaint);
                }
            }
        }

        return bitmap;
    }

    /// <summary>
    /// Bitmap reuse için render target al veya oluştur
    /// Thread-safe: Background thread'den çağrılabilir
    /// </summary>
    private SKBitmap GetOrCreateRenderTarget(int width, int height)
    {
        // Minimum boyut kontrolü - 0 veya negatif boyut SkiaSharp'ı çökertir
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        
        lock (_bitmapLock)
        {
            // Boyut değiştiyse yeni bitmap oluştur
            if (_renderTarget == null || _cachedWidth != width || _cachedHeight != height)
            {
                _renderTarget?.Dispose();
                _renderTarget = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
                _cachedWidth = width;
                _cachedHeight = height;
            }
            
            // Bitmap geçerli değilse yeniden oluştur
            if (_renderTarget.Handle == IntPtr.Zero)
            {
                _renderTarget = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            }
            
            return _renderTarget;
        }
    }

    /// <summary>
    /// Glow için render target al veya oluştur
    /// </summary>
    private SKBitmap GetOrCreateGlowTarget(int width, int height)
    {
        // Minimum boyut kontrolü
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        
        lock (_bitmapLock)
        {
            if (_glowTarget == null || _glowCachedWidth != width || _glowCachedHeight != height)
            {
                _glowTarget?.Dispose();
                _glowTarget = new SKBitmap(width, height);
                _glowCachedWidth = width;
                _glowCachedHeight = height;
            }
            return _glowTarget;
        }
    }

    /// <summary>
    /// Yeni bir bitmap kopyası oluşturur (UI thread'e gönderilecek frame'ler için)
    /// Background thread'de render edilen bitmap'in kopyasını alır
    /// </summary>
    public SKBitmap CreateFrameCopy(SKBitmap source)
    {
        var copy = new SKBitmap(source.Width, source.Height);
        using var canvas = new SKCanvas(copy);
        canvas.DrawBitmap(source, 0, 0);
        return copy;
    }


    /// <inheritdoc/>
    public SKBitmap RenderDisplay(SKColor[,] pixelMatrix, DisplaySettings settings)
    {
        int matrixWidth = pixelMatrix.GetLength(0);
        int matrixHeight = pixelMatrix.GetLength(1);

        // Boş matrix kontrolü
        if (matrixWidth == 0 || matrixHeight == 0)
        {
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        }

        // Piksel boyutu - pitch'e göre ayarlanmış
        int pixelSize = Math.Max(1, settings.PixelSize);
        double ledRatio = settings.Pitch.GetLedDiameterRatio();
        if (settings.Pitch == PixelPitch.Custom)
            ledRatio = settings.CustomPitchRatio;
        
        int ledDiameter = Math.Max(1, (int)(pixelSize * ledRatio));

        // Bitmap boyutları
        int bitmapWidth = Math.Max(1, matrixWidth * pixelSize);
        int bitmapHeight = Math.Max(1, matrixHeight * pixelSize);

        // Her seferinde yeni bitmap oluştur (thread safety için)
        var bitmap = new SKBitmap(bitmapWidth, bitmapHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        
        if (bitmap.Handle == IntPtr.Zero)
        {
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        }
        
        using var canvas = new SKCanvas(bitmap);

        // Arka plan rengi
        SKColor backgroundColor = GetBackgroundColor(settings);
        canvas.Clear(backgroundColor);

        // Yanmayan LED rengi
        SKColor offLedColor = GetOffLedColor(settings);
        _offLedPaint.Color = offLedColor;

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
                        // Aktif pikseli pasif yap - yanmayan LED olarak çiz
                        DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, _offLedPaint);
                        continue;
                    }
                }
                else if (pixelColor.Alpha == 0 || 
                         (pixelColor.Red == 0 && pixelColor.Green == 0 && pixelColor.Blue == 0))
                {
                    // Yanmayan pikseli de çiz - gerçekçi LED panel görünümü
                    DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, _offLedPaint);
                    continue;
                }

                // Parlaklık uygula
                SKColor finalColor = ApplyBrightness(pixelColor, settings.Brightness);
                _ledPaint.Color = finalColor;

                DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, _ledPaint);
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
        // Source bitmap kontrolü
        if (source == null || source.Handle == IntPtr.Zero || source.Width <= 0 || source.Height <= 0)
        {
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        }
        
        // Glow yarıçapı parlaklığa göre 2-10 piksel
        float glowRadius = 2 + (settings.Brightness / 100f) * 8;
        
        // Her seferinde yeni bitmap oluştur
        var result = new SKBitmap(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        
        if (result.Handle == IntPtr.Zero)
        {
            return new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        }
        
        using var canvas = new SKCanvas(result);

        // Arka plan rengi
        SKColor backgroundColor = GetBackgroundColor(settings);
        canvas.Clear(backgroundColor);

        // Glow filtresi önbelleği - sadece radius değiştiğinde yeniden oluştur
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (_cachedGlowFilter == null || _cachedGlowRadius != glowRadius)
        {
            _cachedGlowFilter?.Dispose();
            _cachedGlowFilter = SKImageFilter.CreateBlur(glowRadius, glowRadius);
            _cachedGlowRadius = glowRadius;
        }

        _glowPaint.ImageFilter = _cachedGlowFilter;

        // Önce glow katmanını çiz
        canvas.DrawBitmap(source, 0, 0, _glowPaint);

        // Sonra orijinal görüntüyü üzerine çiz
        canvas.DrawBitmap(source, 0, 0);

        // Filter'ı temizle (sonraki kullanımlar için)
        _glowPaint.ImageFilter = null;

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
            canvas.DrawRect(lineX, 0, gridWidth, canvasHeight, _gridPaint);
        }

        // Yatay grid çizgileri
        for (int y = 0; y <= matrixHeight; y++)
        {
            float lineY = y * pixelSize - gridWidth / 2f;
            canvas.DrawRect(0, lineY, canvasWidth, gridWidth, _gridPaint);
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
    /// Gerçek LED panellerde dikey (vertical) strip yapısı kullanılır
    /// Her sütun tek bir renk kanalına sahiptir (R, G, B döngüsü)
    /// </summary>
    public SKColor GetOneROneGOneBColor(int x, int y)
    {
        // Dikey strip yapısı: x % 3 ile R, G, B döngüsü
        int channel = x % 3;
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
    /// Yanmayan LED'lerin rengini döndürür
    /// Gerçek LED panellerde yanmayan LED'ler tamamen görünmez değildir,
    /// hafif koyu bir renkte görünürler
    /// </summary>
    public SKColor GetOffLedColor(DisplaySettings settings)
    {
        // Arka plan renginden biraz daha açık - LED'in fiziksel varlığını gösterir
        // Gerçek panellerde yanmayan LED'ler koyu gri/kahverengi tonlarında görünür
        byte baseIntensity = (byte)(settings.BackgroundDarkness * 10 / 100);
        byte offIntensity = (byte)Math.Min(255, baseIntensity + 15); // Arka plandan 15 birim daha açık
        return new SKColor(offIntensity, offIntensity, offIntensity);
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

    /// <inheritdoc/>
    public void DrawBorder(bool[,] pixelMatrix, BorderSettings border, int x, int y, int width, int height)
    {
        if (!border.IsEnabled || width <= 0 || height <= 0) return;

        int matrixWidth = pixelMatrix.GetLength(0);
        int matrixHeight = pixelMatrix.GetLength(1);

        // Üst kenar çizgileri
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

        // Alt kenar çizgileri
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

        // Sol kenar çizgileri
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

        // Sağ kenar çizgileri
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

    /// <inheritdoc/>
    public void DrawBorder(SKColor[,] colorMatrix, BorderSettings border, int x, int y, int width, int height)
    {
        if (!border.IsEnabled || width <= 0 || height <= 0) return;

        int matrixWidth = colorMatrix.GetLength(0);
        int matrixHeight = colorMatrix.GetLength(1);
        
        // Avalonia Color'ı SKColor'a dönüştür
        var borderColor = new SKColor(border.Color.R, border.Color.G, border.Color.B, border.Color.A);

        // Üst kenar çizgileri
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

        // Alt kenar çizgileri
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

        // Sol kenar çizgileri
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

        // Sağ kenar çizgileri
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

    /// <summary>
    /// Kaynakları temizle
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        lock (_bitmapLock)
        {
            _renderTarget?.Dispose();
            _renderTarget = null;
            _glowTarget?.Dispose();
            _glowTarget = null;
        }
        
        _ledPaint.Dispose();
        _offLedPaint.Dispose();
        _gridPaint.Dispose();
        _glowPaint.Dispose();
        _cachedGlowFilter?.Dispose();
        _cachedGlowFilter = null;
        
        GC.SuppressFinalize(this);
    }
}
