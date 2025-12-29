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

    // Kolay erişim için kısayollar
    private IFontLoader FontLoader => _engineServices.FontLoader;
    private ILedRenderer LedRenderer => _engineServices.LedRenderer;
    private IAnimationService AnimationService => _engineServices.AnimationService;
    private IExportService ExportService => _engineServices.ExportService;
    private IMultiLineTextRenderer MultiLineTextRenderer => _engineServices.MultiLineTextRenderer;
    private IPreviewRenderer PreviewRenderer => _engineServices.PreviewRenderer;

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
        IZoneManager zoneManager,
        IEngineServices engineServices)
    {
        _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        _zoneManager = zoneManager ?? throw new ArgumentNullException(nameof(zoneManager));
        _engineServices = engineServices ?? throw new ArgumentNullException(nameof(engineServices));

        // Alt ViewModel'leri oluştur
        ControlPanel = new ControlPanelViewModel(_profileManager, _slotManager, FontLoader, _zoneManager);
        Preview = new PreviewViewModel(LedRenderer, AnimationService);
        SlotEditor = new SlotEditorViewModel(_slotManager);
        Playlist = new PlaylistViewModel(AnimationService);
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

        // ControlPanel font listesi değişikliklerini ProgramEditor'a bağla
        ControlPanel.Fonts.CollectionChanged += (s, e) =>
        {
            ProgramEditor.UpdateAvailableFonts(ControlPanel.Fonts);
        };

        // SimpleTabelaEditor değişikliklerini izle
        SimpleTabelaEditor.TabelaChanged += OnSimpleTabelaChanged;

        // ProgramEditor değişikliklerini izle
        ProgramEditor.ItemsChanged += OnProgramItemsChanged;

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

        // Zone değişikliklerini izle
        SlotEditor.Zones.CollectionChanged += (s, e) => RenderTextToPreview();

        // Slot değişikliklerini izle
        ControlPanel.WhenAnyValue(x => x.CurrentSlotNumber)
            .Subscribe(slotNumber => LoadSlot(slotNumber))
            .DisposeWith(Disposables);

        // Animasyon durumu değişikliklerini izle
        AnimationService.StateChanged += OnAnimationStateChanged;
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
            AnimationService.PauseAnimation();
            StatusMessage = "Animasyon duraklatıldı";
        }
        else if (AnimationService.IsPaused)
        {
            AnimationService.ResumeAnimation();
            StatusMessage = "Animasyon devam ediyor";
        }
        else
        {
            AnimationService.StartScrollAnimation(ControlPanel.AnimationSpeed);
            StatusMessage = "Animasyon başlatıldı";
        }
    }

    private void StopAnimation()
    {
        AnimationService.StopAnimation();
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

            // SimpleTabelaEditor'dan zone'ları al
            var simpleTabelaZones = SimpleTabelaEditor.GetZones();
            
            if (simpleTabelaZones.Count > 0)
            {
                // Zone'ların LetterSpacing'ini ControlPanel'den al
                foreach (var zone in simpleTabelaZones)
                {
                    zone.LetterSpacing = settings.LetterSpacing;
                }
                
                // Zone'ları render et (hem RGB hem tek renk modunda)
                var colorMatrix = PreviewRenderer.RenderZonesToColorMatrix(font, simpleTabelaZones, settings);
                Preview.UpdateColorMatrix(colorMatrix);
            }
            else if (ProgramEditor.Items.Count > 0)
            {
                // ProgramEditor'dan öğeleri render et
                RenderProgramToPreview();
            }
            else
            {
                // Zone'ları kontrol et (eski sistem)
                var zones = SlotEditor.Zones;
                
                if (zones.Count > 0)
                {
                    // PreviewRenderer ile zone'ları render et
                    var zoneList = new System.Collections.Generic.List<Zone>(zones);
                    // Zone'ların LetterSpacing'ini ControlPanel'den al
                    foreach (var zone in zoneList)
                    {
                        zone.LetterSpacing = settings.LetterSpacing;
                    }
                    var colorMatrix = PreviewRenderer.RenderZonesToColorMatrix(font, zoneList, settings);
                    Preview.UpdateColorMatrix(colorMatrix);
                }
                else
                {
                    // Tek renkli basit render - SimpleTabelaEditor'dan metin al
                    var text = SimpleTabelaEditor.GetDisplayText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        var ledColor = settings.GetLedColor();
                        var skColor = new SkiaSharp.SKColor(ledColor.R, ledColor.G, ledColor.B, ledColor.A);
                        var textBitmap = FontLoader.RenderText(font, text, skColor, settings.LetterSpacing);

                        if (textBitmap != null)
                        {
                            Preview.UpdateFromTextBitmap(textBitmap);
                            textBitmap.Dispose();
                        }
                    }
                    // İçerik yoksa - ayarlar zaten güncellendi, mevcut görüntü korundu
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

            // Önce ayarları güncelle
            Preview.UpdateSettings(settings);

            // Font yoksa sadece ayarları güncelle
            if (defaultFont == null && ProgramEditor.AvailableFonts.Count == 0)
            {
                return;
            }

            // Display boyutlarını ProgramEditor'a bildir
            ProgramEditor.UpdateDisplaySize(settings.Width, settings.Height);

            // PreviewRenderer ile program öğelerini render et
            var items = new System.Collections.Generic.List<TabelaItem>(ProgramEditor.Items);
            var colorMatrix = PreviewRenderer.RenderProgramToColorMatrix(
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
            AnimationService.StateChanged -= OnAnimationStateChanged;
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
