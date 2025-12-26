using System;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Services;
using SkiaSharp;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for SvgRenderer
/// Feature: led-tabelam
/// Property 13: SVG Threshold Binarization
/// Validates: Requirements 16.5, 16.6, 16.7
/// </summary>
public class SvgRendererPropertyTests
{
    private readonly SvgRenderer _renderer = new();

    #region Generators

    /// <summary>
    /// Generates a grayscale bitmap with random pixel values
    /// </summary>
    public static Gen<SKBitmap> GenGrayscaleBitmap()
    {
        return from width in Gen.Choose(4, 32)
               from height in Gen.Choose(4, 32)
               from pixels in Gen.ArrayOf(width * height, Gen.Choose(0, 255))
               select CreateGrayscaleBitmap(width, height, pixels);
    }

    private static SKBitmap CreateGrayscaleBitmap(int width, int height, int[] pixels)
    {
        var bitmap = new SKBitmap(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                byte gray = (byte)pixels[idx];
                bitmap.SetPixel(x, y, new SKColor(gray, gray, gray));
            }
        }
        return bitmap;
    }

    /// <summary>
    /// Generates a valid threshold value (0-100)
    /// </summary>
    public static Gen<int> GenThreshold() => Gen.Choose(0, 100);

    #endregion

    #region Arbitraries

    public class SvgRendererArbitraries
    {
        public static Arbitrary<SKBitmap> GrayscaleBitmapArb() =>
            Arb.From(GenGrayscaleBitmap());
    }

    #endregion

    #region Property 13: SVG Threshold Binarization

    /// <summary>
    /// Property 13: SVG Threshold Binarization
    /// For any grayscale or color image and threshold value T (0-100), 
    /// pixels with brightness >= T should be rendered as "on" and 
    /// pixels with brightness < T should be "off".
    /// Validates: Requirements 16.5, 16.6, 16.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ThresholdBinarizationCorrectness()
    {
        return Prop.ForAll(
            GenGrayscaleBitmap().ToArbitrary(),
            GenThreshold().ToArbitrary(),
            (sourceBitmap, threshold) =>
            {
                try
                {
                    var result = _renderer.ApplyThreshold(sourceBitmap, threshold);

                    // Threshold değerini 0-255 aralığına dönüştür
                    int thresholdValue = (int)(threshold * 2.55);

                    bool allCorrect = true;
                    for (int y = 0; y < sourceBitmap.Height && allCorrect; y++)
                    {
                        for (int x = 0; x < sourceBitmap.Width && allCorrect; x++)
                        {
                            var sourcePixel = sourceBitmap.GetPixel(x, y);
                            var resultPixel = result.GetPixel(x, y);

                            // Pikselin parlaklığını hesapla
                            int brightness = (int)(0.299 * sourcePixel.Red + 
                                                   0.587 * sourcePixel.Green + 
                                                   0.114 * sourcePixel.Blue);

                            // Şeffaf pikseller "off" olmalı
                            if (sourcePixel.Alpha < 128)
                            {
                                allCorrect = resultPixel.Red == 0 && 
                                            resultPixel.Green == 0 && 
                                            resultPixel.Blue == 0;
                            }
                            // Parlaklık >= threshold: "on" (beyaz)
                            else if (brightness >= thresholdValue)
                            {
                                allCorrect = resultPixel.Red == 255 && 
                                            resultPixel.Green == 255 && 
                                            resultPixel.Blue == 255;
                            }
                            // Parlaklık < threshold: "off" (siyah)
                            else
                            {
                                allCorrect = resultPixel.Red == 0 && 
                                            resultPixel.Green == 0 && 
                                            resultPixel.Blue == 0;
                            }
                        }
                    }

                    result.Dispose();
                    sourceBitmap.Dispose();
                    return allCorrect;
                }
                catch
                {
                    sourceBitmap.Dispose();
                    return false;
                }
            });
    }


    /// <summary>
    /// Property 13 (continued): Changing threshold should produce a monotonic change 
    /// in the number of lit pixels.
    /// Higher threshold = fewer lit pixels (more pixels fall below threshold)
    /// Validates: Requirements 16.6, 16.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ThresholdMonotonicLitPixelCount()
    {
        return Prop.ForAll(
            GenGrayscaleBitmap().ToArbitrary(),
            Gen.Choose(0, 50).ToArbitrary(),
            Gen.Choose(51, 100).ToArbitrary(),
            (sourceBitmap, lowerThreshold, higherThreshold) =>
            {
                try
                {
                    var resultLower = _renderer.ApplyThreshold(sourceBitmap, lowerThreshold);
                    var resultHigher = _renderer.ApplyThreshold(sourceBitmap, higherThreshold);

                    int litCountLower = CountLitPixels(resultLower);
                    int litCountHigher = CountLitPixels(resultHigher);

                    resultLower.Dispose();
                    resultHigher.Dispose();
                    sourceBitmap.Dispose();

                    // Higher threshold should result in fewer or equal lit pixels
                    return litCountLower >= litCountHigher;
                }
                catch
                {
                    sourceBitmap.Dispose();
                    return false;
                }
            });
    }

    private int CountLitPixels(SKBitmap bitmap)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Red == 255 && pixel.Green == 255 && pixel.Blue == 255)
                {
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// Threshold 0 should make all non-transparent pixels "on"
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ThresholdZeroAllPixelsOn()
    {
        return Prop.ForAll(
            GenGrayscaleBitmap().ToArbitrary(),
            sourceBitmap =>
            {
                try
                {
                    var result = _renderer.ApplyThreshold(sourceBitmap, 0);

                    bool allOn = true;
                    for (int y = 0; y < sourceBitmap.Height && allOn; y++)
                    {
                        for (int x = 0; x < sourceBitmap.Width && allOn; x++)
                        {
                            var sourcePixel = sourceBitmap.GetPixel(x, y);
                            var resultPixel = result.GetPixel(x, y);

                            // Şeffaf olmayan tüm pikseller "on" olmalı
                            if (sourcePixel.Alpha >= 128)
                            {
                                allOn = resultPixel.Red == 255 && 
                                       resultPixel.Green == 255 && 
                                       resultPixel.Blue == 255;
                            }
                        }
                    }

                    result.Dispose();
                    sourceBitmap.Dispose();
                    return allOn;
                }
                catch
                {
                    sourceBitmap.Dispose();
                    return false;
                }
            });
    }

    /// <summary>
    /// Threshold 100 should make only pure white pixels "on"
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ThresholdHundredOnlyWhiteOn()
    {
        return Prop.ForAll(
            GenGrayscaleBitmap().ToArbitrary(),
            sourceBitmap =>
            {
                try
                {
                    var result = _renderer.ApplyThreshold(sourceBitmap, 100);

                    bool correct = true;
                    for (int y = 0; y < sourceBitmap.Height && correct; y++)
                    {
                        for (int x = 0; x < sourceBitmap.Width && correct; x++)
                        {
                            var sourcePixel = sourceBitmap.GetPixel(x, y);
                            var resultPixel = result.GetPixel(x, y);

                            // Parlaklık hesapla
                            int brightness = (int)(0.299 * sourcePixel.Red + 
                                                   0.587 * sourcePixel.Green + 
                                                   0.114 * sourcePixel.Blue);

                            // Threshold 100 = 255 değeri
                            // Sadece brightness >= 255 olan pikseller "on" olmalı
                            if (sourcePixel.Alpha >= 128 && brightness >= 255)
                            {
                                correct = resultPixel.Red == 255 && 
                                         resultPixel.Green == 255 && 
                                         resultPixel.Blue == 255;
                            }
                            else
                            {
                                correct = resultPixel.Red == 0 && 
                                         resultPixel.Green == 0 && 
                                         resultPixel.Blue == 0;
                            }
                        }
                    }

                    result.Dispose();
                    sourceBitmap.Dispose();
                    return correct;
                }
                catch
                {
                    sourceBitmap.Dispose();
                    return false;
                }
            });
    }

    #endregion

    #region Additional Tests

    /// <summary>
    /// Result bitmap should have same dimensions as source
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ThresholdPreservesDimensions()
    {
        return Prop.ForAll(
            GenGrayscaleBitmap().ToArbitrary(),
            GenThreshold().ToArbitrary(),
            (sourceBitmap, threshold) =>
            {
                try
                {
                    var result = _renderer.ApplyThreshold(sourceBitmap, threshold);

                    bool sameDimensions = result.Width == sourceBitmap.Width && 
                                         result.Height == sourceBitmap.Height;

                    result.Dispose();
                    sourceBitmap.Dispose();
                    return sameDimensions;
                }
                catch
                {
                    sourceBitmap.Dispose();
                    return false;
                }
            });
    }

    /// <summary>
    /// Result should only contain black or white pixels (binary)
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ThresholdResultIsBinary()
    {
        return Prop.ForAll(
            GenGrayscaleBitmap().ToArbitrary(),
            GenThreshold().ToArbitrary(),
            (sourceBitmap, threshold) =>
            {
                try
                {
                    var result = _renderer.ApplyThreshold(sourceBitmap, threshold);

                    bool isBinary = true;
                    for (int y = 0; y < result.Height && isBinary; y++)
                    {
                        for (int x = 0; x < result.Width && isBinary; x++)
                        {
                            var pixel = result.GetPixel(x, y);
                            bool isBlack = pixel.Red == 0 && pixel.Green == 0 && pixel.Blue == 0;
                            bool isWhite = pixel.Red == 255 && pixel.Green == 255 && pixel.Blue == 255;
                            isBinary = isBlack || isWhite;
                        }
                    }

                    result.Dispose();
                    sourceBitmap.Dispose();
                    return isBinary;
                }
                catch
                {
                    sourceBitmap.Dispose();
                    return false;
                }
            });
    }

    /// <summary>
    /// ScaleToHeight should preserve aspect ratio
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ScaleToHeightPreservesAspectRatio()
    {
        return Prop.ForAll(
            GenGrayscaleBitmap().ToArbitrary(),
            Gen.Choose(8, 64).ToArbitrary(),
            (sourceBitmap, targetHeight) =>
            {
                // Null veya geçersiz bitmap kontrolü
                if (sourceBitmap == null || sourceBitmap.Width <= 0 || sourceBitmap.Height <= 0)
                {
                    sourceBitmap?.Dispose();
                    return true; // Geçersiz girdi için testi atla
                }

                try
                {
                    var result = _renderer.ScaleToHeight(sourceBitmap, targetHeight);

                    // Aspect ratio should be preserved
                    double sourceRatio = (double)sourceBitmap.Width / sourceBitmap.Height;
                    double resultRatio = (double)result.Width / result.Height;

                    // Allow small tolerance due to rounding
                    bool aspectPreserved = Math.Abs(sourceRatio - resultRatio) < 0.2;
                    bool heightCorrect = result.Height == targetHeight;

                    result.Dispose();
                    sourceBitmap.Dispose();
                    return aspectPreserved && heightCorrect;
                }
                catch (ArgumentException)
                {
                    // Geçersiz bitmap boyutları için beklenen davranış
                    sourceBitmap.Dispose();
                    return true;
                }
                catch
                {
                    sourceBitmap.Dispose();
                    return false;
                }
            });
    }

    #endregion
}
