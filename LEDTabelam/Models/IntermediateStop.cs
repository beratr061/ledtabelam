using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Ara durak bilgisi - bir metin öğesine bağlı durak noktası
/// Requirements: 4.6
/// </summary>
public class IntermediateStop : ReactiveObject
{
    private int _order;
    private string _stopName = string.Empty;

    /// <summary>
    /// Durak sırası (0'dan başlar)
    /// </summary>
    public int Order
    {
        get => _order;
        set => this.RaiseAndSetIfChanged(ref _order, value);
    }

    /// <summary>
    /// Durak adı
    /// </summary>
    public string StopName
    {
        get => _stopName;
        set => this.RaiseAndSetIfChanged(ref _stopName, value ?? string.Empty);
    }

    /// <summary>
    /// Varsayılan constructor
    /// </summary>
    public IntermediateStop()
    {
    }

    /// <summary>
    /// Parametreli constructor
    /// </summary>
    /// <param name="order">Durak sırası</param>
    /// <param name="stopName">Durak adı</param>
    public IntermediateStop(int order, string stopName)
    {
        _order = order;
        _stopName = stopName ?? string.Empty;
    }
}
