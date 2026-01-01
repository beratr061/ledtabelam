using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ReactiveUI;
using LEDTabelam.Models;
using LEDTabelam.Services;

namespace LEDTabelam.ViewModels;

#region Converters

/// <summary>
/// Bool değerini "Açık"/"Kapalı" metnine dönüştürür
/// </summary>
public class BoolToOnOffConverter : IValueConverter
{
    public static readonly BoolToOnOffConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOn)
            return isOn ? "Açık" : "Kapalı";
        return "Kapalı";
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Bool değerini seçim arka plan rengine dönüştürür
/// </summary>
public class BoolToSelectionBrushConverter : IValueConverter
{
    public static readonly BoolToSelectionBrushConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
            return new SolidColorBrush(Color.FromRgb(0, 90, 158));
        return new SolidColorBrush(Color.FromRgb(45, 45, 45));
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// TabelaItemType'ı ikon karakterine dönüştürür
/// </summary>
public class ItemTypeToIconConverter : IValueConverter
{
    public static readonly ItemTypeToIconConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TabelaItemType itemType)
        {
            return itemType switch
            {
                TabelaItemType.Text => "T",
                TabelaItemType.Symbol => "◆",
                TabelaItemType.Image => "🖼",
                TabelaItemType.Clock => "⏰",
                TabelaItemType.Date => "📅",
                _ => "?"
            };
        }
        return "?";
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Color'ı SolidColorBrush'a dönüştürür
/// </summary>
public class ColorToBrushConverter : IValueConverter
{
    public static readonly ColorToBrushConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
            return new SolidColorBrush(color);
        return Brushes.Transparent;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Öğe içeriğini görüntüleme metnine dönüştürür
/// </summary>
public class ItemContentDisplayConverter : IMultiValueConverter
{
    public static readonly ItemContentDisplayConverter Instance = new();
    
    public object? Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3) return "";
        
        var itemType = values[0] as TabelaItemType? ?? TabelaItemType.Text;
        var content = values[1] as string ?? "";
        var symbolName = values[2] as string ?? "";
        
        return itemType switch
        {
            TabelaItemType.Symbol => string.IsNullOrEmpty(symbolName) ? "(Sembol seçin)" : symbolName,
            TabelaItemType.Clock => "Saat",
            TabelaItemType.Date => "Tarih",
            _ => string.IsNullOrEmpty(content) ? "(Boş)" : content
        };
    }
}

/// <summary>
/// Bool değerini play/pause renk durumuna dönüştürür
/// </summary>
public class BoolToPlayColorConverter : IValueConverter
{
    public static readonly BoolToPlayColorConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPlaying && isPlaying)
            return new SolidColorBrush(Color.FromRgb(0, 255, 0));
        return new SolidColorBrush(Color.FromRgb(224, 224, 224));
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Bool değerini play/pause ikonuna dönüştürür
/// Requirements: 7.1
/// </summary>
public class BoolToPlayPauseIconConverter : IValueConverter
{
    public static readonly BoolToPlayPauseIconConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPlaying && isPlaying)
            return "⏸"; // Pause icon
        return "▶"; // Play icon
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// HorizontalAlignment'ı ComboBox index'ine dönüştürür
/// </summary>
public class HAlignToIndexConverter : IValueConverter
{
    public static readonly HAlignToIndexConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is HorizontalAlignment align)
        {
            return align switch
            {
                HorizontalAlignment.Left => 0,
                HorizontalAlignment.Center => 1,
                HorizontalAlignment.Right => 2,
                _ => 0
            };
        }
        return 0;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                0 => HorizontalAlignment.Left,
                1 => HorizontalAlignment.Center,
                2 => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Left
            };
        }
        return HorizontalAlignment.Left;
    }
}

/// <summary>
/// VerticalAlignment'ı ComboBox index'ine dönüştürür
/// </summary>
public class VAlignToIndexConverter : IValueConverter
{
    public static readonly VAlignToIndexConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VerticalAlignment align)
        {
            return align switch
            {
                VerticalAlignment.Top => 0,
                VerticalAlignment.Center => 1,
                VerticalAlignment.Bottom => 2,
                _ => 0
            };
        }
        return 0;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                0 => VerticalAlignment.Top,
                1 => VerticalAlignment.Center,
                2 => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Top
            };
        }
        return VerticalAlignment.Top;
    }
}

/// <summary>
/// ScrollDirection'ı ComboBox index'ine dönüştürür
/// </summary>
public class ScrollDirToIndexConverter : IValueConverter
{
    public static readonly ScrollDirToIndexConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ScrollDirection dir)
        {
            return dir switch
            {
                ScrollDirection.Left => 0,
                ScrollDirection.Right => 1,
                ScrollDirection.Up => 2,
                ScrollDirection.Down => 3,
                _ => 0
            };
        }
        return 0;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                0 => ScrollDirection.Left,
                1 => ScrollDirection.Right,
                2 => ScrollDirection.Up,
                3 => ScrollDirection.Down,
                _ => ScrollDirection.Left
            };
        }
        return ScrollDirection.Left;
    }
}

/// <summary>
/// ProgramTransitionType'ı ComboBox index'ine dönüştürür
/// Requirements: 3.1
/// </summary>
public class ProgramTransitionToIndexConverter : IValueConverter
{
    public static readonly ProgramTransitionToIndexConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProgramTransitionType transition)
        {
            return transition switch
            {
                ProgramTransitionType.Direct => 0,
                ProgramTransitionType.Fade => 1,
                ProgramTransitionType.SlideLeft => 2,
                ProgramTransitionType.SlideRight => 3,
                ProgramTransitionType.SlideUp => 4,
                ProgramTransitionType.SlideDown => 5,
                _ => 0
            };
        }
        return 0;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                0 => ProgramTransitionType.Direct,
                1 => ProgramTransitionType.Fade,
                2 => ProgramTransitionType.SlideLeft,
                3 => ProgramTransitionType.SlideRight,
                4 => ProgramTransitionType.SlideUp,
                5 => ProgramTransitionType.SlideDown,
                _ => ProgramTransitionType.Direct
            };
        }
        return ProgramTransitionType.Direct;
    }
}

/// <summary>
/// StopAnimationType'ı ComboBox index'ine dönüştürür
/// Requirements: 6.1
/// </summary>
public class StopAnimationToIndexConverter : IValueConverter
{
    public static readonly StopAnimationToIndexConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is StopAnimationType animation)
        {
            return animation switch
            {
                StopAnimationType.Direct => 0,
                StopAnimationType.Fade => 1,
                StopAnimationType.SlideUp => 2,
                StopAnimationType.SlideDown => 3,
                _ => 0
            };
        }
        return 0;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                0 => StopAnimationType.Direct,
                1 => StopAnimationType.Fade,
                2 => StopAnimationType.SlideUp,
                3 => StopAnimationType.SlideDown,
                _ => StopAnimationType.Direct
            };
        }
        return StopAnimationType.Direct;
    }
}

#endregion

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

    // Program yönetimi için
    // Requirements: 1.1, 1.2, 1.5, 1.6, 1.7
    private ObservableCollection<TabelaProgram> _programs = new();
    private TabelaProgram? _selectedProgram;
    private int _nextProgramId = 1;
    private IProgramSequencer? _programSequencer;

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
    
    /// <summary>
    /// Seçili öğe çok renkli mod kullanıyor mu
    /// </summary>
    public bool IsColoredSegmentsMode => SelectedItem?.UseColoredSegments == true;

    /// <summary>
    /// Seçili segment index'i (çok renkli modda)
    /// </summary>
    private int _selectedSegmentIndex = -1;
    public int SelectedSegmentIndex
    {
        get => _selectedSegmentIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSegmentIndex, value);
            this.RaisePropertyChanged(nameof(SelectedSegment));
            this.RaisePropertyChanged(nameof(HasSegmentSelection));
        }
    }

    /// <summary>
    /// Seçili segment
    /// </summary>
    public ColoredTextSegment? SelectedSegment => 
        SelectedItem?.UseColoredSegments == true && 
        _selectedSegmentIndex >= 0 && 
        _selectedSegmentIndex < (SelectedItem?.ColoredSegments.Count ?? 0)
            ? SelectedItem?.ColoredSegments[_selectedSegmentIndex]
            : null;

    /// <summary>
    /// Segment seçili mi
    /// </summary>
    public bool HasSegmentSelection => SelectedSegment != null;

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

    #region Program Yönetimi Properties
    // Requirements: 1.1, 1.2, 1.5, 1.6, 1.7

    /// <summary>
    /// Program koleksiyonu
    /// </summary>
    public ObservableCollection<TabelaProgram> Programs
    {
        get => _programs;
        set
        {
            this.RaiseAndSetIfChanged(ref _programs, value ?? new ObservableCollection<TabelaProgram>());
            if (_programs.Count > 0 && SelectedProgram == null)
            {
                SelectedProgram = _programs[0];
            }
            UpdateNextProgramId();
        }
    }

    /// <summary>
    /// Seçili program
    /// </summary>
    public TabelaProgram? SelectedProgram
    {
        get => _selectedProgram;
        set
        {
            // Önce mevcut programın öğelerini kaydet
            if (_selectedProgram != null)
            {
                SaveCurrentItemsToProgram();
                _selectedProgram.IsActive = false;
            }
            
            this.RaiseAndSetIfChanged(ref _selectedProgram, value);
            
            if (_selectedProgram != null)
            {
                _selectedProgram.IsActive = true;
                // Seçili programın öğelerini Items koleksiyonuna yükle
                LoadProgramItems(_selectedProgram);
            }
            
            this.RaisePropertyChanged(nameof(HasPrograms));
            this.RaisePropertyChanged(nameof(CanRemoveProgram));
            this.RaisePropertyChanged(nameof(CurrentProgramDisplay));
        }
    }

    /// <summary>
    /// Program var mı
    /// </summary>
    public bool HasPrograms => Programs.Count > 0;

    /// <summary>
    /// Program silinebilir mi (en az 2 program varsa)
    /// Requirements: 1.8
    /// </summary>
    public bool CanRemoveProgram => Programs.Count > 1;

    /// <summary>
    /// Mevcut program gösterimi (örn: "1/3")
    /// Requirements: 7.5
    /// </summary>
    public string CurrentProgramDisplay
    {
        get
        {
            if (Programs.Count == 0 || SelectedProgram == null)
                return "0/0";
            
            var index = Programs.IndexOf(SelectedProgram);
            return $"{index + 1}/{Programs.Count}";
        }
    }

    /// <summary>
    /// Mevcut ara durak gösterimi (örn: "Durak 1/5")
    /// Requirements: 10.5
    /// </summary>
    public string CurrentStopDisplay
    {
        get
        {
            if (SelectedItem?.IntermediateStops?.IsEnabled != true)
                return "Durak Yok";
            
            var stops = SelectedItem.IntermediateStops.Stops;
            if (stops.Count == 0)
                return "Durak Yok";
            
            // ProgramSequencer'dan mevcut durak index'ini al
            int currentIndex = 0;
            if (_programSequencer is ProgramSequencer sequencer)
            {
                currentIndex = sequencer.GetCurrentStopIndex(SelectedItem.Id);
            }
            
            var currentStop = currentIndex < stops.Count ? stops[currentIndex] : null;
            var stopName = currentStop?.StopName ?? "?";
            
            return $"{stopName} ({currentIndex + 1}/{stops.Count})";
        }
    }

    /// <summary>
    /// Oynatma durumu metni
    /// Requirements: 10.5, 10.6
    /// </summary>
    public string PlaybackStatusText
    {
        get
        {
            if (IsAnimationPlaying)
                return "▶ Oynatılıyor";
            return "⏸ Duraklatıldı";
        }
    }

    /// <summary>
    /// Program sequencer referansı
    /// </summary>
    public IProgramSequencer? ProgramSequencer
    {
        get => _programSequencer;
        set
        {
            // Eski sequencer'dan event'leri kaldır
            if (_programSequencer != null)
            {
                _programSequencer.ProgramChanged -= OnSequencerProgramChanged;
                _programSequencer.StopChanged -= OnSequencerStopChanged;
                _programSequencer.MainContentShowing -= OnSequencerMainContentShowing;
            }
            
            _programSequencer = value;
            
            if (_programSequencer != null)
            {
                _programSequencer.Programs = Programs;
                
                // Yeni sequencer'a event'leri bağla
                // Requirements: 8.1, 8.2
                _programSequencer.ProgramChanged += OnSequencerProgramChanged;
                _programSequencer.StopChanged += OnSequencerStopChanged;
                _programSequencer.MainContentShowing += OnSequencerMainContentShowing;
            }
        }
    }

    /// <summary>
    /// ProgramSequencer program değişikliği event handler'ı
    /// Requirements: 8.1
    /// </summary>
    private void OnSequencerProgramChanged(TabelaProgram program)
    {
        // UI thread'de property değişikliklerini bildir
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            this.RaisePropertyChanged(nameof(CurrentProgramDisplay));
            this.RaisePropertyChanged(nameof(CurrentStopDisplay));
        });
    }

    /// <summary>
    /// ProgramSequencer ara durak değişikliği event handler'ı
    /// Requirements: 8.1, 8.2
    /// </summary>
    private void OnSequencerStopChanged(TabelaItem item, IntermediateStop stop)
    {
        // UI thread'de property değişikliklerini bildir
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            this.RaisePropertyChanged(nameof(CurrentStopDisplay));
        });
    }

    /// <summary>
    /// ProgramSequencer ana içerik gösterimi event handler'ı
    /// Requirements: 8.1, 8.2
    /// </summary>
    private void OnSequencerMainContentShowing(TabelaItem item)
    {
        // UI thread'de property değişikliklerini bildir
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            this.RaisePropertyChanged(nameof(CurrentStopDisplay));
        });
    }

    #endregion

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
            
            // Status bar güncellemesi
            // Requirements: 10.5, 10.6
            this.RaisePropertyChanged(nameof(PlaybackStatusText));
            
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
    
    // Çok renkli metin komutları
    public ReactiveCommand<Unit, Unit> EnableColoredSegmentsCommand { get; }
    public ReactiveCommand<Unit, Unit> DisableColoredSegmentsCommand { get; }
    public ReactiveCommand<Unit, Unit> ApplyRainbowColorsCommand { get; }

    // Program yönetimi komutları
    // Requirements: 1.1, 1.2, 1.5, 1.6, 1.7
    public ReactiveCommand<Unit, Unit> AddProgramCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveProgramCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveProgramUpCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveProgramDownCommand { get; }

    // Playback kontrol komutları
    // Requirements: 7.1, 7.4, 7.5
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    public ReactiveCommand<Unit, Unit> PauseCommand { get; }
    public ReactiveCommand<Unit, Unit> NextProgramCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviousProgramCommand { get; }

    // Ara durak yönetimi komutları
    // Requirements: 4.3, 4.4, 4.5, 4.7, 4.8
    public ReactiveCommand<Unit, Unit> AddIntermediateStopCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveIntermediateStopCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveStopUpCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveStopDownCommand { get; }

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
        
        // Çok renkli metin komutları
        var isTextSelected = this.WhenAnyValue(x => x.IsTextItemSelected);
        EnableColoredSegmentsCommand = ReactiveCommand.Create(EnableColoredSegments, isTextSelected);
        DisableColoredSegmentsCommand = ReactiveCommand.Create(DisableColoredSegments, isTextSelected);
        ApplyRainbowColorsCommand = ReactiveCommand.Create(ApplyRainbowColors, isTextSelected);

        // Program yönetimi komutları
        // Requirements: 1.1, 1.2, 1.5, 1.6, 1.7
        var canRemoveProgram = this.WhenAnyValue(x => x.CanRemoveProgram);
        var hasPrograms = this.WhenAnyValue(x => x.HasPrograms);
        var hasSelectedProgram = this.WhenAnyValue(x => x.SelectedProgram).Select(p => p != null);
        
        AddProgramCommand = ReactiveCommand.Create(AddProgram);
        RemoveProgramCommand = ReactiveCommand.Create(RemoveSelectedProgram, canRemoveProgram);
        MoveProgramUpCommand = ReactiveCommand.Create(MoveProgramUp, hasSelectedProgram);
        MoveProgramDownCommand = ReactiveCommand.Create(MoveProgramDown, hasSelectedProgram);

        // Playback kontrol komutları
        // Requirements: 7.1, 7.4, 7.5
        PlayCommand = ReactiveCommand.Create(PlayPrograms, hasPrograms);
        PauseCommand = ReactiveCommand.Create(PausePrograms);
        NextProgramCommand = ReactiveCommand.Create(GoToNextProgram, hasPrograms);
        PreviousProgramCommand = ReactiveCommand.Create(GoToPreviousProgram, hasPrograms);

        // Ara durak yönetimi komutları
        // Requirements: 4.3, 4.4, 4.5, 4.7, 4.8
        AddIntermediateStopCommand = ReactiveCommand.Create(AddIntermediateStop, isTextSelected);
        RemoveIntermediateStopCommand = ReactiveCommand.Create(RemoveSelectedIntermediateStop, isTextSelected);
        MoveStopUpCommand = ReactiveCommand.Create(MoveStopUp, isTextSelected);
        MoveStopDownCommand = ReactiveCommand.Create(MoveStopDown, isTextSelected);

        Items.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (TabelaItem item in e.NewItems)
                {
                    item.PropertyChanged += OnItemPropertyChanged;
                    // Border property değişikliklerini de dinle
                    item.Border.PropertyChanged += OnBorderPropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (TabelaItem item in e.OldItems)
                {
                    item.PropertyChanged -= OnItemPropertyChanged;
                    item.Border.PropertyChanged -= OnBorderPropertyChanged;
                }
            }
        };

        // Varsayılan program oluştur
        InitializeDefaultProgram();
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TabelaItem.IsSelected))
        {
            OnItemsChanged();
        }
    }

    private void OnBorderPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Border ayarları değiştiğinde render'ı tetikle
        OnItemsChanged();
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
        
        // Varsayılan öğeleri seçili programa da ekle
        if (SelectedProgram != null)
        {
            SelectedProgram.Items.Clear();
            foreach (var item in Items)
            {
                SelectedProgram.Items.Add(item);
            }
        }
        
        OnItemsChanged();
    }

    private void AddTextItem()
    {
        var itemWidth = Math.Min(60, DisplayWidth);
        var itemHeight = Math.Min(DisplayHeight, DisplayHeight);
        var (x, y) = FindEmptySpace(itemWidth, itemHeight);
        
        var item = new TabelaItem
        {
            Id = _nextItemId++,
            Name = $"Metin {Items.Count + 1}",
            Content = "YENİ METİN",
            ItemType = TabelaItemType.Text,
            X = x, Y = y,
            Width = itemWidth,
            Height = itemHeight,
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
        var (x, y) = FindEmptySpace(symbolSize, symbolSize);
        
        var item = new TabelaItem
        {
            Id = _nextItemId++,
            Name = $"Sembol {Items.Count + 1}",
            Content = "",
            ItemType = TabelaItemType.Symbol,
            SymbolName = "",
            SymbolSize = symbolSize,
            X = x,
            Y = y,
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

    /// <summary>
    /// Tabelada boş alan bulur. Bulamazsa (0,0) döner.
    /// </summary>
    private (int x, int y) FindEmptySpace(int width, int height)
    {
        // Önce mevcut öğelerin kapladığı alanları belirle
        var occupiedRects = Items.Select(i => new { i.X, i.Y, Right = i.X + i.Width, Bottom = i.Y + i.Height }).ToList();
        
        // Satır satır tara, boş alan bul
        for (int y = 0; y <= DisplayHeight - height; y++)
        {
            for (int x = 0; x <= DisplayWidth - width; x++)
            {
                // Bu pozisyon boş mu kontrol et
                bool isEmpty = true;
                foreach (var rect in occupiedRects)
                {
                    // Çakışma kontrolü
                    if (x < rect.Right && x + width > rect.X &&
                        y < rect.Bottom && y + height > rect.Y)
                    {
                        isEmpty = false;
                        // Çakışan öğenin sağına atla
                        x = rect.Right - 1; // for döngüsü x++ yapacak
                        break;
                    }
                }
                
                if (isEmpty)
                {
                    return (x, y);
                }
            }
        }
        
        // Boş alan bulunamadı, sağ alt köşeye yerleştir (mevcut öğelerin altına)
        if (occupiedRects.Count > 0)
        {
            var maxBottom = occupiedRects.Max(r => r.Bottom);
            if (maxBottom < DisplayHeight)
            {
                return (0, Math.Min(maxBottom, DisplayHeight - height));
            }
        }
        
        // Hiç yer yoksa (0,0)
        return (0, 0);
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
        // Öğeler değiştiğinde mevcut programa kaydet
        SyncItemsToCurrentProgram();
        ItemsChanged?.Invoke();
    }

    /// <summary>
    /// Items koleksiyonunu mevcut programa senkronize eder
    /// </summary>
    private void SyncItemsToCurrentProgram()
    {
        if (SelectedProgram == null) return;
        
        // Programın Items koleksiyonunu güncelle
        SelectedProgram.Items.Clear();
        foreach (var item in Items)
        {
            SelectedProgram.Items.Add(item);
        }
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

    #region Çok Renkli Metin Metodları

    /// <summary>
    /// Seçili metin öğesini çok renkli moda geçirir
    /// Her harf ayrı bir segment olur
    /// </summary>
    private void EnableColoredSegments()
    {
        if (SelectedItem == null || SelectedItem.ItemType != TabelaItemType.Text) return;
        
        SelectedItem.ConvertToColoredSegments();
        SelectedSegmentIndex = SelectedItem.ColoredSegments.Count > 0 ? 0 : -1;
        
        this.RaisePropertyChanged(nameof(IsColoredSegmentsMode));
        OnItemsChanged();
    }

    /// <summary>
    /// Çok renkli modu kapatır, tek renkli moda döner
    /// </summary>
    private void DisableColoredSegments()
    {
        if (SelectedItem == null || SelectedItem.ItemType != TabelaItemType.Text) return;
        
        SelectedItem.ConvertToSingleColor();
        SelectedSegmentIndex = -1;
        
        this.RaisePropertyChanged(nameof(IsColoredSegmentsMode));
        OnItemsChanged();
    }

    /// <summary>
    /// Gökkuşağı renkleri uygular (her harfe farklı renk)
    /// </summary>
    private void ApplyRainbowColors()
    {
        if (SelectedItem == null || SelectedItem.ItemType != TabelaItemType.Text) return;
        
        // Önce çok renkli moda geç
        if (!SelectedItem.UseColoredSegments)
        {
            SelectedItem.ConvertToColoredSegments();
        }
        
        // Gökkuşağı renkleri
        var rainbowColors = new[]
        {
            Color.FromRgb(255, 0, 0),     // Kırmızı
            Color.FromRgb(255, 127, 0),   // Turuncu
            Color.FromRgb(255, 255, 0),   // Sarı
            Color.FromRgb(0, 255, 0),     // Yeşil
            Color.FromRgb(0, 0, 255),     // Mavi
            Color.FromRgb(75, 0, 130),    // Indigo
            Color.FromRgb(148, 0, 211)    // Mor
        };
        
        for (int i = 0; i < SelectedItem.ColoredSegments.Count; i++)
        {
            SelectedItem.ColoredSegments[i].Color = rainbowColors[i % rainbowColors.Length];
        }
        
        this.RaisePropertyChanged(nameof(IsColoredSegmentsMode));
        OnItemsChanged();
    }

    /// <summary>
    /// Belirli bir segmentin rengini değiştirir
    /// </summary>
    public void SetSegmentColor(int segmentIndex, Color color)
    {
        if (SelectedItem?.UseColoredSegments != true) return;
        if (segmentIndex < 0 || segmentIndex >= SelectedItem.ColoredSegments.Count) return;
        
        SelectedItem.ColoredSegments[segmentIndex].Color = color;
        OnItemsChanged();
    }

    /// <summary>
    /// Seçili segmentin rengini değiştirir
    /// </summary>
    public void SetSelectedSegmentColor(Color color)
    {
        if (SelectedSegment != null)
        {
            SelectedSegment.Color = color;
            OnItemsChanged();
        }
    }

    /// <summary>
    /// Segment seçer
    /// </summary>
    public void SelectSegment(int index)
    {
        if (SelectedItem?.UseColoredSegments != true) return;
        if (index < 0 || index >= SelectedItem.ColoredSegments.Count)
        {
            SelectedSegmentIndex = -1;
            return;
        }
        SelectedSegmentIndex = index;
    }

    /// <summary>
    /// Tüm segmentlere aynı rengi uygular
    /// </summary>
    public void ApplyColorToAllSegments(Color color)
    {
        if (SelectedItem?.UseColoredSegments != true) return;
        
        foreach (var segment in SelectedItem.ColoredSegments)
        {
            segment.Color = color;
        }
        OnItemsChanged();
    }

    /// <summary>
    /// Gradient (geçişli) renk uygular
    /// </summary>
    public void ApplyGradientColors(Color startColor, Color endColor)
    {
        if (SelectedItem?.UseColoredSegments != true) return;
        if (SelectedItem.ColoredSegments.Count == 0) return;
        
        int count = SelectedItem.ColoredSegments.Count;
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0;
            var color = Color.FromRgb(
                (byte)(startColor.R + (endColor.R - startColor.R) * t),
                (byte)(startColor.G + (endColor.G - startColor.G) * t),
                (byte)(startColor.B + (endColor.B - startColor.B) * t)
            );
            SelectedItem.ColoredSegments[i].Color = color;
        }
        OnItemsChanged();
    }

    #endregion

    #region Program Yönetimi Metodları
    // Requirements: 1.1, 1.2, 1.5, 1.6, 1.7

    /// <summary>
    /// Varsayılan program oluşturur
    /// </summary>
    private void InitializeDefaultProgram()
    {
        if (Programs.Count == 0)
        {
            var defaultProgram = new TabelaProgram
            {
                Id = _nextProgramId++,
                Name = "Program 1"
            };
            Programs.Add(defaultProgram);
            SelectedProgram = defaultProgram;
        }
        
        // Varsayılan öğeleri ekle
        AddDefaultItems();
    }

    /// <summary>
    /// Sonraki program ID'sini günceller
    /// </summary>
    private void UpdateNextProgramId()
    {
        if (Programs.Count > 0)
        {
            _nextProgramId = Programs.Max(p => p.Id) + 1;
        }
        else
        {
            _nextProgramId = 1;
        }
    }

    /// <summary>
    /// Yeni program ekler
    /// Requirements: 1.1, 1.2
    /// </summary>
    public void AddProgram()
    {
        var newProgram = new TabelaProgram
        {
            Id = _nextProgramId++,
            Name = $"Program {Programs.Count + 1}"
        };
        Programs.Add(newProgram);
        SelectedProgram = newProgram;
        
        this.RaisePropertyChanged(nameof(HasPrograms));
        this.RaisePropertyChanged(nameof(CanRemoveProgram));
        this.RaisePropertyChanged(nameof(CurrentProgramDisplay));
    }

    /// <summary>
    /// Seçili programı siler
    /// Requirements: 1.7, 1.8
    /// </summary>
    public void RemoveSelectedProgram()
    {
        if (SelectedProgram == null || Programs.Count <= 1)
            return;

        var index = Programs.IndexOf(SelectedProgram);
        Programs.Remove(SelectedProgram);
        
        // Yeni seçim yap
        if (Programs.Count > 0)
        {
            SelectedProgram = Programs[Math.Min(index, Programs.Count - 1)];
        }
        
        this.RaisePropertyChanged(nameof(HasPrograms));
        this.RaisePropertyChanged(nameof(CanRemoveProgram));
        this.RaisePropertyChanged(nameof(CurrentProgramDisplay));
    }

    /// <summary>
    /// Belirtilen programı seçer
    /// Requirements: 1.5
    /// </summary>
    public void SelectProgram(TabelaProgram program)
    {
        if (program != null && Programs.Contains(program))
        {
            SelectedProgram = program;
        }
    }

    /// <summary>
    /// Seçili programı yukarı taşır (sıralama)
    /// Requirements: 1.6
    /// </summary>
    public void MoveProgramUp()
    {
        if (SelectedProgram == null) return;
        
        var index = Programs.IndexOf(SelectedProgram);
        if (index > 0)
        {
            Programs.Move(index, index - 1);
            this.RaisePropertyChanged(nameof(CurrentProgramDisplay));
        }
    }

    /// <summary>
    /// Seçili programı aşağı taşır (sıralama)
    /// Requirements: 1.6
    /// </summary>
    public void MoveProgramDown()
    {
        if (SelectedProgram == null) return;
        
        var index = Programs.IndexOf(SelectedProgram);
        if (index < Programs.Count - 1)
        {
            Programs.Move(index, index + 1);
            this.RaisePropertyChanged(nameof(CurrentProgramDisplay));
        }
    }

    /// <summary>
    /// Seçili programın öğelerini Items koleksiyonuna yükler
    /// </summary>
    private void LoadProgramItems(TabelaProgram program)
    {
        // Mevcut öğeleri temizle
        Items.Clear();
        SelectedItem = null;
        
        // Programın öğelerini yükle
        foreach (var item in program.Items)
        {
            Items.Add(item);
        }
        
        // İlk öğeyi seç
        if (Items.Count > 0)
        {
            SelectedItem = Items[0];
        }
        
        // NextItemId'yi güncelle
        if (Items.Count > 0)
        {
            _nextItemId = Items.Max(i => i.Id) + 1;
        }
        else
        {
            _nextItemId = 1;
        }
        
        OnItemsChanged();
    }

    /// <summary>
    /// Mevcut Items koleksiyonunu seçili programa kaydeder
    /// </summary>
    public void SaveCurrentItemsToProgram()
    {
        if (SelectedProgram == null) return;
        
        SelectedProgram.Items.Clear();
        foreach (var item in Items)
        {
            SelectedProgram.Items.Add(item);
        }
    }

    /// <summary>
    /// Programları bir Profile'dan yükler
    /// </summary>
    public void LoadProgramsFromProfile(Profile profile)
    {
        if (profile == null) return;
        
        // Minimum program garantisi
        profile.EnsureMinimumProgram();
        
        Programs = profile.Programs;
        
        if (Programs.Count > 0)
        {
            SelectedProgram = Programs[0];
        }
        
        // Sequencer'ı güncelle
        if (_programSequencer != null)
        {
            _programSequencer.Programs = Programs;
        }
    }

    #endregion

    #region Playback Kontrol Metodları
    // Requirements: 7.1, 7.4, 7.5

    /// <summary>
    /// Program döngüsünü başlatır
    /// Requirements: 7.2
    /// </summary>
    private void PlayPrograms()
    {
        if (_programSequencer != null)
        {
            // Önce mevcut öğeleri kaydet
            SaveCurrentItemsToProgram();
            
            // Sequencer'ın Programs referansını güncelle (Items değişmiş olabilir)
            _programSequencer.Programs = Programs;
            
            _programSequencer.Play();
        }
        IsAnimationPlaying = true;
    }

    /// <summary>
    /// Program döngüsünü duraklatır
    /// Requirements: 7.3
    /// </summary>
    private void PausePrograms()
    {
        if (_programSequencer != null)
        {
            _programSequencer.Stop(); // Pause yerine Stop - ilk programa döner
        }
        IsAnimationPlaying = false;
        
        // İlk programa dön
        if (Programs.Count > 0)
        {
            SaveCurrentItemsToProgram();
            SelectedProgram = Programs[0];
        }
        
        this.RaisePropertyChanged(nameof(CurrentProgramDisplay));
    }

    /// <summary>
    /// Sonraki programa geçer
    /// Requirements: 7.4
    /// </summary>
    private void GoToNextProgram()
    {
        if (_programSequencer != null)
        {
            SaveCurrentItemsToProgram();
            _programSequencer.NextProgram();
            
            // Sequencer'ın mevcut programını seç
            if (_programSequencer.CurrentProgram != null)
            {
                SelectedProgram = _programSequencer.CurrentProgram;
            }
        }
        else if (Programs.Count > 0 && SelectedProgram != null)
        {
            var index = Programs.IndexOf(SelectedProgram);
            var nextIndex = (index + 1) % Programs.Count;
            SaveCurrentItemsToProgram();
            SelectedProgram = Programs[nextIndex];
        }
        
        this.RaisePropertyChanged(nameof(CurrentProgramDisplay));
    }

    /// <summary>
    /// Önceki programa geçer
    /// Requirements: 7.4
    /// </summary>
    private void GoToPreviousProgram()
    {
        if (_programSequencer != null)
        {
            SaveCurrentItemsToProgram();
            _programSequencer.PreviousProgram();
            
            // Sequencer'ın mevcut programını seç
            if (_programSequencer.CurrentProgram != null)
            {
                SelectedProgram = _programSequencer.CurrentProgram;
            }
        }
        else if (Programs.Count > 0 && SelectedProgram != null)
        {
            var index = Programs.IndexOf(SelectedProgram);
            var prevIndex = index > 0 ? index - 1 : Programs.Count - 1;
            SaveCurrentItemsToProgram();
            SelectedProgram = Programs[prevIndex];
        }
        
        this.RaisePropertyChanged(nameof(CurrentProgramDisplay));
    }

    #endregion

    #region Ara Durak Yönetimi Metodları
    // Requirements: 4.3, 4.4, 4.5, 4.7, 4.8

    // Seçili ara durak index'i
    private int _selectedStopIndex = -1;
    
    /// <summary>
    /// Seçili ara durak index'i
    /// </summary>
    public int SelectedStopIndex
    {
        get => _selectedStopIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedStopIndex, value);
            this.RaisePropertyChanged(nameof(SelectedStop));
            this.RaisePropertyChanged(nameof(HasSelectedStop));
        }
    }

    /// <summary>
    /// Seçili ara durak
    /// </summary>
    public IntermediateStop? SelectedStop
    {
        get
        {
            if (SelectedItem?.IntermediateStops?.Stops == null) return null;
            if (_selectedStopIndex < 0 || _selectedStopIndex >= SelectedItem.IntermediateStops.Stops.Count) return null;
            return SelectedItem.IntermediateStops.Stops[_selectedStopIndex];
        }
    }

    /// <summary>
    /// Ara durak seçili mi
    /// </summary>
    public bool HasSelectedStop => SelectedStop != null;

    /// <summary>
    /// Yeni ara durak ekler
    /// Requirements: 4.3, 4.4
    /// </summary>
    public void AddIntermediateStop()
    {
        if (SelectedItem == null || SelectedItem.ItemType != TabelaItemType.Text) return;
        
        var stops = SelectedItem.IntermediateStops.Stops;
        var newStop = new IntermediateStop
        {
            Order = stops.Count,
            StopName = $"Durak {stops.Count + 1}"
        };
        
        stops.Add(newStop);
        
        // Ara durak sistemini aktif et
        if (!SelectedItem.IntermediateStops.IsEnabled)
        {
            SelectedItem.IntermediateStops.IsEnabled = true;
        }
        
        SelectedStopIndex = stops.Count - 1;
        OnItemsChanged();
    }

    /// <summary>
    /// Belirtilen isimle ara durak ekler
    /// Requirements: 4.3, 4.4
    /// </summary>
    public void AddIntermediateStop(string stopName)
    {
        if (SelectedItem == null || SelectedItem.ItemType != TabelaItemType.Text) return;
        
        var stops = SelectedItem.IntermediateStops.Stops;
        var newStop = new IntermediateStop
        {
            Order = stops.Count,
            StopName = stopName ?? $"Durak {stops.Count + 1}"
        };
        
        stops.Add(newStop);
        
        // Ara durak sistemini aktif et
        if (!SelectedItem.IntermediateStops.IsEnabled)
        {
            SelectedItem.IntermediateStops.IsEnabled = true;
        }
        
        SelectedStopIndex = stops.Count - 1;
        OnItemsChanged();
    }

    /// <summary>
    /// Seçili ara durağı siler
    /// Requirements: 4.8
    /// </summary>
    public void RemoveSelectedIntermediateStop()
    {
        if (SelectedItem == null || SelectedStop == null) return;
        
        var stops = SelectedItem.IntermediateStops.Stops;
        var index = _selectedStopIndex;
        
        stops.RemoveAt(index);
        
        // Order değerlerini güncelle
        for (int i = 0; i < stops.Count; i++)
        {
            stops[i].Order = i;
        }
        
        // Yeni seçim yap
        if (stops.Count > 0)
        {
            SelectedStopIndex = Math.Min(index, stops.Count - 1);
        }
        else
        {
            SelectedStopIndex = -1;
        }
        
        OnItemsChanged();
    }

    /// <summary>
    /// Belirtilen ara durağı siler
    /// Requirements: 4.8
    /// </summary>
    public void RemoveIntermediateStop(IntermediateStop stop)
    {
        if (SelectedItem == null || stop == null) return;
        
        var stops = SelectedItem.IntermediateStops.Stops;
        stops.Remove(stop);
        
        // Order değerlerini güncelle
        for (int i = 0; i < stops.Count; i++)
        {
            stops[i].Order = i;
        }
        
        OnItemsChanged();
    }

    /// <summary>
    /// Seçili ara durağı yukarı taşır
    /// Requirements: 4.7
    /// </summary>
    public void MoveStopUp()
    {
        if (SelectedItem == null || SelectedStop == null) return;
        
        var stops = SelectedItem.IntermediateStops.Stops;
        var index = _selectedStopIndex;
        
        if (index > 0)
        {
            // Swap
            var temp = stops[index];
            stops[index] = stops[index - 1];
            stops[index - 1] = temp;
            
            // Order değerlerini güncelle
            stops[index].Order = index;
            stops[index - 1].Order = index - 1;
            
            SelectedStopIndex = index - 1;
            OnItemsChanged();
        }
    }

    /// <summary>
    /// Seçili ara durağı aşağı taşır
    /// Requirements: 4.7
    /// </summary>
    public void MoveStopDown()
    {
        if (SelectedItem == null || SelectedStop == null) return;
        
        var stops = SelectedItem.IntermediateStops.Stops;
        var index = _selectedStopIndex;
        
        if (index < stops.Count - 1)
        {
            // Swap
            var temp = stops[index];
            stops[index] = stops[index + 1];
            stops[index + 1] = temp;
            
            // Order değerlerini güncelle
            stops[index].Order = index;
            stops[index + 1].Order = index + 1;
            
            SelectedStopIndex = index + 1;
            OnItemsChanged();
        }
    }

    /// <summary>
    /// Ara durak seçer
    /// </summary>
    public void SelectStop(int index)
    {
        if (SelectedItem?.IntermediateStops?.Stops == null) return;
        
        if (index >= 0 && index < SelectedItem.IntermediateStops.Stops.Count)
        {
            SelectedStopIndex = index;
        }
        else
        {
            SelectedStopIndex = -1;
        }
    }

    #endregion
}
