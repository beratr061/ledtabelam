using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Media;
using ReactiveUI;
using LEDTabelam.Models;
using LEDTabelam.Services;

namespace LEDTabelam.ViewModels;

/// <summary>
/// HD2020 tarzı program düzenleyici ViewModel
/// Her öğe için ayrı pozisyon, font ve renk ayarları
/// </summary>
public class ProgramEditorViewModel : ViewModelBase
{
    private int _programNumber = 1;
    private TabelaItem? _selectedItem;
    private int _displayWidth = 160;
    private int _displayHeight = 24;
    private int _nextItemId = 1;
    private AssetInfo? _selectedSymbol;
    private string _selectedCategory = "all";

    /// <summary>
    /// Kullanılabilir fontlar (ControlPanel'den alınır)
    /// </summary>
    public ObservableCollection<BitmapFont> AvailableFonts { get; } = new();

    /// <summary>
    /// Font adları listesi (ComboBox için)
    /// </summary>
    public ObservableCollection<string> FontNames { get; } = new();

    /// <summary>
    /// Kullanılabilir semboller
    /// </summary>
    public ObservableCollection<AssetInfo> AvailableSymbols { get; } = new();

    /// <summary>
    /// Sembol kategorileri
    /// </summary>
    public ObservableCollection<AssetCategory> SymbolCategories { get; } = new();

    /// <summary>
    /// Seçili sembol kategorisi
    /// </summary>
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            FilterSymbolsByCategory();
        }
    }

    /// <summary>
    /// Seçili sembol (sembol seçici için)
    /// </summary>
    public AssetInfo? SelectedSymbol
    {
        get => _selectedSymbol;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSymbol, value);
            // Seçili öğe sembol ise, sembol adını güncelle
            if (value != null && SelectedItem?.ItemType == TabelaItemType.Symbol)
            {
                SelectedItem.SymbolName = value.Name;
                SelectedItem.Content = value.DisplayName; // Görüntüleme için
                OnItemsChanged();
            }
        }
    }

    /// <summary>
    /// Filtrelenmiş semboller (kategoriye göre)
    /// </summary>
    public ObservableCollection<AssetInfo> FilteredSymbols { get; } = new();

    private IAssetLibrary? _assetLibrary;

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
            this.RaisePropertyChanged(nameof(IsTextItemSelected));
            this.RaisePropertyChanged(nameof(IsSymbolItemSelected));
            
            // Sembol seçiliyse, mevcut sembolü seç
            if (_selectedItem?.ItemType == TabelaItemType.Symbol && !string.IsNullOrEmpty(_selectedItem.SymbolName))
            {
                _selectedSymbol = AvailableSymbols.FirstOrDefault(s => s.Name == _selectedItem.SymbolName);
                this.RaisePropertyChanged(nameof(SelectedSymbol));
            }
        }
    }

    /// <summary>
    /// Seçili öğe var mı
    /// </summary>
    public bool HasSelection => SelectedItem != null;

    /// <summary>
    /// Seçili öğe metin mi
    /// </summary>
    public bool IsTextItemSelected => SelectedItem?.ItemType == TabelaItemType.Text;

    /// <summary>
    /// Seçili öğe sembol mü
    /// </summary>
    public bool IsSymbolItemSelected => SelectedItem?.ItemType == TabelaItemType.Symbol;

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
            // X veya Y değiştiğinde boyutları yeniden hesapla
            if ((e.PropertyName == nameof(TabelaItem.X) || e.PropertyName == nameof(TabelaItem.Y)) 
                && sender is TabelaItem item)
            {
                RecalculateItemSize(item);
            }
            
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
            Color = Color.FromRgb(255, 0, 0), // Kırmızı
            FontName = "PolarisRGB6x10M",
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
        };
        RecalculateItemSize(hatKodu);
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
            Color = Color.FromRgb(0, 255, 0), // Yeşil
            FontName = "PolarisRGB6x10M",
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
        };
        RecalculateItemSize(guzergah1);
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
            Color = Color.FromRgb(0, 255, 0), // Yeşil
            FontName = "PolarisRGB6x10M",
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
        };
        RecalculateItemSize(guzergah2);
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
            Color = Color.FromRgb(255, 176, 0) // Amber
        };
        RecalculateItemSize(item);
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
            SymbolName = "", // Boş başla, kullanıcı seçecek
            SymbolSize = 16,
            X = DisplayWidth - 20,
            Y = (DisplayHeight - 16) / 2, // Dikey ortala
            Width = 16,
            Height = 16,
            Color = Color.FromRgb(255, 176, 0),
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
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
            Color = SelectedItem.Color,
            FontName = SelectedItem.FontName,
            LetterSpacing = SelectedItem.LetterSpacing,
            SymbolName = SelectedItem.SymbolName,
            SymbolSize = SelectedItem.SymbolSize,
            HAlign = SelectedItem.HAlign,
            VAlign = SelectedItem.VAlign,
            IsScrolling = SelectedItem.IsScrolling,
            ScrollDirection = SelectedItem.ScrollDirection,
            ScrollSpeed = SelectedItem.ScrollSpeed
        };
        RecalculateItemSize(newItem);
        
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
    /// Display boyutlarını günceller ve öğelerin boyutlarını yeniden hesaplar
    /// </summary>
    public void UpdateDisplaySize(int width, int height)
    {
        // Sadece değişiklik varsa güncelle
        if (_displayWidth != width || _displayHeight != height)
        {
            _displayWidth = width;
            _displayHeight = height;
            this.RaisePropertyChanged(nameof(DisplayWidth));
            this.RaisePropertyChanged(nameof(DisplayHeight));
            
            // Tüm öğelerin boyutlarını yeniden hesapla
            RecalculateItemSizes();
        }
    }

    /// <summary>
    /// Tüm öğelerin boyutlarını tabela boyutuna göre yeniden hesaplar
    /// </summary>
    private void RecalculateItemSizes()
    {
        foreach (var item in Items)
        {
            // Genişlik: X pozisyonundan tabela sonuna kadar
            item.Width = Math.Max(1, DisplayWidth - item.X);
            
            // Yükseklik: Y pozisyonundan tabela sonuna kadar
            item.Height = Math.Max(1, DisplayHeight - item.Y);
        }
        OnItemsChanged();
    }

    /// <summary>
    /// Tek bir öğenin boyutunu pozisyonuna göre hesaplar
    /// </summary>
    private void RecalculateItemSize(TabelaItem item)
    {
        item.Width = Math.Max(1, DisplayWidth - item.X);
        item.Height = Math.Max(1, DisplayHeight - item.Y);
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

    /// <summary>
    /// AssetLibrary'yi ayarlar ve sembolleri yükler
    /// </summary>
    public void SetAssetLibrary(IAssetLibrary assetLibrary)
    {
        _assetLibrary = assetLibrary;
        LoadSymbols();
    }

    /// <summary>
    /// Sembolleri AssetLibrary'den yükler
    /// </summary>
    private void LoadSymbols()
    {
        if (_assetLibrary == null) return;

        AvailableSymbols.Clear();
        SymbolCategories.Clear();
        FilteredSymbols.Clear();

        // Kategorileri yükle
        var categories = _assetLibrary.GetCategories();
        foreach (var cat in categories)
        {
            SymbolCategories.Add(cat);
        }

        // Tüm sembolleri yükle
        var allAssets = _assetLibrary.GetAllAssets();
        foreach (var asset in allAssets)
        {
            AvailableSymbols.Add(asset);
            FilteredSymbols.Add(asset);
        }
    }

    /// <summary>
    /// Sembolleri kategoriye göre filtreler
    /// </summary>
    private void FilterSymbolsByCategory()
    {
        FilteredSymbols.Clear();

        if (_selectedCategory == "all" || string.IsNullOrEmpty(_selectedCategory))
        {
            foreach (var symbol in AvailableSymbols)
            {
                FilteredSymbols.Add(symbol);
            }
        }
        else
        {
            foreach (var symbol in AvailableSymbols.Where(s => s.Category == _selectedCategory))
            {
                FilteredSymbols.Add(symbol);
            }
        }
    }

    /// <summary>
    /// Seçili sembol öğesinin boyutunu değiştirir
    /// </summary>
    public void SetSymbolSize(int size)
    {
        if (SelectedItem?.ItemType == TabelaItemType.Symbol)
        {
            SelectedItem.SymbolSize = size;
            SelectedItem.Width = size;
            SelectedItem.Height = size;
            OnItemsChanged();
        }
    }

    private void OnItemsChanged()
    {
        ItemsChanged?.Invoke();
    }
}
