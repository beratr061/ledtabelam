using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Dışa aktarma servisi implementasyonu - PNG, GIF, WebP export
/// Requirements: 7.1, 7.2, 7.3, 7.5, 7.6, 7.7
/// </summary>
public class ExportService : IExportService
{
    /// <summary>
    /// Minimum FPS değeri
    /// </summary>
    public const int MinFps = 1;

    /// <summary>
    /// Maksimum FPS değeri
    /// </summary>
    public const int MaxFps = 60;

    /// <summary>
    /// Minimum zoom seviyesi (%)
    /// </summary>
    public const int MinZoom = 50;

    /// <summary>
    /// Maksimum zoom seviyesi (%)
    /// </summary>
    public const int MaxZoom = 400;

    private static readonly IReadOnlyList<ExportFormat> _supportedFormats = new[]
    {
        ExportFormat.Png,
        ExportFormat.Jpeg,
        ExportFormat.Gif,
        ExportFormat.WebP
    };

    /// <inheritdoc/>
    public IReadOnlyList<ExportFormat> SupportedFormats => _supportedFormats;

    /// <inheritdoc/>
    public async Task<bool> ExportPngAsync(SKBitmap bitmap, string filePath, bool useZoom = false, int zoomLevel = 100)
    {
        if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty", nameof(filePath));

        try
        {
            var outputBitmap = bitmap;

            if (useZoom && zoomLevel != 100)
            {
                zoomLevel = Math.Clamp(zoomLevel, MinZoom, MaxZoom);
                outputBitmap = ScaleBitmap(bitmap, zoomLevel / 100.0);
            }

            return await Task.Run(() =>
            {
                try
                {
                    using var image = SKImage.FromBitmap(outputBitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    
                    // Dizin yoksa oluştur
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using var stream = File.OpenWrite(filePath);
                    data.SaveTo(stream);
                    return true;
                }
                finally
                {
                    // Eğer yeni bitmap oluşturulduysa dispose et
                    if (outputBitmap != bitmap)
                    {
                        outputBitmap.Dispose();
                    }
                }
            });
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExportGifAsync(IReadOnlyList<SKBitmap> frames, string filePath, int fps = 30)
    {
        if (frames == null || frames.Count == 0) throw new ArgumentException("Frames cannot be empty", nameof(frames));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty", nameof(filePath));

        fps = Math.Clamp(fps, MinFps, MaxFps);
        var frameDelay = 1000 / fps; // ms per frame

        try
        {
            return await Task.Run(() =>
            {
                // Dizin yoksa oluştur
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // GIF encoding using SkiaSharp's built-in encoder
                // Note: SkiaSharp doesn't have native animated GIF support
                // We'll create a simple GIF with multiple frames using raw GIF format
                using var stream = File.Create(filePath);
                return WriteAnimatedGif(stream, frames, frameDelay);
            });
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExportWebPAsync(IReadOnlyList<SKBitmap> frames, string filePath, int fps = 30)
    {
        if (frames == null || frames.Count == 0) throw new ArgumentException("Frames cannot be empty", nameof(frames));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty", nameof(filePath));

        fps = Math.Clamp(fps, MinFps, MaxFps);

        try
        {
            return await Task.Run(() =>
            {
                // Dizin yoksa oluştur
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // For animated WebP, we need to use WebP animation API
                // SkiaSharp supports WebP encoding but animated WebP requires special handling
                // For now, export as static WebP with first frame
                // Full animated WebP support would require additional libraries
                using var image = SKImage.FromBitmap(frames[0]);
                using var data = image.Encode(SKEncodedImageFormat.Webp, 100);
                using var fileStream = File.Create(filePath);
                data.SaveTo(fileStream);
                return true;
            });
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExportAsync(SKBitmap bitmap, string filePath, ExportFormat format, int quality = 100)
    {
        if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty", nameof(filePath));

        quality = Math.Clamp(quality, 0, 100);

        try
        {
            return await Task.Run(() =>
            {
                var skFormat = format switch
                {
                    ExportFormat.Png => SKEncodedImageFormat.Png,
                    ExportFormat.Jpeg => SKEncodedImageFormat.Jpeg,
                    ExportFormat.WebP => SKEncodedImageFormat.Webp,
                    ExportFormat.Gif => SKEncodedImageFormat.Gif,
                    _ => SKEncodedImageFormat.Png
                };

                // Dizin yoksa oluştur
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(skFormat, quality);
                using var stream = File.OpenWrite(filePath);
                data.SaveTo(stream);
                return true;
            });
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Bitmap'i belirtilen ölçekte yeniden boyutlandırır
    /// </summary>
    private static SKBitmap ScaleBitmap(SKBitmap source, double scale)
    {
        var newWidth = (int)(source.Width * scale);
        var newHeight = (int)(source.Height * scale);

        var scaledBitmap = new SKBitmap(newWidth, newHeight);
        
        using var canvas = new SKCanvas(scaledBitmap);
        using var paint = new SKPaint
        {
            IsAntialias = false
        };

        // Scale the source bitmap to fit the new dimensions
        var destRect = new SKRect(0, 0, newWidth, newHeight);
        canvas.DrawBitmap(source, destRect, paint);
        return scaledBitmap;
    }

    /// <summary>
    /// Animasyonlu GIF dosyası yazar
    /// </summary>
    private static bool WriteAnimatedGif(Stream stream, IReadOnlyList<SKBitmap> frames, int frameDelayMs)
    {
        if (frames.Count == 0) return false;

        var width = frames[0].Width;
        var height = frames[0].Height;

        // GIF Header
        var header = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; // GIF89a
        stream.Write(header, 0, header.Length);

        // Logical Screen Descriptor
        WriteUInt16(stream, (ushort)width);
        WriteUInt16(stream, (ushort)height);
        stream.WriteByte(0xF7); // Global Color Table Flag, Color Resolution, Sort Flag, Size of Global Color Table
        stream.WriteByte(0x00); // Background Color Index
        stream.WriteByte(0x00); // Pixel Aspect Ratio

        // Global Color Table (256 colors)
        WriteGlobalColorTable(stream);

        // Netscape Application Extension (for looping)
        var netscapeExt = new byte[] { 0x21, 0xFF, 0x0B, 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50, 0x45, 0x32, 0x2E, 0x30, 0x03, 0x01, 0x00, 0x00, 0x00 };
        stream.Write(netscapeExt, 0, netscapeExt.Length);

        // Write each frame
        foreach (var frame in frames)
        {
            WriteGifFrame(stream, frame, frameDelayMs / 10); // GIF uses centiseconds
        }

        // GIF Trailer
        stream.WriteByte(0x3B);

        return true;
    }

    private static void WriteGlobalColorTable(Stream stream)
    {
        // Write 256 color palette (grayscale + basic colors)
        for (int i = 0; i < 256; i++)
        {
            if (i < 216)
            {
                // Web-safe colors (6x6x6 cube)
                int r = (i / 36) * 51;
                int g = ((i / 6) % 6) * 51;
                int b = (i % 6) * 51;
                stream.WriteByte((byte)r);
                stream.WriteByte((byte)g);
                stream.WriteByte((byte)b);
            }
            else
            {
                // Grayscale for remaining
                int gray = (i - 216) * 6;
                stream.WriteByte((byte)gray);
                stream.WriteByte((byte)gray);
                stream.WriteByte((byte)gray);
            }
        }
    }

    private static void WriteGifFrame(Stream stream, SKBitmap bitmap, int delayCentiseconds)
    {
        // Graphic Control Extension
        stream.WriteByte(0x21); // Extension Introducer
        stream.WriteByte(0xF9); // Graphic Control Label
        stream.WriteByte(0x04); // Block Size
        stream.WriteByte(0x00); // Packed byte (no transparency, no disposal)
        WriteUInt16(stream, (ushort)delayCentiseconds); // Delay Time
        stream.WriteByte(0x00); // Transparent Color Index
        stream.WriteByte(0x00); // Block Terminator

        // Image Descriptor
        stream.WriteByte(0x2C); // Image Separator
        WriteUInt16(stream, 0); // Left Position
        WriteUInt16(stream, 0); // Top Position
        WriteUInt16(stream, (ushort)bitmap.Width); // Width
        WriteUInt16(stream, (ushort)bitmap.Height); // Height
        stream.WriteByte(0x00); // Packed byte (no local color table)

        // Image Data (LZW compressed)
        WriteLzwImageData(stream, bitmap);
    }

    private static void WriteLzwImageData(Stream stream, SKBitmap bitmap)
    {
        var minCodeSize = 8;
        stream.WriteByte((byte)minCodeSize);

        // Convert bitmap to color indices
        var pixels = new byte[bitmap.Width * bitmap.Height];
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                pixels[y * bitmap.Width + x] = ColorToIndex(color);
            }
        }

        // Simple LZW encoding
        var encoded = LzwEncode(pixels, minCodeSize);
        
        // Write sub-blocks
        int offset = 0;
        while (offset < encoded.Length)
        {
            int blockSize = Math.Min(255, encoded.Length - offset);
            stream.WriteByte((byte)blockSize);
            stream.Write(encoded, offset, blockSize);
            offset += blockSize;
        }

        stream.WriteByte(0x00); // Block Terminator
    }

    private static byte ColorToIndex(SKColor color)
    {
        // Map color to web-safe palette index
        int r = (color.Red + 25) / 51;
        int g = (color.Green + 25) / 51;
        int b = (color.Blue + 25) / 51;
        
        r = Math.Clamp(r, 0, 5);
        g = Math.Clamp(g, 0, 5);
        b = Math.Clamp(b, 0, 5);
        
        return (byte)(r * 36 + g * 6 + b);
    }

    private static byte[] LzwEncode(byte[] data, int minCodeSize)
    {
        var output = new List<byte>();
        var bitBuffer = 0;
        var bitCount = 0;

        var clearCode = 1 << minCodeSize;
        var endCode = clearCode + 1;
        var codeSize = minCodeSize + 1;

        // Initialize dictionary
        var dictionary = new Dictionary<string, int>();
        for (int i = 0; i < clearCode; i++)
        {
            dictionary[((char)i).ToString()] = i;
        }

        var nextCode = endCode + 1;
        var maxCode = (1 << codeSize) - 1;

        // Output clear code
        AddBits(ref bitBuffer, ref bitCount, clearCode, codeSize, output);

        if (data.Length == 0)
        {
            AddBits(ref bitBuffer, ref bitCount, endCode, codeSize, output);
            if (bitCount > 0)
            {
                output.Add((byte)bitBuffer);
            }
            return output.ToArray();
        }

        var current = ((char)data[0]).ToString();

        for (int i = 1; i < data.Length; i++)
        {
            var next = current + (char)data[i];

            if (dictionary.ContainsKey(next))
            {
                current = next;
            }
            else
            {
                AddBits(ref bitBuffer, ref bitCount, dictionary[current], codeSize, output);

                if (nextCode <= 4095)
                {
                    dictionary[next] = nextCode++;

                    if (nextCode > maxCode && codeSize < 12)
                    {
                        codeSize++;
                        maxCode = (1 << codeSize) - 1;
                    }
                }

                current = ((char)data[i]).ToString();
            }
        }

        AddBits(ref bitBuffer, ref bitCount, dictionary[current], codeSize, output);
        AddBits(ref bitBuffer, ref bitCount, endCode, codeSize, output);

        if (bitCount > 0)
        {
            output.Add((byte)bitBuffer);
        }

        return output.ToArray();
    }

    private static void AddBits(ref int bitBuffer, ref int bitCount, int code, int codeSize, List<byte> output)
    {
        bitBuffer |= code << bitCount;
        bitCount += codeSize;

        while (bitCount >= 8)
        {
            output.Add((byte)(bitBuffer & 0xFF));
            bitBuffer >>= 8;
            bitCount -= 8;
        }
    }

    private static void WriteUInt16(Stream stream, ushort value)
    {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
    }
}
