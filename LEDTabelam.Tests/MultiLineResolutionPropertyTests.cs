using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using LEDTabelam.Services;
using SkiaSharp;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for multi-line text rendering and resolution validation
/// Feature: led-tabelam, Property 4: Resolution Bounds Validation
/// Feature: led-tabelam, Property 11: Multi-line Text Height Calculation
/// Validates: Requirements 1.5, 1.6, 14.x
/// </summary>
public class MultiLineResolutionPropertyTests : IDisposable
{
    private readonly string _testDir;
    private readonly FontLoader _fontLoader;
    private readonly MultiLineTextRenderer _multiLineRenderer;
    private readonly ResolutionValidator _resolutionValidator;

    public MultiLineResolutionPropertyTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"MultiLineTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _fontLoader = new FontLoader();
        _multiLineRenderer = new MultiLineTextRenderer(_fontLoader);
        _resolutionValidator = new ResolutionValidator();
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

    private string CreateBMFontXml(string name, int lineHeight, string pngFileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<font>");
        sb.AppendLine($"  <info face=\"{name}\" size=\"{lineHeight}\" />");
        sb.AppendLine($"  <common lineHeight=\"{lineHeight}\" base=\"{lineHeight - 3}\" scaleW=\"256\" scaleH=\"256\" pages=\"1\" />");
        sb.AppendLine("  <pages>");
        sb.AppendLine($"    <page id=\"0\" file=\"{pngFileName}\" />");
        sb.AppendLine("  </pages>");
        sb.AppendLine("  <chars>");
        
        // Add basic ASCII characters
        for (int i = 32; i <= 126; i++)
        {
            int row = (i - 32) / 16;
            int col = (i - 32) % 16;
            sb.AppendLine($"    <char id=\"{i}\" x=\"{col * 16}\" y=\"{row * 16}\" width=\"8\" height=\"{lineHeight}\" xoffset=\"0\" yoffset=\"0\" xadvance=\"9\" />");
        }
        
        sb.AppendLine("  </chars>");
        sb.AppendLine("  <kernings />");
        sb.AppendLine("</font>");

        var path = Path.Combine(_testDir, $"{name}.fnt");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private BitmapFont CreateTestFont(int lineHeight = 16)
    {
        var fontName = $"TestFont_{Guid.NewGuid():N}";
        var pngFileName = $"{fontName}.png";
        
        using var testImage = CreateTestFontImage();
        SaveTestPng(testImage, fontName);
        
        var fntPath = CreateBMFontXml(fontName, lineHeight, pngFileName);
        return _fontLoader.LoadBMFontAsync(fntPath).GetAwaiter().GetResult();
    }

    #endregion

    #region Generators

    public static Gen<int> GenValidResolution()
    {
        return Gen.Choose(1, 512);
    }

    public static Gen<int> GenInvalidResolutionLow()
    {
        return Gen.Choose(-100, 0);
    }

    public static Gen<int> GenInvalidResolutionHigh()
    {
        return Gen.Choose(513, 1000);
    }

    public static Gen<int> GenLineCount()
    {
        return Gen.Choose(1, 10);
    }

    public static Gen<int> GenLineSpacing()
    {
        return Gen.Choose(0, 10);
    }

    public static Gen<int> GenFontLineHeight()
    {
        return Gen.Choose(8, 32);
    }

    public class MultiLineArbitraries
    {
        public static Arbitrary<int> ValidResolutionArb() => Arb.From(GenValidResolution());
        public static Arbitrary<int> LineCountArb() => Arb.From(GenLineCount());
        public static Arbitrary<int> LineSpacingArb() => Arb.From(GenLineSpacing());
    }

    #endregion


    #region Property 4: Resolution Bounds Validation

    /// <summary>
    /// Property 4: Resolution Bounds Validation
    /// For any resolution value within 1-512 range for both width and height, 
    /// the system should accept the value. For any value outside this range, 
    /// the system should reject it and maintain the previous valid value.
    /// Validates: Requirements 1.5, 1.6
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ResolutionBoundsValidation_ValidValues_Accepted(PositiveInt value)
    {
        // Clamp to valid range for testing
        int testValue = Math.Min(value.Get, 512);
        
        // Valid values (1-512) should be accepted
        var result = _resolutionValidator.ValidateResolution(testValue, 128);
        
        return result.IsValid && result.Value == testValue && !result.WasCorrected;
    }

    /// <summary>
    /// Property 4: Resolution Bounds Validation - Invalid low values
    /// Validates: Requirements 1.5, 1.6
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ResolutionBoundsValidation_InvalidLowValues_Rejected(NegativeInt value)
    {
        int testValue = value.Get;
        int lastValidValue = 128;
        
        // Invalid values (< 1) should be rejected and last valid value preserved
        var result = _resolutionValidator.ValidateResolution(testValue, lastValidValue);
        
        return !result.IsValid && 
               result.Value == lastValidValue && 
               result.WasCorrected &&
               result.ErrorMessage != null;
    }

    /// <summary>
    /// Property 4: Resolution Bounds Validation - Invalid high values
    /// Validates: Requirements 1.5, 1.6
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ResolutionBoundsValidation_InvalidHighValues_Rejected(PositiveInt value)
    {
        int testValue = 512 + value.Get; // Always > 512
        int lastValidValue = 128;
        
        // Invalid values (> 512) should be rejected and last valid value preserved
        var result = _resolutionValidator.ValidateResolution(testValue, lastValidValue);
        
        return !result.IsValid && 
               result.Value == lastValidValue && 
               result.WasCorrected &&
               result.ErrorMessage != null;
    }

    /// <summary>
    /// Property 4: Resolution Bounds Validation - Boundary values
    /// Validates: Requirements 1.5
    /// </summary>
    [Fact]
    public void ResolutionBoundsValidation_BoundaryValues()
    {
        // Test minimum boundary (1)
        var minResult = _resolutionValidator.ValidateResolution(1, 128);
        Assert.True(minResult.IsValid);
        Assert.Equal(1, minResult.Value);

        // Test maximum boundary (512)
        var maxResult = _resolutionValidator.ValidateResolution(512, 128);
        Assert.True(maxResult.IsValid);
        Assert.Equal(512, maxResult.Value);

        // Test just below minimum (0)
        var belowMinResult = _resolutionValidator.ValidateResolution(0, 128);
        Assert.False(belowMinResult.IsValid);
        Assert.Equal(128, belowMinResult.Value);

        // Test just above maximum (513)
        var aboveMaxResult = _resolutionValidator.ValidateResolution(513, 128);
        Assert.False(aboveMaxResult.IsValid);
        Assert.Equal(128, aboveMaxResult.Value);
    }

    /// <summary>
    /// Property 4: Resolution pair validation
    /// Validates: Requirements 1.5, 1.6
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ResolutionPairValidation_BothValid(PositiveInt w, PositiveInt h)
    {
        int width = Math.Min(w.Get, 512);
        int height = Math.Min(h.Get, 512);
        
        var result = _resolutionValidator.ValidateResolutionPair(width, height, 128, 16);
        
        return result.IsValid && 
               result.Width == width && 
               result.Height == height;
    }

    #endregion

    #region Property 11: Multi-line Text Height Calculation

    /// <summary>
    /// Property 11: Multi-line Text Height Calculation
    /// For any multi-line text input and font with known line height, the total rendered height 
    /// should equal: (number_of_lines * font_line_height) + ((number_of_lines - 1) * line_spacing).
    /// If this exceeds display height, a warning should be generated.
    /// Validates: Requirements 14.1, 14.2, 14.3, 14.4, 14.5
    /// </summary>
    [Property(MaxTest = 100)]
    public bool MultiLineHeightCalculation_CorrectFormula(PositiveInt lineCountGen, PositiveInt lineSpacingGen)
    {
        int lineCount = Math.Min(lineCountGen.Get, 10);
        int lineSpacing = Math.Min(lineSpacingGen.Get, 10);
        int fontLineHeight = 16;
        
        using var font = CreateTestFont(fontLineHeight);
        
        // Calculate expected height using the formula
        // (number_of_lines * font_line_height) + ((number_of_lines - 1) * line_spacing)
        int expectedHeight = (lineCount * fontLineHeight);
        if (lineCount > 1)
        {
            expectedHeight += (lineCount - 1) * lineSpacing;
        }
        
        int actualHeight = _multiLineRenderer.CalculateMultiLineHeight(font, lineCount, lineSpacing);
        
        return actualHeight == expectedHeight;
    }

    /// <summary>
    /// Property 11: Multi-line rendering produces correct height
    /// Validates: Requirements 14.2, 14.3
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MultiLineRendering_ProducesCorrectHeight(PositiveInt lineCountGen, PositiveInt lineSpacingGen)
    {
        int lineCount = Math.Clamp(lineCountGen.Get, 1, 5);
        int lineSpacing = Math.Min(lineSpacingGen.Get, 5);
        int fontLineHeight = 16;
        
        using var font = CreateTestFont(fontLineHeight);
        
        // Create multi-line text
        var lines = new List<string>();
        for (int i = 0; i < lineCount; i++)
        {
            lines.Add($"Line{i}");
        }
        string text = string.Join("\n", lines);
        
        // Calculate expected height
        int expectedHeight = (lineCount * fontLineHeight);
        if (lineCount > 1)
        {
            expectedHeight += (lineCount - 1) * lineSpacing;
        }
        
        using var rendered = _multiLineRenderer.RenderMultiLineText(font, text, SKColors.White, lineSpacing);
        
        return rendered.Height == expectedHeight;
    }

    /// <summary>
    /// Property 11: Height exceeds display generates warning
    /// Validates: Requirements 14.4
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MultiLineHeightExceedsDisplay_GeneratesWarning(PositiveInt lineCountGen)
    {
        int lineCount = Math.Clamp(lineCountGen.Get, 3, 10);
        int lineSpacing = 2;
        int fontLineHeight = 16;
        int displayHeight = 16; // Single line display
        
        using var font = CreateTestFont(fontLineHeight);
        
        // Create multi-line text that will exceed display height
        var lines = new List<string>();
        for (int i = 0; i < lineCount; i++)
        {
            lines.Add($"Line{i}");
        }
        string text = string.Join("\n", lines);
        
        var result = _multiLineRenderer.RenderMultiLineTextWithInfo(font, text, SKColors.White, lineSpacing, displayHeight);
        
        // Should exceed display height and have warning
        return result.ExceedsDisplayHeight && 
               result.WarningMessage != null &&
               result.TotalHeight > displayHeight;
    }

    /// <summary>
    /// Property 11: Line count detection
    /// Validates: Requirements 14.1
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LineCountDetection_Correct(PositiveInt lineCountGen)
    {
        int expectedLineCount = Math.Clamp(lineCountGen.Get, 1, 20);
        
        // Create text with expected number of lines
        var lines = new List<string>();
        for (int i = 0; i < expectedLineCount; i++)
        {
            lines.Add($"Line{i}");
        }
        string text = string.Join("\n", lines);
        
        int actualLineCount = _multiLineRenderer.GetLineCount(text);
        
        return actualLineCount == expectedLineCount;
    }

    /// <summary>
    /// Property 11: Default line spacing
    /// Validates: Requirements 14.6
    /// </summary>
    [Fact]
    public void DefaultLineSpacing_IsTwo()
    {
        var settings = new DisplaySettings();
        Assert.Equal(2, settings.LineSpacing);
    }

    #endregion

    #region Unit Tests for Edge Cases

    [Fact]
    public void MultiLineRenderer_EmptyText_ReturnsMinimalBitmap()
    {
        using var font = CreateTestFont(16);
        using var result = _multiLineRenderer.RenderMultiLineText(font, "", SKColors.White, 2);
        
        Assert.Equal(1, result.Width);
        Assert.Equal(16, result.Height);
    }

    [Fact]
    public void MultiLineRenderer_SingleLine_CorrectHeight()
    {
        using var font = CreateTestFont(16);
        using var result = _multiLineRenderer.RenderMultiLineText(font, "Hello", SKColors.White, 2);
        
        Assert.Equal(16, result.Height);
    }

    [Fact]
    public void MultiLineRenderer_TwoLines_CorrectHeight()
    {
        using var font = CreateTestFont(16);
        int lineSpacing = 2;
        using var result = _multiLineRenderer.RenderMultiLineText(font, "Hello\nWorld", SKColors.White, lineSpacing);
        
        // Expected: 16 + 2 + 16 = 34
        Assert.Equal(34, result.Height);
    }

    [Fact]
    public void MultiLineRenderer_ThreeLines_CorrectHeight()
    {
        using var font = CreateTestFont(16);
        int lineSpacing = 4;
        using var result = _multiLineRenderer.RenderMultiLineText(font, "Line1\nLine2\nLine3", SKColors.White, lineSpacing);
        
        // Expected: 16 + 4 + 16 + 4 + 16 = 56
        Assert.Equal(56, result.Height);
    }

    [Fact]
    public void ResolutionValidator_ZeroValue_Invalid()
    {
        var result = _resolutionValidator.ValidateResolution(0, 128);
        Assert.False(result.IsValid);
        Assert.Equal(128, result.Value);
    }

    [Fact]
    public void ResolutionValidator_NegativeValue_Invalid()
    {
        var result = _resolutionValidator.ValidateResolution(-10, 128);
        Assert.False(result.IsValid);
        Assert.Equal(128, result.Value);
    }

    [Fact]
    public void ResolutionValidator_ExceedsMax_Invalid()
    {
        var result = _resolutionValidator.ValidateResolution(1000, 128);
        Assert.False(result.IsValid);
        Assert.Equal(128, result.Value);
    }

    [Fact]
    public void GetLineCount_VariousLineEndings()
    {
        // Unix style (\n)
        Assert.Equal(3, _multiLineRenderer.GetLineCount("a\nb\nc"));
        
        // Windows style (\r\n)
        Assert.Equal(3, _multiLineRenderer.GetLineCount("a\r\nb\r\nc"));
        
        // Old Mac style (\r)
        Assert.Equal(3, _multiLineRenderer.GetLineCount("a\rb\rc"));
    }

    #endregion
}
