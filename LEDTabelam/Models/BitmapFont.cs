using System;
using System.Collections.Generic;
using SkiaSharp;

namespace LEDTabelam.Models;

/// <summary>
/// Bitmap font karakter bilgisi
/// Requirements: 4.4, 4.5, 4.6
/// </summary>
public class FontChar
{
    /// <summary>
    /// Karakter Unicode ID'si
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Font görüntüsündeki X koordinatı
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Font görüntüsündeki Y koordinatı
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Karakter genişliği (piksel)
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Karakter yüksekliği (piksel)
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// X offset (çizim pozisyonu ayarı)
    /// </summary>
    public int XOffset { get; set; }

    /// <summary>
    /// Y offset (çizim pozisyonu ayarı)
    /// </summary>
    public int YOffset { get; set; }

    /// <summary>
    /// Sonraki karaktere geçiş mesafesi
    /// </summary>
    public int XAdvance { get; set; }
}

/// <summary>
/// Bitmap font tanımı
/// Requirements: 4.4, 4.5, 4.6, 4.7
/// </summary>
public class BitmapFont : IDisposable
{
    private bool _disposed = false;

    /// <summary>
    /// Font adı
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Font dosya yolu
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Satır yüksekliği (piksel)
    /// </summary>
    public int LineHeight { get; set; }

    /// <summary>
    /// Taban çizgisi (baseline)
    /// </summary>
    public int Base { get; set; }

    /// <summary>
    /// Font görüntü dosyası (SKBitmap)
    /// </summary>
    public SKBitmap? FontImage { get; set; }

    /// <summary>
    /// Karakter tanımları (Unicode ID -> FontChar)
    /// </summary>
    public Dictionary<int, FontChar> Characters { get; set; } = new();

    /// <summary>
    /// Kerning bilgileri ((ilk karakter, ikinci karakter) -> mesafe ayarı)
    /// </summary>
    public Dictionary<(int, int), int> Kernings { get; set; } = new();

    /// <summary>
    /// Belirtilen karakterin font'ta tanımlı olup olmadığını kontrol eder
    /// </summary>
    public bool HasCharacter(char c)
    {
        return Characters.ContainsKey(c);
    }

    /// <summary>
    /// Belirtilen karakterin bilgisini döndürür
    /// </summary>
    public FontChar? GetCharacter(char c)
    {
        return Characters.TryGetValue(c, out var fontChar) ? fontChar : null;
    }

    /// <summary>
    /// İki karakter arasındaki kerning değerini döndürür
    /// </summary>
    public int GetKerning(char first, char second)
    {
        return Kernings.TryGetValue((first, second), out var kerning) ? kerning : 0;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                FontImage?.Dispose();
                FontImage = null;
            }
            _disposed = true;
        }
    }

    ~BitmapFont()
    {
        Dispose(false);
    }
}
