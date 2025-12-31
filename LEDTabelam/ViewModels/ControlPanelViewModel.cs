using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using LEDTabelam.Models;
using LEDTabelam.Services;

namespace LEDTabelam.ViewModels;

/// <summary>
/// Kontrol paneli ViewModel'i
/// Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 4.1, 5.1, 5.3, 5.5, 5.7, 9.13, 20.4
/// </summary>
public class ControlPanelViewModel : ViewModelBase
{
    private readonly IProfileManager _profileManager;
    private readonly ISlotManager _slotManager;
    private readonly IFontLoader _fontLoader;
    private readonly IZoneManager _zoneManager;

    #region Resolution Properties

    private string _selectedResolution = "160x24";
    private int _panelWidth = 160;  // P10 referansında panel genişliği
    private int _panelHeight = 24;  // P10 referansında panel yüksekliği
    private bool _isCustomResolution = false;

    /// <summary>
    /// Standart panel boyutu seçenekleri (P10 referansında)
    /// </summary>
    public ObservableCollection<string> Resolutions { get; } = new()
    {
        "160x24", "144x19", "Özel"
    };

    /// <summary>
    /// Seçili panel boyutu
    /// </summary>
    public string SelectedResolution
    {
        get => _selectedResolution;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedResolution, value);
            IsCustomResolution = value == "Özel";
            if (!IsCustomResolution && !string.IsNullOrEmpty(value))
            {
                ParseResolution(value);
            }
        }
    }

    /// <summary>
    /// Panel genişliği (P10 referansında)
    /// </summary>
    public int PanelWidth
    {
        get => _panelWidth;
        set
        {
            var validValue = Math.Clamp(value, 1, 512);
            this.RaiseAndSetIfChanged(ref _panelWidth, validValue);
            this.RaisePropertyChanged(nameof(ActualWidth));
            UpdateCurrentSettings();
        }
    }

    /// <summary>
    /// Panel yüksekliği (P10 referansında)
    /// </summary>
    public int PanelHeight
    {
        get => _panelHeight;
        set
        {
            var validValue = Math.Clamp(value, 1, 512);
            this.RaiseAndSetIfChanged(ref _panelHeight, validValue);
            this.RaisePropertyChanged(nameof(ActualHeight));
            UpdateCurrentSettings();
        }
    }

    /// <summary>
    /// Çözünürlük genişliği (piksel) - Pitch çarpanı uygulanmış
    /// </summary>
    public int ActualWidth => PanelWidth * SelectedPitch.GetResolutionMultiplier();

    /// <summary>
    /// Çözünürlük yüksekliği (piksel) - Pitch çarpanı uygulanmış
    /// </summary>
    public int ActualHeight => PanelHeight * SelectedPitch.GetResolutionMultiplier();

    /// <summary>
    /// Özel çözünürlük modu aktif mi
    /// </summary>
    public bool IsCustomResolution
    {
        get => _isCustomResolution;
        set => this.RaiseAndSetIfChanged(ref _isCustomResolution, value);
    }

    #endregion

    #region Color Properties

    private LedColorType _selectedColorType = LedColorType.FullRGB;

    /// <summary>
    /// Seçili LED renk tipi
    /// </summary>
    public LedColorType SelectedColorType
    {
        get => _selectedColorType;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedColorType, value);
            UpdateCurrentSettings();
        }
    }

    /// <summary>
    /// LED renk tipi seçenekleri
    /// </summary>
    public LedColorType[] ColorTypes { get; } = Enum.GetValues<LedColorType>();

    #endregion

    #region Font Properties

    private BitmapFont? _selectedFont;

    /// <summary>
    /// Yüklenmiş fontlar
    /// </summary>
    public ObservableCollection<BitmapFont> Fonts { get; } = new();

    /// <summary>
    /// Seçili font
    /// </summary>
    public BitmapFont? SelectedFont
    {
        get => _selectedFont;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedFont, value);
            UpdateCurrentSettings();
        }
    }

    /// <summary>
    /// Çözünürlük bilgisi metni - Pitch çarpanı uygulanmış gerçek değeri gösterir
    /// </summary>
    public string ActualResolutionText => $"Çözünürlük: {ActualWidth}x{ActualHeight} piksel";

    #endregion

    #region Visual Settings Properties

    private int _brightness = 100;
    private int _backgroundDarkness = 100;
    private int _letterSpacing = 1;
    private PixelPitch _selectedPitch = PixelPitch.P10;
    private double _customPitchRatio = 0.7;
    private PixelShape _selectedShape = PixelShape.Round;
    private bool _invertColors = false;
    private int _agingPercent = 0;

    /// <summary>
    /// Parlaklık seviyesi (%0-100)
    /// </summary>
    public int Brightness
    {
        get => _brightness;
        set
        {
            var validValue = Math.Clamp(value, 0, 100);
            this.RaiseAndSetIfChanged(ref _brightness, validValue);
            UpdateCurrentSettings();
        }
    }

    /// <summary>
    /// Arka plan karartma seviyesi (%0-100)
    /// </summary>
    public int BackgroundDarkness
    {
        get => _backgroundDarkness;
        set
        {
            var validValue = Math.Clamp(value, 0, 100);
            this.RaiseAndSetIfChanged(ref _backgroundDarkness, validValue);
            UpdateCurrentSettings();
        }
    }

    /// <summary>
    /// Harf aralığı (0-10 piksel)
    /// </summary>
    public int LetterSpacing
    {
        get => _letterSpacing;
        set
        {
            var validValue = Math.Clamp(value, 0, 10);
            this.RaiseAndSetIfChanged(ref _letterSpacing, validValue);
            UpdateCurrentSettings();
        }
    }

    /// <summary>
    /// Seçili piksel pitch değeri
    /// </summary>
    public PixelPitch SelectedPitch
    {
        get => _selectedPitch;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPitch, value);
            IsCustomPitch = value == PixelPitch.Custom;
            // Pitch değiştiğinde gerçek çözünürlük de değişir
            this.RaisePropertyChanged(nameof(ActualWidth));
            this.RaisePropertyChanged(nameof(ActualHeight));
            this.RaisePropertyChanged(nameof(ActualResolutionText));
            UpdateCurrentSettings();
        }
    }

    /// <summary>
    /// Piksel pitch seçenekleri
    /// </summary>
    public PixelPitch[] PitchOptions { get; } = Enum.GetValues<PixelPitch>();

    /// <summary>
    /// Özel pitch oranı
    /// </summary>
    public double CustomPitchRatio
    {
        get => _customPitchRatio;
        set
        {
            var validValue = Math.Clamp(value, 0.1, 1.0);
            this.RaiseAndSetIfChanged(ref _customPitchRatio, validValue);
            UpdateCurrentSettings();
        }
    }

    private bool _isCustomPitch = false;

    /// <summary>
    /// Özel pitch modu aktif mi
    /// </summary>
    public bool IsCustomPitch
    {
        get => _isCustomPitch;
        set => this.RaiseAndSetIfChanged(ref _isCustomPitch, value);
    }

    /// <summary>
    /// Seçili piksel şekli
    /// </summary>
    public PixelShape SelectedShape
    {
        get => _selectedShape;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedShape, value);
            UpdateCurrentSettings();
        }
    }

    /// <summary>
    /// Piksel şekli seçenekleri
    /// </summary>
    public PixelShape[] ShapeOptions { get; } = Enum.GetValues<PixelShape>();

    /// <summary>
    /// Ters renk modu
    /// </summary>
    public bool InvertColors
    {
        get => _invertColors;
        set
        {
            this.RaiseAndSetIfChanged(ref _invertColors, value);
            UpdateCurrentSettings();
        }
    }

    /// <summary>
    /// Eskime efekti yüzdesi (%0-5)
    /// </summary>
    public int AgingPercent
    {
        get => _agingPercent;
        set
        {
            var validValue = Math.Clamp(value, 0, 5);
            this.RaiseAndSetIfChanged(ref _agingPercent, validValue);
            UpdateCurrentSettings();
        }
    }

    #endregion

    #region Profile Properties

    private Profile? _selectedProfile;

    /// <summary>
    /// Profil listesi
    /// </summary>
    public ObservableCollection<Profile> Profiles { get; } = new();

    /// <summary>
    /// Seçili profil
    /// </summary>
    public Profile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedProfile, value);
            if (value != null)
            {
                LoadProfileSettings(value);
                LoadCurrentSlot(); // Profil değiştiğinde slot'u da yükle
            }
        }
    }

    #endregion

    #region Slot Properties

    private int _currentSlotNumber = 1;
    private string _slotSearchQuery = string.Empty;
    private TabelaSlot? _currentSlot;

    /// <summary>
    /// Mevcut slot numarası (1-999)
    /// </summary>
    public int CurrentSlotNumber
    {
        get => _currentSlotNumber;
        set
        {
            var validValue = Math.Clamp(value, 1, 999);
            if (_currentSlotNumber != validValue)
            {
                _currentSlotNumber = validValue;
                this.RaisePropertyChanged(nameof(CurrentSlotNumber));
                LoadCurrentSlot();
            }
        }
    }

    /// <summary>
    /// Mevcut slot
    /// </summary>
    public TabelaSlot? CurrentSlot
    {
        get => _currentSlot;
        private set => this.RaiseAndSetIfChanged(ref _currentSlot, value);
    }

    /// <summary>
    /// Slot arama sorgusu
    /// </summary>
    public string SlotSearchQuery
    {
        get => _slotSearchQuery;
        set => this.RaiseAndSetIfChanged(ref _slotSearchQuery, value);
    }

    /// <summary>
    /// Arama sonuçları
    /// </summary>
    public ObservableCollection<TabelaSlot> SearchResults { get; } = new();

    /// <summary>
    /// Slot değiştiğinde tetiklenir
    /// </summary>
    public event Action<TabelaSlot?>? SlotChanged;

    #endregion

    #region Animation Properties

    private int _animationSpeed = 20;

    /// <summary>
    /// Animasyon hızı (piksel/saniye, 1-100)
    /// </summary>
    public int AnimationSpeed
    {
        get => _animationSpeed;
        set
        {
            var validValue = Math.Clamp(value, 1, 100);
            this.RaiseAndSetIfChanged(ref _animationSpeed, validValue);
        }
    }

    #endregion

    #region Current Settings

    private DisplaySettings _currentSettings = new();

    /// <summary>
    /// Mevcut görüntüleme ayarları
    /// </summary>
    public DisplaySettings CurrentSettings
    {
        get => _currentSettings;
        private set => this.RaiseAndSetIfChanged(ref _currentSettings, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Font yükleme komutu
    /// </summary>
    public ReactiveCommand<string, Unit> LoadFontCommand { get; }

    /// <summary>
    /// Profil kaydetme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveProfileCommand { get; }

    /// <summary>
    /// Yeni profil oluşturma komutu
    /// </summary>
    public ReactiveCommand<string, Unit> CreateProfileCommand { get; }

    /// <summary>
    /// Profil silme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteProfileCommand { get; }

    /// <summary>
    /// Slot arama komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> SearchSlotsCommand { get; }

    /// <summary>
    /// Çözünürlük ayarlama komutu
    /// </summary>
    public ReactiveCommand<string, Unit> SetResolutionCommand { get; }

    #endregion

    /// <summary>
    /// ControlPanelViewModel constructor
    /// </summary>
    public ControlPanelViewModel(
        IProfileManager profileManager,
        ISlotManager slotManager,
        IFontLoader fontLoader,
        IZoneManager zoneManager)
    {
        _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        _fontLoader = fontLoader ?? throw new ArgumentNullException(nameof(fontLoader));
        _zoneManager = zoneManager ?? throw new ArgumentNullException(nameof(zoneManager));

        // Komutları oluştur
        LoadFontCommand = ReactiveCommand.CreateFromTask<string>(LoadFontAsync);
        SaveProfileCommand = ReactiveCommand.CreateFromTask(SaveProfileAsync);
        CreateProfileCommand = ReactiveCommand.CreateFromTask<string>(CreateProfileAsync);
        DeleteProfileCommand = ReactiveCommand.CreateFromTask(DeleteProfileAsync);
        SearchSlotsCommand = ReactiveCommand.Create(SearchSlots);
        SetResolutionCommand = ReactiveCommand.Create<string>(SetResolution);

        // Slot arama sorgusunu izle
        this.WhenAnyValue(x => x.SlotSearchQuery)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => SearchSlots())
            .DisposeWith(Disposables);

        // Başlangıç ayarlarını uygula
        UpdateCurrentSettings();
    }

    #region Private Methods

    private void ParseResolution(string resolution)
    {
        var parts = resolution.Split('x');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var width) &&
            int.TryParse(parts[1], out var height))
        {
            _panelWidth = width;
            _panelHeight = height;
            this.RaisePropertyChanged(nameof(PanelWidth));
            this.RaisePropertyChanged(nameof(PanelHeight));
            this.RaisePropertyChanged(nameof(ActualWidth));
            this.RaisePropertyChanged(nameof(ActualHeight));
            this.RaisePropertyChanged(nameof(ActualResolutionText));
            UpdateCurrentSettings();
        }
    }

    /// <summary>
    /// Çözünürlük ayarlar (buton komutu için)
    /// </summary>
    private void SetResolution(string resolution)
    {
        if (resolution == "Özel")
        {
            IsCustomResolution = true;
            SelectedResolution = "Özel";
        }
        else
        {
            IsCustomResolution = false;
            SelectedResolution = resolution;
            ParseResolution(resolution);
        }
    }

    private void UpdateCurrentSettings()
    {
        CurrentSettings = new DisplaySettings
        {
            PanelWidth = PanelWidth,
            PanelHeight = PanelHeight,
            // Width ve Height artık Pitch'e göre otomatik hesaplanıyor
            ColorType = SelectedColorType,
            Brightness = Brightness,
            BackgroundDarkness = BackgroundDarkness,
            LetterSpacing = LetterSpacing,
            PixelSize = 4, // Sabit değer - zoom ile ölçeklenir
            Pitch = SelectedPitch,
            CustomPitchRatio = CustomPitchRatio,
            Shape = SelectedShape,
            InvertColors = InvertColors,
            AgingPercent = AgingPercent
        };
    }

    private void LoadProfileSettings(Profile profile)
    {
        var settings = profile.Settings;

        // Panel boyutu
        var resolutionString = $"{settings.PanelWidth}x{settings.PanelHeight}";
        if (Resolutions.Contains(resolutionString))
        {
            SelectedResolution = resolutionString;
        }
        else
        {
            SelectedResolution = "Özel";
            PanelWidth = settings.PanelWidth;
            PanelHeight = settings.PanelHeight;
        }

        // Renk
        SelectedColorType = settings.ColorType;

        // Görsel ayarlar
        Brightness = settings.Brightness;
        BackgroundDarkness = settings.BackgroundDarkness;
        // PixelSize artık sabit, yükleme gerekmiyor
        SelectedPitch = settings.Pitch;
        CustomPitchRatio = settings.CustomPitchRatio;
        SelectedShape = settings.Shape;
        InvertColors = settings.InvertColors;
        AgingPercent = settings.AgingPercent;

        // Zone'ları yükle
        _zoneManager.LoadZones(profile.DefaultZones);
    }

    private async Task LoadFontAsync(string filePath)
    {
        try
        {
            BitmapFont font;
            if (filePath.EndsWith(".fnt", StringComparison.OrdinalIgnoreCase))
            {
                font = await _fontLoader.LoadBMFontAsync(filePath);
            }
            else if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                font = await _fontLoader.LoadJsonFontAsync(filePath);
            }
            else
            {
                throw new ArgumentException("Desteklenmeyen font formatı. .fnt veya .json dosyası seçin.");
            }

            if (_fontLoader.ValidateFont(font))
            {
                Fonts.Add(font);
                SelectedFont = font;
            }
        }
        catch (Exception)
        {
            // Hata View tarafından ele alınacak
            throw;
        }
    }

    private async Task SaveProfileAsync()
    {
        if (SelectedProfile != null)
        {
            SelectedProfile.Settings = CurrentSettings;
            SelectedProfile.FontName = SelectedFont?.Name ?? string.Empty;
            SelectedProfile.DefaultZones = _zoneManager.GetZones();
            SelectedProfile.ModifiedAt = DateTime.UtcNow;
            // Slot'lar zaten profil içinde, ayrıca kaydetmeye gerek yok

            await _profileManager.SaveProfileAsync(SelectedProfile);
        }
    }

    private async Task CreateProfileAsync(string name)
    {
        var profile = new Profile
        {
            Name = name,
            Settings = CurrentSettings,
            FontName = SelectedFont?.Name ?? string.Empty,
            DefaultZones = _zoneManager.GetZones(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await _profileManager.SaveProfileAsync(profile);
        Profiles.Add(profile);
        SelectedProfile = profile;
    }

    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile != null)
        {
            var name = SelectedProfile.Name;
            if (await _profileManager.DeleteProfileAsync(name))
            {
                Profiles.Remove(SelectedProfile);
                SelectedProfile = Profiles.FirstOrDefault();
            }
        }
    }

    private void SearchSlots()
    {
        SearchResults.Clear();
        if (!string.IsNullOrWhiteSpace(SlotSearchQuery) && SelectedProfile != null)
        {
            var query = SlotSearchQuery.ToLowerInvariant();
            var results = SelectedProfile.Slots.Values
                .Where(s => s.IsDefined && 
                    (s.Name.ToLowerInvariant().Contains(query) ||
                     s.Summary.ToLowerInvariant().Contains(query) ||
                     s.SlotNumber.ToString().Contains(query)))
                .OrderBy(s => s.SlotNumber)
                .Take(20);
            
            foreach (var slot in results)
            {
                SearchResults.Add(slot);
            }
        }
    }

    /// <summary>
    /// Mevcut slot numarasındaki slot'u yükler
    /// </summary>
    private void LoadCurrentSlot()
    {
        if (SelectedProfile == null)
        {
            CurrentSlot = null;
            return;
        }

        CurrentSlot = SelectedProfile.GetSlot(CurrentSlotNumber);
        SlotChanged?.Invoke(CurrentSlot);
    }

    /// <summary>
    /// Mevcut slot'u TabelaItem listesinden kaydeder
    /// Tüm öğelerin pozisyonu, boyutu, rengi, fontu, çerçevesi vb. kaydedilir
    /// </summary>
    public void SaveSlotFromItems(List<TabelaItem> items)
    {
        if (SelectedProfile == null) return;

        // Boş liste ise slot'u sil
        if (items.Count == 0)
        {
            SelectedProfile.Slots.Remove(CurrentSlotNumber);
            CurrentSlot = null;
            return;
        }

        var slot = new TabelaSlot 
        { 
            SlotNumber = CurrentSlotNumber,
            Name = CurrentSlot?.Name ?? string.Empty
        };
        
        // Tüm öğeleri derin kopyala
        slot.Items = items.Select(CloneTabelaItem).ToList();

        SelectedProfile.SetSlot(CurrentSlotNumber, slot);
        CurrentSlot = slot;
    }

    /// <summary>
    /// TabelaItem'ı derin kopyalar
    /// </summary>
    private static TabelaItem CloneTabelaItem(TabelaItem source)
    {
        var clone = new TabelaItem
        {
            Id = source.Id,
            Name = source.Name,
            Content = source.Content,
            ItemType = source.ItemType,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            Color = source.Color,
            UseColoredSegments = source.UseColoredSegments,
            HAlign = source.HAlign,
            VAlign = source.VAlign,
            FontName = source.FontName,
            LetterSpacing = source.LetterSpacing,
            SymbolName = source.SymbolName,
            SymbolSize = source.SymbolSize,
            IsScrolling = source.IsScrolling,
            ScrollDirection = source.ScrollDirection,
            ScrollSpeed = source.ScrollSpeed,
            IsVisible = source.IsVisible
        };

        // Renkli segmentleri kopyala
        if (source.UseColoredSegments && source.ColoredSegments != null)
        {
            foreach (var segment in source.ColoredSegments)
            {
                clone.ColoredSegments.Add(new ColoredTextSegment(segment.Text, segment.Color));
            }
        }

        // Çerçeve ayarlarını kopyala
        if (source.Border != null)
        {
            clone.Border = source.Border.Clone();
        }

        return clone;
    }

    /// <summary>
    /// Slot'u TabelaItem listesine dönüştürür
    /// </summary>
    public List<TabelaItem> GetSlotAsItems()
    {
        if (CurrentSlot == null || CurrentSlot.Items == null || CurrentSlot.Items.Count == 0)
        {
            return new List<TabelaItem>();
        }

        // Tüm öğeleri derin kopyala
        return CurrentSlot.Items.Select(CloneTabelaItem).ToList();
    }

    #endregion

    /// <summary>
    /// Profilleri yükler
    /// </summary>
    public async Task LoadProfilesAsync()
    {
        var profiles = await _profileManager.GetAllProfilesAsync();
        Profiles.Clear();
        foreach (var profile in profiles)
        {
            Profiles.Add(profile);
        }

        if (!Profiles.Any())
        {
            var defaultProfile = await _profileManager.GetOrCreateDefaultProfileAsync();
            Profiles.Add(defaultProfile);
        }

        SelectedProfile = Profiles.FirstOrDefault();
    }

    /// <summary>
    /// Dahili fontları yükler (Assets/Fonts klasöründen)
    /// Önce embedded resource olarak, sonra fiziksel dosya olarak dener
    /// </summary>
    public async Task LoadBuiltInFontsAsync()
    {
        var loadedFonts = new List<string>();
        var errors = new List<string>();

        try
        {
            // Önce fiziksel dosya yolunu dene (publish sonrası)
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var fontsDir = System.IO.Path.Combine(appDir, "Assets", "Fonts");

            if (System.IO.Directory.Exists(fontsDir))
            {
                await LoadFontsFromDirectoryAsync(fontsDir, loadedFonts, errors);
            }

            // Eğer hiç font yüklenemezse, embedded resource olarak dene
            if (Fonts.Count == 0)
            {
                await LoadEmbeddedFontsAsync(loadedFonts, errors);
            }

            // İlk fontu seç
            if (Fonts.Count > 0 && SelectedFont == null)
            {
                SelectedFont = Fonts[0];
            }

            // Hata varsa loglama için bilgi ver (debug modda)
            if (errors.Count > 0 && Fonts.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"Font yükleme uyarısı: {errors.Count} font yüklenemedi.");
                foreach (var error in errors)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {error}");
                }
            }
        }
        catch (Exception ex)
        {
            // Kritik hata - loglama
            System.Diagnostics.Debug.WriteLine($"Font yükleme hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Belirtilen dizinden fontları yükler
    /// </summary>
    private async Task LoadFontsFromDirectoryAsync(string fontsDir, List<string> loadedFonts, List<string> errors)
    {
        // .fnt dosyalarını yükle
        var fntFiles = System.IO.Directory.GetFiles(fontsDir, "*.fnt");
        foreach (var fntFile in fntFiles)
        {
            try
            {
                var font = await _fontLoader.LoadBMFontAsync(fntFile);
                if (_fontLoader.ValidateFont(font))
                {
                    Fonts.Add(font);
                    loadedFonts.Add(fntFile);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{System.IO.Path.GetFileName(fntFile)}: {ex.Message}");
            }
        }

        // .json dosyalarını yükle
        var jsonFiles = System.IO.Directory.GetFiles(fontsDir, "*.json");
        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                var font = await _fontLoader.LoadJsonFontAsync(jsonFile);
                if (_fontLoader.ValidateFont(font))
                {
                    Fonts.Add(font);
                    loadedFonts.Add(jsonFile);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{System.IO.Path.GetFileName(jsonFile)}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Embedded resource olarak fontları yüklemeyi dener
    /// </summary>
    private async Task LoadEmbeddedFontsAsync(List<string> loadedFonts, List<string> errors)
    {
        try
        {
            // Avalonia asset URI'leri ile embedded resource'ları dene
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames)
            {
                if (resourceName.EndsWith(".fnt", StringComparison.OrdinalIgnoreCase) ||
                    resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream != null)
                        {
                            // Geçici dosyaya yaz ve yükle
                            var tempPath = System.IO.Path.Combine(
                                System.IO.Path.GetTempPath(),
                                System.IO.Path.GetFileName(resourceName));

                            using (var fileStream = System.IO.File.Create(tempPath))
                            {
                                await stream.CopyToAsync(fileStream);
                            }

                            BitmapFont font;
                            if (resourceName.EndsWith(".fnt", StringComparison.OrdinalIgnoreCase))
                            {
                                font = await _fontLoader.LoadBMFontAsync(tempPath);
                            }
                            else
                            {
                                font = await _fontLoader.LoadJsonFontAsync(tempPath);
                            }

                            if (_fontLoader.ValidateFont(font))
                            {
                                Fonts.Add(font);
                                loadedFonts.Add(resourceName);
                            }

                            // Geçici dosyayı temizle
                            try { System.IO.File.Delete(tempPath); } catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Embedded {resourceName}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Embedded resource tarama hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Tüm başlangıç verilerini yükler
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadBuiltInFontsAsync();
        await LoadProfilesAsync();
    }
}
