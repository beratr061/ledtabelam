using System.Collections.Generic;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Tabela slot tanımı (001-999 arası numaralı)
/// Requirements: 20.1, 20.2
/// </summary>
public class TabelaSlot : ReactiveObject
{
    private int _slotNumber;
    private string _routeNumber = string.Empty;
    private string _routeText = string.Empty;
    private string? _iconPath;
    private List<Zone> _zones = new();
    private TextStyle _textStyle = new();
    private Models.HorizontalAlignment _hAlign = Models.HorizontalAlignment.Center;
    private Models.VerticalAlignment _vAlign = Models.VerticalAlignment.Center;

    /// <summary>
    /// Slot numarası (1-999)
    /// </summary>
    public int SlotNumber
    {
        get => _slotNumber;
        set => this.RaiseAndSetIfChanged(ref _slotNumber, value);
    }

    /// <summary>
    /// Hat numarası (örn: "34", "19K", "M1")
    /// </summary>
    public string RouteNumber
    {
        get => _routeNumber;
        set => this.RaiseAndSetIfChanged(ref _routeNumber, value);
    }

    /// <summary>
    /// Güzergah metni (örn: "Zincirlikuyu - Söğütlüçeşme")
    /// </summary>
    public string RouteText
    {
        get => _routeText;
        set => this.RaiseAndSetIfChanged(ref _routeText, value);
    }

    /// <summary>
    /// İkon/logo dosya yolu (opsiyonel)
    /// </summary>
    public string? IconPath
    {
        get => _iconPath;
        set => this.RaiseAndSetIfChanged(ref _iconPath, value);
    }

    /// <summary>
    /// Slot'a özgü zone tanımları
    /// </summary>
    public List<Zone> Zones
    {
        get => _zones;
        set => this.RaiseAndSetIfChanged(ref _zones, value);
    }

    /// <summary>
    /// Metin stil ayarları
    /// </summary>
    public TextStyle TextStyle
    {
        get => _textStyle;
        set => this.RaiseAndSetIfChanged(ref _textStyle, value);
    }

    /// <summary>
    /// Yatay hizalama
    /// </summary>
    public Models.HorizontalAlignment HAlign
    {
        get => _hAlign;
        set => this.RaiseAndSetIfChanged(ref _hAlign, value);
    }

    /// <summary>
    /// Dikey hizalama
    /// </summary>
    public Models.VerticalAlignment VAlign
    {
        get => _vAlign;
        set => this.RaiseAndSetIfChanged(ref _vAlign, value);
    }

    /// <summary>
    /// Slot'un tanımlı olup olmadığını kontrol eder
    /// </summary>
    public bool IsDefined => !string.IsNullOrEmpty(RouteNumber) || !string.IsNullOrEmpty(RouteText);
}
