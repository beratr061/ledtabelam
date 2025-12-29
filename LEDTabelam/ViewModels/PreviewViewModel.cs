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
        private set
        {
            this.RaiseAndSetIfChanged(ref _displayBitmap, value);
            this.RaisePropertyChanged(nameof(IsEmpty));
        }
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
            if (_zoomLevel != validValue)
            {
                this.RaiseAndSetIfChanged(ref _zoomLevel, validValue);
                _settings.ZoomLevel = validValue;
                // Zoom değiştiğinde boyutları güncelle
                this.RaisePropertyChanged(nameof(ScaledWidth));
                this.RaisePropertyChanged(nameof(ScaledHeight));
            }
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

    /// <summary>
    /// İçerik boş mu (empty state göstermek için)
    /// </summary>
    public bool IsEmpty => _displayBitmap == null;

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

        // Başlangıç matrisini oluştur - render'ı UpdateSettings'e bırak
        // RenderDisplay() burada çağrılmıyor çünkü settings henüz geçerli değil
    }

    #region Public Methods

    /// <summary>
    /// Görüntüleme ayarlarını günceller
    /// İçeriği silmez, sadece render ayarlarını günceller
    /// </summary>
    public void UpdateSettings(DisplaySettings settings)
    {
        var oldWidth = _settings.Width;
        var oldHeight = _settings.Height;
        
        _settings = settings;
        _isRgbMode = settings.ColorType == LedColorType.OneROneGOneB ||
                     settings.ColorType == LedColorType.FullRGB;

        // Geçersiz boyut kontrolü
        if (settings.Width <= 0 || settings.Height <= 0)
        {
            return;
        }

        // Matris boyutu değiştiyse yeniden oluştur
        bool sizeChanged = oldWidth != settings.Width || oldHeight != settings.Height;
            
        if (sizeChanged)
        {
            // Eski içeriği yeni boyuta kopyala
            var oldPixelMatrix = _pixelMatrix;
            var oldColorMatrix = _colorMatrix;
            
            // Yeni matrisler oluştur
            _pixelMatrix = new bool[settings.Width, settings.Height];
            _colorMatrix = new SKColor[settings.Width, settings.Height];
            
            // Eski içeriği kopyala (mümkün olduğunca)
            if (oldPixelMatrix != null)
            {
                int copyWidth = Math.Min(oldPixelMatrix.GetLength(0), settings.Width);
                int copyHeight = Math.Min(oldPixelMatrix.GetLength(1), settings.Height);
                for (int x = 0; x < copyWidth; x++)
                {
                    for (int y = 0; y < copyHeight; y++)
                    {
                        _pixelMatrix[x, y] = oldPixelMatrix[x, y];
                    }
                }
            }
            
            if (oldColorMatrix != null)
            {
                int copyWidth = Math.Min(oldColorMatrix.GetLength(0), settings.Width);
                int copyHeight = Math.Min(oldColorMatrix.GetLength(1), settings.Height);
                for (int x = 0; x < copyWidth; x++)
                {
                    for (int y = 0; y < copyHeight; y++)
                    {
                        _colorMatrix[x, y] = oldColorMatrix[x, y];
                    }
                }
            }
        }
        
        // Matris yoksa oluştur (ilk çağrı için) - ama boş bırak
        if (_pixelMatrix == null)
        {
            _pixelMatrix = new bool[settings.Width, settings.Height];
        }
        if (_colorMatrix == null)
        {
            _colorMatrix = new SKColor[settings.Width, settings.Height];
        }

        // Mevcut içerikle yeniden render et (ayarlar değişmiş olabilir - parlaklık, renk vs.)
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
        // Minimum boyut kontrolü - 0 boyut SkiaSharp'ı çökertir
        var width = Math.Max(1, _settings.Width);
        var height = Math.Max(1, _settings.Height);
        
        _pixelMatrix = new bool[width, height];
        _colorMatrix = new SKColor[width, height];
    }

    private void RenderDisplay()
    {
        try
        {
            // Boyut kontrolü - geçersiz boyutlarda render yapma
            if (_settings.Width <= 0 || _settings.Height <= 0)
            {
                return;
            }
            
            // Matrix kontrolü - matrix yoksa render yapma
            if (_pixelMatrix == null && _colorMatrix == null)
            {
                return;
            }
            
            // Matrix boyut kontrolü
            if (_isRgbMode && _colorMatrix != null)
            {
                if (_colorMatrix.GetLength(0) == 0 || _colorMatrix.GetLength(1) == 0)
                    return;
            }
            else if (_pixelMatrix != null)
            {
                if (_pixelMatrix.GetLength(0) == 0 || _pixelMatrix.GetLength(1) == 0)
                    return;
            }
            
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
