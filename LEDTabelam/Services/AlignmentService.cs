using System;
using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Hizalama hesaplama servisi implementasyonu
/// Requirements: 21.1, 21.2, 21.3, 21.4, 21.5, 21.6
/// </summary>
public class AlignmentService : IAlignmentService
{
    /// <inheritdoc/>
    public int CalculateHorizontalPosition(int containerWidth, int contentWidth, HorizontalAlignment alignment)
    {
        return alignment switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Center => (containerWidth - contentWidth) / 2,
            HorizontalAlignment.Right => containerWidth - contentWidth,
            _ => 0
        };
    }

    /// <inheritdoc/>
    public int CalculateVerticalPosition(int containerHeight, int contentHeight, VerticalAlignment alignment)
    {
        return alignment switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Center => (containerHeight - contentHeight) / 2,
            VerticalAlignment.Bottom => containerHeight - contentHeight,
            _ => 0
        };
    }

    /// <inheritdoc/>
    public (int x, int y) CalculatePosition(
        int containerWidth, int containerHeight,
        int contentWidth, int contentHeight,
        HorizontalAlignment hAlign, VerticalAlignment vAlign)
    {
        int x = CalculateHorizontalPosition(containerWidth, contentWidth, hAlign);
        int y = CalculateVerticalPosition(containerHeight, contentHeight, vAlign);
        return (x, y);
    }

    /// <inheritdoc/>
    public (int x, int y) CalculateZoneContentPosition(
        Zone zone,
        int displayWidth, int displayHeight,
        int contentWidth, int contentHeight,
        int zoneStartX)
    {
        if (zone == null)
            throw new ArgumentNullException(nameof(zone));

        // Zone genişliğini piksel olarak hesapla
        int zoneWidth = (int)(displayWidth * zone.WidthPercent / 100.0);

        // Zone içindeki pozisyonu hesapla
        int relativeX = CalculateHorizontalPosition(zoneWidth, contentWidth, zone.HAlign);
        int y = CalculateVerticalPosition(displayHeight, contentHeight, zone.VAlign);

        // Mutlak X pozisyonunu hesapla (zone başlangıcı + relatif pozisyon)
        int absoluteX = zoneStartX + relativeX;

        return (absoluteX, y);
    }

    /// <inheritdoc/>
    public SKBitmap AlignBitmap(
        SKBitmap source,
        int targetWidth, int targetHeight,
        HorizontalAlignment hAlign, VerticalAlignment vAlign)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (targetWidth <= 0 || targetHeight <= 0)
            throw new ArgumentException("Hedef boyutlar pozitif olmalıdır.");

        // Hedef bitmap oluştur
        var result = new SKBitmap(targetWidth, targetHeight);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Pozisyonu hesapla
        var (x, y) = CalculatePosition(
            targetWidth, targetHeight,
            source.Width, source.Height,
            hAlign, vAlign);

        // Kaynak bitmap'i hedef pozisyona çiz
        canvas.DrawBitmap(source, x, y);

        return result;
    }
}
