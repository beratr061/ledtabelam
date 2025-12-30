using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia;
using Avalonia.Media;
using ReactiveUI;
using LEDTabelam.Models;

namespace LEDTabelam.ViewModels;

/// <summary>
/// Görsel bölge düzenleyici ViewModel
/// Canvas üzerinde sürükle-bırak ile zone düzenleme
/// </summary>
public class VisualZoneEditorViewModel : ViewModelBase
{
    private int _displayWidth = 160;
    private int _displayHeight = 24;
    private int _zoomLevel = 300; // %300 varsayılan (LED tabela küçük olduğu için)
    private TabelaItem? _selectedItem;
    private string _mousePosition = "0, 0";
    private int _nextItemId = 1;

    /// <summary>
    /// Tabela öğeleri
    /// </summary>
    public ObservableCollection<TabelaItem> Items { get; } = new();

    /// <summary>
    /// Kullanılabilir fontlar
    /// </summary>
    public ObservableCollection<BitmapFont> AvailableFonts { get; } = new();

    /// <summary>
    /// Font adları listesi (ComboBox için)
    /// </summary>
    public ObservableCollection<string> FontNames { get; } = new();

    /// <summary>
    /// Display genişliği (piksel)
    /// </summary>
    public int DisplayWidth
    {
        get => _displayWidth;
        set
        {
            this.RaiseAndSetIfChanged(ref _displayWidth, value);
            this.RaisePropertyChanged(nameof(CanvasWidth));
            this.RaisePropertyChanged(nameof(DisplaySize));
        }
    }

    /// <summary>
    /// Display yüksekliği (piksel)
    /// </summary>
    public int DisplayHeight
    {
        get => _displayHeight;
        set
        {
            this.RaiseAndSetIfChanged(ref _displayHeight, value);
            this.RaisePropertyChanged(nameof(CanvasHeight));
            this.RaisePropertyChanged(nameof(DisplaySize));
        }
    }

    /// <summary>
    /// Zoom seviyesi (%)
    /// </summary>
    public int ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            this.RaiseAndSetIfChanged(ref _zoomLevel, Math.Clamp(value, 100, 800));
            this.RaisePropertyChanged(nameof(CanvasWidth));
            this.RaisePropertyChanged(nameof(CanvasHeight));
        }
    }

    /// <summary>
    /// Canvas genişliği (zoom uygulanmış)
    /// </summary>
    public double CanvasWidth => DisplayWidth * ZoomLevel / 100.0;

    /// <summary>
    /// Canvas yüksekliği (zoom uygulanmış)
    /// </summary>
    public double CanvasHeight => DisplayHeight * ZoomLevel / 100.0;

    /// <summary>
    /// Display boyut bilgisi
    /// </summary>
    public string DisplaySize => $"{DisplayWidth}x{DisplayHeight} px";

    /// <summary>
    /// Seçili öğe
    /// </summary>
    public TabelaItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != null)
                _selectedItem.IsSelected = false;
            
            this.RaiseAndSetIfChanged(ref _selectedItem, value);
            
            if (_selectedItem != null)
                _selectedItem.IsSelected = true;
            
            this.RaisePropertyChanged(nameof(HasSelection));
        }
    }

    /// <summary>
    /// Seçili öğe var mı
    /// </summary>
    public bool HasSelection => SelectedItem != null;

    /// <summary>
    /// Mouse pozisyonu
    /// </summary>
    public string MousePosition
    {
        get => _mousePosition;
        set => this.RaiseAndSetIfChanged(ref _mousePosition, value);
    }

    #region Commands

    public ReactiveCommand<Unit, Unit> AddTextZoneCommand { get; }
    public ReactiveCommand<Unit, Unit> AddSymbolZoneCommand { get; }
    public ReactiveCommand<Unit, Unit> AddClockZoneCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearAllCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }

    #endregion

    /// <summary>
    /// Öğe değişiklik eventi
    /// </summary>
    public event Action? ItemsChanged;

    public VisualZoneEditorViewModel()
    {
        AddTextZoneCommand = ReactiveCommand.Create(AddTextZone);
        AddSymbolZoneCommand = ReactiveCommand.Create(AddSymbolZone);
        AddClockZoneCommand = ReactiveCommand.Create(AddClockZone);
        DeleteSelectedCommand = ReactiveCommand.Create(DeleteSelected);
        ClearAllCommand = ReactiveCommand.Create(ClearAll);
        ZoomInCommand = ReactiveCommand.Create(() => { ZoomLevel = Math.Min(800, ZoomLevel + 50); });
        ZoomOutCommand = ReactiveCommand.Create(() => { ZoomLevel = Math.Max(100, ZoomLevel - 50); });

        // Items değişikliklerini izle
        Items.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (TabelaItem item in e.NewItems)
                {
                    item.PropertyChanged += (_, _) => OnItemsChanged();
                }
            }
            OnItemsChanged();
        };
    }

    /// <summary>
    /// Yeni metin bölgesi ekle
    /// </summary>
    private void AddTextZone()
    {
        var item = new TabelaItem
        {
            Id = _nextItemId++,
            Name = $"Metin {Items.Count + 1}",
            Content = "METİN",
            ItemType = TabelaItemType.Text,
            X = 0,
            Y = 0,
            Width = DisplayWidth,  // Tam genişlik
            Height = DisplayHeight, // Tam yükseklik
            Color = Color.FromRgb(255, 176, 0),
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
        };
        Items.Add(item);
        SelectedItem = item;
    }

    /// <summary>
    /// Yeni sembol bölgesi ekle
    /// </summary>
    private void AddSymbolZone()
    {
        var item = new TabelaItem
        {
            Id = _nextItemId++,
            Name = $"Sembol {Items.Count + 1}",
            Content = "",
            ItemType = TabelaItemType.Symbol,
            X = DisplayWidth - 20,
            Y = 0,
            Width = 20,
            Height = DisplayHeight,
            Color = Color.FromRgb(255, 176, 0),
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
        };
        Items.Add(item);
        SelectedItem = item;
    }

    /// <summary>
    /// Saat bölgesi ekle
    /// </summary>
    private void AddClockZone()
    {
        var item = new TabelaItem
        {
            Id = _nextItemId++,
            Name = "Saat",
            Content = "HH:MM",
            ItemType = TabelaItemType.Clock,
            X = 0,
            Y = 0,
            Width = 40,
            Height = DisplayHeight,
            Color = Color.FromRgb(0, 255, 0),
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
        };
        Items.Add(item);
        SelectedItem = item;
    }

    /// <summary>
    /// Seçili öğeyi sil
    /// </summary>
    private void DeleteSelected()
    {
        if (SelectedItem == null) return;
        
        var index = Items.IndexOf(SelectedItem);
        Items.Remove(SelectedItem);
        
        if (Items.Count > 0)
            SelectedItem = Items[Math.Min(index, Items.Count - 1)];
        else
            SelectedItem = null;
    }

    /// <summary>
    /// Tüm öğeleri temizle
    /// </summary>
    private void ClearAll()
    {
        Items.Clear();
        SelectedItem = null;
        _nextItemId = 1;
    }

    /// <summary>
    /// Öğeyi seç
    /// </summary>
    public void SelectItem(TabelaItem item)
    {
        SelectedItem = item;
    }

    /// <summary>
    /// Seçimi kaldır
    /// </summary>
    public void ClearSelection()
    {
        SelectedItem = null;
    }

    /// <summary>
    /// Mouse pozisyonunu güncelle
    /// </summary>
    public void UpdateMousePosition(Point point)
    {
        var scale = ZoomLevel / 100.0;
        var x = (int)(point.X / scale);
        var y = (int)(point.Y / scale);
        MousePosition = $"{x}, {y}";
    }

    /// <summary>
    /// Display boyutlarını güncelle
    /// </summary>
    public void UpdateDisplaySize(int width, int height)
    {
        DisplayWidth = width;
        DisplayHeight = height;
    }

    private void OnItemsChanged()
    {
        ItemsChanged?.Invoke();
    }

    /// <summary>
    /// Kullanılabilir fontları günceller
    /// </summary>
    public void UpdateAvailableFonts(System.Collections.Generic.IEnumerable<BitmapFont> fonts)
    {
        AvailableFonts.Clear();
        FontNames.Clear();
        
        foreach (var font in fonts)
        {
            AvailableFonts.Add(font);
            FontNames.Add(font.Name);
        }
    }

    /// <summary>
    /// Font adına göre BitmapFont döndürür
    /// </summary>
    public BitmapFont? GetFontByName(string fontName)
    {
        foreach (var font in AvailableFonts)
        {
            if (font.Name == fontName)
                return font;
        }
        return null;
    }
}
