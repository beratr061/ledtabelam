using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Hizalama hesaplama servisi interface'i
/// Requirements: 21.1, 21.2, 21.3, 21.4, 21.5, 21.6
/// </summary>
public interface IAlignmentService
{
    /// <summary>
    /// İçeriğin yatay pozisyonunu hesaplar
    /// </summary>
    /// <param name="containerWidth">Konteyner genişliği</param>
    /// <param name="contentWidth">İçerik genişliği</param>
    /// <param name="alignment">Yatay hizalama</param>
    /// <returns>X pozisyonu</returns>
    int CalculateHorizontalPosition(int containerWidth, int contentWidth, HorizontalAlignment alignment);

    /// <summary>
    /// İçeriğin dikey pozisyonunu hesaplar
    /// </summary>
    /// <param name="containerHeight">Konteyner yüksekliği</param>
    /// <param name="contentHeight">İçerik yüksekliği</param>
    /// <param name="alignment">Dikey hizalama</param>
    /// <returns>Y pozisyonu</returns>
    int CalculateVerticalPosition(int containerHeight, int contentHeight, VerticalAlignment alignment);

    /// <summary>
    /// İçeriğin tam pozisyonunu hesaplar (x, y)
    /// </summary>
    /// <param name="containerWidth">Konteyner genişliği</param>
    /// <param name="containerHeight">Konteyner yüksekliği</param>
    /// <param name="contentWidth">İçerik genişliği</param>
    /// <param name="contentHeight">İçerik yüksekliği</param>
    /// <param name="hAlign">Yatay hizalama</param>
    /// <param name="vAlign">Dikey hizalama</param>
    /// <returns>Pozisyon (x, y)</returns>
    (int x, int y) CalculatePosition(
        int containerWidth, int containerHeight,
        int contentWidth, int contentHeight,
        HorizontalAlignment hAlign, VerticalAlignment vAlign);

    /// <summary>
    /// Zone içindeki içeriğin pozisyonunu hesaplar
    /// </summary>
    /// <param name="zone">Zone bilgisi</param>
    /// <param name="displayWidth">Toplam display genişliği</param>
    /// <param name="displayHeight">Toplam display yüksekliği</param>
    /// <param name="contentWidth">İçerik genişliği</param>
    /// <param name="contentHeight">İçerik yüksekliği</param>
    /// <param name="zoneStartX">Zone başlangıç X pozisyonu</param>
    /// <returns>İçerik pozisyonu (x, y)</returns>
    (int x, int y) CalculateZoneContentPosition(
        Zone zone,
        int displayWidth, int displayHeight,
        int contentWidth, int contentHeight,
        int zoneStartX);

    /// <summary>
    /// Bitmap'i hizalamaya göre hedef bitmap'e yerleştirir
    /// </summary>
    /// <param name="source">Kaynak bitmap</param>
    /// <param name="targetWidth">Hedef genişlik</param>
    /// <param name="targetHeight">Hedef yükseklik</param>
    /// <param name="hAlign">Yatay hizalama</param>
    /// <param name="vAlign">Dikey hizalama</param>
    /// <returns>Hizalanmış bitmap</returns>
    SKBitmap AlignBitmap(
        SKBitmap source,
        int targetWidth, int targetHeight,
        HorizontalAlignment hAlign, VerticalAlignment vAlign);
}
