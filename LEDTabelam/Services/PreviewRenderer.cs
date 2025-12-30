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
    private IAssetLibrary? _assetLibrary;

    public PreviewRenderer(IFontLoader fontLoader, IMultiLineTextRenderer multiLineTextRenderer)
    {
        _fontLoader = fontLoader ?? throw new ArgumentNullException(nameof(fontLoader));
        _multiLineTextRenderer = multiLineTextRenderer ?? throw new ArgumentNullException(nameof(multiLineTextRenderer));
    }

    /// <summary>
    /// AssetLibrary'yi ayarlar (sembol render için gerekli)
    /// </summary>
    public void SetAssetLibrary(IAssetLibrary assetLibrary)
    {
        _assetLibrary = assetLibrary;
    }

    /// <inheritdoc/>
    public SKColor[,] RenderZonesToColorMatrix(BitmapFont font, IReadOnlyList<Zone> zones, DisplaySettings settings)
    {
        int totalWidth = settings.Width;
        int totalHeight = settings.Height;
        var colorMatrix = new SKColor[totalWidth, totalHeight];

        // Tek renk modunda mı kontrol et
        bool isRgbMode = settings.ColorType == LedColorType.OneROneGOneB ||
                         settings.ColorType == LedColorType.FullRGB;
        
        // Tek renk modunda kullanılacak varsayılan renk
        var defaultLedColor = settings.GetLedColor();
        var defaultSkColor = new SKColor(defaultLedColor.R, defaultLedColor.G, defaultLedColor.B);

        int currentX = 0;
        foreach (var zone in zones)
        {
            int zoneWidth = (int)(totalWidth * zone.WidthPercent / 100.0);
            if (zoneWidth <= 0) continue;

            // RGB modunda zone rengini, tek renk modunda varsayılan rengi kullan
            var zoneColor = isRgbMode 
                ? new SKColor(zone.TextColor.R, zone.TextColor.G, zone.TextColor.B)
                : defaultSkColor;
            var content = zone.Content;

            // Çerçeve varsa içerik alanını hesapla
            int contentX = currentX;
            int contentWidth = zoneWidth;
            int contentY = 0;
            int contentHeight = totalHeight;
            
            if (zone.Border != null && zone.Border.IsEnabled)
            {
                // Çerçeve için alan ayır
                int borderLeft = zone.Border.VerticalLines + zone.Border.Padding;
                int borderRight = zone.Border.VerticalLines + zone.Border.Padding;
                int borderTop = zone.Border.HorizontalLines + zone.Border.Padding;
                int borderBottom = zone.Border.HorizontalLines + zone.Border.Padding;
                
                contentX = currentX + borderLeft;
                contentWidth = zoneWidth - borderLeft - borderRight;
                contentY = borderTop;
                contentHeight = totalHeight - borderTop - borderBottom;
                
                // Çerçeveyi çiz
                var borderColor = isRgbMode
                    ? new SKColor(zone.Border.Color.R, zone.Border.Color.G, zone.Border.Color.B)
                    : defaultSkColor;
                DrawZoneBorder(colorMatrix, zone.Border, borderColor, currentX, 0, zoneWidth, totalHeight, totalWidth, totalHeight);
            }

            if (!string.IsNullOrEmpty(content) && contentWidth > 0 && contentHeight > 0)
            {
                RenderZoneContentWithBorder(font, content, zoneColor, zone, contentX, contentY, contentWidth, contentHeight, totalWidth, totalHeight, colorMatrix);
            }

            currentX += zoneWidth;
        }

        return colorMatrix;
    }

    /// <summary>
    /// Zone çerçevesini renk matrisine çizer
    /// </summary>
    private void DrawZoneBorder(
        SKColor[,] colorMatrix,
        BorderSettings border,
        SKColor borderColor,
        int x, int y, int width, int height,
        int maxWidth, int maxHeight)
    {
        if (!border.IsEnabled || width <= 0 || height <= 0) return;

        // Üst kenar çizgileri
        for (int line = 0; line < border.HorizontalLines; line++)
        {
            int lineY = y + line;
            if (lineY >= 0 && lineY < maxHeight)
            {
                for (int px = x; px < x + width && px < maxWidth; px++)
                {
                    if (px >= 0) colorMatrix[px, lineY] = borderColor;
                }
            }
        }

        // Alt kenar çizgileri
        for (int line = 0; line < border.HorizontalLines; line++)
        {
            int lineY = y + height - 1 - line;
            if (lineY >= 0 && lineY < maxHeight)
            {
                for (int px = x; px < x + width && px < maxWidth; px++)
                {
                    if (px >= 0) colorMatrix[px, lineY] = borderColor;
                }
            }
        }

        // Sol kenar çizgileri
        for (int line = 0; line < border.VerticalLines; line++)
        {
            int lineX = x + line;
            if (lineX >= 0 && lineX < maxWidth)
            {
                for (int py = y; py < y + height && py < maxHeight; py++)
                {
                    if (py >= 0) colorMatrix[lineX, py] = borderColor;
                }
            }
        }

        // Sağ kenar çizgileri
        for (int line = 0; line < border.VerticalLines; line++)
        {
            int lineX = x + width - 1 - line;
            if (lineX >= 0 && lineX < maxWidth)
            {
                for (int py = y; py < y + height && py < maxHeight; py++)
                {
                    if (py >= 0) colorMatrix[lineX, py] = borderColor;
                }
            }
        }
    }

    /// <summary>
    /// Zone içeriğini çerçeve sınırları içinde render eder
    /// </summary>
    private void RenderZoneContentWithBorder(
        BitmapFont font,
        string content,
        SKColor zoneColor,
        Zone zone,
        int contentX,
        int contentY,
        int contentWidth,
        int contentHeight,
        int totalWidth,
        int totalHeight,
        SKColor[,] colorMatrix)
    {
        int lineCount = _multiLineTextRenderer.GetLineCount(content);
        int letterSpacing = zone.LetterSpacing;
        int lineSpacing = zone.LineSpacing;
        SKBitmap? textBitmap = null;

        try
        {
            textBitmap = lineCount > 1
                ? _multiLineTextRenderer.RenderMultiLineText(font, content, zoneColor, lineSpacing, letterSpacing)
                : _fontLoader.RenderText(font, content, zoneColor, letterSpacing);

            if (textBitmap == null) return;

            int textWidth = Math.Min(textBitmap.Width, contentWidth);
            int textHeight = textBitmap.Height;

            // Yatay hizalama (içerik alanı içinde)
            int offsetX = zone.HAlign switch
            {
                HorizontalAlignment.Left => 0,
                HorizontalAlignment.Center => Math.Max(0, (contentWidth - textWidth) / 2),
                HorizontalAlignment.Right => Math.Max(0, contentWidth - textWidth),
                _ => 0
            };

            // Dikey hizalama (içerik alanı içinde)
            int offsetY = textHeight >= contentHeight ? 0 : zone.VAlign switch
            {
                VerticalAlignment.Top => 0,
                VerticalAlignment.Center => (contentHeight - textHeight) / 2,
                VerticalAlignment.Bottom => contentHeight - textHeight,
                _ => 0
            };

            // Pikselleri kopyala (içerik alanı sınırları içinde)
            for (int y = 0; y < textHeight && (contentY + offsetY + y) < totalHeight; y++)
            {
                int destY = contentY + offsetY + y;
                if (destY < 0 || destY < contentY || destY >= contentY + contentHeight) continue;

                for (int x = 0; x < textWidth; x++)
                {
                    int destX = contentX + offsetX + x;
                    if (destX < 0 || destX >= totalWidth) continue;
                    if (destX < contentX || destX >= contentX + contentWidth) continue;

                    var pixel = textBitmap.GetPixel(x, y);
                    if (pixel.Alpha > 128)
                    {
                        colorMatrix[destX, destY] = zoneColor;
                    }
                }
            }
        }
        finally
        {
            textBitmap?.Dispose();
        }
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
        int letterSpacing = zone.LetterSpacing;
        int lineSpacing = zone.LineSpacing;
        SKBitmap? textBitmap = null;

        try
        {
            textBitmap = lineCount > 1
                ? _multiLineTextRenderer.RenderMultiLineText(font, content, zoneColor, lineSpacing, letterSpacing)
                : _fontLoader.RenderText(font, content, zoneColor, letterSpacing);

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
            if (!item.IsVisible)
                continue;

            // Sembol öğesi için farklı render
            if (item.ItemType == TabelaItemType.Symbol)
            {
                RenderSymbolItem(item, totalWidth, totalHeight, colorMatrix);
                continue;
            }

            // Metin öğesi için normal render
            if (string.IsNullOrEmpty(item.Content))
                continue;

            var itemFont = fontResolver(item.FontName) ?? defaultFont;
            if (itemFont == null) continue;

            var itemColor = new SKColor(item.Color.R, item.Color.G, item.Color.B);
            // Her öğenin kendi harf aralığını kullan
            RenderProgramItem(itemFont, item, itemColor, totalWidth, totalHeight, colorMatrix, item.LetterSpacing);
        }

        return colorMatrix;
    }

    private void RenderSymbolItem(
        TabelaItem item,
        int totalWidth,
        int totalHeight,
        SKColor[,] colorMatrix)
    {
        if (_assetLibrary == null || string.IsNullOrEmpty(item.SymbolName))
            return;

        var itemColor = new SKColor(item.Color.R, item.Color.G, item.Color.B);
        
        // Çerçeve varsa içerik alanını hesapla
        int contentX = item.X;
        int contentY = item.Y;
        int contentWidth = item.Width;
        int contentHeight = item.Height;
        
        if (item.Border != null && item.Border.IsEnabled)
        {
            int borderLeft = item.Border.VerticalLines + item.Border.Padding;
            int borderRight = item.Border.VerticalLines + item.Border.Padding;
            int borderTop = item.Border.HorizontalLines + item.Border.Padding;
            int borderBottom = item.Border.HorizontalLines + item.Border.Padding;
            
            contentX = item.X + borderLeft;
            contentY = item.Y + borderTop;
            contentWidth = item.Width - borderLeft - borderRight;
            contentHeight = item.Height - borderTop - borderBottom;
            
            // Çerçeveyi çiz
            var borderColor = new SKColor(item.Border.Color.R, item.Border.Color.G, item.Border.Color.B);
            DrawZoneBorder(colorMatrix, item.Border, borderColor, item.X, item.Y, item.Width, item.Height, totalWidth, totalHeight);
        }
        
        // İçerik alanı çok küçükse render etme
        if (contentWidth <= 0 || contentHeight <= 0) return;
        
        using var symbolBitmap = _assetLibrary.RenderAsset(item.SymbolName, item.SymbolSize, itemColor);
        if (symbolBitmap == null) return;

        int symbolWidth = symbolBitmap.Width;
        int symbolHeight = symbolBitmap.Height;

        // Sembol pozisyonunu hesapla (hizalama ile) - içerik alanına göre
        int offsetX = item.HAlign switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Center => Math.Max(0, (contentWidth - symbolWidth) / 2),
            HorizontalAlignment.Right => Math.Max(0, contentWidth - symbolWidth),
            _ => 0
        };

        int offsetY = item.VAlign switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Center => Math.Max(0, (contentHeight - symbolHeight) / 2),
            VerticalAlignment.Bottom => Math.Max(0, contentHeight - symbolHeight),
            _ => 0
        };

        // Pikselleri kopyala (içerik alanı sınırları içinde)
        for (int y = 0; y < symbolHeight; y++)
        {
            int destY = contentY + offsetY + y;
            if (destY < 0 || destY >= totalHeight) continue;
            if (destY < contentY || destY >= contentY + contentHeight) continue;

            for (int x = 0; x < symbolWidth; x++)
            {
                int destX = contentX + offsetX + x;
                if (destX < 0 || destX >= totalWidth) continue;
                if (destX < contentX || destX >= contentX + contentWidth) continue;

                var pixel = symbolBitmap.GetPixel(x, y);
                if (pixel.Alpha > 128)
                {
                    // Orijinal piksel rengini kullan (çok renkli ikonlar için)
                    colorMatrix[destX, destY] = pixel;
                }
            }
        }
    }

    private void RenderProgramItem(
        BitmapFont font,
        TabelaItem item,
        SKColor itemColor,
        int totalWidth,
        int totalHeight,
        SKColor[,] colorMatrix,
        int letterSpacing)
    {
        SKBitmap? textBitmap = null;
        int lineSpacing = 2;   // Varsayılan

        // Çerçeve varsa içerik alanını hesapla
        int contentX = item.X;
        int contentY = item.Y;
        int contentWidth = item.Width;
        int contentHeight = item.Height;
        
        if (item.Border != null && item.Border.IsEnabled)
        {
            int borderLeft = item.Border.VerticalLines + item.Border.Padding;
            int borderRight = item.Border.VerticalLines + item.Border.Padding;
            int borderTop = item.Border.HorizontalLines + item.Border.Padding;
            int borderBottom = item.Border.HorizontalLines + item.Border.Padding;
            
            contentX = item.X + borderLeft;
            contentY = item.Y + borderTop;
            contentWidth = item.Width - borderLeft - borderRight;
            contentHeight = item.Height - borderTop - borderBottom;
            
            // Çerçeveyi çiz
            var borderColor = new SKColor(item.Border.Color.R, item.Border.Color.G, item.Border.Color.B);
            DrawZoneBorder(colorMatrix, item.Border, borderColor, item.X, item.Y, item.Width, item.Height, totalWidth, totalHeight);
        }
        
        // İçerik alanı çok küçükse render etme
        if (contentWidth <= 0 || contentHeight <= 0) return;

        try
        {
            int lineCount = _multiLineTextRenderer.GetLineCount(item.Content);
            textBitmap = lineCount > 1
                ? _multiLineTextRenderer.RenderMultiLineText(font, item.Content, itemColor, lineSpacing, letterSpacing)
                : _fontLoader.RenderText(font, item.Content, itemColor, letterSpacing);

            if (textBitmap == null) return;

            int textWidth = textBitmap.Width;
            int textHeight = textBitmap.Height;

            // Kayan yazı için offset hesapla
            int scrollOffsetX = 0;
            int scrollOffsetY = 0;
            
            if (item.IsScrolling)
            {
                switch (item.ScrollDirection)
                {
                    case ScrollDirection.Left:
                    case ScrollDirection.Right:
                        scrollOffsetX = (int)item.ScrollOffset;
                        break;
                    case ScrollDirection.Up:
                    case ScrollDirection.Down:
                        scrollOffsetY = (int)item.ScrollOffset;
                        break;
                }
            }

            // Öğe sınırları içinde hizalama (kayan yazı değilse) - içerik alanına göre
            int offsetX = item.IsScrolling && (item.ScrollDirection == ScrollDirection.Left || item.ScrollDirection == ScrollDirection.Right)
                ? scrollOffsetX
                : item.HAlign switch
                {
                    HorizontalAlignment.Left => 0,
                    HorizontalAlignment.Center => Math.Max(0, (contentWidth - textWidth) / 2),
                    HorizontalAlignment.Right => Math.Max(0, contentWidth - textWidth),
                    _ => 0
                };

            int offsetY = item.IsScrolling && (item.ScrollDirection == ScrollDirection.Up || item.ScrollDirection == ScrollDirection.Down)
                ? scrollOffsetY
                : item.VAlign switch
                {
                    VerticalAlignment.Top => 0,
                    VerticalAlignment.Center => Math.Max(0, (contentHeight - textHeight) / 2),
                    VerticalAlignment.Bottom => Math.Max(0, contentHeight - textHeight),
                    _ => 0
                };

            // Dikey hizalama (yatay kayan yazıda)
            if (item.IsScrolling && (item.ScrollDirection == ScrollDirection.Left || item.ScrollDirection == ScrollDirection.Right))
            {
                offsetY = item.VAlign switch
                {
                    VerticalAlignment.Top => 0,
                    VerticalAlignment.Center => Math.Max(0, (contentHeight - textHeight) / 2),
                    VerticalAlignment.Bottom => Math.Max(0, contentHeight - textHeight),
                    _ => 0
                };
            }
            
            // Yatay hizalama (dikey kayan yazıda)
            if (item.IsScrolling && (item.ScrollDirection == ScrollDirection.Up || item.ScrollDirection == ScrollDirection.Down))
            {
                offsetX = item.HAlign switch
                {
                    HorizontalAlignment.Left => 0,
                    HorizontalAlignment.Center => Math.Max(0, (contentWidth - textWidth) / 2),
                    HorizontalAlignment.Right => Math.Max(0, contentWidth - textWidth),
                    _ => 0
                };
            }

            // Pikselleri kopyala (içerik alanı sınırları içinde - clipping)
            for (int y = 0; y < textHeight; y++)
            {
                int destY = contentY + offsetY + y;
                if (destY < 0 || destY >= totalHeight) continue;
                if (destY < contentY || destY >= contentY + contentHeight) continue; // İçerik sınırı

                for (int x = 0; x < textWidth; x++)
                {
                    int destX = contentX + offsetX + x;
                    if (destX < 0 || destX >= totalWidth) continue;
                    if (destX < contentX || destX >= contentX + contentWidth) continue; // İçerik sınırı

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

        using var textBitmap = _fontLoader.RenderText(font, text, skColor, 1); // Varsayılan letterSpacing
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
