using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using LEDTabelam.Maui.Models;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Bitmap font yükleme ve metin render servisi
/// </summary>
public class FontLoader : IFontLoader
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const char PlaceholderChar = '□';
    private const int PlaceholderCharId = 0x25A1;

    private readonly Dictionary<string, BitmapFont> _loadedFonts = new();
    private bool _defaultFontsLoaded = false;

    /// <inheritdoc/>
    public BitmapFont? GetFont(string fontName)
    {
        if (string.IsNullOrEmpty(fontName))
            return null;

        // Varsayılan fontları yükle (henüz yüklenmemişse)
        if (!_defaultFontsLoaded)
        {
            _ = LoadDefaultFontsAsync();
        }

        // "Default" isteniyorsa ilk yüklü fontu döndür
        if (fontName == "Default" && _loadedFonts.Count > 0)
        {
            return _loadedFonts.Values.First();
        }

        return _loadedFonts.TryGetValue(fontName, out var font) ? font : null;
    }

    /// <summary>
    /// Varsayılan fontları yükler
    /// </summary>
    public async Task LoadDefaultFontsAsync()
    {
        if (_defaultFontsLoaded)
            return;

        try
        {
            // Assets/Fonts klasöründeki fontları yükle
            var fontFiles = new[]
            {
                "PolarisRGB6x8.json",
                "PolarisRGB6x10M.json",
                "PolarisRGB10x11.json",
                "PolarisA7x10.json",
                "PolarisA14x16.json"
            };

            foreach (var fontFile in fontFiles)
            {
                try
                {
                    // MAUI'de Assets klasöründeki dosyalara erişim
                    using var stream = await FileSystem.OpenAppPackageFileAsync($"Fonts/{fontFile}");
                    using var reader = new StreamReader(stream);
                    var jsonContent = await reader.ReadToEndAsync();
                    
                    // Geçici dosyaya yaz ve yükle
                    var tempPath = Path.Combine(FileSystem.CacheDirectory, fontFile);
                    await File.WriteAllTextAsync(tempPath, jsonContent);
                    
                    await LoadJsonFontAsync(tempPath);
                    System.Diagnostics.Debug.WriteLine($"✅ Font yüklendi: {fontFile}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Font yüklenemedi: {fontFile} - {ex.Message}");
                }
            }

            _defaultFontsLoaded = true;
            System.Diagnostics.Debug.WriteLine($"✅ Toplam {_loadedFonts.Count} font yüklendi");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Font yükleme hatası: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAvailableFonts()
    {
        return _loadedFonts.Keys.ToList();
    }

    public async Task<BitmapFont> LoadBMFontAsync(string fntPath)
    {
        if (string.IsNullOrEmpty(fntPath))
            throw new ArgumentException("Font dosya yolu boş olamaz", nameof(fntPath));

        if (!File.Exists(fntPath))
            throw new FileNotFoundException("Font dosyası bulunamadı", fntPath);

        var fileInfo = new FileInfo(fntPath);
        if (fileInfo.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"Font dosyası çok büyük (max {MaxFileSizeBytes / 1024 / 1024}MB)");

        var font = new BitmapFont
        {
            FilePath = fntPath,
            Name = Path.GetFileNameWithoutExtension(fntPath)
        };

        var xmlContent = await File.ReadAllTextAsync(fntPath);
        var doc = XDocument.Parse(xmlContent);
        var root = doc.Root;

        if (root == null)
            throw new InvalidOperationException("Geçersiz BMFont XML formatı");

        var info = root.Element("info");
        if (info != null)
            font.Name = info.Attribute("face")?.Value ?? font.Name;

        var common = root.Element("common");
        if (common != null)
        {
            font.LineHeight = int.Parse(common.Attribute("lineHeight")?.Value ?? "16");
            font.Base = int.Parse(common.Attribute("base")?.Value ?? "13");
        }

        var pages = root.Element("pages");
        string? pngFileName = null;
        if (pages != null)
        {
            var page = pages.Element("page");
            if (page != null)
                pngFileName = page.Attribute("file")?.Value;
        }

        if (!string.IsNullOrEmpty(pngFileName))
        {
            var pngPath = Path.Combine(Path.GetDirectoryName(fntPath) ?? "", pngFileName);
            if (File.Exists(pngPath))
            {
                using var stream = File.OpenRead(pngPath);
                font.FontImage = SKBitmap.Decode(stream);
            }
            else
                throw new FileNotFoundException("Font görüntü dosyası bulunamadı", pngPath);
        }

        var chars = root.Element("chars");
        if (chars != null)
        {
            foreach (var charElement in chars.Elements("char"))
            {
                var fontChar = new FontChar
                {
                    Id = int.Parse(charElement.Attribute("id")?.Value ?? "0"),
                    X = int.Parse(charElement.Attribute("x")?.Value ?? "0"),
                    Y = int.Parse(charElement.Attribute("y")?.Value ?? "0"),
                    Width = int.Parse(charElement.Attribute("width")?.Value ?? "0"),
                    Height = int.Parse(charElement.Attribute("height")?.Value ?? "0"),
                    XOffset = int.Parse(charElement.Attribute("xoffset")?.Value ?? "0"),
                    YOffset = int.Parse(charElement.Attribute("yoffset")?.Value ?? "0"),
                    XAdvance = int.Parse(charElement.Attribute("xadvance")?.Value ?? "0")
                };
                font.Characters[fontChar.Id] = fontChar;
            }
        }

        var kernings = root.Element("kernings");
        if (kernings != null)
        {
            foreach (var kerningElement in kernings.Elements("kerning"))
            {
                var first = int.Parse(kerningElement.Attribute("first")?.Value ?? "0");
                var second = int.Parse(kerningElement.Attribute("second")?.Value ?? "0");
                var amount = int.Parse(kerningElement.Attribute("amount")?.Value ?? "0");
                font.Kernings[(first, second)] = amount;
            }
        }

        // Font'u cache'e ekle
        _loadedFonts[font.Name] = font;

        return font;
    }


    public async Task<BitmapFont> LoadJsonFontAsync(string jsonPath)
    {
        if (string.IsNullOrEmpty(jsonPath))
            throw new ArgumentException("Font dosya yolu boş olamaz", nameof(jsonPath));

        if (!File.Exists(jsonPath))
            throw new FileNotFoundException("Font dosyası bulunamadı", jsonPath);

        var fileInfo = new FileInfo(jsonPath);
        if (fileInfo.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"Font dosyası çok büyük (max {MaxFileSizeBytes / 1024 / 1024}MB)");

        var font = new BitmapFont
        {
            FilePath = jsonPath,
            Name = Path.GetFileNameWithoutExtension(jsonPath)
        };

        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("name", out var nameElement))
            font.Name = nameElement.GetString() ?? font.Name;

        if (root.TryGetProperty("lineHeight", out var lineHeightElement))
            font.LineHeight = lineHeightElement.GetInt32();

        if (root.TryGetProperty("base", out var baseElement))
            font.Base = baseElement.GetInt32();

        if (root.TryGetProperty("characters", out var charactersElement))
        {
            var firstProp = charactersElement.EnumerateObject().GetEnumerator();
            if (firstProp.MoveNext() && firstProp.Current.Value.ValueKind == JsonValueKind.Array)
                return await LoadLedBitmapFontAsync(jsonPath, root);
        }

        string? pngFileName = null;
        if (root.TryGetProperty("imageFile", out var imageFileElement))
            pngFileName = imageFileElement.GetString();

        if (!string.IsNullOrEmpty(pngFileName))
        {
            var pngPath = Path.Combine(Path.GetDirectoryName(jsonPath) ?? "", pngFileName);
            if (File.Exists(pngPath))
            {
                using var stream = File.OpenRead(pngPath);
                font.FontImage = SKBitmap.Decode(stream);
            }
            else
                throw new FileNotFoundException("Font görüntü dosyası bulunamadı", pngPath);
        }

        if (root.TryGetProperty("characters", out charactersElement))
        {
            foreach (var charProperty in charactersElement.EnumerateObject())
            {
                if (int.TryParse(charProperty.Name, out var charId))
                {
                    var charData = charProperty.Value;
                    var fontChar = new FontChar
                    {
                        Id = charId,
                        X = charData.TryGetProperty("x", out var x) ? x.GetInt32() : 0,
                        Y = charData.TryGetProperty("y", out var y) ? y.GetInt32() : 0,
                        Width = charData.TryGetProperty("width", out var w) ? w.GetInt32() : 0,
                        Height = charData.TryGetProperty("height", out var h) ? h.GetInt32() : 0,
                        XOffset = charData.TryGetProperty("xoffset", out var xo) ? xo.GetInt32() : 0,
                        YOffset = charData.TryGetProperty("yoffset", out var yo) ? yo.GetInt32() : 0,
                        XAdvance = charData.TryGetProperty("xadvance", out var xa) ? xa.GetInt32() : 0
                    };
                    font.Characters[fontChar.Id] = fontChar;
                }
            }
        }

        if (root.TryGetProperty("kernings", out var kerningsElement))
        {
            foreach (var kerningProperty in kerningsElement.EnumerateObject())
            {
                var parts = kerningProperty.Name.Split(',');
                if (parts.Length == 2 && 
                    int.TryParse(parts[0], out var first) && 
                    int.TryParse(parts[1], out var second))
                {
                    var amount = kerningProperty.Value.GetInt32();
                    font.Kernings[(first, second)] = amount;
                }
            }
        }

        // Font'u cache'e ekle
        _loadedFonts[font.Name] = font;

        return font;
    }

    private async Task<BitmapFont> LoadLedBitmapFontAsync(string jsonPath, JsonElement root)
    {
        var font = new BitmapFont
        {
            FilePath = jsonPath,
            Name = Path.GetFileNameWithoutExtension(jsonPath)
        };

        if (root.TryGetProperty("name", out var nameElement))
            font.Name = nameElement.GetString() ?? font.Name;

        int jsonLineHeight = 0;
        if (root.TryGetProperty("lineHeight", out var lineHeightElement))
            jsonLineHeight = lineHeightElement.GetInt32();

        int letterspace = 1;
        if (root.TryGetProperty("letterspace", out var letterspaceElement))
        {
            int rawLetterspace = letterspaceElement.GetInt32();
            letterspace = rawLetterspace > 10 ? 1 : Math.Max(1, rawLetterspace);
        }

        var characterData = new Dictionary<int, int[]>();
        int maxWidth = 0;
        int maxHeight = 0;

        if (root.TryGetProperty("characters", out var charactersElement))
        {
            foreach (var charProperty in charactersElement.EnumerateObject())
            {
                if (int.TryParse(charProperty.Name, out var charId))
                {
                    var pixelArray = charProperty.Value;
                    var pixels = new List<int>();
                    foreach (var pixel in pixelArray.EnumerateArray())
                        pixels.Add(pixel.GetInt32());
                    characterData[charId] = pixels.ToArray();

                    int charWidth = 0;
                    foreach (var row in pixels)
                    {
                        int rowWidth = GetBitWidth(row);
                        if (rowWidth > charWidth) charWidth = rowWidth;
                    }
                    if (charWidth > maxWidth) maxWidth = charWidth;
                    if (pixels.Count > maxHeight) maxHeight = pixels.Count;
                }
            }
        }

        font.LineHeight = jsonLineHeight > 0 ? jsonLineHeight : (maxHeight > 0 ? maxHeight : 16);
        font.Base = font.LineHeight - 2;

        int charsPerRow = 16;
        int charCellWidth = maxWidth + 2;
        int charCellHeight = maxHeight;
        int imageWidth = charsPerRow * charCellWidth;
        int imageHeight = ((characterData.Count + charsPerRow - 1) / charsPerRow) * charCellHeight;

        font.FontImage = new SKBitmap(imageWidth, imageHeight);
        using var canvas = new SKCanvas(font.FontImage);
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = false };

        int charIndex = 0;
        foreach (var kvp in characterData)
        {
            int charId = kvp.Key;
            int[] pixels = kvp.Value;

            int col = charIndex % charsPerRow;
            int row = charIndex / charsPerRow;
            int startX = col * charCellWidth;
            int startY = row * charCellHeight;

            int charMinBit = int.MaxValue;
            int charMaxBit = 0;
            for (int y = 0; y < pixels.Length; y++)
            {
                if (pixels[y] != 0)
                {
                    int rowMin = GetMinBit(pixels[y]);
                    int rowMax = GetBitWidth(pixels[y]) - 1;
                    if (rowMin < charMinBit) charMinBit = rowMin;
                    if (rowMax > charMaxBit) charMaxBit = rowMax;
                }
            }

            int charWidth;
            int xOffset = 0;
            if (charMinBit == int.MaxValue)
                charWidth = 4;
            else
            {
                charWidth = charMaxBit - charMinBit + 1;
                xOffset = charMinBit;
            }

            for (int y = 0; y < pixels.Length; y++)
            {
                int rowData = pixels[y];
                for (int x = 0; x < 16; x++)
                {
                    if ((rowData & (1 << x)) != 0)
                        canvas.DrawPoint(startX + x - xOffset, startY + y, paint);
                }
            }

            var fontChar = new FontChar
            {
                Id = charId,
                X = startX,
                Y = startY,
                Width = charWidth > 0 ? charWidth : 1,
                Height = pixels.Length,
                XOffset = 0,
                YOffset = 0,
                XAdvance = charWidth > 0 ? charWidth : 4
            };
            font.Characters[charId] = fontChar;
            charIndex++;
        }

        // Font'u cache'e ekle
        _loadedFonts[font.Name] = font;

        return font;
    }

    private static int GetBitWidth(int value)
    {
        if (value == 0) return 0;
        int maxBit = 0;
        int temp = value;
        int bit = 0;
        while (temp > 0)
        {
            if ((temp & 1) != 0) maxBit = bit;
            temp >>= 1;
            bit++;
        }
        return maxBit + 1;
    }

    private static int GetMinBit(int value)
    {
        if (value == 0) return 0;
        int bit = 0;
        while ((value & (1 << bit)) == 0) bit++;
        return bit;
    }

    /// <summary>
    /// Font dosyasının geçerliliğini kontrol eder
    /// </summary>
    public bool ValidateFont(BitmapFont font)
    {
        if (font == null)
            return false;

        if (font.FontImage == null)
            return false;

        if (font.Characters == null || font.Characters.Count == 0)
            return false;

        if (font.FontImage.Width <= 0 || font.FontImage.Height <= 0)
            return false;

        if (font.LineHeight <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Metni bitmap font kullanarak SKBitmap'e render eder
    /// </summary>
    public SKBitmap RenderText(BitmapFont font, string text, SKColor color, int letterSpacing = 1)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        if (font.FontImage == null)
            throw new InvalidOperationException("Font görüntüsü yüklenmemiş");

        if (string.IsNullOrEmpty(text))
        {
            var emptyBitmap = new SKBitmap(1, font.LineHeight);
            emptyBitmap.Erase(SKColors.Transparent);
            return emptyBitmap;
        }

        int totalWidth = CalculateTextWidth(font, text, letterSpacing);
        int height = font.LineHeight;

        if (totalWidth <= 0)
            totalWidth = 1;

        var bitmap = new SKBitmap(totalWidth, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint
        {
            IsAntialias = false,
            BlendMode = SKBlendMode.SrcOver
        };

        if (color != SKColors.White)
        {
            var colorMatrix = new float[]
            {
                color.Red / 255f, 0, 0, 0, 0,
                0, color.Green / 255f, 0, 0, 0,
                0, 0, color.Blue / 255f, 0, 0,
                0, 0, 0, color.Alpha / 255f, 0
            };
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(colorMatrix);
        }

        int currentX = 0;
        char? previousChar = null;

        foreach (char c in text)
        {
            if (previousChar.HasValue)
                currentX += font.GetKerning(previousChar.Value, c);

            FontChar? fontChar = GetFontCharOrPlaceholder(font, c);

            if (fontChar != null && font.FontImage != null)
            {
                var srcRect = new SKRect(
                    fontChar.X,
                    fontChar.Y,
                    fontChar.X + fontChar.Width,
                    fontChar.Y + fontChar.Height
                );

                var destRect = new SKRect(
                    currentX + fontChar.XOffset,
                    fontChar.YOffset,
                    currentX + fontChar.XOffset + fontChar.Width,
                    fontChar.YOffset + fontChar.Height
                );

                canvas.DrawBitmap(font.FontImage, srcRect, destRect, paint);
                currentX += fontChar.XAdvance + letterSpacing;
            }

            previousChar = c;
        }

        return bitmap;
    }

    /// <summary>
    /// Çok renkli metin segmentlerini bitmap font kullanarak SKBitmap'e render eder
    /// </summary>
    public SKBitmap RenderColoredText(BitmapFont font, IEnumerable<(string Text, SKColor Color)> segments, int letterSpacing = 1)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        if (font.FontImage == null)
            throw new InvalidOperationException("Font görüntüsü yüklenmemiş");

        var segmentList = segments?.ToList() ?? new List<(string Text, SKColor Color)>();

        if (segmentList.Count == 0)
        {
            var emptyBitmap = new SKBitmap(1, font.LineHeight);
            emptyBitmap.Erase(SKColors.Transparent);
            return emptyBitmap;
        }

        string fullText = string.Concat(segmentList.Select(s => s.Text));
        int totalWidth = CalculateTextWidth(font, fullText, letterSpacing);
        int height = font.LineHeight;

        if (totalWidth <= 0)
            totalWidth = 1;

        var bitmap = new SKBitmap(totalWidth, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        int currentX = 0;
        char? previousChar = null;

        foreach (var segment in segmentList)
        {
            if (string.IsNullOrEmpty(segment.Text))
                continue;

            using var paint = new SKPaint
            {
                IsAntialias = false,
                BlendMode = SKBlendMode.SrcOver
            };

            if (segment.Color != SKColors.White)
            {
                var colorMatrix = new float[]
                {
                    segment.Color.Red / 255f, 0, 0, 0, 0,
                    0, segment.Color.Green / 255f, 0, 0, 0,
                    0, 0, segment.Color.Blue / 255f, 0, 0,
                    0, 0, 0, segment.Color.Alpha / 255f, 0
                };
                paint.ColorFilter = SKColorFilter.CreateColorMatrix(colorMatrix);
            }

            foreach (char c in segment.Text)
            {
                if (previousChar.HasValue)
                    currentX += font.GetKerning(previousChar.Value, c);

                FontChar? fontChar = GetFontCharOrPlaceholder(font, c);

                if (fontChar != null && font.FontImage != null)
                {
                    var srcRect = new SKRect(
                        fontChar.X,
                        fontChar.Y,
                        fontChar.X + fontChar.Width,
                        fontChar.Y + fontChar.Height
                    );

                    var destRect = new SKRect(
                        currentX + fontChar.XOffset,
                        fontChar.YOffset,
                        currentX + fontChar.XOffset + fontChar.Width,
                        fontChar.YOffset + fontChar.Height
                    );

                    canvas.DrawBitmap(font.FontImage, srcRect, destRect, paint);
                    currentX += fontChar.XAdvance + letterSpacing;
                }

                previousChar = c;
            }
        }

        return bitmap;
    }

    /// <summary>
    /// Metnin toplam genişliğini hesaplar
    /// </summary>
    public int CalculateTextWidth(BitmapFont font, string text, int letterSpacing = 1)
    {
        if (font == null || string.IsNullOrEmpty(text))
            return 0;

        int totalWidth = 0;
        char? previousChar = null;

        foreach (char c in text)
        {
            if (previousChar.HasValue)
                totalWidth += font.GetKerning(previousChar.Value, c);

            FontChar? fontChar = GetFontCharOrPlaceholder(font, c);
            if (fontChar != null)
                totalWidth += fontChar.XAdvance + letterSpacing;

            previousChar = c;
        }

        if (totalWidth > 0)
            totalWidth -= letterSpacing;

        return totalWidth;
    }

    private FontChar? GetFontCharOrPlaceholder(BitmapFont font, char c)
    {
        var fontChar = font.GetCharacter(c);
        if (fontChar != null)
            return fontChar;

        fontChar = font.GetCharacter(PlaceholderChar);
        if (fontChar != null)
            return fontChar;

        if (font.Characters.TryGetValue(PlaceholderCharId, out fontChar))
            return fontChar;

        fontChar = font.GetCharacter(' ');
        if (fontChar != null)
            return fontChar;

        return null;
    }
}
