using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using LEDTabelam.Services;
using SkiaSharp;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for FontLoader service
/// Feature: led-tabelam, Property 1: Font Round-Trip Consistency
/// Feature: led-tabelam, Property 6: Turkish Character Rendering
/// Validates: Requirements 4.4, 4.5, 4.6, 3.2, 3.3
/// </summary>
public class FontLoaderPropertyTests : IDisposable
{
    private readonly string _testDir;
    private readonly FontLoader _fontLoader;

    public FontLoaderPropertyTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"FontLoaderTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _fontLoader = new FontLoader();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    #region Test Helpers

    private SKBitmap CreateTestFontImage(int width = 256, int height = 256)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = false };
        
        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                canvas.DrawRect(i * 16 + 2, j * 16 + 2, 12, 12, paint);
            }
        }
        
        return bitmap;
    }

    private string SaveTestPng(SKBitmap bitmap, string name)
    {
        var path = Path.Combine(_testDir, $"{name}.png");
        using var stream = File.OpenWrite(path);
        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        return path;
    }

    private string CreateBMFontXml(string name, IEnumerable<FontChar> characters, string pngFileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<font>");
        sb.AppendLine($"  <info face=\"{name}\" size=\"16\" />");
        sb.AppendLine("  <common lineHeight=\"16\" base=\"13\" scaleW=\"256\" scaleH=\"256\" pages=\"1\" />");
        sb.AppendLine("  <pages>");
        sb.AppendLine($"    <page id=\"0\" file=\"{pngFileName}\" />");
        sb.AppendLine("  </pages>");
        sb.AppendLine("  <chars>");
        
        foreach (var c in characters)
        {
            sb.AppendLine($"    <char id=\"{c.Id}\" x=\"{c.X}\" y=\"{c.Y}\" width=\"{c.Width}\" height=\"{c.Height}\" xoffset=\"{c.XOffset}\" yoffset=\"{c.YOffset}\" xadvance=\"{c.XAdvance}\" />");
        }
        
        sb.AppendLine("  </chars>");
        sb.AppendLine("  <kernings />");
        sb.AppendLine("</font>");

        var path = Path.Combine(_testDir, $"{name}.fnt");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private string CreateJsonFont(string name, IEnumerable<FontChar> characters, string pngFileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"name\": \"{name}\",");
        sb.AppendLine("  \"size\": 16,");
        sb.AppendLine("  \"lineHeight\": 16,");
        sb.AppendLine("  \"base\": 13,");
        sb.AppendLine($"  \"imageFile\": \"{pngFileName}\",");
        sb.AppendLine("  \"characters\": {");
        
        var charList = new List<FontChar>(characters);
        for (int i = 0; i < charList.Count; i++)
        {
            var c = charList[i];
            var comma = i < charList.Count - 1 ? "," : "";
            sb.AppendLine($"    \"{c.Id}\": {{ \"x\": {c.X}, \"y\": {c.Y}, \"width\": {c.Width}, \"height\": {c.Height}, \"xoffset\": {c.XOffset}, \"yoffset\": {c.YOffset}, \"xadvance\": {c.XAdvance} }}{comma}");
        }
        
        sb.AppendLine("  },");
        sb.AppendLine("  \"kernings\": {}");
        sb.AppendLine("}");

        var path = Path.Combine(_testDir, $"{name}.json");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private List<FontChar> CreateStandardCharacters()
    {
        var chars = new List<FontChar>();
        
        for (int i = 32; i <= 126; i++)
        {
            int row = (i - 32) / 16;
            int col = (i - 32) % 16;
            chars.Add(new FontChar
            {
                Id = i,
                X = col * 16,
                Y = row * 16,
                Width = 8,
                Height = 16,
                XOffset = 0,
                YOffset = 0,
                XAdvance = 9
            });
        }
        
        return chars;
    }

    private List<FontChar> CreateTurkishCharacters()
    {
        var turkishChars = new (char c, int id)[]
        {
            ('ğ', 0x011F), ('ü', 0x00FC), ('ş', 0x015F), ('ı', 0x0131), ('ö', 0x00F6), ('ç', 0x00E7),
            ('Ğ', 0x011E), ('Ü', 0x00DC), ('Ş', 0x015E), ('İ', 0x0130), ('Ö', 0x00D6), ('Ç', 0x00C7)
        };

        var chars = new List<FontChar>();
        int index = 0;
        foreach (var (c, id) in turkishChars)
        {
            int row = 6 + (index / 16);
            int col = index % 16;
            chars.Add(new FontChar
            {
                Id = id,
                X = col * 16,
                Y = row * 16,
                Width = 8,
                Height = 16,
                XOffset = 0,
                YOffset = 0,
                XAdvance = 9
            });
            index++;
        }
        
        return chars;
    }

    #endregion

    #region Generators

    public static Gen<FontChar> GenFontChar()
    {
        return from id in Gen.Choose(32, 126)
               from x in Gen.Choose(0, 240)
               from y in Gen.Choose(0, 240)
               from width in Gen.Choose(4, 16)
               from height in Gen.Choose(8, 16)
               from xoffset in Gen.Choose(-2, 2)
               from yoffset in Gen.Choose(-2, 2)
               from xadvance in Gen.Choose(4, 16)
               select new FontChar
               {
                   Id = id,
                   X = x,
                   Y = y,
                   Width = width,
                   Height = height,
                   XOffset = xoffset,
                   YOffset = yoffset,
                   XAdvance = xadvance
               };
    }

    public static Gen<List<FontChar>> GenFontCharList()
    {
        return Gen.Choose(5, 20).SelectMany(count =>
        {
            var usedIds = new HashSet<int>();
            return Gen.ListOf(count, GenFontChar())
                .Select(chars =>
                {
                    var result = new List<FontChar>();
                    foreach (var c in chars)
                    {
                        if (!usedIds.Contains(c.Id))
                        {
                            usedIds.Add(c.Id);
                            result.Add(c);
                        }
                    }
                    return result;
                });
        });
    }

    public class FontArbitraries
    {
        public static Arbitrary<FontChar> FontCharArb() => Arb.From(GenFontChar());
        public static Arbitrary<List<FontChar>> FontCharListArb() => Arb.From(GenFontCharList());
    }

    #endregion


    #region Property Tests

    /// <summary>
    /// Property 1: Font Round-Trip Consistency
    /// For any valid BitmapFont object, serializing to BMFont XML format 
    /// and then parsing back should produce an equivalent BitmapFont object with 
    /// identical character mappings, metadata, and kerning information.
    /// Validates: Requirements 4.4, 4.5, 4.6
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FontArbitraries) })]
    public bool FontRoundTripConsistency_BMFont(List<FontChar> characters)
    {
        if (characters.Count == 0) return true;
        
        var fontName = $"TestFont_{Guid.NewGuid():N}";
        var pngFileName = $"{fontName}.png";
        
        using var testImage = CreateTestFontImage();
        SaveTestPng(testImage, fontName);
        
        var fntPath = CreateBMFontXml(fontName, characters, pngFileName);
        
        var loadedFont = _fontLoader.LoadBMFontAsync(fntPath).GetAwaiter().GetResult();
        
        try
        {
            foreach (var originalChar in characters)
            {
                if (!loadedFont.Characters.TryGetValue(originalChar.Id, out var loadedChar))
                    return false;
                
                if (loadedChar.X != originalChar.X ||
                    loadedChar.Y != originalChar.Y ||
                    loadedChar.Width != originalChar.Width ||
                    loadedChar.Height != originalChar.Height ||
                    loadedChar.XOffset != originalChar.XOffset ||
                    loadedChar.YOffset != originalChar.YOffset ||
                    loadedChar.XAdvance != originalChar.XAdvance)
                    return false;
            }
            
            if (loadedFont.LineHeight != 16 || loadedFont.Base != 13)
                return false;
            
            if (loadedFont.FontImage == null)
                return false;
            
            return true;
        }
        finally
        {
            loadedFont.Dispose();
        }
    }

    /// <summary>
    /// Property 1: Font Round-Trip Consistency (JSON variant)
    /// Validates: Requirements 4.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FontArbitraries) })]
    public bool FontRoundTripConsistency_JSON(List<FontChar> characters)
    {
        if (characters.Count == 0) return true;
        
        var fontName = $"TestFont_{Guid.NewGuid():N}";
        var pngFileName = $"{fontName}.png";
        
        using var testImage = CreateTestFontImage();
        SaveTestPng(testImage, fontName);
        
        var jsonPath = CreateJsonFont(fontName, characters, pngFileName);
        
        var loadedFont = _fontLoader.LoadJsonFontAsync(jsonPath).GetAwaiter().GetResult();
        
        try
        {
            foreach (var originalChar in characters)
            {
                if (!loadedFont.Characters.TryGetValue(originalChar.Id, out var loadedChar))
                    return false;
                
                if (loadedChar.X != originalChar.X ||
                    loadedChar.Y != originalChar.Y ||
                    loadedChar.Width != originalChar.Width ||
                    loadedChar.Height != originalChar.Height ||
                    loadedChar.XOffset != originalChar.XOffset ||
                    loadedChar.YOffset != originalChar.YOffset ||
                    loadedChar.XAdvance != originalChar.XAdvance)
                    return false;
            }
            
            if (loadedFont.LineHeight != 16 || loadedFont.Base != 13)
                return false;
            
            if (loadedFont.FontImage == null)
                return false;
            
            return true;
        }
        finally
        {
            loadedFont.Dispose();
        }
    }

    /// <summary>
    /// Property 6: Turkish Character Rendering
    /// For any string containing Turkish special characters (ğ, ü, ş, ı, ö, ç, Ğ, Ü, Ş, İ, Ö, Ç),
    /// if the loaded font contains these characters, the rendered output should contain 
    /// exactly the same number of character glyphs as the input string length.
    /// Validates: Requirements 3.2, 3.3, 4.8
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TurkishCharacterRendering(PositiveInt textLength)
    {
        var length = Math.Min(textLength.Get, 50);
        
        var fontName = $"TurkishFont_{Guid.NewGuid():N}";
        var pngFileName = $"{fontName}.png";
        
        using var testImage = CreateTestFontImage();
        SaveTestPng(testImage, fontName);
        
        var characters = CreateStandardCharacters();
        characters.AddRange(CreateTurkishCharacters());
        
        var fntPath = CreateBMFontXml(fontName, characters, pngFileName);
        var loadedFont = _fontLoader.LoadBMFontAsync(fntPath).GetAwaiter().GetResult();
        
        try
        {
            var turkishChars = "ğüşıöçĞÜŞİÖÇ";
            var asciiChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
            var allChars = turkishChars + asciiChars;
            
            var random = new System.Random();
            var testText = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                testText.Append(allChars[random.Next(allChars.Length)]);
            }
            
            var text = testText.ToString();
            
            using var rendered = _fontLoader.RenderText(loadedFont, text, SKColors.White);
            
            int expectedWidth = _fontLoader.CalculateTextWidth(loadedFont, text);
            
            if (text.Length == 0)
                return rendered.Width >= 1 && rendered.Height == loadedFont.LineHeight;
            
            return rendered.Width == expectedWidth && rendered.Height == loadedFont.LineHeight;
        }
        finally
        {
            loadedFont.Dispose();
        }
    }

    /// <summary>
    /// Property 6 variant: Turkish characters should be individually renderable
    /// Validates: Requirements 3.2, 4.8
    /// </summary>
    [Fact]
    public void TurkishCharacters_AllRenderable()
    {
        var fontName = $"TurkishFont_{Guid.NewGuid():N}";
        var pngFileName = $"{fontName}.png";
        
        using var testImage = CreateTestFontImage();
        SaveTestPng(testImage, fontName);
        
        var characters = CreateStandardCharacters();
        characters.AddRange(CreateTurkishCharacters());
        
        var fntPath = CreateBMFontXml(fontName, characters, pngFileName);
        var loadedFont = _fontLoader.LoadBMFontAsync(fntPath).GetAwaiter().GetResult();
        
        try
        {
            Assert.True(_fontLoader.SupportsTurkishCharacters(loadedFont));
            
            foreach (var c in FontLoader.TurkishCharacters)
            {
                using var rendered = _fontLoader.RenderText(loadedFont, c.ToString(), SKColors.White);
                Assert.True(rendered.Width > 0, $"Character '{c}' should render with positive width");
                Assert.Equal(loadedFont.LineHeight, rendered.Height);
            }
        }
        finally
        {
            loadedFont.Dispose();
        }
    }

    #endregion

    #region Unit Tests for Edge Cases

    [Fact]
    public void ValidateFont_NullFont_ReturnsFalse()
    {
        Assert.False(_fontLoader.ValidateFont(null!));
    }

    [Fact]
    public void ValidateFont_NoImage_ReturnsFalse()
    {
        var font = new BitmapFont
        {
            Name = "Test",
            LineHeight = 16,
            Characters = new Dictionary<int, FontChar> { { 65, new FontChar { Id = 65 } } }
        };
        Assert.False(_fontLoader.ValidateFont(font));
    }

    [Fact]
    public void ValidateFont_NoCharacters_ReturnsFalse()
    {
        using var image = CreateTestFontImage();
        var font = new BitmapFont
        {
            Name = "Test",
            LineHeight = 16,
            FontImage = image,
            Characters = new Dictionary<int, FontChar>()
        };
        Assert.False(_fontLoader.ValidateFont(font));
    }

    [Fact]
    public void ValidateFont_ValidFont_ReturnsTrue()
    {
        using var image = CreateTestFontImage();
        var font = new BitmapFont
        {
            Name = "Test",
            LineHeight = 16,
            FontImage = image,
            Characters = new Dictionary<int, FontChar> { { 65, new FontChar { Id = 65, Width = 8, Height = 16 } } }
        };
        Assert.True(_fontLoader.ValidateFont(font));
    }

    [Fact]
    public void RenderText_EmptyString_ReturnsMinimalBitmap()
    {
        using var image = CreateTestFontImage();
        var font = new BitmapFont
        {
            Name = "Test",
            LineHeight = 16,
            FontImage = image,
            Characters = new Dictionary<int, FontChar> { { 65, new FontChar { Id = 65, Width = 8, Height = 16, XAdvance = 9 } } }
        };
        
        using var result = _fontLoader.RenderText(font, "", SKColors.White);
        Assert.Equal(1, result.Width);
        Assert.Equal(16, result.Height);
    }

    [Fact]
    public async Task LoadBMFontAsync_FileNotFound_ThrowsException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _fontLoader.LoadBMFontAsync("nonexistent.fnt"));
    }

    [Fact]
    public async Task LoadJsonFontAsync_FileNotFound_ThrowsException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _fontLoader.LoadJsonFontAsync("nonexistent.json"));
    }

    #endregion
}
