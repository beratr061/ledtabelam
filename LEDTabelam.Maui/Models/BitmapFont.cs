using System;
using System.Collections.Generic;
using SkiaSharp;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Bitmap font karakter bilgisi
/// </summary>
public class FontChar
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int XAdvance { get; set; }
}

/// <summary>
/// Bitmap font tanımı
/// </summary>
public class BitmapFont : IDisposable
{
    private bool _disposed = false;

    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineHeight { get; set; }
    public int Base { get; set; }
    public SKBitmap? FontImage { get; set; }
    public Dictionary<int, FontChar> Characters { get; set; } = new();
    public Dictionary<(int, int), int> Kernings { get; set; } = new();

    public bool HasCharacter(char c)
    {
        return Characters.ContainsKey(c);
    }

    public FontChar? GetCharacter(char c)
    {
        return Characters.TryGetValue(c, out var fontChar) ? fontChar : null;
    }

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
