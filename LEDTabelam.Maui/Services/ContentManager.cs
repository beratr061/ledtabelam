using System;
using LEDTabelam.Maui.Models;
using SkiaSharp;
using HAlign = LEDTabelam.Maui.Models.HorizontalAlignment;
using VAlign = LEDTabelam.Maui.Models.VerticalAlignment;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// İçerik yönetimi servisi implementasyonu
/// </summary>
public class ContentManager : IContentManager
{
    private readonly ILedRenderer _ledRenderer;
    private readonly IFontLoader _fontLoader;

    public ContentManager(ILedRenderer ledRenderer, IFontLoader fontLoader)
    {
        _ledRenderer = ledRenderer ?? throw new ArgumentNullException(nameof(ledRenderer));
        _fontLoader = fontLoader ?? throw new ArgumentNullException(nameof(fontLoader));
    }

    /// <inheritdoc/>
    public TextContent CreateTextContent()
    {
        return new TextContent
        {
            Id = Guid.NewGuid().ToString(),
            Name = GetDefaultName(ContentType.Text),
            ContentType = ContentType.Text,
            Text = "",
            FontName = "Default",
            FontSize = 16,
            ForegroundColor = Color.FromRgb(255, 176, 0), // Amber
            BackgroundColor = Colors.Transparent,
            HorizontalAlignment = HAlign.Center,
            VerticalAlignment = VAlign.Center,
            Width = 128,
            Height = 16,
            X = 0,
            Y = 0,
            DurationMs = 3000,
            ShowImmediately = true,
            EntryEffect = new EffectConfig { EffectType = EffectType.Immediate },
            ExitEffect = new EffectConfig { EffectType = EffectType.Immediate }
        };
    }

    /// <inheritdoc/>
    public ClockContent CreateClockContent()
    {
        return new ClockContent
        {
            Id = Guid.NewGuid().ToString(),
            Name = GetDefaultName(ContentType.Clock),
            ContentType = ContentType.Clock,
            Format = "HH:mm:ss",
            FontName = "Default",
            ForegroundColor = Color.FromRgb(255, 176, 0), // Amber
            ShowSeconds = true,
            Is24Hour = true,
            Width = 128,
            Height = 16,
            X = 0,
            Y = 0,
            DurationMs = 0, // Saat sürekli gösterilir
            ShowImmediately = true,
            EntryEffect = new EffectConfig { EffectType = EffectType.Immediate },
            ExitEffect = new EffectConfig { EffectType = EffectType.Immediate }
        };
    }


    /// <inheritdoc/>
    public DateContent CreateDateContent()
    {
        return new DateContent
        {
            Id = Guid.NewGuid().ToString(),
            Name = GetDefaultName(ContentType.Date),
            ContentType = ContentType.Date,
            Format = "dd.MM.yyyy",
            FontName = "Default",
            ForegroundColor = Color.FromRgb(255, 176, 0), // Amber
            Width = 128,
            Height = 16,
            X = 0,
            Y = 0,
            DurationMs = 0, // Tarih sürekli gösterilir
            ShowImmediately = true,
            EntryEffect = new EffectConfig { EffectType = EffectType.Immediate },
            ExitEffect = new EffectConfig { EffectType = EffectType.Immediate }
        };
    }

    /// <inheritdoc/>
    public CountdownContent CreateCountdownContent()
    {
        return new CountdownContent
        {
            Id = Guid.NewGuid().ToString(),
            Name = GetDefaultName(ContentType.Countdown),
            ContentType = ContentType.Countdown,
            TargetDateTime = DateTime.Now.AddHours(1),
            Format = "HH:mm:ss",
            FontName = "Default",
            ForegroundColor = Color.FromRgb(255, 176, 0), // Amber
            CompletedText = "SÜRE DOLDU",
            Width = 128,
            Height = 16,
            X = 0,
            Y = 0,
            DurationMs = 0, // Geri sayım sürekli gösterilir
            ShowImmediately = true,
            EntryEffect = new EffectConfig { EffectType = EffectType.Immediate },
            ExitEffect = new EffectConfig { EffectType = EffectType.Immediate }
        };
    }

    /// <inheritdoc/>
    public void UpdateContent(ContentItem content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        // ObservableObject otomatik olarak PropertyChanged tetikler
        // Burada ek işlemler yapılabilir (örn: validasyon, loglama)
    }

    /// <inheritdoc/>
    public SKBitmap RenderContent(ContentItem content, DisplaySettings settings)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        return content.ContentType switch
        {
            ContentType.Text => RenderTextContent((TextContent)content, settings),
            ContentType.Clock => RenderClockContent((ClockContent)content, settings),
            ContentType.Date => RenderDateContent((DateContent)content, settings),
            ContentType.Countdown => RenderCountdownContent((CountdownContent)content, settings),
            _ => CreateEmptyBitmap(content.Width, content.Height)
        };
    }

    /// <inheritdoc/>
    public SKBitmap RenderTextContent(TextContent content, DisplaySettings settings)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        // DisplaySettings'den genişlik ve yükseklik al
        int width = settings.PanelWidth > 0 ? settings.PanelWidth : content.Width;
        int height = settings.PanelHeight > 0 ? settings.PanelHeight : content.Height;
        
        System.Diagnostics.Debug.WriteLine($"🔵 RenderTextContent: Matrix size = {width}x{height}");

        // Piksel matrisi oluştur
        var pixelMatrix = new SKColor[width, height];
        
        // Arka plan rengini ayarla
        var bgColor = ToSKColor(content.BackgroundColor);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                pixelMatrix[x, y] = bgColor;
            }
        }

        // Metin render et (boşsa varsayılan metin göster)
        var textToRender = string.IsNullOrEmpty(content.Text) ? "Metin Yaz" : content.Text;
        var textColor = ToSKColor(content.ForegroundColor);
        RenderTextToMatrix(pixelMatrix, textToRender, content.FontName, textColor,
            content.HorizontalAlignment, content.VerticalAlignment);

        var bitmap = _ledRenderer.RenderDisplay(pixelMatrix, settings);
        System.Diagnostics.Debug.WriteLine($"🔵 RenderTextContent: Bitmap created = {bitmap?.Width}x{bitmap?.Height}");
        
        return bitmap!;
    }

    /// <inheritdoc/>
    public SKBitmap RenderClockContent(ClockContent content, DisplaySettings settings)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        // Saat metnini oluştur
        string timeText = DateTime.Now.ToString(content.Format);

        // DisplaySettings'den genişlik ve yükseklik al
        int width = settings.PanelWidth > 0 ? settings.PanelWidth : content.Width;
        int height = settings.PanelHeight > 0 ? settings.PanelHeight : content.Height;

        // Piksel matrisi oluştur
        var pixelMatrix = new SKColor[width, height];
        var textColor = ToSKColor(content.ForegroundColor);

        RenderTextToMatrix(pixelMatrix, timeText, content.FontName, textColor,
            HAlign.Center, VAlign.Center);

        return _ledRenderer.RenderDisplay(pixelMatrix, settings);
    }

    /// <inheritdoc/>
    public SKBitmap RenderDateContent(DateContent content, DisplaySettings settings)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        // Tarih metnini oluştur
        string dateText = DateTime.Now.ToString(content.Format);

        // DisplaySettings'den genişlik ve yükseklik al
        int width = settings.PanelWidth > 0 ? settings.PanelWidth : content.Width;
        int height = settings.PanelHeight > 0 ? settings.PanelHeight : content.Height;

        // Piksel matrisi oluştur
        var pixelMatrix = new SKColor[width, height];
        var textColor = ToSKColor(content.ForegroundColor);

        RenderTextToMatrix(pixelMatrix, dateText, content.FontName, textColor,
            HAlign.Center, VAlign.Center);

        return _ledRenderer.RenderDisplay(pixelMatrix, settings);
    }

    /// <inheritdoc/>
    public SKBitmap RenderCountdownContent(CountdownContent content, DisplaySettings settings)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        // Kalan süreyi hesapla
        var remaining = content.TargetDateTime - DateTime.Now;
        string countdownText;

        if (remaining.TotalSeconds <= 0)
        {
            countdownText = content.CompletedText;
        }
        else
        {
            countdownText = remaining.ToString(content.Format);
        }

        // DisplaySettings'den genişlik ve yükseklik al
        int width = settings.PanelWidth > 0 ? settings.PanelWidth : content.Width;
        int height = settings.PanelHeight > 0 ? settings.PanelHeight : content.Height;

        // Piksel matrisi oluştur
        var pixelMatrix = new SKColor[width, height];
        var textColor = ToSKColor(content.ForegroundColor);

        RenderTextToMatrix(pixelMatrix, countdownText, content.FontName, textColor,
            HAlign.Center, VAlign.Center);

        return _ledRenderer.RenderDisplay(pixelMatrix, settings);
    }

    /// <inheritdoc/>
    public string GetDefaultName(ContentType contentType)
    {
        return contentType switch
        {
            ContentType.Text => "Metin Yazı",
            ContentType.Clock => "Saat",
            ContentType.Date => "Tarih",
            ContentType.Countdown => "Geri Sayım",
            ContentType.Image => "Resim",
            _ => "İçerik"
        };
    }

    /// <inheritdoc/>
    public ContentItem CloneContent(ContentItem content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        return content.ContentType switch
        {
            ContentType.Text => CloneTextContent((TextContent)content),
            ContentType.Clock => CloneClockContent((ClockContent)content),
            ContentType.Date => CloneDateContent((DateContent)content),
            ContentType.Countdown => CloneCountdownContent((CountdownContent)content),
            _ => throw new NotSupportedException($"İçerik tipi desteklenmiyor: {content.ContentType}")
        };
    }

    private TextContent CloneTextContent(TextContent source)
    {
        return new TextContent
        {
            Id = Guid.NewGuid().ToString(),
            Name = source.Name + " (Kopya)",
            ContentType = source.ContentType,
            Text = source.Text,
            FontName = source.FontName,
            FontSize = source.FontSize,
            ForegroundColor = source.ForegroundColor,
            BackgroundColor = source.BackgroundColor,
            HorizontalAlignment = source.HorizontalAlignment,
            VerticalAlignment = source.VerticalAlignment,
            IsBold = source.IsBold,
            IsItalic = source.IsItalic,
            IsUnderline = source.IsUnderline,
            IsRightToLeft = source.IsRightToLeft,
            IsScrolling = source.IsScrolling,
            ScrollSpeed = source.ScrollSpeed,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            DurationMs = source.DurationMs,
            ShowImmediately = source.ShowImmediately,
            EntryEffect = CloneEffectConfig(source.EntryEffect),
            ExitEffect = CloneEffectConfig(source.ExitEffect)
        };
    }

    private ClockContent CloneClockContent(ClockContent source)
    {
        return new ClockContent
        {
            Id = Guid.NewGuid().ToString(),
            Name = source.Name + " (Kopya)",
            ContentType = source.ContentType,
            Format = source.Format,
            FontName = source.FontName,
            ForegroundColor = source.ForegroundColor,
            ShowSeconds = source.ShowSeconds,
            Is24Hour = source.Is24Hour,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            DurationMs = source.DurationMs,
            ShowImmediately = source.ShowImmediately,
            EntryEffect = CloneEffectConfig(source.EntryEffect),
            ExitEffect = CloneEffectConfig(source.ExitEffect)
        };
    }

    private DateContent CloneDateContent(DateContent source)
    {
        return new DateContent
        {
            Id = Guid.NewGuid().ToString(),
            Name = source.Name + " (Kopya)",
            ContentType = source.ContentType,
            Format = source.Format,
            FontName = source.FontName,
            ForegroundColor = source.ForegroundColor,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            DurationMs = source.DurationMs,
            ShowImmediately = source.ShowImmediately,
            EntryEffect = CloneEffectConfig(source.EntryEffect),
            ExitEffect = CloneEffectConfig(source.ExitEffect)
        };
    }

    private CountdownContent CloneCountdownContent(CountdownContent source)
    {
        return new CountdownContent
        {
            Id = Guid.NewGuid().ToString(),
            Name = source.Name + " (Kopya)",
            ContentType = source.ContentType,
            TargetDateTime = source.TargetDateTime,
            Format = source.Format,
            FontName = source.FontName,
            ForegroundColor = source.ForegroundColor,
            CompletedText = source.CompletedText,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            DurationMs = source.DurationMs,
            ShowImmediately = source.ShowImmediately,
            EntryEffect = CloneEffectConfig(source.EntryEffect),
            ExitEffect = CloneEffectConfig(source.ExitEffect)
        };
    }

    private EffectConfig CloneEffectConfig(EffectConfig source)
    {
        return new EffectConfig
        {
            EffectType = source.EffectType,
            SpeedMs = source.SpeedMs,
            Direction = source.Direction
        };
    }

    private void RenderTextToMatrix(SKColor[,] matrix, string text, string fontName, 
        SKColor color, HAlign hAlign, VAlign vAlign)
    {
        if (string.IsNullOrEmpty(text))
            return;

        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        // Font'u al
        var font = _fontLoader.GetFont(fontName);
        if (font == null)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ RenderTextToMatrix: Font '{fontName}' bulunamadı, basit render yapılıyor");
            // Font yoksa basit bir şekilde pikselleri yak
            RenderSimpleText(matrix, text, color, hAlign, vAlign);
            return;
        }
        
        if (font.FontImage == null)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ RenderTextToMatrix: Font '{fontName}' görüntüsü yok");
            RenderSimpleText(matrix, text, color, hAlign, vAlign);
            return;
        }

        // Metin genişliğini hesapla
        int textWidth = _fontLoader.CalculateTextWidth(font, text, 1);
        int textHeight = font.LineHeight;

        // Hizalama hesapla
        int startX = hAlign switch
        {
            HAlign.Left => 0,
            HAlign.Center => (width - textWidth) / 2,
            HAlign.Right => width - textWidth,
            _ => 0
        };

        int startY = vAlign switch
        {
            VAlign.Top => 0,
            VAlign.Center => (height - textHeight) / 2,
            VAlign.Bottom => height - textHeight,
            _ => 0
        };

        // Metni bitmap olarak render et
        using var textBitmap = _fontLoader.RenderText(font, text, color, 1);
        
        // Bitmap'i matrise kopyala
        for (int y = 0; y < textBitmap.Height && (startY + y) < height; y++)
        {
            for (int x = 0; x < textBitmap.Width && (startX + x) < width; x++)
            {
                int px = startX + x;
                int py = startY + y;
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    var pixel = textBitmap.GetPixel(x, y);
                    if (pixel.Alpha > 0)
                    {
                        matrix[px, py] = pixel;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Font olmadan basit metin render eder (her karakter için tek piksel)
    /// </summary>
    private void RenderSimpleText(SKColor[,] matrix, string text, SKColor color, HAlign hAlign, VAlign vAlign)
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);
        
        // Her karakter 6 piksel genişliğinde varsayalım
        int charWidth = 6;
        int charHeight = 8;
        int textWidth = text.Length * charWidth;
        
        int startX = hAlign switch
        {
            HAlign.Left => 0,
            HAlign.Center => (width - textWidth) / 2,
            HAlign.Right => width - textWidth,
            _ => 0
        };

        int startY = vAlign switch
        {
            VAlign.Top => 0,
            VAlign.Center => (height - charHeight) / 2,
            VAlign.Bottom => height - charHeight,
            _ => 0
        };

        // Basit bir şekilde her karakter için dikdörtgen çiz
        for (int i = 0; i < text.Length; i++)
        {
            int charX = startX + i * charWidth;
            
            // Basit 5x7 karakter çiz
            for (int y = 0; y < Math.Min(charHeight, height - startY); y++)
            {
                for (int x = 0; x < Math.Min(charWidth - 1, width - charX); x++)
                {
                    int px = charX + x;
                    int py = startY + y;
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        // Basit karakter deseni (dolu dikdörtgen)
                        if (y == 0 || y == charHeight - 1 || x == 0 || x == charWidth - 2)
                        {
                            matrix[px, py] = color;
                        }
                    }
                }
            }
        }
    }

    private static SKColor ToSKColor(Color color)
    {
        return new SKColor(
            (byte)(color.Red * 255),
            (byte)(color.Green * 255),
            (byte)(color.Blue * 255),
            (byte)(color.Alpha * 255)
        );
    }

    private static SKBitmap CreateEmptyBitmap(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        return bitmap;
    }
}
