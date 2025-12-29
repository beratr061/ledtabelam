using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Media;
using ReactiveUI;
using LEDTabelam.Models;
using LEDTabelam.Services;

namespace LEDTabelam.ViewModels;

/// <summary>
/// Slot düzenleyici ViewModel'i
/// Requirements: 20.9, 21.1, 21.2, 21.3, 22.1, 22.4
/// </summary>
public class SlotEditorViewModel : ViewModelBase
{
    private readonly ISlotManager _slotManager;

    private int _slotNumber;
    private string _routeNumber = string.Empty;
    private string _routeText = string.Empty;
    private string? _iconPath;
    private Models.HorizontalAlignment _horizontalAlignment = Models.HorizontalAlignment.Center;
    private Models.VerticalAlignment _verticalAlignment = Models.VerticalAlignment.Center;
    private bool _hasBackground = false;
    private Color _backgroundColor = Colors.Black;
    private bool _hasStroke = false;
    private int _strokeWidth = 1;
    private Color _strokeColor = Colors.Black;
    private bool _isDirty = false;
    private bool _isNewSlot = false;

    #region Slot Properties

    /// <summary>
    /// Slot numarası (1-999)
    /// </summary>
    public int SlotNumber
    {
        get => _slotNumber;
        private set => this.RaiseAndSetIfChanged(ref _slotNumber, value);
    }

    /// <summary>
    /// Slot numarası formatlanmış (001-999)
    /// </summary>
    public string SlotNumberFormatted => $"{SlotNumber:D3}";

    /// <summary>
    /// Hat numarası (örn: "34", "19K", "M1")
    /// </summary>
    public string RouteNumber
    {
        get => _routeNumber;
        set
        {
            this.RaiseAndSetIfChanged(ref _routeNumber, value);
            IsDirty = true;
        }
    }

    /// <summary>
    /// Güzergah metni
    /// </summary>
    public string RouteText
    {
        get => _routeText;
        set
        {
            this.RaiseAndSetIfChanged(ref _routeText, value);
            IsDirty = true;
        }
    }

    /// <summary>
    /// İkon/logo dosya yolu
    /// </summary>
    public string? IconPath
    {
        get => _iconPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _iconPath, value);
            IsDirty = true;
        }
    }

    /// <summary>
    /// Slot'un tanımlı olup olmadığı
    /// </summary>
    public bool IsDefined => !string.IsNullOrEmpty(RouteNumber) || !string.IsNullOrEmpty(RouteText);

    /// <summary>
    /// Yeni slot mu (henüz kaydedilmemiş)
    /// </summary>
    public bool IsNewSlot
    {
        get => _isNewSlot;
        private set => this.RaiseAndSetIfChanged(ref _isNewSlot, value);
    }

    #endregion

    #region Alignment Properties

    /// <summary>
    /// Yatay hizalama
    /// </summary>
    public Models.HorizontalAlignment HorizontalAlignment
    {
        get => _horizontalAlignment;
        set
        {
            this.RaiseAndSetIfChanged(ref _horizontalAlignment, value);
            IsDirty = true;
        }
    }

    /// <summary>
    /// Dikey hizalama
    /// </summary>
    public Models.VerticalAlignment VerticalAlignment
    {
        get => _verticalAlignment;
        set
        {
            this.RaiseAndSetIfChanged(ref _verticalAlignment, value);
            IsDirty = true;
        }
    }

    /// <summary>
    /// Yatay hizalama seçenekleri
    /// </summary>
    public Models.HorizontalAlignment[] HorizontalAlignmentOptions { get; } = Enum.GetValues<Models.HorizontalAlignment>();

    /// <summary>
    /// Dikey hizalama seçenekleri
    /// </summary>
    public Models.VerticalAlignment[] VerticalAlignmentOptions { get; } = Enum.GetValues<Models.VerticalAlignment>();

    #endregion

    #region Style Properties

    /// <summary>
    /// Metin arkaplanı aktif mi
    /// </summary>
    public bool HasBackground
    {
        get => _hasBackground;
        set
        {
            this.RaiseAndSetIfChanged(ref _hasBackground, value);
            IsDirty = true;
        }
    }

    /// <summary>
    /// Arkaplan rengi
    /// </summary>
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            this.RaiseAndSetIfChanged(ref _backgroundColor, value);
            IsDirty = true;
        }
    }

    /// <summary>
    /// Stroke aktif mi
    /// </summary>
    public bool HasStroke
    {
        get => _hasStroke;
        set
        {
            this.RaiseAndSetIfChanged(ref _hasStroke, value);
            IsDirty = true;
        }
    }

    /// <summary>
    /// Stroke kalınlığı (1-3 piksel)
    /// </summary>
    public int StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            var validValue = Math.Clamp(value, 1, 3);
            this.RaiseAndSetIfChanged(ref _strokeWidth, validValue);
            IsDirty = true;
        }
    }

    /// <summary>
    /// Stroke rengi
    /// </summary>
    public Color StrokeColor
    {
        get => _strokeColor;
        set
        {
            this.RaiseAndSetIfChanged(ref _strokeColor, value);
            IsDirty = true;
        }
    }

    #endregion

    #region Zone Properties

    /// <summary>
    /// Slot'a özgü zone'lar
    /// </summary>
    public ObservableCollection<Zone> Zones { get; } = new();

    #endregion

    #region State Properties

    /// <summary>
    /// Değişiklik yapıldı mı
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        private set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Slot kaydetme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>
    /// Değişiklikleri iptal etme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>
    /// Slot silme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

    /// <summary>
    /// İkon seçme komutu
    /// </summary>
    public ReactiveCommand<string, Unit> SelectIconCommand { get; }

    /// <summary>
    /// İkonu kaldırma komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> RemoveIconCommand { get; }

    /// <summary>
    /// Zone ekleme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddZoneCommand { get; }

    /// <summary>
    /// Zone silme komutu
    /// </summary>
    public ReactiveCommand<int, Unit> RemoveZoneCommand { get; }

    /// <summary>
    /// Zone rengi ayarlama komutu
    /// </summary>
    public ReactiveCommand<Zone, Unit> SetZoneColorCommand { get; }

    #endregion

    /// <summary>
    /// SlotEditorViewModel constructor
    /// </summary>
    public SlotEditorViewModel(ISlotManager slotManager)
    {
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));

        // Komutları oluştur
        var canSave = this.WhenAnyValue(x => x.IsDirty);
        SaveCommand = ReactiveCommand.Create(SaveSlot, canSave);
        CancelCommand = ReactiveCommand.Create(CancelChanges);
        DeleteCommand = ReactiveCommand.Create(DeleteSlot);
        SelectIconCommand = ReactiveCommand.Create<string>(SelectIcon);
        RemoveIconCommand = ReactiveCommand.Create(RemoveIcon);
        AddZoneCommand = ReactiveCommand.Create(AddZone);
        RemoveZoneCommand = ReactiveCommand.Create<int>(RemoveZone);
        SetZoneColorCommand = ReactiveCommand.Create<Zone>(SetZoneColor);
    }

    #region Public Methods

    /// <summary>
    /// Mevcut slot'u yükler
    /// </summary>
    public void LoadSlot(TabelaSlot slot)
    {
        SlotNumber = slot.SlotNumber;
        RouteNumber = slot.RouteNumber;
        RouteText = slot.RouteText;
        IconPath = slot.IconPath;
        HorizontalAlignment = slot.HAlign;
        VerticalAlignment = slot.VAlign;
        HasBackground = slot.TextStyle.HasBackground;
        BackgroundColor = slot.TextStyle.BackgroundColor;
        HasStroke = slot.TextStyle.HasStroke;
        StrokeWidth = slot.TextStyle.StrokeWidth;
        StrokeColor = slot.TextStyle.StrokeColor;

        Zones.Clear();
        foreach (var zone in slot.Zones)
        {
            Zones.Add(zone);
        }

        IsNewSlot = false;
        IsDirty = false;
    }

    /// <summary>
    /// Yeni slot oluşturur
    /// </summary>
    public void CreateNewSlot(int slotNumber)
    {
        SlotNumber = slotNumber;
        RouteNumber = string.Empty;
        RouteText = string.Empty;
        IconPath = null;
        HorizontalAlignment = Models.HorizontalAlignment.Center;
        VerticalAlignment = Models.VerticalAlignment.Center;
        HasBackground = false;
        BackgroundColor = Colors.Black;
        HasStroke = false;
        StrokeWidth = 1;
        StrokeColor = Colors.Black;

        Zones.Clear();

        IsNewSlot = true;
        IsDirty = false;
    }

    /// <summary>
    /// Mevcut slot verilerini TabelaSlot nesnesine dönüştürür
    /// </summary>
    public TabelaSlot ToSlot()
    {
        return new TabelaSlot
        {
            SlotNumber = SlotNumber,
            RouteNumber = RouteNumber,
            RouteText = RouteText,
            IconPath = IconPath,
            HAlign = HorizontalAlignment,
            VAlign = VerticalAlignment,
            TextStyle = new TextStyle
            {
                HasBackground = HasBackground,
                BackgroundColor = BackgroundColor,
                HasStroke = HasStroke,
                StrokeWidth = StrokeWidth,
                StrokeColor = StrokeColor
            },
            Zones = new System.Collections.Generic.List<Zone>(Zones)
        };
    }

    #endregion

    #region Private Methods

    private void SaveSlot()
    {
        var slot = ToSlot();
        _slotManager.SetSlot(SlotNumber, slot);
        IsNewSlot = false;
        IsDirty = false;
    }

    private void CancelChanges()
    {
        // Mevcut slot'u yeniden yükle
        var existingSlot = _slotManager.GetSlot(SlotNumber);
        if (existingSlot != null)
        {
            LoadSlot(existingSlot);
        }
        else
        {
            CreateNewSlot(SlotNumber);
        }
    }

    private void DeleteSlot()
    {
        if (_slotManager.RemoveSlot(SlotNumber))
        {
            CreateNewSlot(SlotNumber);
        }
    }

    private void SelectIcon(string iconPath)
    {
        IconPath = iconPath;
    }

    private void RemoveIcon()
    {
        IconPath = null;
    }

    private void AddZone()
    {
        var newZone = new Zone
        {
            Index = Zones.Count,
            WidthPercent = 100.0 / (Zones.Count + 1),
            ContentType = ZoneContentType.Text,
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
        };

        // Mevcut zone'ların genişliklerini yeniden hesapla
        var newWidthPercent = 100.0 / (Zones.Count + 1);
        foreach (var zone in Zones)
        {
            zone.WidthPercent = newWidthPercent;
        }

        Zones.Add(newZone);
        IsDirty = true;
    }

    private void RemoveZone(int index)
    {
        if (index >= 0 && index < Zones.Count)
        {
            Zones.RemoveAt(index);

            // Zone indekslerini güncelle
            for (int i = 0; i < Zones.Count; i++)
            {
                Zones[i].Index = i;
            }

            // Genişlikleri normalize et
            if (Zones.Count > 0)
            {
                var widthPercent = 100.0 / Zones.Count;
                foreach (var zone in Zones)
                {
                    zone.WidthPercent = widthPercent;
                }
            }

            IsDirty = true;
        }
    }

    private static int _colorCycleIndex = 0;
    private static readonly Color[] PresetColors = new[]
    {
        Color.FromRgb(255, 0, 0),     // Kırmızı
        Color.FromRgb(0, 255, 0),     // Yeşil
        Color.FromRgb(255, 176, 0),   // Amber
        Color.FromRgb(255, 255, 255), // Beyaz
    };

    private void SetZoneColor(Zone zone)
    {
        if (zone != null)
        {
            // Renkleri döngüsel olarak değiştir
            zone.TextColor = PresetColors[_colorCycleIndex % PresetColors.Length];
            _colorCycleIndex++;
            IsDirty = true;
        }
    }

    #endregion
}
