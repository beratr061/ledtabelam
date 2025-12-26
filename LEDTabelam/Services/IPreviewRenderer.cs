using System.Collections.Generic;
using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Önizleme render servisi interface'i
/// Zone ve Program öğelerini renk matrisine dönüştürür
/// </summary>
public interface IPreviewRenderer
{
    /// <summary>
    /// Zone listesini renk matrisine render eder
    /// </summary>
    /// <param name="font">Kullanılacak font</param>
    /// <param name="zones">Zone listesi</param>
    /// <param name="settings">Display ayarları</param>
    /// <returns>Renk matrisi</returns>
    SKColor[,] RenderZonesToColorMatrix(BitmapFont font, IReadOnlyList<Zone> zones, DisplaySettings settings);

    /// <summary>
    /// Program öğelerini renk matrisine render eder
    /// </summary>
    /// <param name="items">Program öğeleri</param>
    /// <param name="defaultFont">Varsayılan font</param>
    /// <param name="fontResolver">Font adından font çözümleyici</param>
    /// <param name="settings">Display ayarları</param>
    /// <returns>Renk matrisi</returns>
    SKColor[,] RenderProgramToColorMatrix(
        IReadOnlyList<TabelaItem> items,
        BitmapFont? defaultFont,
        System.Func<string, BitmapFont?> fontResolver,
        DisplaySettings settings);

    /// <summary>
    /// Tek metin satırını piksel matrisine render eder
    /// </summary>
    /// <param name="font">Kullanılacak font</param>
    /// <param name="text">Metin</param>
    /// <param name="settings">Display ayarları</param>
    /// <returns>Piksel matrisi</returns>
    bool[,] RenderTextToPixelMatrix(BitmapFont font, string text, DisplaySettings settings);
}
