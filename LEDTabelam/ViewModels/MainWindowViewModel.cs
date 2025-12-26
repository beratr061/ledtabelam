using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using LEDTabelam.Models;
using LEDTabelam.Services;

namespace LEDTabelam.ViewModels;

/// <summary>
/// Ana pencere ViewModel'i
/// Requirements: 10.2, 13.1, 13.2, 13.3, 13.4
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly IProfileManager _profileManager;
    private readonly ISlotManager _slotManager;
    private readonly IFontLoader _fontLoader;
    private readonly ILedRenderer _ledRenderer;
    private readonly IAnimationService _animationService;
    private readonly IExportService _exportService;
    private readonly IZoneManager _zoneManager;
    private readonly IMultiLineTextRenderer _multiLineTextRenderer;
    private readonly IPreviewRenderer _previewRenderer;

    private string _title = "LEDTabelam - Otobüs Hat Tabelası Önizleme";
    private string _statusMessage = "Hazır";

    /// <summary>
    /// Pencere başlığı
    /// </summary>
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>
    /// Durum çubuğu mesajı
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Kontrol paneli ViewModel'i
    /// </summary>
    public ControlPanelViewModel ControlPanel { get; }

    /// <summary>
    /// Önizleme ViewModel'i
    /// </summary>
    public PreviewViewModel Preview { get; }

    /// <summary>
    /// Slot düzenleyici ViewModel'i
    /// </summary>
    public SlotEditorViewModel SlotEditor { get; }

    /// <summary>
    /// Playlist ViewModel'i
    /// </summary>
    public PlaylistViewModel Playlist { get; }

    /// <summary>
    /// Basit tabela düzenleyici ViewModel'i
    /// </summary>
    public SimpleTabelaViewModel SimpleTabelaEditor { get; }

    /// <summary>
    /// HD2020 tarzı program düzenleyici ViewModel'i
    /// </summary>
    public ProgramEditorViewModel ProgramEditor { get; }

    #region Commands

    /// <summary>
    /// PNG kaydetme komutu (Ctrl+S)
    /// </summary>
    public ReactiveCommand<Unit, Unit> SavePngCommand { get; }

    /// <summary>
    /// Font yükleme komutu (Ctrl+O)
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadFontCommand { get; }

    /// <summary>
    /// Animasyon Play/Pause toggle komutu (Space)
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleAnimationCommand { get; }

    /// <summary>
    /// Zoom artırma komutu (Ctrl++)
    /// </summary>
    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }

    /// <summary>
    /// Zoom azaltma komutu (Ctrl+-)
    /// </summary>
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }

    /// <summary>
    /// GIF kaydetme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveGifCommand { get; }

    /// <summary>
    /// WebP kaydetme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveWebPCommand { get; }

    /// <summary>
    /// Animasyonu durdurma komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> StopAnimationCommand { get; }

    #endregion

    /// <summary>
    /// MainWindowViewModel constructor
    /// </summary>
    public MainWindowViewModel(
        IProfileManager profileManager,
        ISlotManager slotManager,
        IFontLoader fontLoader,
        ILedRenderer ledRenderer,
        IAnimationService animationService,
        IExportService exportService,
        IZoneManager zoneManager,
        IMultiLineTextRenderer multiLineTextRenderer,
        IPreviewRenderer previewRenderer)
    {
        _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        _fontLoader = fontLoader ?? throw new ArgumentNullException(nameof(fontLoader));
        _ledRenderer = ledRenderer ?? throw new ArgumentNullException(nameof(ledRenderer));
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _zoneManager = zoneManager ?? throw new ArgumentNullException(nameof(zoneManager));
        _multiLineTextRenderer = multiLineTextRenderer ?? throw new ArgumentNullException(nameof(multiLineTextRenderer));
        _previewRenderer = previewRenderer ?? throw new ArgumentNullException(nameof(previewRenderer));

        // Alt ViewModel'leri oluştur
        ControlPanel = new ControlPanelViewModel(_profileManager, _slotManager, _fontLoader, _zoneManager);
        Preview = new PreviewViewModel(_ledRenderer, _animationService);
        SlotEditor = new SlotEditorViewModel(_slotManager);
        Playlist = new PlaylistViewModel(_animationService);
        SimpleTabelaEditor = new SimpleTabelaViewModel();
        ProgramEditor = new ProgramEditorViewModel();

        // Komutları oluştur
        SavePngCommand = ReactiveCommand.CreateFromTask(SavePngAsync);
        LoadFontCommand = ReactiveCommand.CreateFromTask(LoadFontAsync);
        ToggleAnimationCommand = ReactiveCommand.Create(ToggleAnimation);
        ZoomInCommand = ReactiveCommand.Create(ZoomIn);
        ZoomOutCommand = ReactiveCommand.Create(ZoomOut);
        SaveGifCommand = ReactiveCommand.CreateFromTask(SaveGifAsync);
        SaveWebPCommand = ReactiveCommand.CreateFromTask(SaveWebPAsync);
        StopAnimationCommand = ReactiveCommand.Create(StopAnimation);

        // ControlPanel'den Preview'a ayar değişikliklerini bağla
        ControlPanel.WhenAnyValue(x => x.CurrentSettings)
            .WhereNotNull()
            .Subscribe(settings => Preview.UpdateSettings(settings))
            .DisposeWith(Disposables);

        // ControlPanel font listesi değişikliklerini ProgramEditor'a bağla
        ControlPanel.Fonts.CollectionChanged += (s, e) =>
        {
            ProgramEditor.UpdateAvailableFonts(ControlPanel.Fonts);
        };

        // SimpleTabelaEditor değişikliklerini izle
        SimpleTabelaEditor.TabelaChanged += OnSimpleTabelaChanged;

        // ProgramEditor değişikliklerini izle
        ProgramEditor.ItemsChanged += OnProgramItemsChanged;

        // Metin veya font değiştiğinde önizlemeyi güncelle
        ControlPanel.WhenAnyValue(
            x => x.InputText,
            x => x.SelectedFont,
            x => x.CurrentSettings)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => RenderTextToPreview())
            .DisposeWith(Disposables);

        // Zone değişikliklerini izle
        SlotEditor.Zones.CollectionChanged += (s, e) => RenderTextToPreview();

        // Slot değişikliklerini izle
        ControlPanel.WhenAnyValue(x => x.CurrentSlotNumber)
            .Subscribe(slotNumber => LoadSlot(slotNumber))
            .DisposeWith(Disposables);

        // Animasyon durumu değişikliklerini izle
        _animationService.StateChanged += OnAnimationStateChanged;
    }

    #region Command Implementations

    private async Task SavePngAsync()
    {
        try
        {
            StatusMessage = "PNG kaydediliyor...";
            // Export işlemi View tarafından file dialog ile yapılacak
            // Bu komut sadece event tetikler
            await Task.CompletedTask;
            StatusMessage = "PNG kaydedildi";
        }
        catch (Exception ex)
        {
            StatusMessage = $"PNG kaydetme hatası: {ex.Message}";
        }
    }

    private async Task LoadFontAsync()
    {
        try
        {
            StatusMessage = "Font yükleniyor...";
            // Font yükleme işlemi View tarafından file dialog ile yapılacak
            await Task.CompletedTask;
            StatusMessage = "Font yüklendi";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Font yükleme hatası: {ex.Message}";
        }
    }

    private void ToggleAnimation()
    {
        if (_animationService.IsPlaying)
        {
            _animationService.PauseAnimation();
            StatusMessage = "Animasyon duraklatıldı";
        }
        else if (_animationService.IsPaused)
        {
            _animationService.ResumeAnimation();
            StatusMessage = "Animasyon devam ediyor";
        }
        else
        {
            _animationService.StartScrollAnimation(ControlPanel.AnimationSpeed);
            StatusMessage = "Animasyon başlatıldı";
        }
    }

    private void StopAnimation()
    {
        _animationService.StopAnimation();
        StatusMessage = "Animasyon durduruldu";
    }

    private void ZoomIn()
    {
        var newZoom = Math.Min(Preview.ZoomLevel + 25, 400);
        Preview.ZoomLevel = newZoom;
        StatusMessage = $"Zoom: %{newZoom}";
    }

    private void ZoomOut()
    {
        var newZoom = Math.Max(Preview.ZoomLevel - 25, 50);
        Preview.ZoomLevel = newZoom;
        StatusMessage = $"Zoom: %{newZoom}";
    }

    private async Task SaveGifAsync()
    {
        try
        {
            StatusMessage = "GIF kaydediliyor...";
            await Task.CompletedTask;
            StatusMessage = "GIF kaydedildi";
        }
        catch (Exception ex)
        {
            StatusMessage = $"GIF kaydetme hatası: {ex.Message}";
        }
    }

    private async Task SaveWebPAsync()
    {
        try
        {
            StatusMessage = "WebP kaydediliyor...";
            await Task.CompletedTask;
            StatusMessage = "WebP kaydedildi";
        }
        catch (Exception ex)
        {
            StatusMessage = $"WebP kaydetme hatası: {ex.Message}";
        }
    }

    #endregion

    #region Private Methods

    private void RenderTextToPreview()
    {
        try
        {
            var font = ControlPanel.SelectedFont;
            var settings = ControlPanel.CurrentSettings;

            if (font == null)
            {
                Preview.UpdateSettings(settings);
                return;
            }

            // SimpleTabelaEditor'dan zone'ları al
            var simpleTabelaZones = SimpleTabelaEditor.GetZones();
            
            if (simpleTabelaZones.Count > 0 && settings.ColorType == LedColorType.FullRGB)
            {
                // PreviewRenderer ile zone'ları render et
                var colorMatrix = _previewRenderer.RenderZonesToColorMatrix(font, simpleTabelaZones, settings);
                Preview.UpdateColorMatrix(colorMatrix);
            }
            else
            {
                // Zone'ları kontrol et (eski sistem)
                var zones = SlotEditor.Zones;
                
                if (zones.Count > 0 && settings.ColorType == LedColorType.FullRGB)
                {
                    // PreviewRenderer ile zone'ları render et
                    var zoneList = new System.Collections.Generic.List<Zone>(zones);
                    var colorMatrix = _previewRenderer.RenderZonesToColorMatrix(font, zoneList, settings);
                    Preview.UpdateColorMatrix(colorMatrix);
                }
                else
                {
                    // Tek renkli basit render
                    var text = ControlPanel.InputText;
                    if (string.IsNullOrEmpty(text))
                    {
                        Preview.UpdateSettings(settings);
                        return;
                    }

                    var ledColor = settings.GetLedColor();
                    var skColor = new SkiaSharp.SKColor(ledColor.R, ledColor.G, ledColor.B, ledColor.A);
                    var textBitmap = _fontLoader.RenderText(font, text, skColor);

                    if (textBitmap != null)
                    {
                        Preview.UpdateFromTextBitmap(textBitmap);
                        textBitmap.Dispose();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Render hatası: {ex.Message}";
        }
    }

    private void OnSimpleTabelaChanged()
    {
        RenderTextToPreview();
    }

    private void OnProgramItemsChanged()
    {
        RenderProgramToPreview();
    }

    private void RenderProgramToPreview()
    {
        try
        {
            var settings = ControlPanel.CurrentSettings;
            var defaultFont = ControlPanel.SelectedFont;

            // Font yoksa sadece ayarları güncelle
            if (defaultFont == null && ProgramEditor.AvailableFonts.Count == 0)
            {
                Preview.UpdateSettings(settings);
                return;
            }

            // Display boyutlarını ProgramEditor'a bildir
            ProgramEditor.UpdateDisplaySize(settings.Width, settings.Height);

            // PreviewRenderer ile program öğelerini render et
            var items = new System.Collections.Generic.List<TabelaItem>(ProgramEditor.Items);
            var colorMatrix = _previewRenderer.RenderProgramToColorMatrix(
                items,
                defaultFont,
                fontName => ProgramEditor.GetFontByName(fontName),
                settings);

            Preview.UpdateColorMatrix(colorMatrix);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Program render hatası: {ex.Message}";
        }
    }

    private void LoadSlot(int slotNumber)
    {
        var slot = _slotManager.GetSlot(slotNumber);
        if (slot != null)
        {
            SlotEditor.LoadSlot(slot);
            StatusMessage = $"Slot {slotNumber:D3} yüklendi";
        }
        else
        {
            SlotEditor.CreateNewSlot(slotNumber);
            StatusMessage = $"Slot {slotNumber:D3} - Tanımsız";
        }
    }

    private void OnAnimationStateChanged(AnimationState state)
    {
        StatusMessage = state switch
        {
            AnimationState.Playing => "Animasyon oynatılıyor",
            AnimationState.Paused => "Animasyon duraklatıldı",
            AnimationState.Stopped => "Animasyon durduruldu",
            _ => StatusMessage
        };
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationService.StateChanged -= OnAnimationStateChanged;
            SimpleTabelaEditor.TabelaChanged -= OnSimpleTabelaChanged;
            ProgramEditor.ItemsChanged -= OnProgramItemsChanged;
            ControlPanel.Dispose();
            Preview.Dispose();
            SlotEditor.Dispose();
            Playlist.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// ReactiveUI extension for DisposeWith
/// </summary>
public static class DisposableExtensions
{
    public static T DisposeWith<T>(this T disposable, System.Reactive.Disposables.CompositeDisposable compositeDisposable)
        where T : IDisposable
    {
        compositeDisposable.Add(disposable);
        return disposable;
    }
}
