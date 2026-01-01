using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Ara durak bilgisi - bir metin öğesine bağlı durak noktası
/// </summary>
public partial class IntermediateStop : ObservableObject
{
    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private string _stopName = string.Empty;

    public IntermediateStop() { }

    public IntermediateStop(int order, string stopName)
    {
        _order = order;
        _stopName = stopName ?? string.Empty;
    }
}
