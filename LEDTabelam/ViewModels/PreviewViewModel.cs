using System;
using System.Reactive;
using ReactiveUI;
using LEDTabelam.Models;
using LEDTabelam.Services;
using SkiaSharp;

namespace LEDTabelam.ViewModels;

/// <summary>
/// LED önizleme ViewModel'i
/// Requirements: 6.7, 6.8, 6.9
/// </summary>
public class PreviewViewModel : ViewModelBase
{
    private readonly ILedRenderer _ledRenderer;
    private readonly IAnimationService _animationService;

    private SKBitmap? _displayBitmap;
    private DisplaySettings _settings = new();
    private int _zoomLevel = 100;
    private bool[,]? _pixelMatrix;
    private SKColor[,]? _colorMatrix;
    private bool _isRgbMode = false;
    private int _scrollOffset = 0;

    /// <summary>
    /// LED görüntüsü (render edilmiş bitmap)
    /// </summary>
    public SKBitmap? DisplayBitmap
    {
        get => _displayBitmap;
        private set => this.RaiseAndSetIfChanged(ref _displayBitmap, value);
    }

    /// <summary>
    /// Zoom seviyesi (%50-400)
    /// </summary>
    public int ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            var validValue = Math.Clamp(value, 50, 400);
            this.RaiseAndSetIfChanged(ref _zoomLevel, validValue);
            _settings.ZoomLevel = validValue;
            RenderDisplay();
        }
    }

    /// <summary>
    /// Görüntülenen genişlik (piksel)
    /// </summary>
    public int DisplayWidth => _settings.Width;

    /// <summary>
    /// Görüntülenen yükseklik (piksel)
    /// </summary>
    public int DisplayHeight => _settings.Height;

    /// <summary>
    /// Zoom uygulanmış genişlik
    /// </summary>
    public int ScaledWidth => (int)(_settings.Width * _settings.PixelSize * ZoomLevel / 100.0);

    /// <summary>
    /// Zoom uygulanmış yükseklik
    /// </summary>
    public int ScaledHeight => (int)(_settings.Height * _settings.PixelSize * ZoomLevel / 100.0);

    /// <summary>
    /// Mevcut scroll offset değeri
    /// </summary>
    public int ScrollOffset
    {
        get => _scrollOffset;
        private set => this.RaiseAndSetIfChanged(ref _scrollOffset, value);
    }

    /// <summary>
    /// Animasyon oynatılıyor mu
    /// </summary>
    public bool IsAnimating => _animationService.IsPlaying;

    #region Commands

    /// <summary>
    /// Zoom artırma komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }

    /// <summary>
    /// Zoom azaltma komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }

    /// <summary>
    /// Zoom sıfırlama komutu (%100)
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetZoomCommand { get; }

    /// <summary>
    /// Sığdır komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> FitToWindowCommand { get; }

    #endregion

    /// <summary>
    /// PreviewViewModel constructor
    /// </summary>
    public PreviewViewModel(ILedRenderer ledRenderer, IAnimationService animationService)
    {
        _ledRenderer = ledRenderer ?? throw new ArgumentNullException(nameof(ledRenderer));
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));

        // Komutları oluştur
        ZoomInCommand = ReactiveCommand.Create(ZoomIn);
        ZoomOutCommand = ReactiveCommand.Create(ZoomOut);
        ResetZoomCommand = ReactiveCommand.Create(ResetZoom);
        FitToWindowCommand = ReactiveCommand.Create(FitToWindow);

        // Animasyon frame güncellemelerini dinle
        _animationService.OnFrameUpdate += OnAnimationFrameUpdate;

        // Başlangıç render'ı
        InitializePixelMatrix();
        RenderDisplay();
    }

    #region Public Methods

    /// <summary>
    /// Görüntüleme ayarlarını günceller
    /// </summary>
    public void UpdateSettings(DisplaySettings settings)
    {
        _settings = settings;
        _isRgbMode = settings.ColorType == LedColorType.OneROneGOneB ||
                     settings.ColorType == LedColorType.FullRGB;

        // Matris boyutu değiştiyse yeniden oluştur
        if (_pixelMatrix == null ||
            _pixelMatrix.GetLength(0) != settings.Width ||
            _pixelMatrix.GetLength(1) != settings.Height)
        {
            InitializePixelMatrix();
        }

        RenderDisplay();

        this.RaisePropertyChanged(nameof(DisplayWidth));
        this.RaisePropertyChanged(nameof(DisplayHeight));
        this.RaisePropertyChanged(nameof(ScaledWidth));
        this.RaisePropertyChanged(nameof(ScaledHeight));
    }

    /// <summary>
    /// Piksel matrisini günceller (tek renk mod)
    /// </summary>
    public void UpdatePixelMatrix(bool[,] matrix)
    {
        _pixelMatrix = matrix;
        _isRgbMode = false;
        RenderDisplay();
    }

    /// <summary>
    /// Renk matrisini günceller (RGB mod)
    /// </summary>
    public void UpdateColorMatrix(SKColor[,] matrix)
    {
        _colorMatrix = matrix;
        _isRgbMode = true;
        RenderDisplay();
    }

    /// <summary>
    /// Metin bitmap'ini piksel matrisine dönüştürür
    /// </summary>
    public void UpdateFromTextBitmap(SKBitmap textBitmap)
    {
        if (textBitmap == null) return;

        var width = Math.Min(textBitmap.Width, _settings.Width);
        var height = Math.Min(textBitmap.Height, _settings.Height);

        _pixelMatrix = new bool[_settings.Width, _settings.Height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = textBitmap.GetPixel(x, y);
                // Alpha > 128 ise piksel aktif
                _pixelMatrix[x, y] = pixel.Alpha > 128;
            }
        }

        _isRgbMode = false;
        RenderDisplay();
    }

    /// <summary>
    /// Mevcut görüntüyü döndürür (export için)
    /// </summary>
    public SKBitmap? GetCurrentBitmap()
    {
        return DisplayBitmap;
    }

    /// <summary>
    /// Gerçek çözünürlükte bitmap döndürür (export için)
    /// </summary>
    public SKBitmap? GetOriginalResolutionBitmap()
    {
        if (_pixelMatrix == null && _colorMatrix == null) return null;

        var tempSettings = new DisplaySettings
        {
            Width = _settings.Width,
            Height = _settings.Height,
            ColorType = _settings.ColorType,
            Brightness = _settings.Brightness,
            BackgroundDarkness = _settings.BackgroundDarkness,
            PixelSize = _settings.PixelSize,
            Pitch = _settings.Pitch,
            CustomPitchRatio = _settings.CustomPitchRatio,
            Shape = _settings.Shape,
            InvertColors = _settings.InvertColors,
            AgingPercent = _settings.AgingPercent,
            ZoomLevel = 100 // Zoom olmadan
        };

        if (_isRgbMode && _colorMatrix != null)
        {
            return _ledRenderer.RenderDisplay(_colorMatrix, tempSettings);
        }
        else if (_pixelMatrix != null)
        {
            return _ledRenderer.RenderDisplay(_pixelMatrix, tempSettings);
        }

        return null;
    }

    #endregion

    #region Private Methods

    private void InitializePixelMatrix()
    {
        _pixelMatrix = new bool[_settings.Width, _settings.Height];
        _colorMatrix = new SKColor[_settings.Width, _settings.Height];
    }

    private void RenderDisplay()
    {
        try
        {
            // Eski bitmap'i dispose et
            DisplayBitmap?.Dispose();

            SKBitmap? rendered = null;

            if (_isRgbMode && _colorMatrix != null)
            {
                // Aging efekti uygula
                if (_settings.AgingPercent > 0)
                {
                    var matrixCopy = (SKColor[,])_colorMatrix.Clone();
                    _ledRenderer.ApplyAgingEffect(matrixCopy, _settings.AgingPercent);
                    rendered = _ledRenderer.RenderDisplay(matrixCopy, _settings);
                }
                else
                {
                    rendered = _ledRenderer.RenderDisplay(_colorMatrix, _settings);
                }
            }
            else if (_pixelMatrix != null)
            {
                // Aging efekti uygula
                if (_settings.AgingPercent > 0)
                {
                    var matrixCopy = (bool[,])_pixelMatrix.Clone();
                    _ledRenderer.ApplyAgingEffect(matrixCopy, _settings.AgingPercent);
                    rendered = _ledRenderer.RenderDisplay(matrixCopy, _settings);
                }
                else
                {
                    rendered = _ledRenderer.RenderDisplay(_pixelMatrix, _settings);
                }
            }

            if (rendered != null)
            {
                // Glow efekti uygula
                var withGlow = _ledRenderer.RenderWithGlow(rendered, _settings);
                rendered.Dispose();
                DisplayBitmap = withGlow;
            }
        }
        catch (Exception)
        {
            // Render hatası - sessizce geç
        }
    }

    private void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel + 25, 400);
    }

    private void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel - 25, 50);
    }

    private void ResetZoom()
    {
        ZoomLevel = 100;
    }

    private void FitToWindow()
    {
        // Bu metod View tarafından pencere boyutuna göre hesaplanacak
        // Şimdilik %100'e ayarla
        ZoomLevel = 100;
    }

    private void OnAnimationFrameUpdate(int offset)
    {
        ScrollOffset = offset;
        // Scroll offset'e göre piksel matrisini kaydır ve render et
        ApplyScrollOffset(offset);
    }

    private void ApplyScrollOffset(int offset)
    {
        if (_pixelMatrix == null) return;

        var width = _pixelMatrix.GetLength(0);
        var height = _pixelMatrix.GetLength(1);

        // Kaydırılmış matris oluştur
        var scrolledMatrix = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var sourceX = (x + offset) % width;
                if (sourceX < 0) sourceX += width;
                scrolledMatrix[x, y] = _pixelMatrix[sourceX, y];
            }
        }

        // Geçici olarak matris değiştir ve render et
        var originalMatrix = _pixelMatrix;
        _pixelMatrix = scrolledMatrix;
        RenderDisplay();
        _pixelMatrix = originalMatrix;
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationService.OnFrameUpdate -= OnAnimationFrameUpdate;
            DisplayBitmap?.Dispose();
        }
        base.Dispose(disposing);
    }
}
