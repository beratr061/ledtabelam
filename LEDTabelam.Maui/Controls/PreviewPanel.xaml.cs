using LEDTabelam.Maui.Models;
using LEDTabelam.Maui.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LEDTabelam.Maui.Controls;

/// <summary>
/// Önizleme paneli - LED tabela önizlemesi ve kontrolleri
/// Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8
/// </summary>
public partial class PreviewPanel : ContentView
{
    private SKBitmap? _currentBitmap;
    private float _lastTouchX;
    private float _lastTouchY;
    private bool _isPanning;
    private PreviewViewModel? _currentViewModel;

    /// <summary>
    /// Tam ekran modu değişikliği olayı
    /// </summary>
    public event EventHandler<bool>? FullscreenChanged;

    /// <summary>
    /// Önizleme güncelleme olayı
    /// </summary>
    public event EventHandler? PreviewUpdated;

    public PreviewPanel()
    {
        InitializeComponent();
        BindingContextChanged += OnBindingContextChanged;
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        // Önceki ViewModel'den event handler'ı kaldır
        if (_currentViewModel != null)
        {
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        if (BindingContext is PreviewViewModel viewModel)
        {
            _currentViewModel = viewModel;
            
            // ViewModel property değişikliklerini dinle
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            
            System.Diagnostics.Debug.WriteLine($"🔵 PreviewPanel: BindingContext changed to PreviewViewModel");
            
            // İlk durumu güncelle
            UpdateEmptyState();
            UpdatePlayButtonState();
            
            // Mevcut bitmap varsa göster
            if (viewModel.PreviewBitmap != null)
            {
                _currentBitmap = viewModel.PreviewBitmap;
                System.Diagnostics.Debug.WriteLine($"🔵 PreviewPanel: Initial bitmap - {_currentBitmap?.Width}x{_currentBitmap?.Height}");
                UpdateEmptyState();
                InvalidateCanvas();
            }
        }
        else
        {
            _currentViewModel = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not PreviewViewModel viewModel) return;

        System.Diagnostics.Debug.WriteLine($"🔵 PreviewPanel: Property changed - {e.PropertyName}");

        switch (e.PropertyName)
        {
            case nameof(PreviewViewModel.PreviewBitmap):
                _currentBitmap = viewModel.PreviewBitmap;
                System.Diagnostics.Debug.WriteLine($"🔵 PreviewPanel: Bitmap updated - {_currentBitmap?.Width}x{_currentBitmap?.Height}");
                UpdateEmptyState();
                InvalidateCanvas();
                break;

            case nameof(PreviewViewModel.ZoomLevel):
                InvalidateCanvas();
                break;

            case nameof(PreviewViewModel.IsPlaying):
                UpdatePlayButtonState();
                break;

            case nameof(PreviewViewModel.IsFullscreen):
                FullscreenChanged?.Invoke(this, viewModel.IsFullscreen);
                UpdateFullscreenButtonState();
                break;

            case nameof(PreviewViewModel.CurrentPage):
            case nameof(PreviewViewModel.TotalPages):
                // Sayfa değişikliğinde canvas'ı yenile
                InvalidateCanvas();
                break;
        }
    }

    /// <summary>
    /// Canvas'ı yeniden çizer
    /// </summary>
    public void InvalidateCanvas()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LedCanvas?.InvalidateSurface();
        });
    }

    /// <summary>
    /// Önizleme bitmap'ini günceller
    /// Requirement: 4.1
    /// </summary>
    public void UpdatePreview(SKBitmap? bitmap)
    {
        _currentBitmap?.Dispose();
        _currentBitmap = bitmap;
        
        UpdateEmptyState();
        InvalidateCanvas();
        PreviewUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Boş durum görünürlüğünü günceller
    /// </summary>
    private void UpdateEmptyState()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            bool isEmpty = _currentBitmap == null || 
                           _currentBitmap.Width <= 1 || 
                           _currentBitmap.Height <= 1;
            
            System.Diagnostics.Debug.WriteLine($"🔵 UpdateEmptyState: isEmpty={isEmpty}, bitmap={_currentBitmap?.Width}x{_currentBitmap?.Height}");
            
            EmptyStateOverlay.IsVisible = isEmpty;
        });
    }

    /// <summary>
    /// Play butonu durumunu günceller
    /// </summary>
    private void UpdatePlayButtonState()
    {
        if (BindingContext is not PreviewViewModel viewModel) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            PlayButton.Text = viewModel.IsPlaying ? "⏸" : "▶";
        });
    }

    /// <summary>
    /// Tam ekran butonu durumunu günceller
    /// </summary>
    private void UpdateFullscreenButtonState()
    {
        if (BindingContext is not PreviewViewModel viewModel) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            FullscreenButton.Text = viewModel.IsFullscreen ? "⛶" : "⛶";
        });
    }

    /// <summary>
    /// SkiaSharp canvas paint olayı
    /// Requirement: 4.1, 4.2
    /// </summary>
    private void OnCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;

        System.Diagnostics.Debug.WriteLine($"🎨 OnCanvasPaintSurface: Canvas size = {info.Width}x{info.Height}");

        // Koyu arka plan ile temizle
        // Requirement: 4.2
        canvas.Clear(new SKColor(26, 26, 26)); // #1a1a1a

        if (_currentBitmap == null || _currentBitmap.Width <= 1 || _currentBitmap.Height <= 1)
        {
            System.Diagnostics.Debug.WriteLine($"🎨 OnCanvasPaintSurface: No valid bitmap - {_currentBitmap?.Width}x{_currentBitmap?.Height}");
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"🎨 OnCanvasPaintSurface: Drawing bitmap {_currentBitmap.Width}x{_currentBitmap.Height}");

        // Zoom seviyesini al
        float zoomLevel = 100f;
        if (BindingContext is PreviewViewModel viewModel)
        {
            zoomLevel = viewModel.ZoomLevel;
        }

        // Zoom faktörünü hesapla
        float zoomFactor = zoomLevel / 100f;

        // Ölçeklenmiş boyutları hesapla
        float scaledWidth = _currentBitmap.Width * zoomFactor;
        float scaledHeight = _currentBitmap.Height * zoomFactor;

        // Merkeze hizala
        // Requirement: 4.1
        float x = (info.Width - scaledWidth) / 2f;
        float y = (info.Height - scaledHeight) / 2f;

        // Hedef dikdörtgeni oluştur
        var destRect = new SKRect(x, y, x + scaledWidth, y + scaledHeight);

        // Bitmap'i çiz
        using var paint = new SKPaint
        {
            IsAntialias = false
        };

        canvas.DrawBitmap(_currentBitmap, destRect, paint);
    }

    /// <summary>
    /// Canvas dokunma olayı (pan/zoom için)
    /// </summary>
    private void OnCanvasTouch(object? sender, SKTouchEventArgs e)
    {
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _lastTouchX = e.Location.X;
                _lastTouchY = e.Location.Y;
                _isPanning = true;
                e.Handled = true;
                break;

            case SKTouchAction.Moved:
                if (_isPanning)
                {
                    // Pan işlemi için delta hesapla
                    float deltaX = e.Location.X - _lastTouchX;
                    float deltaY = e.Location.Y - _lastTouchY;
                    
                    _lastTouchX = e.Location.X;
                    _lastTouchY = e.Location.Y;
                    
                    // TODO: Pan offset'i uygula (gelecek geliştirme)
                    e.Handled = true;
                }
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                _isPanning = false;
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Zoom slider değer değişikliği
    /// Requirement: 4.3, 4.6, 4.7
    /// </summary>
    private void OnZoomSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        // Slider değeri değiştiğinde canvas'ı yenile
        InvalidateCanvas();
    }

    /// <summary>
    /// Kaynakları temizler
    /// </summary>
    public void Dispose()
    {
        _currentBitmap?.Dispose();
        _currentBitmap = null;

        if (_currentViewModel != null)
        {
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _currentViewModel = null;
        }
    }
}
