using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Media;
using ReactiveUI;
using LEDTabelam.Models;

namespace LEDTabelam.ViewModels;

/// <summary>
/// HD2020 tarzı program düzenleyici ViewModel
/// Her öğe için ayrı pozisyon, font ve renk ayarları
/// </summary>
public class ProgramEditorViewModel : ViewModelBase
{
    private int _programNumber = 1;
    private TabelaItem? _selectedItem;
    private int _displayWidth = 192;
    private int _displayHeight = 24;
    private int _nextItemId = 1;

    /// <summary>
    /// Kullanılabilir fontlar (ControlPanel'den alınır)
    /// </summary>
    public ObservableCollection<BitmapFont> AvailableFonts { get; } = new();

    /// <summary>
    /// Font adları listesi (ComboBox için)
    /// </summary>
    public ObservableCollection<string> FontNames { get; } = new();

    /// <summary>
    /// Program numarası (1-999)
    /// </summary>
    public int ProgramNumber
    {
        get => _programNumber;
        set => this.RaiseAndSetIfChanged(ref _programNumber, Math.Clamp(value, 1, 999));
    }

    /// <summary>
    /// Program öğeleri listesi
    /// </summary>
    public ObservableCollection<TabelaItem> Items { get; } = new();

    /// <summary>
    /// Seçili öğe
    /// </summary>
    public TabelaItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            // Önceki seçimi kaldır
            if (_selectedItem != null)
                _selectedItem.IsSelected = false;
            
            this.RaiseAndSetIfChanged(ref _selectedItem, value);
            
            // Yeni seçimi işaretle
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
    /// Display genişliği
    /// </summary>
    public int DisplayWidth
    {
        get => _displayWidth;
        set => this.RaiseAndSetIfChanged(ref _displayWidth, value);
    }

    /// <summary>
    /// Display yüksekliği
    /// </summary>
    public int DisplayHeight
    {
        get => _displayHeight;
        set => this.RaiseAndSetIfChanged(ref _displayHeight, value);
    }

    #region Commands

    public ReactiveCommand<Unit, Unit> AddTextCommand { get; }
    public ReactiveCommand<Unit, Unit> AddSymbolCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteItemCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveUpCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveDownCommand { get; }
    public ReactiveCommand<Unit, Unit> DuplicateCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearAllCommand { get; }

    #endregion

    /// <summary>
    /// Öğe değişiklik eventi
    /// </summary>
    public event Action? ItemsChanged;

    public ProgramEditorViewModel()
    {
        var hasSelection = this.WhenAnyValue(x => x.HasSelection);
        
        AddTextCommand = ReactiveCommand.Create(AddTextItem);
        AddSymbolCommand = ReactiveCommand.Create(AddSymbolItem);
        DeleteItemCommand = ReactiveCommand.Create(DeleteSelectedItem, hasSelection);
        MoveUpCommand = ReactiveCommand.Create(MoveItemUp, hasSelection);
        MoveDownCommand = ReactiveCommand.Create(MoveItemDown, hasSelection);
        DuplicateCommand = ReactiveCommand.Create(DuplicateItem, hasSelection);
        ClearAllCommand = ReactiveCommand.Create(ClearAll);

        // Items collection değişikliklerini izle
        Items.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (TabelaItem item in e.NewItems)
                {
                    item.PropertyChanged += OnItemPropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (TabelaItem item in e.OldItems)
                {
                    item.PropertyChanged -= OnItemPropertyChanged;
                }
            }
        };

        // Varsayılan öğeler ekle
        AddDefaultItems();
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // IsSelected değişikliği hariç tüm değişikliklerde render'ı tetikle
        if (e.PropertyName != nameof(TabelaItem.IsSelected))
        {
            OnItemsChanged();
        }
    }

    private void AddDefaultItems()
    {
        // Hat Kodu öğesi
        var hatKodu = new TabelaItem
        {
            Id = _nextItemId++,
            Name = "Hat Kodu",
            Content = "",
            ItemType = TabelaItemType.Text,
            X = 0,
            Y = 0,
            Width = 30,
            Height = DisplayHeight,
            Color = Color.FromRgb(255, 0, 0), // Kırmızı
            FontName = "PolarisRGB6x10M",
            HAlign = HorizontalAlignment.Center,
            VAlign = VerticalAlignment.Center
        };
        Items.Add(hatKodu);

        // Güzergah 1. Satır öğesi
        var guzergah1 = new TabelaItem
        {
            Id = _nextItemId++,
            Name = "Güzergah 1",
            Content = "",
            ItemType = TabelaItemType.Text,
            X = 30,
            Y = 0,
            Width = DisplayWidth - 30,
            Height = DisplayHeight / 2,
            Color = Color.FromRgb(0, 255, 0), // Yeşil
            FontName = "PolarisRGB6x10M",
            HAlign = HorizontalAlignment.Center,
            VAlign = VerticalAlignment.Center
        };
        Items.Add(guzergah1);

        // Güzergah 2. Satır öğesi (ayrı öğe olarak)
        var guzergah2 = new TabelaItem
        {
            Id = _nextItemId++,
            Name = "Güzergah 2",
            Content = "",
            ItemType = TabelaItemType.Text,
            X = 30,
            Y = DisplayHeight / 2,
            Width = DisplayWidth - 30,
            Height = DisplayHeight / 2,
            Color = Color.FromRgb(0, 255, 0), // Yeşil
            FontName = "PolarisRGB6x10M",
            HAlign = HorizontalAlignment.Center,
            VAlign = VerticalAlignment.Center
        };
        Items.Add(guzergah2);

        SelectedItem = hatKodu;
        OnItemsChanged();
    }

    private void AddTextItem()
    {
        var item = new TabelaItem
        {
            Id = _nextItemId++,
            Name = $"Metin {Items.Count + 1}",
            Content = "YENİ METİN",
            ItemType = TabelaItemType.Text,
            X = 0,
            Y = 0,
            Width = 50,
            Height = DisplayHeight,
            Color = Color.FromRgb(255, 176, 0) // Amber
        };
        Items.Add(item);
        SelectedItem = item;
        OnItemsChanged();
    }

    private void AddSymbolItem()
    {
        var item = new TabelaItem
        {
            Id = _nextItemId++,
            Name = $"Sembol {Items.Count + 1}",
            Content = "",
            ItemType = TabelaItemType.Symbol,
            X = DisplayWidth - 24,
            Y = 0,
            Width = 24,
            Height = DisplayHeight,
            Color = Color.FromRgb(255, 176, 0)
        };
        Items.Add(item);
        SelectedItem = item;
        OnItemsChanged();
    }

    private void DeleteSelectedItem()
    {
        if (SelectedItem == null) return;
        
        var index = Items.IndexOf(SelectedItem);
        Items.Remove(SelectedItem);
        
        // Yeni seçim
        if (Items.Count > 0)
        {
            SelectedItem = Items[Math.Min(index, Items.Count - 1)];
        }
        else
        {
            SelectedItem = null;
        }
        OnItemsChanged();
    }

    private void MoveItemUp()
    {
        if (SelectedItem == null) return;
        
        var index = Items.IndexOf(SelectedItem);
        if (index > 0)
        {
            Items.Move(index, index - 1);
            OnItemsChanged();
        }
    }

    private void MoveItemDown()
    {
        if (SelectedItem == null) return;
        
        var index = Items.IndexOf(SelectedItem);
        if (index < Items.Count - 1)
        {
            Items.Move(index, index + 1);
            OnItemsChanged();
        }
    }

    private void DuplicateItem()
    {
        if (SelectedItem == null) return;
        
        var newItem = new TabelaItem
        {
            Id = _nextItemId++,
            Name = SelectedItem.Name + " (Kopya)",
            Content = SelectedItem.Content,
            ItemType = SelectedItem.ItemType,
            X = SelectedItem.X + 5,
            Y = SelectedItem.Y,
            Width = SelectedItem.Width,
            Height = SelectedItem.Height,
            Color = SelectedItem.Color,
            FontName = SelectedItem.FontName,
            HAlign = SelectedItem.HAlign,
            VAlign = SelectedItem.VAlign,
            IsScrolling = SelectedItem.IsScrolling,
            ScrollDirection = SelectedItem.ScrollDirection,
            ScrollSpeed = SelectedItem.ScrollSpeed
        };
        
        Items.Add(newItem);
        SelectedItem = newItem;
        OnItemsChanged();
    }

    private void ClearAll()
    {
        Items.Clear();
        SelectedItem = null;
        _nextItemId = 1;
        OnItemsChanged();
    }

    /// <summary>
    /// Seçili öğenin rengini değiştirir
    /// </summary>
    public void SetSelectedColor(Color color)
    {
        if (SelectedItem != null)
        {
            SelectedItem.Color = color;
            OnItemsChanged();
        }
    }

    /// <summary>
    /// Seçili öğenin yatay hizalamasını değiştirir
    /// </summary>
    public void SetSelectedHAlign(HorizontalAlignment align)
    {
        if (SelectedItem != null)
        {
            SelectedItem.HAlign = align;
            OnItemsChanged();
        }
    }

    /// <summary>
    /// Seçili öğenin dikey hizalamasını değiştirir
    /// </summary>
    public void SetSelectedVAlign(VerticalAlignment align)
    {
        if (SelectedItem != null)
        {
            SelectedItem.VAlign = align;
            OnItemsChanged();
        }
    }

    /// <summary>
    /// Display boyutlarını günceller (event tetiklemeden)
    /// </summary>
    public void UpdateDisplaySize(int width, int height)
    {
        // Sadece değişiklik varsa güncelle, event tetikleme
        if (_displayWidth != width || _displayHeight != height)
        {
            _displayWidth = width;
            _displayHeight = height;
            this.RaisePropertyChanged(nameof(DisplayWidth));
            this.RaisePropertyChanged(nameof(DisplayHeight));
        }
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
        return AvailableFonts.FirstOrDefault(f => f.Name == fontName);
    }

    private void OnItemsChanged()
    {
        ItemsChanged?.Invoke();
    }
}
