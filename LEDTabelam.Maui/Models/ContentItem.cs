using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// İçerik tipi seçenekleri
/// </summary>
public enum ContentType
{
    Text,
    Image,
    Clock,
    Date,
    Countdown
}

/// <summary>
/// İçerik öğesi base sınıfı
/// </summary>
public partial class ContentItem : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "İçerik";

    [ObservableProperty]
    private ContentType _contentType = ContentType.Text;

    [ObservableProperty]
    private int _x = 0;

    [ObservableProperty]
    private int _y = 0;

    [ObservableProperty]
    private int _width = 128;

    [ObservableProperty]
    private int _height = 16;

    [ObservableProperty]
    private EffectConfig _entryEffect = new();

    [ObservableProperty]
    private EffectConfig _exitEffect = new();

    [ObservableProperty]
    private int _durationMs = 3000;

    [ObservableProperty]
    private bool _showImmediately = true;

    [ObservableProperty]
    private bool _isSelected;
}
