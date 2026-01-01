using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LEDTabelam.Maui.Models;
using LEDTabelam.Maui.Services;
using SkiaSharp;

namespace LEDTabelam.Maui.ViewModels;

/// <summary>
/// Ana pencere ViewModel'i
/// TreeView, Preview, Properties ve Editor alt ViewModel'lerini yönetir
/// Requirements: 8.1, 8.2, 8.3, 8.4, 9.4, 13.5, 15.1-15.8
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IProjectManager _projectManager;
    private readonly IContentManager _contentManager;
    private readonly IExportService? _exportService;
    private readonly IFileSaver? _fileSaver;
    private readonly ILedRenderer? _ledRenderer;

    // Undo/Redo stacks for edit operations
    private readonly Stack<UndoableAction> _undoStack = new();
    private readonly Stack<UndoableAction> _redoStack = new();

    // Clipboard for copy/cut/paste operations
    private object? _clipboardItem;
    private bool _isCutOperation;

    [ObservableProperty]
    private TreeViewModel _treeView;

    [ObservableProperty]
    private PreviewViewModel _preview;

    [ObservableProperty]
    private PropertiesViewModel _properties;

    [ObservableProperty]
    private EditorViewModel _editor;

    [ObservableProperty]
    private string _statusMessage = "Hazır";

    [ObservableProperty]
    private string _connectionStatus = "Çevrimdışı";

    [ObservableProperty]
    private string _currentResolution = "128 x 32";

    [ObservableProperty]
    private string _projectName = "Yeni Proje";

    [ObservableProperty]
    private bool _hasUnsavedChanges = false;

    [ObservableProperty]
    private bool _canUndo = false;

    [ObservableProperty]
    private bool _canRedo = false;

    [ObservableProperty]
    private bool _canPaste = false;

    public MainViewModel(
        IProjectManager projectManager,
        IContentManager contentManager,
        IExportService exportService,
        IFileSaver fileSaver,
        ILedRenderer ledRenderer,
        TreeViewModel treeView,
        PreviewViewModel preview,
        PropertiesViewModel properties,
        EditorViewModel editor)
    {
        _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));
        _contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _fileSaver = fileSaver ?? throw new ArgumentNullException(nameof(fileSaver));
        _ledRenderer = ledRenderer ?? throw new ArgumentNullException(nameof(ledRenderer));
        _treeView = treeView ?? throw new ArgumentNullException(nameof(treeView));
        _preview = preview ?? throw new ArgumentNullException(nameof(preview));
        _properties = properties ?? throw new ArgumentNullException(nameof(properties));
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));

        // Alt ViewModel'ler arası bağlantıları kur
        SetupViewModelConnections();
        
        // Başlangıç durumunu ayarla
        UpdateProjectInfo();
        
        // Varsayılan projeyi TreeView'a yükle
        TreeView.LoadProject(_projectManager.CurrentProject);
        
        System.Diagnostics.Debug.WriteLine("✅ MainViewModel: DI constructor completed successfully");
    }

    /// <summary>
    /// Parameterless constructor for design-time and testing
    /// WARNING: Commands will NOT work properly with this constructor!
    /// </summary>
    [Obsolete("Design-time only. Use DI constructor at runtime.")]
    public MainViewModel()
    {
        System.Diagnostics.Debug.WriteLine("⚠️ MainViewModel: Parameterless constructor called - Commands will NOT work!");
        _projectManager = null!;
        _contentManager = null!;
        _exportService = null;
        _fileSaver = null;
        _treeView = new TreeViewModel();
        _preview = new PreviewViewModel();
        _properties = new PropertiesViewModel();
        _editor = new EditorViewModel();
    }

    private void SetupViewModelConnections()
    {
        // TreeView seçim değişikliğini dinle
        TreeView.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TreeViewModel.SelectedItem))
            {
                OnTreeViewSelectionChanged();
            }
        };
        
        // Editor'daki değişiklikleri dinle ve anlık önizleme güncelle
        Editor.PreviewUpdateRequested += (s, e) =>
        {
            if (Editor.EditingContent != null)
            {
                System.Diagnostics.Debug.WriteLine($"🔵 PreviewUpdateRequested: Updating preview for {Editor.EditingContent.Name}");
                RenderPreview(Editor.EditingContent);
            }
        };
        
        // Başlangıçta varsayılan içeriği render et
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Kısa bir gecikme ile fontların yüklenmesini bekle
            await Task.Delay(500);
            
            // İlk ekranın ilk programının ilk içeriğini seç ve render et
            var project = _projectManager.CurrentProject;
            if (project.Screens.Count > 0)
            {
                var screen = project.Screens[0];
                if (screen.Programs.Count > 0)
                {
                    var program = screen.Programs[0];
                    if (program.Contents.Count > 0)
                    {
                        var content = program.Contents[0];
                        TreeView.SelectItem(content);
                        System.Diagnostics.Debug.WriteLine($"✅ SetupViewModelConnections: Auto-selected {content.Name}");
                    }
                }
            }
        });
    }

    private void OnTreeViewSelectionChanged()
    {
        var selectedItem = TreeView.SelectedItem;
        
        if (selectedItem is ContentItem content)
        {
            // İçeriğin ait olduğu programı bul ve Editor.Items'ı güncelle
            var parentProgram = FindParentProgram(content);
            if (parentProgram != null)
            {
                SyncEditorItems(parentProgram);
            }
            
            Properties.SelectedContent = content;
            Editor.EditingContent = content;
            content.IsSelected = true;
            StatusMessage = $"Seçili: {content.Name}";
            
            // Önizlemeyi güncelle
            RenderPreview(content);
        }
        else if (selectedItem is ProgramNode program)
        {
            // Program seçildiğinde tüm içeriklerini Editor'a yükle
            SyncEditorItems(program);
            
            Properties.SelectedContent = null;
            Editor.EditingContent = null;
            StatusMessage = $"Seçili program: {program.Name}";
            Preview.LoadProgram(program);
            
            // İlk içeriği render et
            if (program.Contents.Count > 0)
            {
                RenderPreview(program.Contents[0]);
            }
        }
        else if (selectedItem is ScreenNode screen)
        {
            // Ekranın ilk programının içeriklerini yükle
            if (screen.Programs.Count > 0)
            {
                SyncEditorItems(screen.Programs[0]);
            }
            else
            {
                Editor.Items.Clear();
            }
            
            Properties.SelectedContent = null;
            Editor.EditingContent = null;
            StatusMessage = $"Seçili ekran: {screen.Name}";
            CurrentResolution = $"{screen.Width} x {screen.Height}";
            
            // Ekranın ilk programının ilk içeriğini render et
            if (screen.Programs.Count > 0 && screen.Programs[0].Contents.Count > 0)
            {
                RenderPreview(screen.Programs[0].Contents[0]);
            }
        }
        else
        {
            Editor.Items.Clear();
            Properties.SelectedContent = null;
            Editor.EditingContent = null;
        }
    }

    /// <summary>
    /// Editor.Items'ı program içerikleriyle senkronize eder
    /// </summary>
    private void SyncEditorItems(ProgramNode program)
    {
        Editor.Items.Clear();
        foreach (var content in program.Contents)
        {
            Editor.Items.Add(content);
        }
        Editor.RaiseItemsChanged();
    }

    /// <summary>
    /// İçeriğin ait olduğu programı bulur
    /// </summary>
    private ProgramNode? FindParentProgram(ContentItem content)
    {
        foreach (var screen in _projectManager.CurrentProject.Screens)
        {
            foreach (var program in screen.Programs)
            {
                if (program.Contents.Contains(content))
                {
                    return program;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// İçeriği LED önizleme olarak render eder
    /// </summary>
    private void RenderPreview(ContentItem content)
    {
        try
        {
            if (_contentManager == null || content == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ RenderPreview: ContentManager or content is null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"🔵 RenderPreview: Rendering {content.Name} ({content.ContentType})");

            var globalSettings = _projectManager.CurrentProject.GlobalSettings;
            var settings = new DisplaySettings
            {
                PanelWidth = globalSettings.PanelWidth,
                PanelHeight = globalSettings.PanelHeight,
                PixelSize = 4,
                Pitch = globalSettings.Pitch,
                ColorType = globalSettings.ColorType,
                Brightness = globalSettings.Brightness,
                Shape = globalSettings.Shape
            };

            // PreviewViewModel'e DisplaySettings'i de gönder
            Preview.DisplaySettings = settings;

            System.Diagnostics.Debug.WriteLine($"🔵 RenderPreview: Settings - PanelWidth={settings.PanelWidth}, PanelHeight={settings.PanelHeight}");

            var bitmap = _contentManager.RenderContent(content, settings);
            
            System.Diagnostics.Debug.WriteLine($"🔵 RenderPreview: Bitmap created - {bitmap?.Width}x{bitmap?.Height}");
            
            Preview.UpdatePreview(bitmap);
            
            System.Diagnostics.Debug.WriteLine("✅ RenderPreview: Preview updated");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ RenderPreview Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
        }
    }

    private void UpdateProjectInfo()
    {
        if (_projectManager?.CurrentProject != null)
        {
            ProjectName = _projectManager.CurrentProject.Name;
            var settings = _projectManager.CurrentProject.GlobalSettings;
            CurrentResolution = $"{settings.Width} x {settings.Height}";
        }
    }

    /// <summary>
    /// Yeni proje oluşturur
    /// Requirement: 8.5
    /// </summary>
    [RelayCommand]
    private async Task NewProjectAsync()
    {
        try
        {
            StatusMessage = "Yeni proje oluşturuluyor...";
            
            var project = await _projectManager.NewProjectAsync();
            TreeView.LoadProject(project);
            UpdateProjectInfo();
            HasUnsavedChanges = false;
            
            StatusMessage = "Yeni proje oluşturuldu";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
        }
    }

    /// <summary>
    /// Proje açar
    /// Requirement: 8.6
    /// </summary>
    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        try
        {
            StatusMessage = "Proje açılıyor...";
            
            // FilePicker ile dosya seçimi (MAUI'de platform-specific)
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Proje Dosyası Seç",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".ledproj", ".json" } }
                })
            });

            if (result != null)
            {
                var project = await _projectManager.LoadProjectAsync(result.FullPath);
                TreeView.LoadProject(project);
                UpdateProjectInfo();
                HasUnsavedChanges = false;
                
                StatusMessage = $"Proje açıldı: {project.Name}";
            }
            else
            {
                StatusMessage = "Proje açma iptal edildi";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Proje açma hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Projeyi kaydeder
    /// Requirement: 8.7
    /// </summary>
    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        try
        {
            StatusMessage = "Proje kaydediliyor...";
            
            if (string.IsNullOrEmpty(_projectManager.CurrentProject.FilePath))
            {
                await SaveProjectAsAsync();
                return;
            }
            
            await _projectManager.SaveProjectAsync();
            HasUnsavedChanges = false;
            
            StatusMessage = "Proje kaydedildi";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Kaydetme hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Projeyi farklı kaydeder
    /// Requirement: 8.7
    /// </summary>
    [RelayCommand]
    private async Task SaveProjectAsAsync()
    {
        try
        {
            StatusMessage = "Dosya konumu seçiliyor...";
            
            // Serialize project to JSON
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(_projectManager.CurrentProject, jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            using var stream = new MemoryStream(bytes);
            var fileName = $"{_projectManager.CurrentProject.Name}.ledproj";
            
            // Use FileSaver from CommunityToolkit.Maui
            if (_fileSaver != null)
            {
                var result = await _fileSaver.SaveAsync(fileName, stream, CancellationToken.None);
                
                if (result.IsSuccessful)
                {
                    // Update project file path
                    _projectManager.CurrentProject.FilePath = result.FilePath;
                    UpdateProjectInfo();
                    HasUnsavedChanges = false;
                    
                    StatusMessage = $"Proje kaydedildi: {Path.GetFileName(result.FilePath)}";
                }
                else
                {
                    StatusMessage = result.Exception?.Message ?? "Kaydetme iptal edildi";
                }
            }
            else
            {
                // Fallback: Save to Documents folder
                var filePath = await GetSaveFilePathAsync();
                if (!string.IsNullOrEmpty(filePath))
                {
                    await _projectManager.SaveProjectAsync(filePath);
                    UpdateProjectInfo();
                    HasUnsavedChanges = false;
                    StatusMessage = $"Proje kaydedildi: {filePath}";
                }
                else
                {
                    StatusMessage = "Kaydetme iptal edildi";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Kaydetme hatası: {ex.Message}";
        }
    }

    private async Task<string?> GetSaveFilePathAsync()
    {
        // Fallback: Platform-specific dosya kaydetme
        var fileName = $"{_projectManager.CurrentProject.Name}.ledproj";
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return await Task.FromResult(Path.Combine(documentsPath, fileName));
    }

    /// <summary>
    /// Yeni ekran ekler
    /// Requirement: 3.8
    /// </summary>
    [RelayCommand]
    private void AddScreen()
    {
        try
        {
            if (_projectManager == null)
            {
                StatusMessage = "Hata: Proje yöneticisi başlatılmadı";
                System.Diagnostics.Debug.WriteLine("❌ AddScreen: _projectManager is null!");
                return;
            }
            
            var screen = new ScreenNode
            {
                Name = _projectManager.GenerateScreenName(),
                Width = _projectManager.CurrentProject.GlobalSettings.Width,
                Height = _projectManager.CurrentProject.GlobalSettings.Height
            };
            
            _projectManager.AddScreen(screen);
            TreeView.RefreshScreens();
            TreeView.SelectItem(screen);
            HasUnsavedChanges = true;
            
            StatusMessage = $"Ekran eklendi: {screen.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ekran ekleme hatası: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"❌ AddScreen Error: {ex}");
        }
    }

    /// <summary>
    /// Seçili ekrana yeni program ekler
    /// Requirement: 3.9
    /// </summary>
    [RelayCommand]
    private void AddProgram()
    {
        System.Diagnostics.Debug.WriteLine("🔵 AddProgram command executed");
        try
        {
            if (_projectManager == null)
            {
                StatusMessage = "Hata: Proje yöneticisi başlatılmadı";
                System.Diagnostics.Debug.WriteLine("❌ AddProgram: _projectManager is null!");
                return;
            }
            
            var selectedScreen = GetSelectedScreen();
            if (selectedScreen == null)
            {
                StatusMessage = "Lütfen önce bir ekran seçin";
                return;
            }
            
            var program = new ProgramNode
            {
                Name = _projectManager.GenerateProgramName(selectedScreen)
            };
            
            _projectManager.AddProgram(selectedScreen, program);
            TreeView.RefreshScreens();
            TreeView.SelectItem(program);
            HasUnsavedChanges = true;
            
            StatusMessage = $"Program eklendi: {program.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Program ekleme hatası: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"❌ AddProgram Error: {ex}");
        }
    }

    /// <summary>
    /// Seçili programa metin içeriği ekler
    /// Requirement: 11.1
    /// </summary>
    [RelayCommand]
    private void AddTextContent()
    {
        System.Diagnostics.Debug.WriteLine("🔵 AddTextContent command executed");
        try
        {
            if (_contentManager == null || _projectManager == null)
            {
                StatusMessage = "Hata: Servisler başlatılmadı";
                System.Diagnostics.Debug.WriteLine("❌ AddTextContent: Services are null!");
                return;
            }
            
            var selectedProgram = GetSelectedProgram();
            if (selectedProgram == null)
            {
                StatusMessage = "Lütfen önce bir program seçin";
                return;
            }
            
            var content = _contentManager.CreateTextContent();
            _projectManager.AddContent(selectedProgram, content);
            TreeView.RefreshScreens();
            TreeView.SelectItem(content);
            HasUnsavedChanges = true;
            
            StatusMessage = $"Metin içeriği eklendi: {content.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"İçerik ekleme hatası: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"❌ AddTextContent Error: {ex}");
        }
    }

    /// <summary>
    /// Seçili programa saat içeriği ekler
    /// Requirement: 11.3
    /// </summary>
    [RelayCommand]
    private void AddClockContent()
    {
        try
        {
            var selectedProgram = GetSelectedProgram();
            if (selectedProgram == null)
            {
                StatusMessage = "Lütfen önce bir program seçin";
                return;
            }
            
            var content = _contentManager.CreateClockContent();
            _projectManager.AddContent(selectedProgram, content);
            TreeView.RefreshScreens();
            TreeView.SelectItem(content);
            HasUnsavedChanges = true;
            
            StatusMessage = $"Saat içeriği eklendi: {content.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"İçerik ekleme hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Seçili programa tarih içeriği ekler
    /// Requirement: 11.4
    /// </summary>
    [RelayCommand]
    private void AddDateContent()
    {
        try
        {
            var selectedProgram = GetSelectedProgram();
            if (selectedProgram == null)
            {
                StatusMessage = "Lütfen önce bir program seçin";
                return;
            }
            
            var content = _contentManager.CreateDateContent();
            _projectManager.AddContent(selectedProgram, content);
            TreeView.RefreshScreens();
            TreeView.SelectItem(content);
            HasUnsavedChanges = true;
            
            StatusMessage = $"Tarih içeriği eklendi: {content.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"İçerik ekleme hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Seçili programa geri sayım içeriği ekler
    /// Requirement: 11.5
    /// </summary>
    [RelayCommand]
    private void AddCountdownContent()
    {
        try
        {
            var selectedProgram = GetSelectedProgram();
            if (selectedProgram == null)
            {
                StatusMessage = "Lütfen önce bir program seçin";
                return;
            }
            
            var content = _contentManager.CreateCountdownContent();
            _projectManager.AddContent(selectedProgram, content);
            TreeView.RefreshScreens();
            TreeView.SelectItem(content);
            HasUnsavedChanges = true;
            
            StatusMessage = $"Geri sayım içeriği eklendi: {content.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"İçerik ekleme hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Önizlemeyi başlatır/durdurur
    /// Requirement: 4.1
    /// </summary>
    [RelayCommand]
    private void StartPreview()
    {
        Preview.TogglePlayCommand.Execute(null);
        StatusMessage = Preview.IsPlaying ? "Önizleme başlatıldı" : "Önizleme durduruldu";
    }

    #region Export Commands (Requirement: 13.5)

    /// <summary>
    /// Mevcut önizlemeyi PNG olarak dışa aktarır
    /// Requirement: 13.5
    /// </summary>
    [RelayCommand]
    private async Task ExportPngAsync()
    {
        try
        {
            if (Preview.PreviewBitmap == null)
            {
                StatusMessage = "Dışa aktarılacak önizleme yok";
                return;
            }

            StatusMessage = "PNG dışa aktarılıyor...";
            
            // Encode bitmap to PNG
            using var image = SKImage.FromBitmap(Preview.PreviewBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream();
            data.SaveTo(stream);
            stream.Position = 0;
            
            var fileName = $"{_projectManager.CurrentProject.Name}.png";
            
            if (_fileSaver != null)
            {
                var result = await _fileSaver.SaveAsync(fileName, stream, CancellationToken.None);
                
                if (result.IsSuccessful)
                {
                    StatusMessage = $"PNG kaydedildi: {Path.GetFileName(result.FilePath)}";
                }
                else
                {
                    StatusMessage = result.Exception?.Message ?? "Dışa aktarma iptal edildi";
                }
            }
            else if (_exportService != null)
            {
                // Fallback: Save to Documents folder
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var filePath = Path.Combine(documentsPath, fileName);
                var success = await _exportService.ExportPngAsync(Preview.PreviewBitmap, filePath);
                StatusMessage = success ? $"PNG kaydedildi: {filePath}" : "PNG kaydetme başarısız";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"PNG dışa aktarma hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Animasyonu GIF olarak dışa aktarır
    /// Requirement: 13.5
    /// </summary>
    [RelayCommand]
    private async Task ExportGifAsync()
    {
        try
        {
            if (_exportService == null)
            {
                StatusMessage = "Dışa aktarma servisi kullanılamıyor";
                return;
            }

            StatusMessage = "GIF dışa aktarılıyor...";
            
            // Generate frames from current program
            var frames = await GenerateAnimationFramesAsync();
            if (frames.Count == 0)
            {
                StatusMessage = "Dışa aktarılacak frame yok";
                return;
            }

            var fileName = $"{_projectManager.CurrentProject.Name}.gif";
            
            if (_fileSaver != null)
            {
                // Create GIF in memory first
                using var memStream = new MemoryStream();
                var tempPath = Path.GetTempFileName();
                var success = await _exportService.ExportGifAsync(frames, tempPath, 30);
                
                if (success && File.Exists(tempPath))
                {
                    var gifBytes = await File.ReadAllBytesAsync(tempPath);
                    using var gifStream = new MemoryStream(gifBytes);
                    
                    var result = await _fileSaver.SaveAsync(fileName, gifStream, CancellationToken.None);
                    
                    // Clean up temp file
                    try { File.Delete(tempPath); } catch { }
                    
                    if (result.IsSuccessful)
                    {
                        StatusMessage = $"GIF kaydedildi: {Path.GetFileName(result.FilePath)}";
                    }
                    else
                    {
                        StatusMessage = result.Exception?.Message ?? "Dışa aktarma iptal edildi";
                    }
                }
                else
                {
                    StatusMessage = "GIF oluşturma başarısız";
                }
            }
            else
            {
                // Fallback: Save to Documents folder
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var filePath = Path.Combine(documentsPath, fileName);
                var success = await _exportService.ExportGifAsync(frames, filePath, 30);
                StatusMessage = success ? $"GIF kaydedildi: {filePath}" : "GIF kaydetme başarısız";
            }

            // Dispose frames
            foreach (var frame in frames)
            {
                frame.Dispose();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"GIF dışa aktarma hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Animasyonu WebP olarak dışa aktarır
    /// Requirement: 13.5
    /// </summary>
    [RelayCommand]
    private async Task ExportWebPAsync()
    {
        try
        {
            if (_exportService == null)
            {
                StatusMessage = "Dışa aktarma servisi kullanılamıyor";
                return;
            }

            StatusMessage = "WebP dışa aktarılıyor...";
            
            // Generate frames from current program
            var frames = await GenerateAnimationFramesAsync();
            if (frames.Count == 0)
            {
                StatusMessage = "Dışa aktarılacak frame yok";
                return;
            }

            var fileName = $"{_projectManager.CurrentProject.Name}.webp";
            
            if (_fileSaver != null)
            {
                // Create WebP in memory first
                var tempPath = Path.GetTempFileName();
                var success = await _exportService.ExportWebPAsync(frames, tempPath, 30);
                
                if (success && File.Exists(tempPath))
                {
                    var webpBytes = await File.ReadAllBytesAsync(tempPath);
                    using var webpStream = new MemoryStream(webpBytes);
                    
                    var result = await _fileSaver.SaveAsync(fileName, webpStream, CancellationToken.None);
                    
                    // Clean up temp file
                    try { File.Delete(tempPath); } catch { }
                    
                    if (result.IsSuccessful)
                    {
                        StatusMessage = $"WebP kaydedildi: {Path.GetFileName(result.FilePath)}";
                    }
                    else
                    {
                        StatusMessage = result.Exception?.Message ?? "Dışa aktarma iptal edildi";
                    }
                }
                else
                {
                    StatusMessage = "WebP oluşturma başarısız";
                }
            }
            else
            {
                // Fallback: Save to Documents folder
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var filePath = Path.Combine(documentsPath, fileName);
                var success = await _exportService.ExportWebPAsync(frames, filePath, 30);
                StatusMessage = success ? $"WebP kaydedildi: {filePath}" : "WebP kaydetme başarısız";
            }

            // Dispose frames
            foreach (var frame in frames)
            {
                frame.Dispose();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"WebP dışa aktarma hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Animasyon frame'lerini oluşturur
    /// </summary>
    private async Task<List<SKBitmap>> GenerateAnimationFramesAsync()
    {
        var frames = new List<SKBitmap>();
        
        // If we have a preview bitmap, use it as a single frame
        if (Preview.PreviewBitmap != null)
        {
            // Clone the bitmap for the frame list
            var clone = new SKBitmap(Preview.PreviewBitmap.Width, Preview.PreviewBitmap.Height);
            using var canvas = new SKCanvas(clone);
            canvas.DrawBitmap(Preview.PreviewBitmap, 0, 0);
            frames.Add(clone);
        }
        
        return await Task.FromResult(frames);
    }

    #endregion

    private ScreenNode? GetSelectedScreen()
    {
        if (TreeView.SelectedItem is ScreenNode screen)
            return screen;
        
        if (TreeView.SelectedItem is ProgramNode program)
            return _projectManager.FindParentScreen(program);
        
        if (TreeView.SelectedItem is ContentItem content)
        {
            var parentProgram = _projectManager.FindParentProgram(content);
            if (parentProgram != null)
                return _projectManager.FindParentScreen(parentProgram);
        }
        
        // Varsayılan olarak ilk ekranı döndür
        return _projectManager.CurrentProject.Screens.FirstOrDefault();
    }

    private ProgramNode? GetSelectedProgram()
    {
        if (TreeView.SelectedItem is ProgramNode program)
            return program;
        
        if (TreeView.SelectedItem is ContentItem content)
            return _projectManager.FindParentProgram(content);
        
        if (TreeView.SelectedItem is ScreenNode screen)
            return screen.Programs.FirstOrDefault();
        
        return null;
    }

    #region Undo/Redo Commands (Requirements: 15.4, 15.5)

    /// <summary>
    /// Son işlemi geri alır
    /// Requirement: 15.4
    /// </summary>
    [RelayCommand]
    private void Undo()
    {
        if (_undoStack.Count == 0)
        {
            StatusMessage = "Geri alınacak işlem yok";
            return;
        }

        try
        {
            var action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);
            
            UpdateUndoRedoState();
            TreeView.RefreshScreens();
            HasUnsavedChanges = true;
            
            StatusMessage = $"Geri alındı: {action.Description}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Geri alma hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Geri alınan işlemi yineler
    /// Requirement: 15.5
    /// </summary>
    [RelayCommand]
    private void Redo()
    {
        if (_redoStack.Count == 0)
        {
            StatusMessage = "Yinelenecek işlem yok";
            return;
        }

        try
        {
            var action = _redoStack.Pop();
            action.Redo();
            _undoStack.Push(action);
            
            UpdateUndoRedoState();
            TreeView.RefreshScreens();
            HasUnsavedChanges = true;
            
            StatusMessage = $"Yinelendi: {action.Description}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Yineleme hatası: {ex.Message}";
        }
    }

    private void UpdateUndoRedoState()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
    }

    private void AddUndoableAction(UndoableAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear(); // Clear redo stack when new action is performed
        UpdateUndoRedoState();
    }

    #endregion

    #region Clipboard Commands (Requirements: 15.6, 15.7)

    /// <summary>
    /// Seçili öğeyi siler
    /// Requirement: 15.6
    /// </summary>
    [RelayCommand]
    private void DeleteSelected()
    {
        if (TreeView.SelectedItem == null)
        {
            StatusMessage = "Silinecek öğe seçilmedi";
            return;
        }

        try
        {
            var selectedItem = TreeView.SelectedItem;
            var itemName = GetItemName(selectedItem);
            
            // Create undoable action before deleting
            var undoAction = CreateDeleteUndoAction(selectedItem);
            
            TreeView.DeleteSelectedCommand.Execute(null);
            
            if (undoAction != null)
            {
                AddUndoableAction(undoAction);
            }
            
            HasUnsavedChanges = true;
            StatusMessage = $"Silindi: {itemName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Silme hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Seçili öğeyi kopyalar
    /// Requirement: 15.7
    /// </summary>
    [RelayCommand]
    private void Copy()
    {
        if (TreeView.SelectedItem == null)
        {
            StatusMessage = "Kopyalanacak öğe seçilmedi";
            return;
        }

        try
        {
            _clipboardItem = CloneItem(TreeView.SelectedItem);
            _isCutOperation = false;
            CanPaste = _clipboardItem != null;
            
            var itemName = GetItemName(TreeView.SelectedItem);
            StatusMessage = $"Kopyalandı: {itemName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Kopyalama hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Seçili öğeyi keser
    /// Requirement: 15.7
    /// </summary>
    [RelayCommand]
    private void Cut()
    {
        if (TreeView.SelectedItem == null)
        {
            StatusMessage = "Kesilecek öğe seçilmedi";
            return;
        }

        try
        {
            _clipboardItem = TreeView.SelectedItem;
            _isCutOperation = true;
            CanPaste = _clipboardItem != null;
            
            var itemName = GetItemName(TreeView.SelectedItem);
            StatusMessage = $"Kesildi: {itemName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Kesme hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Panodaki öğeyi yapıştırır
    /// Requirement: 15.7
    /// </summary>
    [RelayCommand]
    private void Paste()
    {
        if (_clipboardItem == null)
        {
            StatusMessage = "Yapıştırılacak öğe yok";
            return;
        }

        try
        {
            object? itemToPaste;
            
            if (_isCutOperation)
            {
                // For cut, move the original item
                itemToPaste = _clipboardItem;
                
                // Remove from original location first
                RemoveItemFromParent(_clipboardItem);
                
                // Clear clipboard after cut-paste
                _clipboardItem = null;
                _isCutOperation = false;
                CanPaste = false;
            }
            else
            {
                // For copy, create a clone
                itemToPaste = CloneItem(_clipboardItem);
            }

            if (itemToPaste == null)
            {
                StatusMessage = "Yapıştırma başarısız";
                return;
            }

            // Paste to appropriate location based on selection
            PasteItem(itemToPaste);
            
            TreeView.RefreshScreens();
            HasUnsavedChanges = true;
            
            var itemName = GetItemName(itemToPaste);
            StatusMessage = $"Yapıştırıldı: {itemName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Yapıştırma hatası: {ex.Message}";
        }
    }

    private string GetItemName(object? item)
    {
        return item switch
        {
            ContentItem content => content.Name,
            ProgramNode program => program.Name,
            ScreenNode screen => screen.Name,
            _ => "Bilinmeyen"
        };
    }

    private object? CloneItem(object? item)
    {
        return item switch
        {
            ContentItem content => CloneContent(content),
            ProgramNode program => CloneProgram(program),
            ScreenNode screen => CloneScreen(screen),
            _ => null
        };
    }

    private void RemoveItemFromParent(object? item)
    {
        if (item is ContentItem content)
        {
            foreach (var screen in _projectManager.CurrentProject.Screens)
            {
                foreach (var program in screen.Programs)
                {
                    if (program.Contents.Remove(content))
                        return;
                }
            }
        }
        else if (item is ProgramNode program)
        {
            foreach (var screen in _projectManager.CurrentProject.Screens)
            {
                if (screen.Programs.Remove(program))
                    return;
            }
        }
        else if (item is ScreenNode screen)
        {
            _projectManager.CurrentProject.Screens.Remove(screen);
        }
    }

    private void PasteItem(object? item)
    {
        if (item is ContentItem content)
        {
            var targetProgram = GetSelectedProgram();
            if (targetProgram != null)
            {
                content.Name = EnsureUniqueName(content.Name, targetProgram.Contents.Select(c => c.Name));
                targetProgram.Contents.Add(content);
                TreeView.SelectItem(content);
            }
        }
        else if (item is ProgramNode program)
        {
            var targetScreen = GetSelectedScreen();
            if (targetScreen != null)
            {
                program.Name = EnsureUniqueName(program.Name, targetScreen.Programs.Select(p => p.Name));
                targetScreen.Programs.Add(program);
                TreeView.SelectItem(program);
            }
        }
        else if (item is ScreenNode screen)
        {
            screen.Name = EnsureUniqueName(screen.Name, _projectManager.CurrentProject.Screens.Select(s => s.Name));
            _projectManager.CurrentProject.Screens.Add(screen);
            TreeView.SelectItem(screen);
        }
    }

    private string EnsureUniqueName(string baseName, IEnumerable<string> existingNames)
    {
        var names = existingNames.ToHashSet();
        if (!names.Contains(baseName))
            return baseName;

        var counter = 1;
        var newName = $"{baseName} ({counter})";
        while (names.Contains(newName))
        {
            counter++;
            newName = $"{baseName} ({counter})";
        }
        return newName;
    }

    private UndoableAction? CreateDeleteUndoAction(object? item)
    {
        if (item is ContentItem content)
        {
            var parentProgram = _projectManager.FindParentProgram(content);
            if (parentProgram != null)
            {
                var index = parentProgram.Contents.IndexOf(content);
                return new UndoableAction(
                    $"İçerik silme: {content.Name}",
                    () => parentProgram.Contents.Remove(content),
                    () => parentProgram.Contents.Insert(Math.Min(index, parentProgram.Contents.Count), content)
                );
            }
        }
        else if (item is ProgramNode program)
        {
            var parentScreen = _projectManager.FindParentScreen(program);
            if (parentScreen != null)
            {
                var index = parentScreen.Programs.IndexOf(program);
                return new UndoableAction(
                    $"Program silme: {program.Name}",
                    () => parentScreen.Programs.Remove(program),
                    () => parentScreen.Programs.Insert(Math.Min(index, parentScreen.Programs.Count), program)
                );
            }
        }
        else if (item is ScreenNode screen)
        {
            var index = _projectManager.CurrentProject.Screens.IndexOf(screen);
            return new UndoableAction(
                $"Ekran silme: {screen.Name}",
                () => _projectManager.CurrentProject.Screens.Remove(screen),
                () => _projectManager.CurrentProject.Screens.Insert(Math.Min(index, _projectManager.CurrentProject.Screens.Count), screen)
            );
        }
        return null;
    }

    private static ContentItem CloneContent(ContentItem source)
    {
        return new ContentItem
        {
            Id = Guid.NewGuid().ToString(),
            Name = source.Name,
            ContentType = source.ContentType,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            DurationMs = source.DurationMs,
            ShowImmediately = source.ShowImmediately,
            EntryEffect = new EffectConfig
            {
                EffectType = source.EntryEffect.EffectType,
                SpeedMs = source.EntryEffect.SpeedMs,
                Direction = source.EntryEffect.Direction
            },
            ExitEffect = new EffectConfig
            {
                EffectType = source.ExitEffect.EffectType,
                SpeedMs = source.ExitEffect.SpeedMs,
                Direction = source.ExitEffect.Direction
            }
        };
    }

    private static ProgramNode CloneProgram(ProgramNode source)
    {
        var clone = new ProgramNode
        {
            Id = Guid.NewGuid().ToString(),
            Name = source.Name,
            IsLoop = source.IsLoop,
            TransitionType = source.TransitionType,
            IsExpanded = source.IsExpanded
        };

        foreach (var content in source.Contents)
        {
            clone.Contents.Add(CloneContent(content));
        }

        return clone;
    }

    private static ScreenNode CloneScreen(ScreenNode source)
    {
        var clone = new ScreenNode
        {
            Id = Guid.NewGuid().ToString(),
            Name = source.Name,
            Width = source.Width,
            Height = source.Height,
            IsExpanded = source.IsExpanded
        };

        foreach (var program in source.Programs)
        {
            clone.Programs.Add(CloneProgram(program));
        }

        return clone;
    }

    #endregion
}

/// <summary>
/// Geri alınabilir işlem sınıfı
/// </summary>
public class UndoableAction
{
    public string Description { get; }
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public UndoableAction(string description, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _redoAction(); // Redo action restores the item
    public void Redo() => _undoAction(); // Undo action removes the item again
}
