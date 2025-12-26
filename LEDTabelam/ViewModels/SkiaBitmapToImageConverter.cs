using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace LEDTabelam.ViewModels;

/// <summary>
/// SKBitmap'i Avalonia Bitmap'e dönüştüren converter
/// </summary>
public class SkiaBitmapToImageConverter : IValueConverter
{
    public static readonly SkiaBitmapToImageConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SKBitmap skBitmap)
        {
            try
            {
                using var image = SKImage.FromBitmap(skBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = new MemoryStream();
                data.SaveTo(stream);
                stream.Position = 0;
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
