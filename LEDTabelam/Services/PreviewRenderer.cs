using System;
using System.Collections.Generic;
using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Önizleme render servisi implementasyonu
/// ViewModel'den render mantığını ayırarak MVVM uyumluluğu sağlar
/// </summary>
public class PreviewRenderer : IPreviewRenderer
{
    private readonly IFontLoader _fontLoader;
    private readonly IMultiLineTextRenderer _multiLineTextRenderer;

    public PreviewRenderer(IFontLoader fontLoader, IMultiLineTextRenderer multiLineTextRenderer)
    {
        _fontLoader = fontLoader ?? throw new ArgumentNullException(nameof(fontLoader));
        _multiLineTextRenderer = multiLineTextRenderer ?? throw new ArgumentNullException(nameof(multiLineTextRenderer));
    }

    /// <inheritdoc/>
    public SKColor[,] RenderZonesToColorMatrix(BitmapFont font, IReadOnlyList<Zone> zones, DisplaySettings settings)
    {
        int totalWidth = settings.Width;
        int totalHeight = settings.Height;
        var colorMatrix = new SKColor[totalWidth, totalHeight];

        int currentX = 0;
        foreach (var zone in zones)
        {
            int zoneWidth = (int)(totalWidth * zone.WidthPercent / 100.0);
            if (zoneWidth <= 0) continue;

            var zoneColor = new SKColor(zone.TextColor.R, zone.TextColor.G, zone.TextColor.B);
            var content = zone.Content;

            if (!string.IsNullOrEmpty(content))
            {
                RenderZoneContent(font, content, zoneColor, zone, currentX, zoneWidth, totalWidth, totalHeight, colorMatrix);
            }

            currentX += zoneWidth;
        }

        return colorMatrix;
    }

    private void RenderZoneContent(
        BitmapFont font,
        string content,
        SKColor zoneColor,
        Zone zone,
        int currentX,
        int zoneWidth,
        int totalWidth,
        int totalHeight,
        SKColor[,] colorMatrix)
    {
        int lineCount = _multiLineTextRenderer.GetLineCount(content);
        SKBitmap? textBitmap = null;

        try
        {
            textBitmap = lineCount > 1
                ? _multiLineTextRenderer.RenderMultiLineText(font, content, zoneColor, 0)
                : _fontLoader.RenderText(font, content, zoneColor);

            if (textBitmap == null) return;

            int textWidth = Math.Min(textBitmap.Width, zoneWidth);
            int textHeight = textBitmap.Height;

            // Yatay hizalama
            int offsetX = zone.HAlign switch
            {
                HorizontalAlignment.Left => 0,
                HorizontalAlignment.Center => Math.Max(0, (zoneWidth - textWidth) / 2),
                HorizontalAlignment.Right => Math.Max(0, zoneWidth - textWidth),
                _ => 0
            };

            // Dikey hizalama
            int offsetY = textHeight >= totalHeight ? 0 : zone.VAlign switch
            {
                VerticalAlignment.Top => 0,
                VerticalAlignment.Center => (totalHeight - textHeight) / 2,
                VerticalAlignment.Bottom => totalHeight - textHeight,
                _ => 0
            };

            // Pikselleri kopyala
            CopyPixelsToMatrix(textBitmap, colorMatrix, zoneColor, 
                currentX + offsetX, offsetY, textWidth, textHeight, totalWidth, totalHeight);
        }
        finally
        {
            textBitmap?.Dispose();
        }
    }

    /// <inheritdoc/>
    public SKColor[,] RenderProgramToColorMatrix(
        IReadOnlyList<TabelaItem> items,
        BitmapFont? defaultFont,
        Func<string, BitmapFont?> fontResolver,
        DisplaySettings settings)
    {
        int totalWidth = settings.Width;
        int totalHeight = settings.Height;
        var colorMatrix = new SKColor[totalWidth, totalHeight];

        foreach (var item in items)
        {
            if (!item.IsVisible || string.IsNullOrEmpty(item.Content))
                continue;

            var itemFont = fontResolver(item.FontName) ?? defaultFont;
            if (itemFont == null) continue;

            var itemColor = new SKColor(item.Color.R, item.Color.G, item.Color.B);
            RenderProgramItem(itemFont, item, itemColor, totalWidth, totalHeight, colorMatrix);
        }

        return colorMatrix;
    }

    private void RenderProgramItem(
        BitmapFont font,
        TabelaItem item,
        SKColor itemColor,
        int totalWidth,
        int totalHeight,
        SKColor[,] colorMatrix)
    {
        SKBitmap? textBitmap = null;

        try
        {
            int lineCount = _multiLineTextRenderer.GetLineCount(item.Content);
            textBitmap = lineCount > 1
                ? _multiLineTextRenderer.RenderMultiLineText(font, item.Content, itemColor, 0)
                : _fontLoader.RenderText(font, item.Content, itemColor);

            if (textBitmap == null) return;

            int textWidth = textBitmap.Width;
            int textHeight = textBitmap.Height;

            // Öğe sınırları içinde hizalama
            int offsetX = item.HAlign switch
            {
                HorizontalAlignment.Left => 0,
                HorizontalAlignment.Center => Math.Max(0, (item.Width - textWidth) / 2),
                HorizontalAlignment.Right => Math.Max(0, item.Width - textWidth),
                _ => 0
            };

            int offsetY = item.VAlign switch
            {
                VerticalAlignment.Top => 0,
                VerticalAlignment.Center => Math.Max(0, (item.Height - textHeight) / 2),
                VerticalAlignment.Bottom => Math.Max(0, item.Height - textHeight),
                _ => 0
            };

            // Pikselleri kopyala (öğe sınırları içinde)
            for (int y = 0; y < textHeight; y++)
            {
                int destY = item.Y + offsetY + y;
                if (destY < 0 || destY >= totalHeight) continue;

                for (int x = 0; x < textWidth; x++)
                {
                    int destX = item.X + offsetX + x;
                    if (destX < 0 || destX >= totalWidth) continue;
                    if (destX >= item.X + item.Width) break;

                    var pixel = textBitmap.GetPixel(x, y);
                    if (pixel.Alpha > 128)
                    {
                        colorMatrix[destX, destY] = itemColor;
                    }
                }
            }
        }
        finally
        {
            textBitmap?.Dispose();
        }
    }

    /// <inheritdoc/>
    public bool[,] RenderTextToPixelMatrix(BitmapFont font, string text, DisplaySettings settings)
    {
        var pixelMatrix = new bool[settings.Width, settings.Height];

        if (string.IsNullOrEmpty(text)) return pixelMatrix;

        var ledColor = settings.GetLedColor();
        var skColor = new SKColor(ledColor.R, ledColor.G, ledColor.B, ledColor.A);

        using var textBitmap = _fontLoader.RenderText(font, text, skColor);
        if (textBitmap == null) return pixelMatrix;

        int width = Math.Min(textBitmap.Width, settings.Width);
        int height = Math.Min(textBitmap.Height, settings.Height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = textBitmap.GetPixel(x, y);
                pixelMatrix[x, y] = pixel.Alpha > 128;
            }
        }

        return pixelMatrix;
    }

    private static void CopyPixelsToMatrix(
        SKBitmap source,
        SKColor[,] target,
        SKColor color,
        int startX,
        int startY,
        int width,
        int height,
        int maxWidth,
        int maxHeight)
    {
        for (int y = 0; y < height && (startY + y) < maxHeight; y++)
        {
            if (startY + y < 0) continue;

            for (int x = 0; x < width; x++)
            {
                int destX = startX + x;
                int destY = startY + y;

                if (destX < 0 || destX >= maxWidth) continue;

                var pixel = source.GetPixel(x, y);
                if (pixel.Alpha > 128)
                {
                    target[destX, destY] = color;
                }
            }
        }
    }
}
