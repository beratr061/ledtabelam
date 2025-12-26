using System;
using Avalonia.Media;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using LEDTabelam.Services;
using SkiaSharp;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for Alignment and Text Style services
/// Feature: led-tabelam, Property 14: Alignment Positioning
/// Feature: led-tabelam, Property 19: Stroke Expands Glyph Bounds
/// Validates: Requirements 21.x, 22.x
/// </summary>
public class AlignmentStylePropertyTests
{
    #region Generators

    public static Gen<HorizontalAlignment> GenHAlign()
    {
        return Gen.Elements(HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right);
    }

    public static Gen<VerticalAlignment> GenVAlign()
    {
        return Gen.Elements(VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom);
    }

    public static Gen<(int containerSize, int contentSize)> GenSizePair()
    {
        return from containerSize in Gen.Choose(10, 500)
               from contentSize in Gen.Choose(1, containerSize)
               select (containerSize, contentSize);
    }

    public static Gen<int> GenStrokeWidth()
    {
        return Gen.Choose(1, 3);
    }

    public static Gen<TextStyle> GenTextStyle()
    {
        return from hasBackground in Arb.Generate<bool>()
               from hasStroke in Arb.Generate<bool>()
               from strokeWidth in Gen.Choose(1, 3)
               select new TextStyle
               {
                   HasBackground = hasBackground,
                   BackgroundColor = Colors.Black,
                   HasStroke = hasStroke,
                   StrokeWidth = strokeWidth,
                   StrokeColor = Colors.White
               };
    }

    public static Gen<SKBitmap> GenSmallBitmap()
    {
        return from width in Gen.Choose(5, 50)
               from height in Gen.Choose(5, 30)
               select CreateTestBitmap(width, height);
    }

    private static SKBitmap CreateTestBitmap(int width, int height)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        
        // Draw some white pixels in the center area
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = false };
        int margin = Math.Min(2, Math.Min(width, height) / 4);
        for (int y = margin; y < height - margin; y++)
        {
            for (int x = margin; x < width - margin; x++)
            {
                canvas.DrawPoint(x, y, paint);
            }
        }
        
        return bitmap;
    }

    #endregion

    #region Arbitraries

    public class AlignmentArbitraries
    {
        public static Arbitrary<HorizontalAlignment> HAlignArb() => Arb.From(GenHAlign());
        public static Arbitrary<VerticalAlignment> VAlignArb() => Arb.From(GenVAlign());
        public static Arbitrary<(int containerSize, int contentSize)> SizePairArb() => Arb.From(GenSizePair());
        public static Arbitrary<int> StrokeWidthArb() => Arb.From(GenStrokeWidth());
        public static Arbitrary<TextStyle> TextStyleArb() => Arb.From(GenTextStyle());
        public static Arbitrary<SKBitmap> SmallBitmapArb() => Arb.From(GenSmallBitmap());
    }

    #endregion

    #region Property 14: Alignment Positioning Tests

    /// <summary>
    /// Property 14: Alignment Positioning
    /// For any content with known width/height and alignment settings,
    /// the content position should be calculable and consistent.
    /// Center alignment should place content at (container_size - content_size) / 2.
    /// Validates: Requirements 21.1, 21.2, 21.3, 21.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AlignmentArbitraries) })]
    public bool CenterAlignment_PlacesContentAtMiddle((int containerSize, int contentSize) sizes)
    {
        var service = new AlignmentService();
        
        // Test horizontal center
        int hPos = service.CalculateHorizontalPosition(sizes.containerSize, sizes.contentSize, HorizontalAlignment.Center);
        int expectedHPos = (sizes.containerSize - sizes.contentSize) / 2;
        
        // Test vertical center
        int vPos = service.CalculateVerticalPosition(sizes.containerSize, sizes.contentSize, VerticalAlignment.Center);
        int expectedVPos = (sizes.containerSize - sizes.contentSize) / 2;
        
        return hPos == expectedHPos && vPos == expectedVPos;
    }

    /// <summary>
    /// Property 14: Left alignment places content at position 0
    /// Validates: Requirements 21.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AlignmentArbitraries) })]
    public bool LeftAlignment_PlacesContentAtZero((int containerSize, int contentSize) sizes)
    {
        var service = new AlignmentService();
        int pos = service.CalculateHorizontalPosition(sizes.containerSize, sizes.contentSize, HorizontalAlignment.Left);
        return pos == 0;
    }

    /// <summary>
    /// Property 14: Right alignment places content at container_size - content_size
    /// Validates: Requirements 21.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AlignmentArbitraries) })]
    public bool RightAlignment_PlacesContentAtEnd((int containerSize, int contentSize) sizes)
    {
        var service = new AlignmentService();
        int pos = service.CalculateHorizontalPosition(sizes.containerSize, sizes.contentSize, HorizontalAlignment.Right);
        int expected = sizes.containerSize - sizes.contentSize;
        return pos == expected;
    }

    /// <summary>
    /// Property 14: Top alignment places content at position 0
    /// Validates: Requirements 21.2
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AlignmentArbitraries) })]
    public bool TopAlignment_PlacesContentAtZero((int containerSize, int contentSize) sizes)
    {
        var service = new AlignmentService();
        int pos = service.CalculateVerticalPosition(sizes.containerSize, sizes.contentSize, VerticalAlignment.Top);
        return pos == 0;
    }

    /// <summary>
    /// Property 14: Bottom alignment places content at container_size - content_size
    /// Validates: Requirements 21.2
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AlignmentArbitraries) })]
    public bool BottomAlignment_PlacesContentAtEnd((int containerSize, int contentSize) sizes)
    {
        var service = new AlignmentService();
        int pos = service.CalculateVerticalPosition(sizes.containerSize, sizes.contentSize, VerticalAlignment.Bottom);
        int expected = sizes.containerSize - sizes.contentSize;
        return pos == expected;
    }

    /// <summary>
    /// Property 14: Combined alignment is consistent with individual calculations
    /// Validates: Requirements 21.3, 21.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AlignmentArbitraries) })]
    public bool CombinedAlignment_ConsistentWithIndividual(
        (int containerSize, int contentSize) hSizes,
        (int containerSize, int contentSize) vSizes,
        HorizontalAlignment hAlign,
        VerticalAlignment vAlign)
    {
        var service = new AlignmentService();
        
        var (x, y) = service.CalculatePosition(
            hSizes.containerSize, vSizes.containerSize,
            hSizes.contentSize, vSizes.contentSize,
            hAlign, vAlign);
        
        int expectedX = service.CalculateHorizontalPosition(hSizes.containerSize, hSizes.contentSize, hAlign);
        int expectedY = service.CalculateVerticalPosition(vSizes.containerSize, vSizes.contentSize, vAlign);
        
        return x == expectedX && y == expectedY;
    }

    #endregion

    #region Property 19: Stroke Expands Glyph Bounds Tests

    /// <summary>
    /// Property 19: Stroke Expands Glyph Bounds
    /// For any text with stroke enabled and stroke width W,
    /// the rendered glyph bounds should be expanded by W pixels in all directions
    /// compared to the same text without stroke.
    /// Validates: Requirements 22.4, 22.5, 22.6, 22.7
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AlignmentArbitraries) })]
    public bool StrokeExpandsBounds_ByStrokeWidthInAllDirections(int strokeWidth)
    {
        var service = new TextStyleRenderer();
        
        int originalWidth = 50;
        int originalHeight = 20;
        
        var (expandedWidth, expandedHeight) = service.CalculateStrokeExpandedBounds(
            originalWidth, originalHeight, strokeWidth);
        
        // Stroke should expand by strokeWidth * 2 (both sides)
        int expectedExpansion = strokeWidth * 2;
        
        return expandedWidth == originalWidth + expectedExpansion &&
               expandedHeight == originalHeight + expectedExpansion;
    }

    /// <summary>
    /// Property 19: Stroke width is clamped to 1-3 range
    /// Validates: Requirements 22.5
    /// </summary>
    [Fact]
    public void StrokeWidth_ClampedTo1To3Range()
    {
        var service = new TextStyleRenderer();
        
        // Test with stroke width 0 (should be clamped to 1)
        var style0 = new TextStyle { HasStroke = true, StrokeWidth = 0, StrokeColor = Colors.White };
        var bitmap = CreateTestBitmap(10, 10);
        var result0 = service.ApplyStroke(bitmap, style0);
        
        // With stroke width clamped to 1, expansion should be 2 (1*2)
        Assert.Equal(12, result0.Width);
        Assert.Equal(12, result0.Height);
        
        // Test with stroke width 5 (should be clamped to 3)
        var style5 = new TextStyle { HasStroke = true, StrokeWidth = 5, StrokeColor = Colors.White };
        var result5 = service.ApplyStroke(bitmap, style5);
        
        // With stroke width clamped to 3, expansion should be 6 (3*2)
        Assert.Equal(16, result5.Width);
        Assert.Equal(16, result5.Height);
        
        bitmap.Dispose();
        result0.Dispose();
        result5.Dispose();
    }

    /// <summary>
    /// Property 19: No stroke means no expansion
    /// Validates: Requirements 22.8
    /// </summary>
    [Fact]
    public void NoStroke_NoExpansion()
    {
        var service = new TextStyleRenderer();
        var style = new TextStyle { HasStroke = false, StrokeWidth = 2 };
        var bitmap = CreateTestBitmap(20, 15);
        
        var result = service.ApplyStroke(bitmap, style);
        
        // Should return same bitmap (no expansion)
        Assert.Equal(bitmap.Width, result.Width);
        Assert.Equal(bitmap.Height, result.Height);
        
        bitmap.Dispose();
    }

    /// <summary>
    /// Property 19: Stroke expansion is symmetric
    /// Validates: Requirements 22.6, 22.7
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AlignmentArbitraries) })]
    public bool StrokeExpansion_IsSymmetric(int strokeWidth)
    {
        var service = new TextStyleRenderer();
        
        int originalWidth = 30;
        int originalHeight = 20;
        
        var (expandedWidth, expandedHeight) = service.CalculateStrokeExpandedBounds(
            originalWidth, originalHeight, strokeWidth);
        
        // Expansion should be equal on both sides
        int widthExpansion = expandedWidth - originalWidth;
        int heightExpansion = expandedHeight - originalHeight;
        
        // Both should be even numbers (equal expansion on both sides)
        return widthExpansion % 2 == 0 && heightExpansion % 2 == 0;
    }

    #endregion
}
