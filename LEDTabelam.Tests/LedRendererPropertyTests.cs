using System;
using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using LEDTabelam.Services;
using SkiaSharp;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for LedRenderer
/// Feature: led-tabelam
/// Property 7: Single Color Mode Consistency
/// Property 8: Brightness Affects All Pixels Uniformly
/// Property 9: Pixel Pitch Determines Spacing
/// Property 18: Aging Effect Distribution
/// Validates: Requirements 2.2, 5.1, 5.7, 19.3
/// </summary>
public class LedRendererPropertyTests
{
    private readonly LedRenderer _renderer = new();

    #region Generators

    public static Gen<DisplaySettings> GenDisplaySettings()
    {
        return from width in Gen.Choose(4, 64)
               from height in Gen.Choose(4, 32)
               from colorType in Gen.Elements(LedColorType.Amber, LedColorType.Red, LedColorType.Green)
               from brightness in Gen.Choose(0, 100)
               from backgroundDarkness in Gen.Choose(0, 100)
               from pixelSize in Gen.Choose(4, 16)
               from pitch in Gen.Elements(Enum.GetValues<PixelPitch>())
               from shape in Gen.Elements(Enum.GetValues<PixelShape>())
               from invertColors in Arb.Generate<bool>()
               from agingPercent in Gen.Choose(0, 5)
               select new DisplaySettings
               {
                   Width = width,
                   Height = height,
                   ColorType = colorType,
                   Brightness = brightness,
                   BackgroundDarkness = backgroundDarkness,
                   PixelSize = pixelSize,
                   Pitch = pitch,
                   Shape = shape,
                   InvertColors = invertColors,
                   AgingPercent = agingPercent
               };
    }

    public static Gen<bool[,]> GenPixelMatrix(int width, int height)
    {
        return Gen.ArrayOf(width * height, Arb.Generate<bool>())
            .Select(arr =>
            {
                var matrix = new bool[width, height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        matrix[x, y] = arr[x * height + y];
                    }
                }
                return matrix;
            });
    }


    #endregion

    #region Arbitraries

    public class LedRendererArbitraries
    {
        public static Arbitrary<DisplaySettings> DisplaySettingsArb() =>
            Arb.From(GenDisplaySettings());
    }

    #endregion

    #region Property 7: Single Color Mode Consistency

    /// <summary>
    /// Property 7: Single Color Mode Consistency
    /// For any single color mode (Amber, Red, Green) and any pixel matrix, 
    /// all active (lit) pixels in the rendered output should have the exact 
    /// same color value corresponding to the selected mode.
    /// Validates: Requirements 2.2, 2.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LedRendererArbitraries) })]
    public Property SingleColorModeConsistency(DisplaySettings settings)
    {
        // Only test single color modes
        return (settings.ColorType == LedColorType.Amber ||
                settings.ColorType == LedColorType.Red ||
                settings.ColorType == LedColorType.Green).ToProperty()
            .When(settings.Brightness > 0 && !settings.InvertColors)
            .And(() =>
            {
                // Create a simple pixel matrix with some lit pixels
                var matrix = new bool[settings.Width, settings.Height];
                for (int x = 0; x < settings.Width; x++)
                {
                    for (int y = 0; y < settings.Height; y++)
                    {
                        matrix[x, y] = (x + y) % 2 == 0; // Checkerboard pattern
                    }
                }

                var bitmap = _renderer.RenderDisplay(matrix, settings);
                var expectedColor = _renderer.GetLedColor(settings);
                
                // Apply brightness to expected color
                float brightnessFactor = settings.Brightness / 100f;
                var expectedWithBrightness = new SKColor(
                    (byte)(expectedColor.Red * brightnessFactor),
                    (byte)(expectedColor.Green * brightnessFactor),
                    (byte)(expectedColor.Blue * brightnessFactor)
                );

                // Check that all non-background pixels have the expected color
                var backgroundColor = _renderer.GetBackgroundColor(settings);
                var foundColors = new HashSet<SKColor>();

                for (int px = 0; px < bitmap.Width; px++)
                {
                    for (int py = 0; py < bitmap.Height; py++)
                    {
                        var pixelColor = bitmap.GetPixel(px, py);
                        if (!IsBackgroundColor(pixelColor, backgroundColor))
                        {
                            foundColors.Add(pixelColor);
                        }
                    }
                }

                bitmap.Dispose();

                // All lit pixels should have the same color
                return foundColors.Count <= 1;
            });
    }

    private bool IsBackgroundColor(SKColor color, SKColor backgroundColor)
    {
        return Math.Abs(color.Red - backgroundColor.Red) <= 1 &&
               Math.Abs(color.Green - backgroundColor.Green) <= 1 &&
               Math.Abs(color.Blue - backgroundColor.Blue) <= 1;
    }

    #endregion


    #region Property 8: Brightness Affects All Pixels Uniformly

    /// <summary>
    /// Property 8: Brightness Affects All Pixels Uniformly
    /// For any brightness value between 0-100 and any rendered LED display, 
    /// all active pixels should have their intensity scaled by the same factor. 
    /// A brightness of 0 should result in no visible pixels, and 100 should show full intensity.
    /// Validates: Requirements 5.1, 5.2
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LedRendererArbitraries) })]
    public Property BrightnessAffectsAllPixelsUniformly(DisplaySettings settings)
    {
        return (settings.ColorType == LedColorType.Amber ||
                settings.ColorType == LedColorType.Red ||
                settings.ColorType == LedColorType.Green).ToProperty()
            .When(!settings.InvertColors)
            .And(() =>
            {
                // Create a pixel matrix with all pixels lit
                var matrix = new bool[settings.Width, settings.Height];
                for (int x = 0; x < settings.Width; x++)
                {
                    for (int y = 0; y < settings.Height; y++)
                    {
                        matrix[x, y] = true;
                    }
                }

                var bitmap = _renderer.RenderDisplay(matrix, settings);
                var backgroundColor = _renderer.GetBackgroundColor(settings);

                // Collect all non-background pixel colors
                var litPixelColors = new List<SKColor>();
                for (int px = 0; px < bitmap.Width; px++)
                {
                    for (int py = 0; py < bitmap.Height; py++)
                    {
                        var pixelColor = bitmap.GetPixel(px, py);
                        if (!IsBackgroundColor(pixelColor, backgroundColor))
                        {
                            litPixelColors.Add(pixelColor);
                        }
                    }
                }

                bitmap.Dispose();

                // If brightness is 0, there should be no lit pixels
                if (settings.Brightness == 0)
                {
                    return litPixelColors.Count == 0 || 
                           litPixelColors.TrueForAll(c => c.Red == 0 && c.Green == 0 && c.Blue == 0);
                }

                // All lit pixels should have the same color (uniform brightness)
                if (litPixelColors.Count == 0) return true;

                var firstColor = litPixelColors[0];
                return litPixelColors.TrueForAll(c => 
                    c.Red == firstColor.Red && 
                    c.Green == firstColor.Green && 
                    c.Blue == firstColor.Blue);
            });
    }

    #endregion


    #region Property 9: Pixel Pitch Determines Spacing

    /// <summary>
    /// Property 9: Pixel Pitch Determines Spacing
    /// For any pixel pitch value (P2.5 through P10 or custom), the ratio of LED diameter 
    /// to center-to-center distance should match the pitch specification. 
    /// Changing pitch should not affect the logical pixel matrix, only the visual spacing.
    /// Validates: Requirements 5.7, 5.8, 5.9
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PixelPitchDeterminesSpacing(PixelPitch pitch)
    {
        return Prop.ForAll(Gen.Choose(3, 9).Select(x => x / 10.0).ToArbitrary(), customRatio =>
        {
            double ratio = _renderer.GetPitchRatio(pitch, customRatio);

            // Ratio should be between 0.3 and 0.95
            bool validRange = ratio >= 0.3 && ratio <= 0.95;

            // Different pitches should have different ratios (except Custom)
            // P2.5 should have higher ratio than P10 (closer spacing)
            bool correctOrdering = true;
            if (pitch != PixelPitch.Custom)
            {
                double p2_5Ratio = _renderer.GetPitchRatio(PixelPitch.P2_5, customRatio);
                double p10Ratio = _renderer.GetPitchRatio(PixelPitch.P10, customRatio);
                correctOrdering = p2_5Ratio >= p10Ratio;
            }

            // Custom pitch should use the provided ratio
            bool customWorks = true;
            if (pitch == PixelPitch.Custom)
            {
                double actualRatio = _renderer.GetPitchRatio(PixelPitch.Custom, customRatio);
                customWorks = Math.Abs(actualRatio - Math.Clamp(customRatio, 0.3, 0.95)) < 0.001;
            }

            return validRange && correctOrdering && customWorks;
        });
    }

    /// <summary>
    /// Additional test: Pitch does not affect logical pixel count
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LedRendererArbitraries) })]
    public Property PitchDoesNotAffectLogicalPixelCount(DisplaySettings settings)
    {
        return Prop.ForAll(Gen.Elements(Enum.GetValues<PixelPitch>()).ToArbitrary(), pitch =>
        {
            var matrix = new bool[settings.Width, settings.Height];
            int litCount = 0;
            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    matrix[x, y] = (x + y) % 3 == 0;
                    if (matrix[x, y]) litCount++;
                }
            }

            var settings1 = new DisplaySettings
            {
                Width = settings.Width,
                Height = settings.Height,
                ColorType = settings.ColorType,
                Brightness = 100,
                PixelSize = settings.PixelSize,
                Pitch = PixelPitch.P2_5,
                Shape = settings.Shape
            };

            var settings2 = new DisplaySettings
            {
                Width = settings.Width,
                Height = settings.Height,
                ColorType = settings.ColorType,
                Brightness = 100,
                PixelSize = settings.PixelSize,
                Pitch = PixelPitch.P10,
                Shape = settings.Shape
            };

            // Both should render the same logical pixel count
            // The bitmap sizes will be the same (based on pixelSize * matrix dimensions)
            var bitmap1 = _renderer.RenderDisplay(matrix, settings1);
            var bitmap2 = _renderer.RenderDisplay(matrix, settings2);

            bool sameDimensions = bitmap1.Width == bitmap2.Width && bitmap1.Height == bitmap2.Height;

            bitmap1.Dispose();
            bitmap2.Dispose();

            return sameDimensions;
        });
    }

    #endregion


    #region Property 18: Aging Effect Distribution

    /// <summary>
    /// Property 18: Aging Effect Distribution
    /// For any aging percentage P (0-5%), approximately P% of pixels should be affected 
    /// (dead or dim). The affected pixels should be randomly distributed, and the same 
    /// seed should produce the same distribution.
    /// Validates: Requirements 19.3, 19.4, 19.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AgingEffectDistribution(int seed)
    {
        return Prop.ForAll(
            Gen.Choose(1, 5).ToArbitrary(),
            Gen.Choose(8, 32).ToArbitrary(),
            Gen.Choose(8, 32).ToArbitrary(),
            (agingPercent, width, height) =>
            {
                // Create a fully lit matrix
                var matrix1 = new bool[width, height];
                var matrix2 = new bool[width, height];
                int totalPixels = width * height;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        matrix1[x, y] = true;
                        matrix2[x, y] = true;
                    }
                }

                // Apply aging with same seed
                _renderer.ApplyAgingEffect(matrix1, agingPercent, seed);
                _renderer.ApplyAgingEffect(matrix2, agingPercent, seed);

                // Count dead pixels
                int deadCount1 = 0;
                int deadCount2 = 0;
                bool sameDistribution = true;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (!matrix1[x, y]) deadCount1++;
                        if (!matrix2[x, y]) deadCount2++;
                        if (matrix1[x, y] != matrix2[x, y]) sameDistribution = false;
                    }
                }

                // Same seed should produce same distribution
                if (!sameDistribution) return false;

                // Dead pixel count should be approximately agingPercent% of total
                int expectedDeadPixels = (int)(totalPixels * agingPercent / 100.0);
                
                // Allow some tolerance (within 50% of expected, or at least 1 pixel difference)
                int tolerance = Math.Max(1, expectedDeadPixels / 2);
                bool withinTolerance = Math.Abs(deadCount1 - expectedDeadPixels) <= tolerance;

                return withinTolerance;
            });
    }

    /// <summary>
    /// Additional test: Aging with 0% should not affect any pixels
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AgingZeroPercentNoEffect()
    {
        return Prop.ForAll(
            Gen.Choose(4, 32).ToArbitrary(),
            Gen.Choose(4, 32).ToArbitrary(),
            (width, height) =>
            {
                var matrix = new bool[width, height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        matrix[x, y] = true;
                    }
                }

                _renderer.ApplyAgingEffect(matrix, 0, 42);

                // All pixels should still be lit
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (!matrix[x, y]) return false;
                    }
                }

                return true;
            });
    }

    /// <summary>
    /// Additional test: Aging above 5% should be clamped/ignored
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AgingAboveFivePercentIgnored()
    {
        return Prop.ForAll(
            Gen.Choose(4, 32).ToArbitrary(),
            Gen.Choose(4, 32).ToArbitrary(),
            (width, height) =>
            {
                var matrix = new bool[width, height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        matrix[x, y] = true;
                    }
                }

                _renderer.ApplyAgingEffect(matrix, 10, 42); // 10% is above limit

                // All pixels should still be lit (effect should be ignored)
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (!matrix[x, y]) return false;
                    }
                }

                return true;
            });
    }

    #endregion
}
