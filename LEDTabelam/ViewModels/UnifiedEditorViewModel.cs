using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Media;
using ReactiveUI;
using LEDTabelam.Models;
using LEDTabelam.Services;

namespace LEDTabelam.ViewModels;

/// <summary>
/// Birleşik düzenleyici ViewModel - Program ve görsel düzenleyici tek arayüzde
/// </summary>
public class UnifiedEditorViewModel : ViewModelBase
{
    private int _programNumber = 1;
    private TabelaItem? _selectedItem;
    private int _displayWidth = 160;
    private int _displayHeight = 24;
    private int _zoomLevel = 300;
    private int _nextItemId = 1;
    private string _mousePosition = "0, 0";
    private string _selectedCategory = "all";
    private bool _isSnapEnabled = true;
    private bool _isAnimationPlaying = false;
    private IAssetLibrary? _assetLibrary;
    
    // Animasyon throttling için
    private readonly Stopwatch _animationStopwatch = new();
    private const double AnimationRenderIntervalMs = 33.33; // ~30 FPS render (animasyon 60 FPS'de çalışır ama render 30 FPS)

    public ObservableCollection<TabelaItem> Items { get; } = new();
    public ObservableCollection<BitmapFont> AvailableFonts { get; } = new();
    public ObservableCollection<string> FontNames { get; } = new();
    public ObservableCollection<AssetInfo> AvailableSymbols { get; } = new();
    public ObservableCollection<AssetInfo> FilteredSymbols { get; } = new();

    public int ProgramNumber
    {
        get => _programNumber;
        set => this.RaiseAndSetIfChanged(ref _programNumber, Math.Clamp(value, 1, 999));
    }

    public TabelaItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != null)
            {
                _selectedItem.IsSelected = false;
                _selectedItem.PropertyChanged -= OnSelectedItemPropertyChanged;
            }
            
            this.RaiseAndSetIfChanged(ref _selectedItem, value);
            
            if (_selectedItem != null)
            {
                _selectedItem.IsSelected = true;
                _selectedItem.PropertyChanged += OnSelectedItemPropertyChanged;
            }
            
            this.RaisePropertyChanged(nameof(HasSelection));
            this.RaisePropertyChanged(nameof(IsTextItemSelected));
            this.RaisePropertyChanged(nameof(IsSymbolItemSelected));
            this.RaisePropertyChanged(nameof(IsScrollingItemSelected));
        }
    }

    private void OnSelectedItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TabelaItem.IsScrolling))
        {
            this.RaisePropertyChanged(nameof(IsScrollingItemSelected));
        }
    }

    public bool HasSelection => SelectedItem != null;
    public bool IsTextItemSelected => SelectedItem?.ItemType == TabelaItemType.Text;
    public bool IsSymbolItemSelected => SelectedItem?.ItemType == TabelaItemType.Symbol;
    public bool IsScrollingItemSelected => SelectedItem?.ItemType == TabelaItemType.Text && SelectedItem?.IsScrolling == true;

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

    public double CanvasWidth => DisplayWidth * ZoomLevel / 100.0;
    public double CanvasHeight => DisplayHeight * ZoomLevel / 100.0;
    public string DisplaySize => $"{DisplayWidth}x{DisplayHeight}";

    public string MousePosition
    {
        get => _mousePosition;
        set => this.RaiseAndSetIfChanged(ref _mousePosition, value);
    }

    /// <summary>
    /// Mıknatıs (snap) özelliği aktif mi
    /// </summary>
    public bool IsSnapEnabled
    {
        get => _isSnapEnabled;
        set => this.RaiseAndSetIfChanged(ref _isSnapEnabled, value);
    }

    /// <summary>
    /// Animasyon oynatılıyor mu
    /// </summary>
    public bool IsAnimationPlaying
    {
        get => _isAnimationPlaying;
        set
        {
            var oldValue = _isAnimationPlaying;
            this.RaiseAndSetIfChanged(ref _isAnimationPlaying, value);
            
            // Animasyon durduğunda offset'leri ve stopwatch'ı sıfırla
            if (oldValue && !value)
            {
                _animationStopwatch.Stop();
                _animationStopwatch.Reset();
                
                foreach (var item in Items)
                {
                    item.ResetScrollOffset();
                }
                OnItemsChanged();
            }
        }
    }

    #region Commands

    public ReactiveCommand<Unit, Unit> AddTextCommand { get; }
    public ReactiveCommand<Unit, Unit> AddSymbolCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteItemCommand { get; }
    public ReactiveCommand<Unit, Unit> DuplicateCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveUpCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveDownCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }

    #endregion

    public event Action? ItemsChanged;

    public UnifiedEditorViewModel()
    {
        var hasSelection = this.WhenAnyValue(x => x.HasSelection);
        
        AddTextCommand = ReactiveCommand.Create(AddTextItem);
        AddSymbolCommand = ReactiveCommand.Create(AddSymbolItem);
        DeleteItemCommand = ReactiveCommand.Create(DeleteSelectedItem, hasSelection);
        DuplicateCommand = ReactiveCommand.Create(DuplicateItem, hasSelection);
        MoveUpCommand = ReactiveCommand.Create(MoveItemUp, hasSelection);
        MoveDownCommand = ReactiveCommand.Create(MoveItemDown, hasSelection);
        ZoomInCommand = ReactiveCommand.Create(() => { ZoomLevel = Math.Min(800, ZoomLevel + 50); });
        ZoomOutCommand = ReactiveCommand.Create(() => { ZoomLevel = Math.Max(100, ZoomLevel - 50); });

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

        AddDefaultItems();
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TabelaItem.IsSelected))
        {
            OnItemsChanged();
        }
    }

    private void AddDefaultItems()
    {
        // Hat Kodu - sol tarafta, tam yükseklik
        var hatKodu = new TabelaItem
        {
            Id = _nextItemId++,
            Name = "Hat Kodu",
            Content = "",
            ItemType = TabelaItemType.Text,
            X = 0, Y = 0,
            Width = 28, Height = DisplayHeight,
            Color = Color.FromRgb(255, 0, 0),
            FontName = "PolarisRGB6x10M",
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
        };
        Items.Add(hatKodu);

        // Güzergah 1 - üst yarı
        var guzergah1 = new TabelaItem
        {
            Id = _nextItemId++,
            Name = "Güzergah 1",
            Content = "",
            ItemType = TabelaItemType.Text,
            X = 28, Y = 0,
            Width = DisplayWidth - 28, Height = DisplayHeight / 2,
            Color = Color.FromRgb(0, 255, 0),
            FontName = "PolarisRGB6x10M",
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
        };
        Items.Add(guzergah1);

        // Güzergah 2 - alt yarı
        var guzergah2 = new TabelaItem
        {
            Id = _nextItemId++,
            Name = "Güzergah 2",
            Content = "",
            ItemType = TabelaItemType.Text,
            X = 28, Y = DisplayHeight / 2,
            Width = DisplayWidth - 28, Height = DisplayHeight - (DisplayHeight / 2),
            Color = Color.FromRgb(0, 255, 0),
            FontName = "PolarisRGB6x10M",
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
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
            X = 0, Y = 0,
            Width = Math.Min(60, DisplayWidth),
            Height = Math.Min(DisplayHeight, DisplayHeight),
            Color = Color.FromRgb(255, 176, 0),
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center
        };
        Items.Add(item);
        SelectedItem = item;
        OnItemsChanged();
    }

    private void AddSymbolItem()
    {
        var symbolSize = Math.Min(16, Math.Min(DisplayWidth, DisplayHeight));
        var item = new TabelaItem
        {
            Id = _nextItemId++,
            Name = $"Sembol {Items.Count + 1}",
            Content = "",
            ItemType = TabelaItemType.Symbol,
            SymbolName = "",
            SymbolSize = symbolSize,
            X = Math.Max(0, DisplayWidth - symbolSize - 2),
            Y = Math.Max(0, (DisplayHeight - symbolSize) / 2),
            Width = symbolSize,
            Height = symbolSize,
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
        
        if (Items.Count > 0)
            SelectedItem = Items[Math.Min(index, Items.Count - 1)];
        else
            SelectedItem = null;
        
        OnItemsChanged();
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
            LetterSpacing = SelectedItem.LetterSpacing,
            SymbolName = SelectedItem.SymbolName,
            SymbolSize = SelectedItem.SymbolSize,
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

    public void SelectItem(TabelaItem item)
    {
        SelectedItem = item;
    }

    public void ClearSelection()
    {
        SelectedItem = null;
    }

    public void UpdateMousePosition(Point point)
    {
        var scale = ZoomLevel / 100.0;
        var x = (int)(point.X / scale);
        var y = (int)(point.Y / scale);
        MousePosition = $"{x}, {y}";
    }

    public void UpdateDisplaySize(int width, int height)
    {
        var oldWidth = DisplayWidth;
        var oldHeight = DisplayHeight;
        
        DisplayWidth = width;
        DisplayHeight = height;
        
        // Mevcut öğelerin boyutlarını yeni tabela boyutuna göre ayarla
        foreach (var item in Items)
        {
            // X ve Y sınırları
            if (item.X >= width) item.X = Math.Max(0, width - item.Width);
            if (item.Y >= height) item.Y = Math.Max(0, height - item.Height);
            
            // Genişlik ve yükseklik sınırları
            if (item.X + item.Width > width) item.Width = width - item.X;
            if (item.Y + item.Height > height) item.Height = height - item.Y;
            
            // Minimum boyut
            item.Width = Math.Max(1, item.Width);
            item.Height = Math.Max(1, item.Height);
        }
        
        ZonesNeedRedraw?.Invoke();
    }

    public void SetSelectedHAlign(HorizontalAlignment align)
    {
        if (SelectedItem != null)
        {
            SelectedItem.HAlign = align;
            OnItemsChanged();
            ZonesNeedRedraw?.Invoke();
        }
    }

    public void SetSelectedVAlign(VerticalAlignment align)
    {
        if (SelectedItem != null)
        {
            SelectedItem.VAlign = align;
            OnItemsChanged();
            ZonesNeedRedraw?.Invoke();
        }
    }

    /// <summary>
    /// Canvas yeniden çizilmesi gerektiğinde tetiklenir
    /// </summary>
    public event Action? ZonesNeedRedraw;

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

    public BitmapFont? GetFontByName(string fontName)
    {
        return AvailableFonts.FirstOrDefault(f => f.Name == fontName);
    }

    public void SetAssetLibrary(IAssetLibrary assetLibrary)
    {
        _assetLibrary = assetLibrary;
        LoadSymbols();
    }

    private void LoadSymbols()
    {
        if (_assetLibrary == null) return;

        AvailableSymbols.Clear();
        FilteredSymbols.Clear();

        var allAssets = _assetLibrary.GetAllAssets();
        foreach (var asset in allAssets)
        {
            AvailableSymbols.Add(asset);
            FilteredSymbols.Add(asset);
        }
    }

    public void SetSelectedCategory(string category)
    {
        _selectedCategory = category;
        FilterSymbolsByCategory();
    }

    private void FilterSymbolsByCategory()
    {
        FilteredSymbols.Clear();

        if (_selectedCategory == "all" || string.IsNullOrEmpty(_selectedCategory))
        {
            foreach (var symbol in AvailableSymbols)
                FilteredSymbols.Add(symbol);
        }
        else
        {
            foreach (var symbol in AvailableSymbols.Where(s => s.Category == _selectedCategory))
                FilteredSymbols.Add(symbol);
        }
    }

    public void SelectSymbol(string symbolName)
    {
        var symbol = AvailableSymbols.FirstOrDefault(s => s.Name == symbolName);
        if (symbol != null && SelectedItem?.ItemType == TabelaItemType.Symbol)
        {
            SelectedItem.SymbolName = symbol.Name;
            SelectedItem.Content = symbol.DisplayName;
            OnItemsChanged();
        }
    }

    public void OnItemsChanged()
    {
        ItemsChanged?.Invoke();
    }

    /// <summary>
    /// Animasyon tick'i - her frame'de çağrılır
    /// </summary>
    public void OnAnimationTick(double deltaTime)
    {
        if (!IsAnimationPlaying) return;
        
        // Stopwatch başlat (ilk çağrıda)
        if (!_animationStopwatch.IsRunning)
        {
            _animationStopwatch.Start();
        }
        
        bool hasScrollingItems = false;
        
        foreach (var item in Items)
        {
            if (item.IsScrolling && item.ItemType == TabelaItemType.Text)
            {
                hasScrollingItems = true;
                
                // Font'tan gerçek genişliği hesapla
                int estimatedWidth;
                int estimatedHeight;
                
                var font = GetFontByName(item.FontName);
                if (font != null && !string.IsNullOrEmpty(item.Content))
                {
                    // Her karakter için font genişliği + harf aralığı
                    estimatedWidth = 0;
                    foreach (var ch in item.Content)
                    {
                        var fontChar = font.GetCharacter(ch);
                        if (fontChar != null)
                        {
                            estimatedWidth += fontChar.XAdvance + item.LetterSpacing;
                        }
                        else
                        {
                            // Karakter bulunamazsa varsayılan genişlik
                            estimatedWidth += 6 + item.LetterSpacing;
                        }
                    }
                    estimatedHeight = font.LineHeight;
                }
                else
                {
                    // Font yoksa varsayılan tahmin
                    estimatedWidth = string.IsNullOrEmpty(item.Content) ? 0 : item.Content.Length * 7;
                    estimatedHeight = 12;
                }
                
                item.UpdateScrollOffset(deltaTime, estimatedWidth, estimatedHeight);
            }
        }
        
        // Throttling: Sadece belirli aralıklarla render yap (30 FPS)
        if (hasScrollingItems && _animationStopwatch.ElapsedMilliseconds >= AnimationRenderIntervalMs)
        {
            _animationStopwatch.Restart();
            OnItemsChanged();
        }
    }

    /// <summary>
    /// Kayan yazı olan öğe var mı
    /// </summary>
    public bool HasScrollingItems => Items.Any(i => i.IsScrolling && i.ItemType == TabelaItemType.Text);
}
