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

    /// <summary>
    /// Maksimum bellekte tutulacak frame sayısı (streaming için)
    /// </summary>
    private const int MaxFramesInMemory = 50;

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
    public async Task<bool> ExportGifStreamingAsync(FrameGenerator frameGenerator, int totalFrames, string filePath, int fps = 30, Action<int>? progress = null)
    {
        if (frameGenerator == null) throw new ArgumentNullException(nameof(frameGenerator));
        if (totalFrames <= 0) throw new ArgumentException("Total frames must be positive", nameof(totalFrames));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty", nameof(filePath));

        fps = Math.Clamp(fps, MinFps, MaxFps);
        var frameDelayCs = 100 / fps; // centiseconds per frame

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

                // İlk birkaç frame'den palet oluştur
                var sampleFrames = new List<SKBitmap>();
                int sampleCount = Math.Min(10, totalFrames);
                int sampleStep = Math.Max(1, totalFrames / sampleCount);

                for (int i = 0; i < totalFrames && sampleFrames.Count < sampleCount; i += sampleStep)
                {
                    var frame = frameGenerator(i);
                    if (frame != null)
                    {
                        sampleFrames.Add(frame);
                    }
                }

                if (sampleFrames.Count == 0) return false;

                var palette = BuildOptimalPalette(sampleFrames);
                var width = sampleFrames[0].Width;
                var height = sampleFrames[0].Height;

                // Sample frame'leri temizle
                foreach (var frame in sampleFrames)
                {
                    frame.Dispose();
                }
                sampleFrames.Clear();

                using var stream = File.Create(filePath);

                // GIF Header
                var header = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; // GIF89a
                stream.Write(header, 0, header.Length);

                // Logical Screen Descriptor
                WriteUInt16(stream, (ushort)width);
                WriteUInt16(stream, (ushort)height);
                stream.WriteByte(0xF7);
                stream.WriteByte(0x00);
                stream.WriteByte(0x00);

                // Global Color Table
                WriteGlobalColorTable(stream, palette);

                // Netscape Extension (looping)
                var netscapeExt = new byte[] { 0x21, 0xFF, 0x0B, 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50, 0x45, 0x32, 0x2E, 0x30, 0x03, 0x01, 0x00, 0x00, 0x00 };
                stream.Write(netscapeExt, 0, netscapeExt.Length);

                // Streaming: Her frame'i üret, yaz, dispose et
                for (int i = 0; i < totalFrames; i++)
                {
                    var frame = frameGenerator(i);
                    if (frame == null) break;

                    try
                    {
                        WriteGifFrame(stream, frame, frameDelayCs, palette);
                    }
                    finally
                    {
                        frame.Dispose(); // Belleği hemen serbest bırak
                    }

                    // İlerleme bildirimi
                    progress?.Invoke((i + 1) * 100 / totalFrames);
                }

                // GIF Trailer
                stream.WriteByte(0x3B);

                return true;
            });
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// NOT: SkiaSharp'ın standart Encode metodu animasyonlu WebP desteklemez.
    /// Animasyonlu WebP için libwebp wrapper veya harici kütüphane gerekir.
    /// Şu an için: Tek frame ise statik WebP, çoklu frame ise GIF'e fallback yapar.
    /// </remarks>
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

                // Tek frame ise statik WebP olarak kaydet
                if (frames.Count == 1)
                {
                    using var image = SKImage.FromBitmap(frames[0]);
                    using var data = image.Encode(SKEncodedImageFormat.Webp, 100);
                    using var fileStream = File.Create(filePath);
                    data.SaveTo(fileStream);
                    return true;
                }

                // Çoklu frame için: Animasyonlu WebP desteklenmediğinden GIF olarak kaydet
                // Dosya uzantısını .gif olarak değiştir ve kullanıcıyı bilgilendir
                var gifPath = Path.ChangeExtension(filePath, ".gif");
                var frameDelay = 1000 / fps;

                using var stream = File.Create(gifPath);
                var result = WriteAnimatedGif(stream, frames, frameDelay);

                // Orijinal WebP dosyası yerine GIF oluşturulduğunu belirt
                // (Gerçek uygulamada bu bilgi UI'a iletilmeli)
                System.Diagnostics.Debug.WriteLine(
                    $"Animasyonlu WebP desteklenmediğinden GIF olarak kaydedildi: {gifPath}");

                return result;
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
    /// Animasyonlu GIF dosyası yazar (Streaming yaklaşımı ile bellek optimizasyonu)
    /// </summary>
    private bool WriteAnimatedGif(Stream stream, IReadOnlyList<SKBitmap> frames, int frameDelayMs)
    {
        if (frames.Count == 0) return false;

        var width = frames[0].Width;
        var height = frames[0].Height;

        // Optimal renk paleti oluştur (Median Cut algoritması)
        var palette = BuildOptimalPalette(frames);

        // GIF Header
        var header = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; // GIF89a
        stream.Write(header, 0, header.Length);

        // Logical Screen Descriptor
        WriteUInt16(stream, (ushort)width);
        WriteUInt16(stream, (ushort)height);
        stream.WriteByte(0xF7); // Global Color Table Flag, Color Resolution, Sort Flag, Size of Global Color Table
        stream.WriteByte(0x00); // Background Color Index
        stream.WriteByte(0x00); // Pixel Aspect Ratio

        // Global Color Table (256 colors - optimal palette)
        WriteGlobalColorTable(stream, palette);

        // Netscape Application Extension (for looping)
        var netscapeExt = new byte[] { 0x21, 0xFF, 0x0B, 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50, 0x45, 0x32, 0x2E, 0x30, 0x03, 0x01, 0x00, 0x00, 0x00 };
        stream.Write(netscapeExt, 0, netscapeExt.Length);

        // Write each frame
        foreach (var frame in frames)
        {
            WriteGifFrame(stream, frame, frameDelayMs / 10, palette); // GIF uses centiseconds
        }

        // GIF Trailer
        stream.WriteByte(0x3B);

        return true;
    }

    private static void WriteGlobalColorTable(Stream stream, SKColor[] palette)
    {
        // Optimal palet ile 256 renk yaz
        for (int i = 0; i < 256; i++)
        {
            if (i < palette.Length)
            {
                stream.WriteByte(palette[i].Red);
                stream.WriteByte(palette[i].Green);
                stream.WriteByte(palette[i].Blue);
            }
            else
            {
                // Eksik renkler için siyah
                stream.WriteByte(0);
                stream.WriteByte(0);
                stream.WriteByte(0);
            }
        }
    }

    private void WriteGifFrame(Stream stream, SKBitmap bitmap, int delayCentiseconds, SKColor[] palette)
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
        WriteLzwImageData(stream, bitmap, palette);
    }

    private void WriteLzwImageData(Stream stream, SKBitmap bitmap, SKColor[] palette)
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
                pixels[y * bitmap.Width + x] = ColorToIndex(color, palette);
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

    /// <summary>
    /// Rengi palet indeksine dönüştürür (Median Cut algoritması ile oluşturulan palete göre)
    /// </summary>
    private byte ColorToIndex(SKColor color, SKColor[] palette)
    {
        int bestIndex = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < palette.Length; i++)
        {
            // Euclidean distance in RGB space
            int dr = color.Red - palette[i].Red;
            int dg = color.Green - palette[i].Green;
            int db = color.Blue - palette[i].Blue;
            int distance = dr * dr + dg * dg + db * db;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }

            // Tam eşleşme bulundu
            if (distance == 0) break;
        }

        return (byte)bestIndex;
    }

    /// <summary>
    /// Median Cut algoritması ile optimal 256 renk paleti oluşturur
    /// Glow ve blur efektlerindeki yumuşak geçişleri korur
    /// </summary>
    private SKColor[] BuildOptimalPalette(IReadOnlyList<SKBitmap> frames)
    {
        // Tüm frame'lerden renkleri topla (sampling ile)
        var colorCounts = new Dictionary<int, int>();
        int sampleStep = Math.Max(1, frames.Count / 10); // En fazla 10 frame'den örnek al

        for (int f = 0; f < frames.Count; f += sampleStep)
        {
            var frame = frames[f];
            int pixelStep = Math.Max(1, (frame.Width * frame.Height) / 10000); // Max 10K piksel

            for (int i = 0; i < frame.Width * frame.Height; i += pixelStep)
            {
                int x = i % frame.Width;
                int y = i / frame.Width;
                var color = frame.GetPixel(x, y);
                
                // Alpha'yı yoksay, sadece RGB
                int key = (color.Red << 16) | (color.Green << 8) | color.Blue;
                colorCounts.TryGetValue(key, out int count);
                colorCounts[key] = count + 1;
            }
        }

        // Renkleri listeye dönüştür
        var colors = new List<(int r, int g, int b, int count)>();
        foreach (var kvp in colorCounts)
        {
            int r = (kvp.Key >> 16) & 0xFF;
            int g = (kvp.Key >> 8) & 0xFF;
            int b = kvp.Key & 0xFF;
            colors.Add((r, g, b, kvp.Value));
        }

        // Median Cut algoritması
        var palette = MedianCut(colors, 256);

        // 256 renge tamamla
        while (palette.Count < 256)
        {
            palette.Add(SKColors.Black);
        }

        return palette.ToArray();
    }

    /// <summary>
    /// Median Cut algoritması implementasyonu
    /// </summary>
    private List<SKColor> MedianCut(List<(int r, int g, int b, int count)> colors, int targetCount)
    {
        if (colors.Count == 0)
        {
            return new List<SKColor> { SKColors.Black };
        }

        var boxes = new List<ColorBox> { new ColorBox(colors) };

        // Kutuları böl
        while (boxes.Count < targetCount && boxes.Count < colors.Count)
        {
            // En büyük kutuyu bul (renk aralığına göre)
            int maxIndex = 0;
            int maxRange = 0;

            for (int i = 0; i < boxes.Count; i++)
            {
                int range = boxes[i].GetLargestRange();
                if (range > maxRange && boxes[i].Colors.Count > 1)
                {
                    maxRange = range;
                    maxIndex = i;
                }
            }

            if (maxRange == 0) break;

            var boxToSplit = boxes[maxIndex];
            boxes.RemoveAt(maxIndex);

            var (box1, box2) = boxToSplit.Split();
            if (box1.Colors.Count > 0) boxes.Add(box1);
            if (box2.Colors.Count > 0) boxes.Add(box2);
        }

        // Her kutudan ortalama renk al
        var result = new List<SKColor>();
        foreach (var box in boxes)
        {
            result.Add(box.GetAverageColor());
        }

        return result;
    }

    /// <summary>
    /// Median Cut için renk kutusu
    /// </summary>
    private class ColorBox
    {
        public List<(int r, int g, int b, int count)> Colors { get; }

        public ColorBox(List<(int r, int g, int b, int count)> colors)
        {
            Colors = new List<(int r, int g, int b, int count)>(colors);
        }

        public int GetLargestRange()
        {
            if (Colors.Count == 0) return 0;

            int minR = 255, maxR = 0;
            int minG = 255, maxG = 0;
            int minB = 255, maxB = 0;

            foreach (var c in Colors)
            {
                minR = Math.Min(minR, c.r); maxR = Math.Max(maxR, c.r);
                minG = Math.Min(minG, c.g); maxG = Math.Max(maxG, c.g);
                minB = Math.Min(minB, c.b); maxB = Math.Max(maxB, c.b);
            }

            return Math.Max(maxR - minR, Math.Max(maxG - minG, maxB - minB));
        }

        public (ColorBox, ColorBox) Split()
        {
            if (Colors.Count <= 1)
            {
                return (new ColorBox(Colors), new ColorBox(new List<(int, int, int, int)>()));
            }

            // En geniş kanalı bul
            int minR = 255, maxR = 0;
            int minG = 255, maxG = 0;
            int minB = 255, maxB = 0;

            foreach (var c in Colors)
            {
                minR = Math.Min(minR, c.r); maxR = Math.Max(maxR, c.r);
                minG = Math.Min(minG, c.g); maxG = Math.Max(maxG, c.g);
                minB = Math.Min(minB, c.b); maxB = Math.Max(maxB, c.b);
            }

            int rangeR = maxR - minR;
            int rangeG = maxG - minG;
            int rangeB = maxB - minB;

            // En geniş kanala göre sırala
            if (rangeR >= rangeG && rangeR >= rangeB)
                Colors.Sort((a, b) => a.r.CompareTo(b.r));
            else if (rangeG >= rangeR && rangeG >= rangeB)
                Colors.Sort((a, b) => a.g.CompareTo(b.g));
            else
                Colors.Sort((a, b) => a.b.CompareTo(b.b));

            int mid = Colors.Count / 2;
            return (
                new ColorBox(Colors.GetRange(0, mid)),
                new ColorBox(Colors.GetRange(mid, Colors.Count - mid))
            );
        }

        public SKColor GetAverageColor()
        {
            if (Colors.Count == 0) return SKColors.Black;

            long totalR = 0, totalG = 0, totalB = 0, totalCount = 0;

            foreach (var c in Colors)
            {
                totalR += c.r * c.count;
                totalG += c.g * c.count;
                totalB += c.b * c.count;
                totalCount += c.count;
            }

            if (totalCount == 0) return SKColors.Black;

            return new SKColor(
                (byte)(totalR / totalCount),
                (byte)(totalG / totalCount),
                (byte)(totalB / totalCount)
            );
        }
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
