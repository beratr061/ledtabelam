using System;
using System.Globalization;
using Avalonia.Data.Converters;
using LEDTabelam.Models;

namespace LEDTabelam.Converters;

/// <summary>
/// HorizontalAlignment'ı ComboBox index'ine dönüştürür
/// </summary>
public class HAlignToIndexConverter : IValueConverter
{
    public static readonly HAlignToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            Models.HorizontalAlignment.Left => 0,
            Models.HorizontalAlignment.Center => 1,
            Models.HorizontalAlignment.Right => 2,
            _ => 0
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            0 => Models.HorizontalAlignment.Left,
            1 => Models.HorizontalAlignment.Center,
            2 => Models.HorizontalAlignment.Right,
            _ => Models.HorizontalAlignment.Left
        };
    }
}

/// <summary>
/// VerticalAlignment'ı ComboBox index'ine dönüştürür
/// </summary>
public class VAlignToIndexConverter : IValueConverter
{
    public static readonly VAlignToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            Models.VerticalAlignment.Top => 0,
            Models.VerticalAlignment.Center => 1,
            Models.VerticalAlignment.Bottom => 2,
            _ => 0
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            0 => Models.VerticalAlignment.Top,
            1 => Models.VerticalAlignment.Center,
            2 => Models.VerticalAlignment.Bottom,
            _ => Models.VerticalAlignment.Top
        };
    }
}

/// <summary>
/// ScrollDirection'ı ComboBox index'ine dönüştürür
/// </summary>
public class ScrollDirToIndexConverter : IValueConverter
{
    public static readonly ScrollDirToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            ScrollDirection.Left => 0,
            ScrollDirection.Right => 1,
            ScrollDirection.Up => 2,
            ScrollDirection.Down => 3,
            _ => 0
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            0 => ScrollDirection.Left,
            1 => ScrollDirection.Right,
            2 => ScrollDirection.Up,
            3 => ScrollDirection.Down,
            _ => ScrollDirection.Left
        };
    }
}

/// <summary>
/// ProgramTransitionType'ı ComboBox index'ine dönüştürür
/// Requirements: 3.1
/// </summary>
public class ProgramTransitionToIndexConverter : IValueConverter
{
    public static readonly ProgramTransitionToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            ProgramTransitionType.Direct => 0,
            ProgramTransitionType.Fade => 1,
            ProgramTransitionType.SlideLeft => 2,
            ProgramTransitionType.SlideRight => 3,
            ProgramTransitionType.SlideUp => 4,
            ProgramTransitionType.SlideDown => 5,
            _ => 0
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            0 => ProgramTransitionType.Direct,
            1 => ProgramTransitionType.Fade,
            2 => ProgramTransitionType.SlideLeft,
            3 => ProgramTransitionType.SlideRight,
            4 => ProgramTransitionType.SlideUp,
            5 => ProgramTransitionType.SlideDown,
            _ => ProgramTransitionType.Direct
        };
    }
}

/// <summary>
/// StopAnimationType'ı ComboBox index'ine dönüştürür
/// Requirements: 6.1
/// </summary>
public class StopAnimationToIndexConverter : IValueConverter
{
    public static readonly StopAnimationToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            StopAnimationType.Direct => 0,
            StopAnimationType.Fade => 1,
            StopAnimationType.SlideUp => 2,
            StopAnimationType.SlideDown => 3,
            _ => 0
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            0 => StopAnimationType.Direct,
            1 => StopAnimationType.Fade,
            2 => StopAnimationType.SlideUp,
            3 => StopAnimationType.SlideDown,
            _ => StopAnimationType.Direct
        };
    }
}
