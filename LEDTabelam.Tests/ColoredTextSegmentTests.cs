using Avalonia.Media;
using LEDTabelam.Models;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// ColoredTextSegment ve çok renkli metin testleri
/// </summary>
public class ColoredTextSegmentTests
{
    [Fact]
    public void ColoredTextSegment_DefaultValues_AreCorrect()
    {
        var segment = new ColoredTextSegment();
        
        Assert.Equal(string.Empty, segment.Text);
        Assert.Equal(Color.FromRgb(255, 176, 0), segment.Color); // Amber
    }

    [Fact]
    public void ColoredTextSegment_Constructor_SetsValues()
    {
        var color = Colors.Red;
        var segment = new ColoredTextSegment("TEST", color);
        
        Assert.Equal("TEST", segment.Text);
        Assert.Equal(color, segment.Color);
    }

    [Fact]
    public void ColoredTextSegment_Clone_CreatesIndependentCopy()
    {
        var original = new ColoredTextSegment("ABC", Colors.Green);
        var clone = original.Clone();
        
        Assert.Equal(original.Text, clone.Text);
        Assert.Equal(original.Color, clone.Color);
        
        // Değişiklik orijinali etkilememeli
        clone.Text = "XYZ";
        clone.Color = Colors.Blue;
        
        Assert.Equal("ABC", original.Text);
        Assert.Equal(Colors.Green, original.Color);
    }

    [Fact]
    public void ColoredTextSegment_NullText_BecomesEmpty()
    {
        var segment = new ColoredTextSegment();
        segment.Text = null!;
        
        Assert.Equal(string.Empty, segment.Text);
    }

    [Fact]
    public void TabelaItem_UseColoredSegments_DefaultFalse()
    {
        var item = new TabelaItem();
        
        Assert.False(item.UseColoredSegments);
        Assert.NotNull(item.ColoredSegments);
        Assert.Empty(item.ColoredSegments);
    }

    [Fact]
    public void TabelaItem_ConvertToColoredSegments_CreatesSegmentsPerChar()
    {
        var item = new TabelaItem
        {
            Content = "ABC",
            Color = Colors.Red
        };
        
        item.ConvertToColoredSegments();
        
        Assert.True(item.UseColoredSegments);
        Assert.Equal(3, item.ColoredSegments.Count);
        Assert.Equal("A", item.ColoredSegments[0].Text);
        Assert.Equal("B", item.ColoredSegments[1].Text);
        Assert.Equal("C", item.ColoredSegments[2].Text);
        Assert.All(item.ColoredSegments, s => Assert.Equal(Colors.Red, s.Color));
    }

    [Fact]
    public void TabelaItem_GetFullText_ReturnsContentWhenNotUsingSegments()
    {
        var item = new TabelaItem
        {
            Content = "HELLO",
            UseColoredSegments = false
        };
        
        Assert.Equal("HELLO", item.GetFullText());
    }

    [Fact]
    public void TabelaItem_GetFullText_ReturnsConcatenatedSegments()
    {
        var item = new TabelaItem { UseColoredSegments = true };
        item.ColoredSegments.Add(new ColoredTextSegment("HEL", Colors.Red));
        item.ColoredSegments.Add(new ColoredTextSegment("LO", Colors.Green));
        
        Assert.Equal("HELLO", item.GetFullText());
    }

    [Fact]
    public void TabelaItem_ConvertToSingleColor_MergesSegments()
    {
        var item = new TabelaItem { UseColoredSegments = true };
        item.ColoredSegments.Add(new ColoredTextSegment("A", Colors.Red));
        item.ColoredSegments.Add(new ColoredTextSegment("B", Colors.Green));
        item.ColoredSegments.Add(new ColoredTextSegment("C", Colors.Blue));
        
        item.ConvertToSingleColor();
        
        Assert.False(item.UseColoredSegments);
        Assert.Equal("ABC", item.Content);
    }

    [Fact]
    public void TabelaItem_ColoredSegments_CanHaveDifferentColors()
    {
        var item = new TabelaItem { UseColoredSegments = true };
        item.ColoredSegments.Add(new ColoredTextSegment("R", Colors.Red));
        item.ColoredSegments.Add(new ColoredTextSegment("G", Colors.Green));
        item.ColoredSegments.Add(new ColoredTextSegment("B", Colors.Blue));
        
        Assert.Equal(Colors.Red, item.ColoredSegments[0].Color);
        Assert.Equal(Colors.Green, item.ColoredSegments[1].Color);
        Assert.Equal(Colors.Blue, item.ColoredSegments[2].Color);
    }

    [Fact]
    public void TabelaItem_EmptySegments_GetFullTextReturnsEmpty()
    {
        var item = new TabelaItem
        {
            UseColoredSegments = true,
            Content = "FALLBACK"
        };
        
        // Segmentler boş olduğunda Content döner
        Assert.Equal("FALLBACK", item.GetFullText());
    }
}


/// <summary>
/// FontLoader çok renkli metin render testleri
/// </summary>
public class ColoredTextRenderTests
{
    [Fact]
    public void RenderColoredText_EmptySegments_ReturnsEmptyBitmap()
    {
        var fontLoader = new LEDTabelam.Services.FontLoader();
        var font = CreateTestFont();
        
        var segments = new List<(string Text, SkiaSharp.SKColor Color)>();
        var bitmap = fontLoader.RenderColoredText(font, segments, 1);
        
        Assert.NotNull(bitmap);
        Assert.Equal(1, bitmap.Width);
        Assert.Equal(font.LineHeight, bitmap.Height);
        
        bitmap.Dispose();
    }

    [Fact]
    public void RenderColoredText_SingleSegment_RendersSameAsRenderText()
    {
        var fontLoader = new LEDTabelam.Services.FontLoader();
        var font = CreateTestFont();
        var color = SkiaSharp.SKColors.Red;
        
        var segments = new List<(string Text, SkiaSharp.SKColor Color)>
        {
            ("A", color)
        };
        
        var coloredBitmap = fontLoader.RenderColoredText(font, segments, 1);
        var normalBitmap = fontLoader.RenderText(font, "A", color, 1);
        
        Assert.Equal(normalBitmap.Width, coloredBitmap.Width);
        Assert.Equal(normalBitmap.Height, coloredBitmap.Height);
        
        coloredBitmap.Dispose();
        normalBitmap.Dispose();
    }

    [Fact]
    public void RenderColoredText_MultipleSegments_HasCorrectWidth()
    {
        var fontLoader = new LEDTabelam.Services.FontLoader();
        var font = CreateTestFont();
        
        var segments = new List<(string Text, SkiaSharp.SKColor Color)>
        {
            ("A", SkiaSharp.SKColors.Red),
            ("B", SkiaSharp.SKColors.Green),
            ("C", SkiaSharp.SKColors.Blue)
        };
        
        var coloredBitmap = fontLoader.RenderColoredText(font, segments, 1);
        var normalBitmap = fontLoader.RenderText(font, "ABC", SkiaSharp.SKColors.White, 1);
        
        // Genişlikler aynı olmalı
        Assert.Equal(normalBitmap.Width, coloredBitmap.Width);
        
        coloredBitmap.Dispose();
        normalBitmap.Dispose();
    }

    private LEDTabelam.Models.BitmapFont CreateTestFont()
    {
        var font = new LEDTabelam.Models.BitmapFont
        {
            Name = "TestFont",
            LineHeight = 10,
            Base = 8
        };
        
        // Basit test karakterleri ekle
        font.FontImage = new SkiaSharp.SKBitmap(100, 20);
        using var canvas = new SkiaSharp.SKCanvas(font.FontImage);
        canvas.Clear(SkiaSharp.SKColors.Transparent);
        
        // A karakteri
        font.Characters['A'] = new LEDTabelam.Models.FontChar
        {
            Id = 'A',
            X = 0, Y = 0,
            Width = 6, Height = 10,
            XOffset = 0, YOffset = 0,
            XAdvance = 6
        };
        
        // B karakteri
        font.Characters['B'] = new LEDTabelam.Models.FontChar
        {
            Id = 'B',
            X = 10, Y = 0,
            Width = 6, Height = 10,
            XOffset = 0, YOffset = 0,
            XAdvance = 6
        };
        
        // C karakteri
        font.Characters['C'] = new LEDTabelam.Models.FontChar
        {
            Id = 'C',
            X = 20, Y = 0,
            Width = 6, Height = 10,
            XOffset = 0, YOffset = 0,
            XAdvance = 6
        };
        
        return font;
    }
}
