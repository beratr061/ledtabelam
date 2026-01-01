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
    private readonly IZoneManager _zoneManager;
    private readonly IEngineServices _engineServices;
    private readonly IProgramSequencer _programSequencer;

    // Kolay erişim için kısayollar
    private IFontLoader FontLoader => _engineServices.FontLoader;
    private ILedRenderer LedRenderer => _engineServices.LedRenderer;
    private IAnimationService AnimationService => _engineServices.AnimationService;
    private IExportService ExportService => _engineServices.ExportService;
    private IMultiLineTextRenderer MultiLineTextRenderer => _engineServices.MultiLineTextRenderer;
    private IPreviewRenderer PreviewRenderer => _engineServices.PreviewRenderer;

    private string _title = "LEDTabelam - Otobüs Hat Tabelası Önizleme";
    private string _statusMessage = "Hazır";
    private Profile? _currentProfile;

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
    /// Aktif profil
    /// </summary>
    public Profile? CurrentProfile
    {
        get => _currentProfile;
        set
        {
            this.RaiseAndSetIfChanged(ref _currentProfile, value);
            if (value != null)
            {
                Title = $"LEDTabelam - {value.Name}";
            }
        }
    }

    /// <summary>
    /// ProfileManager erişimi (dialog'lar için)
    /// </summary>
    public IProfileManager ProfileManager => _profileManager;

    /// <summary>
    /// Kontrol paneli ViewModel'i
    /// </summary>
    public ControlPanelViewModel ControlPanel { get; }

    /// <summary>
    /// Önizleme ViewModel'i
    /// </summary>
    public PreviewViewModel Preview { get; }

    /// <summary>
    /// Playlist ViewModel'i
    /// </summary>
    public PlaylistViewModel Playlist { get; }

    /// <summary>
    /// Birleşik düzenleyici ViewModel'i (Program + Görsel)
    /// </summary>
    public UnifiedEditorViewModel UnifiedEditor { get; }

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

    /// <summary>
    /// Profil yönetimi komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> ManageProfilesCommand { get; }

    #endregion

    /// <summary>
    /// Profili yükler
    /// </summary>
    public void LoadProfile(Profile profile)
    {
        CurrentProfile = profile;
        ControlPanel.SelectedProfile = profile;
        
        // Programları UnifiedEditor'a yükle
        UnifiedEditor.LoadProgramsFromProfile(profile);
    }

    /// <summary>
    /// MainWindowViewModel constructor
    /// </summary>
    public MainWindowViewModel(
        IProfileManager profileManager,
        ISlotManager slotManager,
        IZoneManager zoneManager,
        IEngineServices engineServices,
        IProgramSequencer programSequencer)
    {
        _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        _zoneManager = zoneManager ?? throw new ArgumentNullException(nameof(zoneManager));
        _engineServices = engineServices ?? throw new ArgumentNullException(nameof(engineServices));
        _programSequencer = programSequencer ?? throw new ArgumentNullException(nameof(programSequencer));

        // Alt ViewModel'leri oluştur
        ControlPanel = new ControlPanelViewModel(_profileManager, _slotManager, FontLoader, _zoneManager);
        Preview = new PreviewViewModel(LedRenderer, AnimationService);
        Playlist = new PlaylistViewModel(AnimationService);
        UnifiedEditor = new UnifiedEditorViewModel();
        
        // ProgramSequencer'ı UnifiedEditor'a bağla
        // Requirements: 3.3, 6.3, 8.3
        UnifiedEditor.ProgramSequencer = _programSequencer;
        
        // ProgramSequencer'ı PreviewRenderer'a bağla (ara durak render için)
        // Requirements: 8.1, 8.2
        PreviewRenderer.SetProgramSequencer(_programSequencer);
        
        // ProgramSequencer event'lerini dinle (önizleme güncellemeleri için)
        // Requirements: 8.1, 8.2
        _programSequencer.ProgramChanged += OnProgramChanged;
        _programSequencer.StopChanged += OnStopChanged;
        _programSequencer.MainContentShowing += OnMainContentShowing;

        // Komutları oluştur
        SavePngCommand = ReactiveCommand.CreateFromTask(SavePngAsync);
        LoadFontCommand = ReactiveCommand.CreateFromTask(LoadFontAsync);
        ToggleAnimationCommand = ReactiveCommand.Create(ToggleAnimation);
        ZoomInCommand = ReactiveCommand.Create(ZoomIn);
        ZoomOutCommand = ReactiveCommand.Create(ZoomOut);
        SaveGifCommand = ReactiveCommand.CreateFromTask(SaveGifAsync);
        SaveWebPCommand = ReactiveCommand.CreateFromTask(SaveWebPAsync);
        StopAnimationCommand = ReactiveCommand.Create(StopAnimation);
        ManageProfilesCommand = ReactiveCommand.Create(() => { }); // View'da handle edilecek

        // ControlPanel font listesi değişikliklerini UnifiedEditor'a bağla
        ControlPanel.Fonts.CollectionChanged += (s, e) =>
        {
            UnifiedEditor.UpdateAvailableFonts(ControlPanel.Fonts);
        };

        // Slot değişikliklerini izle
        ControlPanel.SlotChanged += OnSlotChanged;
        
        // UnifiedEditor değişikliklerini izle
        UnifiedEditor.ItemsChanged += OnUnifiedEditorItemsChanged;

        // ControlPanel ayarları değiştiğinde UnifiedEditor'ı güncelle
        ControlPanel.WhenAnyValue(x => x.CurrentSettings)
            .Subscribe(settings =>
            {
                if (settings != null)
                {
                    UnifiedEditor.UpdateDisplaySize(settings.Width, settings.Height);
                }
            })
            .DisposeWith(Disposables);

        // Font veya ayarlar değiştiğinde önizlemeyi güncelle
        // Throttle ile çok sık güncellemeyi önle
        ControlPanel.WhenAnyValue(
            x => x.SelectedFont,
            x => x.CurrentSettings)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => RenderTextToPreview())
            .DisposeWith(Disposables);

        // LetterSpacing değişikliklerini ayrıca izle
        ControlPanel.WhenAnyValue(x => x.LetterSpacing)
            .Skip(1) // İlk değeri atla
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => RenderTextToPreview())
            .DisposeWith(Disposables);

        // Animasyon durumu değişikliklerini izle
        AnimationService.StateChanged += OnAnimationStateChanged;
        
        // Animasyon tick'lerini UnifiedEditor'a bağla
        AnimationService.OnTick += OnAnimationTick;
        
        // UnifiedEditor animasyon durumu değiştiğinde AnimationService'i kontrol et
        UnifiedEditor.WhenAnyValue(x => x.IsAnimationPlaying)
            .Subscribe(isPlaying =>
            {
                if (isPlaying && !AnimationService.IsPlaying)
                {
                    AnimationService.Start();
                }
                else if (!isPlaying && AnimationService.IsPlaying)
                {
                    AnimationService.Stop();
                }
            })
            .DisposeWith(Disposables);
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
        if (AnimationService.IsPlaying)
        {
            AnimationService.Pause();
            StatusMessage = "Animasyon duraklatıldı";
        }
        else if (AnimationService.IsPaused)
        {
            AnimationService.Resume();
            StatusMessage = "Animasyon devam ediyor";
        }
        else
        {
            AnimationService.Start();
            StatusMessage = "Animasyon başlatıldı";
        }
    }

    private void StopAnimation()
    {
        AnimationService.Stop();
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

            // Önce ayarları güncelle - bu mevcut içeriği koruyarak yeniden render eder
            Preview.UpdateSettings(settings);

            if (font == null)
            {
                // Font yoksa sadece ayarlar güncellendi, içerik korundu
                return;
            }

            // UnifiedEditor'dan öğeleri render et
            RenderUnifiedEditorToPreview();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Render hatası: {ex.Message}";
        }
    }

    private void OnUnifiedEditorItemsChanged()
    {
        RenderUnifiedEditorToPreview();
        
        // Slot'u otomatik kaydet ve profili diske yaz
        if (ControlPanel.SelectedProfile != null)
        {
            ControlPanel.SaveSlotFromItems(new System.Collections.Generic.List<TabelaItem>(UnifiedEditor.Items));
            // Profili diske kaydet (async fire-and-forget)
            _ = SaveCurrentProfileAsync();
        }
    }

    private async System.Threading.Tasks.Task SaveCurrentProfileAsync()
    {
        try
        {
            if (ControlPanel.SelectedProfile != null)
            {
                await _profileManager.SaveProfileAsync(ControlPanel.SelectedProfile);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Profil kaydetme hatası: {ex.Message}";
        }
    }

    private void OnSlotChanged(TabelaSlot? slot)
    {
        // Slot değiştiğinde UnifiedEditor'ı güncelle
        UnifiedEditor.Items.Clear();
        
        if (slot != null && slot.IsDefined)
        {
            var items = ControlPanel.GetSlotAsItems();
            foreach (var item in items)
            {
                UnifiedEditor.Items.Add(item);
            }
        }
        
        RenderUnifiedEditorToPreview();
    }

    private void RenderUnifiedEditorToPreview()
    {
        try
        {
            var settings = ControlPanel.CurrentSettings;
            var defaultFont = ControlPanel.SelectedFont;

            // Önce ayarları güncelle
            Preview.UpdateSettings(settings);

            // UnifiedEditor'ın display boyutlarını güncelle
            UnifiedEditor.UpdateDisplaySize(settings.Width, settings.Height);

            // Font yoksa sadece ayarları güncelle
            if (defaultFont == null && UnifiedEditor.AvailableFonts.Count == 0)
            {
                return;
            }

            // UnifiedEditor'dan öğeleri al ve render et
            var items = new System.Collections.Generic.List<TabelaItem>(UnifiedEditor.Items);
            if (items.Count > 0)
            {
                var colorMatrix = PreviewRenderer.RenderProgramToColorMatrix(
                    items,
                    defaultFont,
                    fontName => UnifiedEditor.GetFontByName(fontName) ?? defaultFont,
                    settings);

                Preview.UpdateColorMatrix(colorMatrix);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Birleşik düzenleyici render hatası: {ex.Message}";
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

    /// <summary>
    /// Program değiştiğinde önizlemeyi günceller
    /// Requirements: 8.1
    /// </summary>
    private void OnProgramChanged(TabelaProgram program)
    {
        // UI thread'de çalıştır
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // Yeni programın öğelerini UnifiedEditor'a yükle
            if (program != null)
            {
                UnifiedEditor.SelectedProgram = program;
            }
            
            // Önizlemeyi güncelle
            RenderUnifiedEditorToPreview();
            
            // Status bar'ı güncelle
            StatusMessage = $"Program: {program?.Name ?? "?"} ({UnifiedEditor.CurrentProgramDisplay})";
        });
    }

    /// <summary>
    /// Ara durak değiştiğinde önizlemeyi günceller
    /// Requirements: 8.1, 8.2
    /// </summary>
    private void OnStopChanged(TabelaItem item, IntermediateStop stop)
    {
        // UI thread'de çalıştır
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // Önizlemeyi güncelle (ara durak içeriği değişti)
            RenderUnifiedEditorToPreview();
            
            // Status bar'ı güncelle
            var stopIndex = item.IntermediateStops.Stops.IndexOf(stop) + 1;
            var totalStops = item.IntermediateStops.Stops.Count;
            StatusMessage = $"Durak: {stop.StopName} ({stopIndex}/{totalStops})";
        });
    }

    /// <summary>
    /// Ana içeriğe dönüldüğünde önizlemeyi günceller
    /// Requirements: 8.1, 8.2
    /// </summary>
    private void OnMainContentShowing(TabelaItem item)
    {
        // UI thread'de çalıştır
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // Önizlemeyi güncelle (ana içerik gösteriliyor)
            RenderUnifiedEditorToPreview();
            
            // Status bar'ı güncelle
            StatusMessage = $"Ana içerik: {item.Content}";
        });
    }

    private void OnAnimationTick(AnimationTick tick)
    {
        // ProgramSequencer'ı güncelle (program ve ara durak timer'ları)
        // Requirements: 3.3, 6.3, 8.3
        if (UnifiedEditor.IsAnimationPlaying)
        {
            _programSequencer.OnTick(tick.DeltaTime);
        }
        
        // UnifiedEditor'daki kayan yazıları güncelle (UI thread'de çalıştır)
        if (UnifiedEditor.IsAnimationPlaying)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UnifiedEditor.OnAnimationTick(tick.DeltaTime);
            });
        }
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AnimationService.StateChanged -= OnAnimationStateChanged;
            AnimationService.OnTick -= OnAnimationTick;
            UnifiedEditor.ItemsChanged -= OnUnifiedEditorItemsChanged;
            ControlPanel.SlotChanged -= OnSlotChanged;
            
            // ProgramSequencer event'lerinden çık
            // Requirements: 8.1, 8.2
            _programSequencer.ProgramChanged -= OnProgramChanged;
            _programSequencer.StopChanged -= OnStopChanged;
            _programSequencer.MainContentShowing -= OnMainContentShowing;
            
            ControlPanel.Dispose();
            Preview.Dispose();
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
