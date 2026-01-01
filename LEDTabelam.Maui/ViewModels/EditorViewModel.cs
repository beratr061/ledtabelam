using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LEDTabelam.Maui.Models;
using SkiaSharp;
using HAlign = LEDTabelam.Maui.Models.HorizontalAlignment;
using VAlign = LEDTabelam.Maui.Models.VerticalAlignment;

namespace LEDTabelam.Maui.ViewModels;

/// <summary>
/// Düzenleyici paneli ViewModel'i
/// Metin ve içerik düzenleme işlemlerini yönetir
/// Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9, 6.10, 6.11
/// </summary>
public partial class EditorViewModel : ObservableObject
{
    private CancellationTokenSource? _debounceTokenSource;
    private const int DebounceDelayMs = 100;

    /// <summary>
    /// Canvas'ta gösterilecek tüm öğeler (mevcut programın içerikleri)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ContentItem> _items = new();

    [ObservableProperty]
    private ContentItem? _editingContent;

    [ObservableProperty]
    private string _text = "";

    [ObservableProperty]
    private string _selectedFont = "Default";

    [ObservableProperty]
    private int _fontSize = 16;

    [ObservableProperty]
    private Color _foregroundColor = Color.FromRgb(255, 176, 0); // Amber

    [ObservableProperty]
    private Color _backgroundColor = Colors.Transparent;

    [ObservableProperty]
    private HAlign _horizontalAlignment = HAlign.Center;

    [ObservableProperty]
    private VAlign _verticalAlignment = VAlign.Center;

    [ObservableProperty]
    private bool _isBold = false;

    [ObservableProperty]
    private bool _isItalic = false;

    [ObservableProperty]
    private bool _isUnderline = false;

    [ObservableProperty]
    private bool _isRightToLeft = false;

    [ObservableProperty]
    private bool _isScrolling = false;

    [ObservableProperty]
    private int _scrollSpeed = 20;

    [ObservableProperty]
    private int _positionX = 0;

    [ObservableProperty]
    private int _positionY = 0;

    [ObservableProperty]
    private int _contentWidth = 128;

    [ObservableProperty]
    private int _contentHeight = 16;

    [ObservableProperty]
    private SKBitmap? _miniPreview;

    [ObservableProperty]
    private ObservableCollection<string> _availableFonts = new()
    {
        "Default",
        "PolarisRGB6x8",
        "PolarisRGB6x10M",
        "PolarisRGB10x11",
        "PolarisA7x10",
        "PolarisA14x16"
    };

    [ObservableProperty]
    private ObservableCollection<int> _availableFontSizes = new()
    {
        8, 10, 12, 14, 16, 18, 20, 24, 28, 32
    };

    [ObservableProperty]
    private bool _hasEditingContent = false;

    [ObservableProperty]
    private bool _isTextContent = false;

    /// <summary>
    /// Mini önizleme güncelleme olayı
    /// </summary>
    public event EventHandler? PreviewUpdateRequested;

    /// <summary>
    /// Canvas öğeleri değiştiğinde tetiklenir
    /// </summary>
    public event EventHandler? ItemsChanged;

    /// <summary>
    /// Öğeyi seçer
    /// </summary>
    public void SelectItem(ContentItem item)
    {
        // Tüm seçimleri temizle
        foreach (var i in Items)
        {
            i.IsSelected = false;
        }
        
        // Yeni öğeyi seç
        item.IsSelected = true;
        EditingContent = item;
    }

    /// <summary>
    /// Seçimi temizler
    /// </summary>
    public void ClearSelection()
    {
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }
        EditingContent = null;
    }

    /// <summary>
    /// Önizleme güncelleme isteği tetikler (debounce olmadan)
    /// </summary>
    public void RaisePreviewUpdate()
    {
        PreviewUpdateRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Items değişikliğini bildirir
    /// </summary>
    public void RaiseItemsChanged()
    {
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnEditingContentChanged(ContentItem? value)
    {
        HasEditingContent = value != null;
        IsTextContent = value is TextContent;
        
        if (value != null)
        {
            LoadContentProperties(value);
        }
        else
        {
            ResetToDefaults();
        }
    }

    partial void OnTextChanged(string value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.Text = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnSelectedFontChanged(string value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.FontName = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnFontSizeChanged(int value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.FontSize = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnForegroundColorChanged(Color value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.ForegroundColor = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnBackgroundColorChanged(Color value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.BackgroundColor = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnHorizontalAlignmentChanged(HAlign value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.HorizontalAlignment = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnVerticalAlignmentChanged(VAlign value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.VerticalAlignment = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnIsBoldChanged(bool value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.IsBold = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnIsItalicChanged(bool value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.IsItalic = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnIsUnderlineChanged(bool value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.IsUnderline = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnIsRightToLeftChanged(bool value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.IsRightToLeft = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnIsScrollingChanged(bool value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.IsScrolling = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnScrollSpeedChanged(int value)
    {
        if (EditingContent is TextContent textContent)
        {
            textContent.ScrollSpeed = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnPositionXChanged(int value)
    {
        if (EditingContent != null)
        {
            EditingContent.X = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnPositionYChanged(int value)
    {
        if (EditingContent != null)
        {
            EditingContent.Y = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnContentWidthChanged(int value)
    {
        if (EditingContent != null)
        {
            EditingContent.Width = value;
            RequestPreviewUpdateDebounced();
        }
    }

    partial void OnContentHeightChanged(int value)
    {
        if (EditingContent != null)
        {
            EditingContent.Height = value;
            RequestPreviewUpdateDebounced();
        }
    }

    private void LoadContentProperties(ContentItem content)
    {
        // Ortak özellikler
        PositionX = content.X;
        PositionY = content.Y;
        ContentWidth = content.Width;
        ContentHeight = content.Height;

        // Metin içeriği özellikleri
        if (content is TextContent textContent)
        {
            Text = textContent.Text;
            SelectedFont = textContent.FontName;
            FontSize = textContent.FontSize;
            ForegroundColor = textContent.ForegroundColor;
            BackgroundColor = textContent.BackgroundColor;
            HorizontalAlignment = textContent.HorizontalAlignment;
            VerticalAlignment = textContent.VerticalAlignment;
            IsBold = textContent.IsBold;
            IsItalic = textContent.IsItalic;
            IsUnderline = textContent.IsUnderline;
            IsRightToLeft = textContent.IsRightToLeft;
            IsScrolling = textContent.IsScrolling;
            ScrollSpeed = textContent.ScrollSpeed;
        }
        else
        {
            // Metin dışı içerikler için varsayılanlar
            Text = "";
            SelectedFont = "Default";
            FontSize = 16;
            ForegroundColor = Color.FromRgb(255, 176, 0);
            BackgroundColor = Colors.Transparent;
            HorizontalAlignment = HAlign.Center;
            VerticalAlignment = VAlign.Center;
            IsBold = false;
            IsItalic = false;
            IsUnderline = false;
            IsRightToLeft = false;
            IsScrolling = false;
            ScrollSpeed = 20;
        }
    }

    private void ResetToDefaults()
    {
        Text = "";
        SelectedFont = "Default";
        FontSize = 16;
        ForegroundColor = Color.FromRgb(255, 176, 0);
        BackgroundColor = Colors.Transparent;
        HorizontalAlignment = HAlign.Center;
        VerticalAlignment = VAlign.Center;
        IsBold = false;
        IsItalic = false;
        IsUnderline = false;
        IsRightToLeft = false;
        IsScrolling = false;
        ScrollSpeed = 20;
        PositionX = 0;
        PositionY = 0;
        ContentWidth = 128;
        ContentHeight = 16;
    }

    /// <summary>
    /// Debounce ile önizleme güncelleme isteği
    /// Requirement: 6.11 - 100ms debounce
    /// </summary>
    private void RequestPreviewUpdateDebounced()
    {
        _debounceTokenSource?.Cancel();
        _debounceTokenSource = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelayMs, _debounceTokenSource.Token);
                PreviewUpdateRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                // Debounce cancelled
            }
        });
    }

    /// <summary>
    /// Mini önizlemeyi günceller
    /// Requirement: 6.10
    /// </summary>
    public void UpdateMiniPreview(SKBitmap? bitmap)
    {
        MiniPreview?.Dispose();
        MiniPreview = bitmap;
    }

    /// <summary>
    /// Sola hizalar
    /// Requirement: 6.5
    /// </summary>
    [RelayCommand]
    public void AlignLeft()
    {
        HorizontalAlignment = HAlign.Left;
    }

    /// <summary>
    /// Ortaya hizalar
    /// Requirement: 6.5
    /// </summary>
    [RelayCommand]
    public void AlignCenter()
    {
        HorizontalAlignment = HAlign.Center;
    }

    /// <summary>
    /// Sağa hizalar
    /// Requirement: 6.5
    /// </summary>
    [RelayCommand]
    public void AlignRight()
    {
        HorizontalAlignment = HAlign.Right;
    }

    /// <summary>
    /// Üste hizalar
    /// </summary>
    [RelayCommand]
    public void AlignTop()
    {
        VerticalAlignment = VAlign.Top;
    }

    /// <summary>
    /// Dikey ortaya hizalar
    /// </summary>
    [RelayCommand]
    public void AlignMiddle()
    {
        VerticalAlignment = VAlign.Center;
    }

    /// <summary>
    /// Alta hizalar
    /// </summary>
    [RelayCommand]
    public void AlignBottom()
    {
        VerticalAlignment = VAlign.Bottom;
    }

    /// <summary>
    /// Kalın stili açar/kapatır
    /// Requirement: 6.6
    /// </summary>
    [RelayCommand]
    public void ToggleBold()
    {
        IsBold = !IsBold;
    }

    /// <summary>
    /// İtalik stili açar/kapatır
    /// Requirement: 6.6
    /// </summary>
    [RelayCommand]
    public void ToggleItalic()
    {
        IsItalic = !IsItalic;
    }

    /// <summary>
    /// Altı çizili stili açar/kapatır
    /// Requirement: 6.6
    /// </summary>
    [RelayCommand]
    public void ToggleUnderline()
    {
        IsUnderline = !IsUnderline;
    }

    /// <summary>
    /// Sağdan sola yazımı açar/kapatır
    /// Requirement: 6.7
    /// </summary>
    [RelayCommand]
    public void ToggleRightToLeft()
    {
        IsRightToLeft = !IsRightToLeft;
    }

    /// <summary>
    /// Kayan yazıyı açar/kapatır
    /// </summary>
    [RelayCommand]
    public void ToggleScrolling()
    {
        IsScrolling = !IsScrolling;
    }

    /// <summary>
    /// Ön plan rengini seçer
    /// Requirement: 6.4
    /// </summary>
    [RelayCommand]
    public void SelectForegroundColor()
    {
        // Renk seçici dialogu açılacak (View tarafında handle edilir)
    }

    /// <summary>
    /// Arka plan rengini seçer
    /// Requirement: 6.4
    /// </summary>
    [RelayCommand]
    public void SelectBackgroundColor()
    {
        // Renk seçici dialogu açılacak (View tarafında handle edilir)
    }

    /// <summary>
    /// Font boyutunu artırır
    /// Requirement: 6.3
    /// </summary>
    [RelayCommand]
    public void IncreaseFontSize()
    {
        var currentIndex = AvailableFontSizes.IndexOf(FontSize);
        if (currentIndex < AvailableFontSizes.Count - 1)
        {
            FontSize = AvailableFontSizes[currentIndex + 1];
        }
        else if (FontSize < 64)
        {
            FontSize += 2;
        }
    }

    /// <summary>
    /// Font boyutunu azaltır
    /// Requirement: 6.3
    /// </summary>
    [RelayCommand]
    public void DecreaseFontSize()
    {
        var currentIndex = AvailableFontSizes.IndexOf(FontSize);
        if (currentIndex > 0)
        {
            FontSize = AvailableFontSizes[currentIndex - 1];
        }
        else if (FontSize > 6)
        {
            FontSize -= 2;
        }
    }

    /// <summary>
    /// İçeriği tam genişliğe ayarlar
    /// </summary>
    public void FitToWidth(int displayWidth)
    {
        PositionX = 0;
        ContentWidth = displayWidth;
    }

    /// <summary>
    /// İçeriği tam yüksekliğe ayarlar
    /// </summary>
    public void FitToHeight(int displayHeight)
    {
        PositionY = 0;
        ContentHeight = displayHeight;
    }

    /// <summary>
    /// İçeriği tam ekrana ayarlar
    /// </summary>
    public void FitToScreen(int displayWidth, int displayHeight)
    {
        PositionX = 0;
        PositionY = 0;
        ContentWidth = displayWidth;
        ContentHeight = displayHeight;
    }

    /// <summary>
    /// Kaynakları temizler
    /// </summary>
    public void Dispose()
    {
        _debounceTokenSource?.Cancel();
        _debounceTokenSource?.Dispose();
        MiniPreview?.Dispose();
        MiniPreview = null;
    }
}
