using LEDTabelam.Models;
using Avalonia.Media;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// BorderSettings model testleri
/// </summary>
public class BorderSettingsTests
{
    [Fact]
    public void BorderSettings_DefaultValues_AreCorrect()
    {
        var border = new BorderSettings();
        
        Assert.False(border.IsEnabled);
        Assert.Equal(1, border.HorizontalLines);
        Assert.Equal(1, border.VerticalLines);
        Assert.Equal(1, border.Padding);
        Assert.Equal(Color.FromRgb(255, 0, 0), border.Color);
    }

    [Fact]
    public void BorderSettings_CreateDefault_ReturnsCorrectValues()
    {
        var border = BorderSettings.CreateDefault();
        
        Assert.False(border.IsEnabled);
        Assert.Equal(1, border.HorizontalLines);
        Assert.Equal(1, border.VerticalLines);
        Assert.Equal(1, border.Padding);
    }

    [Fact]
    public void BorderSettings_Clone_CreatesIndependentCopy()
    {
        var original = new BorderSettings
        {
            IsEnabled = true,
            HorizontalLines = 2,
            VerticalLines = 3,
            Padding = 4,
            Color = Color.FromRgb(0, 255, 0)
        };
        
        var clone = original.Clone();
        
        Assert.Equal(original.IsEnabled, clone.IsEnabled);
        Assert.Equal(original.HorizontalLines, clone.HorizontalLines);
        Assert.Equal(original.VerticalLines, clone.VerticalLines);
        Assert.Equal(original.Padding, clone.Padding);
        Assert.Equal(original.Color, clone.Color);
        
        // Bağımsızlık testi
        clone.HorizontalLines = 5;
        Assert.NotEqual(original.HorizontalLines, clone.HorizontalLines);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    [InlineData(6, 5)]
    [InlineData(10, 5)]
    public void BorderSettings_HorizontalLines_ClampedTo1To5(int input, int expected)
    {
        var border = new BorderSettings { HorizontalLines = input };
        Assert.Equal(expected, border.HorizontalLines);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    [InlineData(6, 5)]
    public void BorderSettings_VerticalLines_ClampedTo1To5(int input, int expected)
    {
        var border = new BorderSettings { VerticalLines = input };
        Assert.Equal(expected, border.VerticalLines);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    [InlineData(11, 10)]
    public void BorderSettings_Padding_ClampedTo0To10(int input, int expected)
    {
        var border = new BorderSettings { Padding = input };
        Assert.Equal(expected, border.Padding);
    }

    [Fact]
    public void BorderSettings_TotalThickness_CalculatedCorrectly()
    {
        var border = new BorderSettings
        {
            HorizontalLines = 2,
            VerticalLines = 3,
            Padding = 4
        };
        
        Assert.Equal(6, border.TotalHorizontalThickness); // 2 + 4
        Assert.Equal(7, border.TotalVerticalThickness);   // 3 + 4
    }

    [Fact]
    public void Zone_Border_DefaultIsNotNull()
    {
        var zone = new Zone();
        Assert.NotNull(zone.Border);
        Assert.False(zone.Border.IsEnabled);
    }

    [Fact]
    public void Zone_Border_CanBeSet()
    {
        var zone = new Zone();
        var border = new BorderSettings
        {
            IsEnabled = true,
            HorizontalLines = 2,
            Color = Color.FromRgb(255, 0, 0)
        };
        
        zone.Border = border;
        
        Assert.True(zone.Border.IsEnabled);
        Assert.Equal(2, zone.Border.HorizontalLines);
    }
}
