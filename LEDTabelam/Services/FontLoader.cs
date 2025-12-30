using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Bitmap font yükleme ve metin render servisi
/// Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10, 4.11
/// </summary>
public class FontLoader : IFontLoader
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const char PlaceholderChar = '□';
    private const int PlaceholderCharId = 0x25A1; // Unicode for □

    /// <summary>
    /// BMFont XML formatındaki font dosyasını yükler (.fnt + .png)
    /// Requirements: 4.4, 4.6, 4.7
    /// </summary>
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

        // Parse info element
        var info = root.Element("info");
        if (info != null)
        {
            font.Name = info.Attribute("face")?.Value ?? font.Name;
        }

        // Parse common element
        var common = root.Element("common");
        if (common != null)
        {
            font.LineHeight = int.Parse(common.Attribute("lineHeight")?.Value ?? "16");
            font.Base = int.Parse(common.Attribute("base")?.Value ?? "13");
        }

        // Parse pages element to get PNG file path
        var pages = root.Element("pages");
        string? pngFileName = null;
        if (pages != null)
        {
            var page = pages.Element("page");
            if (page != null)
            {
                pngFileName = page.Attribute("file")?.Value;
            }
        }

        // Load PNG image
        if (!string.IsNullOrEmpty(pngFileName))
        {
            var pngPath = Path.Combine(Path.GetDirectoryName(fntPath) ?? "", pngFileName);
            if (File.Exists(pngPath))
            {
                var pngFileInfo = new FileInfo(pngPath);
                if (pngFileInfo.Length > MaxFileSizeBytes)
                    throw new InvalidOperationException($"Font görüntü dosyası çok büyük (max {MaxFileSizeBytes / 1024 / 1024}MB)");

                using var stream = File.OpenRead(pngPath);
                font.FontImage = SKBitmap.Decode(stream);
            }
            else
            {
                throw new FileNotFoundException("Font görüntü dosyası bulunamadı", pngPath);
            }
        }

        // Parse chars element
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

        // Parse kernings element
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

        return font;
    }


    /// <summary>
    /// JSON formatındaki font dosyasını yükler (.json + .png)
    /// Requirements: 4.5
    /// </summary>
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

        // Parse basic info
        if (root.TryGetProperty("name", out var nameElement))
            font.Name = nameElement.GetString() ?? font.Name;

        if (root.TryGetProperty("lineHeight", out var lineHeightElement))
            font.LineHeight = lineHeightElement.GetInt32();

        if (root.TryGetProperty("base", out var baseElement))
            font.Base = baseElement.GetInt32();

        // Check if this is LED bitmap format (characters as arrays)
        if (root.TryGetProperty("characters", out var charactersElement))
        {
            var firstProp = charactersElement.EnumerateObject().GetEnumerator();
            if (firstProp.MoveNext() && firstProp.Current.Value.ValueKind == JsonValueKind.Array)
            {
                // LED bitmap format - generate image from pixel data
                return await LoadLedBitmapFontAsync(jsonPath, root);
            }
        }

        // Get PNG file path (standard JSON format)
        string? pngFileName = null;
        if (root.TryGetProperty("imageFile", out var imageFileElement))
            pngFileName = imageFileElement.GetString();

        // Load PNG image
        if (!string.IsNullOrEmpty(pngFileName))
        {
            var pngPath = Path.Combine(Path.GetDirectoryName(jsonPath) ?? "", pngFileName);
            if (File.Exists(pngPath))
            {
                var pngFileInfo = new FileInfo(pngPath);
                if (pngFileInfo.Length > MaxFileSizeBytes)
                    throw new InvalidOperationException($"Font görüntü dosyası çok büyük (max {MaxFileSizeBytes / 1024 / 1024}MB)");

                using var stream = File.OpenRead(pngPath);
                font.FontImage = SKBitmap.Decode(stream);
            }
            else
            {
                throw new FileNotFoundException("Font görüntü dosyası bulunamadı", pngPath);
            }
        }

        // Parse characters (standard format)
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

        // Parse kernings
        if (root.TryGetProperty("kernings", out var kerningsElement))
        {
            foreach (var kerningProperty in kerningsElement.EnumerateObject())
            {
                // Format: "first,second" -> amount
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

        return font;
    }

    /// <summary>
    /// LED bitmap formatındaki font dosyasını yükler (piksel dizileri içeren JSON)
    /// Her karakter için satır bazlı bitmap verisi içerir
    /// </summary>
    private async Task<BitmapFont> LoadLedBitmapFontAsync(string jsonPath, JsonElement root)
    {
        var font = new BitmapFont
        {
            FilePath = jsonPath,
            Name = Path.GetFileNameWithoutExtension(jsonPath)
        };

        // Parse basic info
        if (root.TryGetProperty("name", out var nameElement))
            font.Name = nameElement.GetString() ?? font.Name;

        // LineHeight'ı JSON'dan oku (varsa)
        int jsonLineHeight = 0;
        if (root.TryGetProperty("lineHeight", out var lineHeightElement))
            jsonLineHeight = lineHeightElement.GetInt32();

        // Get letterspace for character spacing (varsayılan 1px)
        // Not: Bazı font formatlarında letterspace farklı birimde olabilir
        // 64 gibi büyük değerler genellikle farklı bir ölçek kullanır
        int letterspace = 1;
        if (root.TryGetProperty("letterspace", out var letterspaceElement))
        {
            int rawLetterspace = letterspaceElement.GetInt32();
            // Eğer değer çok büyükse (>10), muhtemelen farklı bir birim kullanılıyor
            // Bu durumda varsayılan 1px kullan
            letterspace = rawLetterspace > 10 ? 1 : Math.Max(1, rawLetterspace);
        }

        // Parse characters and determine dimensions
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
                    {
                        pixels.Add(pixel.GetInt32());
                    }
                    characterData[charId] = pixels.ToArray();

                    // Calculate character width from pixel data
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

        // Set line height - JSON'dan gelen değeri kullan, yoksa hesaplanan değeri kullan
        font.LineHeight = jsonLineHeight > 0 ? jsonLineHeight : (maxHeight > 0 ? maxHeight : 16);
        font.Base = font.LineHeight - 2;

        // Generate font image from pixel data
        int charsPerRow = 16;
        int charCellWidth = maxWidth + 2;
        int charCellHeight = maxHeight;
        int imageWidth = charsPerRow * charCellWidth;
        int imageHeight = ((characterData.Count + charsPerRow - 1) / charsPerRow) * charCellHeight;

        font.FontImage = new SKBitmap(imageWidth, imageHeight);
        using var canvas = new SKCanvas(font.FontImage);
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = false
        };

        int charIndex = 0;
        foreach (var kvp in characterData)
        {
            int charId = kvp.Key;
            int[] pixels = kvp.Value;

            int col = charIndex % charsPerRow;
            int row = charIndex / charsPerRow;
            int startX = col * charCellWidth;
            int startY = row * charCellHeight;

            // Calculate actual character bounds (min and max bit positions)
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
            
            // Eğer karakter boşsa (space gibi)
            int charWidth;
            int xOffset = 0;
            if (charMinBit == int.MaxValue)
            {
                charWidth = 4; // Boşluk için varsayılan genişlik
            }
            else
            {
                charWidth = charMaxBit - charMinBit + 1;
                xOffset = charMinBit; // Karakterin başladığı bit pozisyonu
            }

            // Draw character pixels
            for (int y = 0; y < pixels.Length; y++)
            {
                int rowData = pixels[y];
                for (int x = 0; x < 16; x++)
                {
                    if ((rowData & (1 << x)) != 0)
                    {
                        // Pikseli sola hizala (xOffset kadar kaydır)
                        canvas.DrawPoint(startX + x - xOffset, startY + y, paint);
                    }
                }
            }

            // Create FontChar entry
            // XAdvance sadece karakter genişliği - letterSpacing render sırasında eklenir
            var fontChar = new FontChar
            {
                Id = charId,
                X = startX,
                Y = startY,
                Width = charWidth > 0 ? charWidth : 1,
                Height = pixels.Length,
                XOffset = 0, // Artık pikseller zaten sola hizalı
                YOffset = 0,
                XAdvance = charWidth > 0 ? charWidth : 4 // Boşluk için varsayılan 4px
            };
            font.Characters[charId] = fontChar;

            charIndex++;
        }

        return font;
    }

    /// <summary>
    /// Bir sayının en yüksek set edilmiş bit pozisyonunu bulur (karakter genişliği için)
    /// </summary>
    private static int GetBitWidth(int value)
    {
        if (value == 0) return 0;
        
        // En düşük ve en yüksek set edilmiş bit pozisyonlarını bul
        int minBit = -1;
        int maxBit = 0;
        int temp = value;
        int bit = 0;
        
        while (temp > 0)
        {
            if ((temp & 1) != 0)
            {
                if (minBit == -1) minBit = bit;
                maxBit = bit;
            }
            temp >>= 1;
            bit++;
        }
        
        // Genişlik = en yüksek bit - en düşük bit + 1
        // Ama LED fontlarda genellikle 0. bitten başlar, bu yüzden maxBit + 1 döndür
        return maxBit + 1;
    }
    
    /// <summary>
    /// Bir satırdaki en soldaki (en düşük) set edilmiş bit pozisyonunu bulur
    /// </summary>
    private static int GetMinBit(int value)
    {
        if (value == 0) return 0;
        int bit = 0;
        while ((value & (1 << bit)) == 0)
        {
            bit++;
        }
        return bit;
    }


    /// <summary>
    /// Font dosyasının geçerliliğini kontrol eder
    /// Requirements: 4.9, 4.10, 4.11
    /// </summary>
    public bool ValidateFont(BitmapFont font)
    {
        if (font == null)
            return false;

        // PNG dosyası varlık kontrolü (4.11 - PNG dosyası varlık kontrolü)
        if (font.FontImage == null)
            return false;

        // Boş karakter seti kontrolü (4.10 - boş karakter seti kontrolü)
        if (font.Characters == null || font.Characters.Count == 0)
            return false;

        // Font görüntüsünün geçerli boyutlarda olduğunu kontrol et
        if (font.FontImage.Width <= 0 || font.FontImage.Height <= 0)
            return false;

        // LineHeight'ın geçerli olduğunu kontrol et
        if (font.LineHeight <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Font dosya boyutunu kontrol eder (yükleme öncesi)
    /// Requirements: 4.9
    /// </summary>
    public bool ValidateFileSize(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return false;

        var fileInfo = new FileInfo(filePath);
        return fileInfo.Length <= MaxFileSizeBytes;
    }


    /// <summary>
    /// Metni bitmap font kullanarak SKBitmap'e render eder
    /// Requirements: 3.2, 3.3, 3.5, 4.8
    /// </summary>
    public SKBitmap RenderText(BitmapFont font, string text, SKColor color, int letterSpacing = 1)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        if (font.FontImage == null)
            throw new InvalidOperationException("Font görüntüsü yüklenmemiş");

        if (string.IsNullOrEmpty(text))
        {
            // Boş metin için 1x1 şeffaf bitmap döndür
            var emptyBitmap = new SKBitmap(1, font.LineHeight);
            emptyBitmap.Erase(SKColors.Transparent);
            return emptyBitmap;
        }

        // Toplam genişliği hesapla
        int totalWidth = CalculateTextWidth(font, text, letterSpacing);
        int height = font.LineHeight;

        // Minimum genişlik kontrolü
        if (totalWidth <= 0)
            totalWidth = 1;

        var bitmap = new SKBitmap(totalWidth, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        // Renk filtresi için paint oluştur
        using var paint = new SKPaint
        {
            IsAntialias = false,
            BlendMode = SKBlendMode.SrcOver
        };

        // Renk matrisi ile tint uygula
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
            // Kerning uygula
            if (previousChar.HasValue)
            {
                currentX += font.GetKerning(previousChar.Value, c);
            }

            FontChar? fontChar = GetFontCharOrPlaceholder(font, c);
            
            if (fontChar != null && font.FontImage != null)
            {
                // Kaynak dikdörtgen (font görüntüsündeki karakter)
                var srcRect = new SKRect(
                    fontChar.X,
                    fontChar.Y,
                    fontChar.X + fontChar.Width,
                    fontChar.Y + fontChar.Height
                );

                // Hedef dikdörtgen (çıktı bitmap'indeki pozisyon)
                var destRect = new SKRect(
                    currentX + fontChar.XOffset,
                    fontChar.YOffset,
                    currentX + fontChar.XOffset + fontChar.Width,
                    fontChar.YOffset + fontChar.Height
                );

                canvas.DrawBitmap(font.FontImage, srcRect, destRect, paint);
                
                // XAdvance kullan - bu değer bir sonraki karakterin başlangıç pozisyonunu belirler
                // Boşluk gibi görünmez karakterler için de doğru çalışır
                // letterSpacing ek olarak eklenir
                currentX += fontChar.XAdvance + letterSpacing;
            }

            previousChar = c;
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
            // Kerning uygula
            if (previousChar.HasValue)
            {
                totalWidth += font.GetKerning(previousChar.Value, c);
            }

            FontChar? fontChar = GetFontCharOrPlaceholder(font, c);
            if (fontChar != null)
            {
                // XAdvance kullan - boşluk gibi görünmez karakterler için de doğru çalışır
                totalWidth += fontChar.XAdvance + letterSpacing;
            }

            previousChar = c;
        }

        // Son karakterden sonra boşluk ekleme
        if (totalWidth > 0)
            totalWidth -= letterSpacing;

        return totalWidth;
    }

    /// <summary>
    /// Karakter için FontChar döndürür, bulunamazsa placeholder kullanır
    /// Requirements: 3.5 - Eksik karakterler için placeholder desteği
    /// </summary>
    private FontChar? GetFontCharOrPlaceholder(BitmapFont font, char c)
    {
        // Önce karakteri ara
        var fontChar = font.GetCharacter(c);
        if (fontChar != null)
            return fontChar;

        // Placeholder karakteri dene (□)
        fontChar = font.GetCharacter(PlaceholderChar);
        if (fontChar != null)
            return fontChar;

        // Placeholder Unicode ID ile dene
        if (font.Characters.TryGetValue(PlaceholderCharId, out fontChar))
            return fontChar;

        // Boşluk karakterini dene
        fontChar = font.GetCharacter(' ');
        if (fontChar != null)
            return fontChar;

        // Hiçbiri yoksa null döndür
        return null;
    }

    /// <summary>
    /// Türkçe özel karakterlerin listesi
    /// Requirements: 3.2, 4.8
    /// </summary>
    public static readonly char[] TurkishCharacters = new[]
    {
        'ğ', 'ü', 'ş', 'ı', 'ö', 'ç',
        'Ğ', 'Ü', 'Ş', 'İ', 'Ö', 'Ç'
    };

    /// <summary>
    /// Font'un Türkçe karakterleri destekleyip desteklemediğini kontrol eder
    /// </summary>
    public bool SupportsTurkishCharacters(BitmapFont font)
    {
        if (font == null)
            return false;

        foreach (var c in TurkishCharacters)
        {
            if (!font.HasCharacter(c))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Çok renkli metin segmentlerini bitmap font kullanarak SKBitmap'e render eder
    /// Her segment farklı renkte olabilir - LED tabelalarda gökkuşağı efekti için
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

        // Toplam genişliği hesapla
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

            // Bu segment için paint oluştur
            using var paint = new SKPaint
            {
                IsAntialias = false,
                BlendMode = SKBlendMode.SrcOver
            };

            // Renk matrisi ile tint uygula
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
                // Kerning uygula
                if (previousChar.HasValue)
                {
                    currentX += font.GetKerning(previousChar.Value, c);
                }

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
}
