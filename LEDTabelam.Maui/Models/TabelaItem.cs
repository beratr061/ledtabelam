using System;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Tabela üzerindeki tek bir öğe (metin, sembol vb.)
/// </summary>
public partial class TabelaItem : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private TabelaItemType _itemType = TabelaItemType.Text;

    [ObservableProperty]
    private int _x = 0;

    [ObservableProperty]
    private int _y = 0;

    [ObservableProperty]
    private int _width = 50;

    [ObservableProperty]
    private int _height = 16;

    [ObservableProperty]
    private Color _color = Color.FromRgb(255, 176, 0);

    [ObservableProperty]
    private bool _useColoredSegments = false;

    [ObservableProperty]
    private ObservableCollection<ColoredTextSegment> _coloredSegments = new();

    [ObservableProperty]
    private HorizontalAlignment _hAlign = HorizontalAlignment.Left;

    [ObservableProperty]
    private VerticalAlignment _vAlign = VerticalAlignment.Center;

    [ObservableProperty]
    private string _fontName = "PolarisRGB6x10M";

    [ObservableProperty]
    private int _letterSpacing = 1;

    [ObservableProperty]
    private string _symbolName = string.Empty;

    [ObservableProperty]
    private int _symbolSize = 16;

    [ObservableProperty]
    private bool _isScrolling = false;

    [ObservableProperty]
    private ScrollDirection _scrollDirection = ScrollDirection.Left;

    [ObservableProperty]
    private int _scrollSpeed = 20;

    [ObservableProperty]
    private double _scrollOffset = 0;

    [ObservableProperty]
    private BorderSettings _border = new();

    [ObservableProperty]
    private bool _isSelected = false;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private IntermediateStopSettings _intermediateStops = new();

    partial void OnLetterSpacingChanging(int value)
    {
        if (value < 0) _letterSpacing = 0;
        else if (value > 20) _letterSpacing = 20;
    }

    partial void OnSymbolSizeChanging(int value)
    {
        if (value < 16) _symbolSize = 16;
        else if (value > 32) _symbolSize = 32;
    }

    public string GetFullText()
    {
        if (!UseColoredSegments || ColoredSegments.Count == 0)
            return Content;

        var sb = new StringBuilder();
        foreach (var segment in ColoredSegments)
        {
            sb.Append(segment.Text);
        }
        return sb.ToString();
    }

    public void ConvertToColoredSegments()
    {
        ColoredSegments.Clear();
        foreach (char c in Content)
        {
            ColoredSegments.Add(new ColoredTextSegment(c.ToString(), Color));
        }
        UseColoredSegments = true;
    }

    public void ConvertToSingleColor()
    {
        Content = GetFullText();
        UseColoredSegments = false;
    }

    public void ResetScrollOffset()
    {
        ScrollOffset = 0;
    }

    public void UpdateScrollOffset(double deltaTime, int contentWidth, int contentHeight)
    {
        if (!IsScrolling) return;

        double speed = ScrollSpeed * deltaTime;

        switch (ScrollDirection)
        {
            case ScrollDirection.Left:
                ScrollOffset -= speed;
                if (ScrollOffset < -contentWidth)
                    ScrollOffset = Width;
                break;

            case ScrollDirection.Right:
                ScrollOffset += speed;
                if (ScrollOffset > Width)
                    ScrollOffset = -contentWidth;
                break;

            case ScrollDirection.Up:
                ScrollOffset -= speed;
                if (ScrollOffset < -contentHeight)
                    ScrollOffset = Height;
                break;

            case ScrollDirection.Down:
                ScrollOffset += speed;
                if (ScrollOffset > Height)
                    ScrollOffset = -contentHeight;
                break;
        }
    }

    public bool HasIntermediateStops =>
        IntermediateStops.IsEnabled && IntermediateStops.Stops.Count > 0;

    public int Right => X + Width;
    public int Bottom => Y + Height;
}
