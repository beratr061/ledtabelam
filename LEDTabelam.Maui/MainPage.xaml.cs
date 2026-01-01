using System.Threading.Tasks;
using LEDTabelam.Maui.Controls;
using LEDTabelam.Maui.Helpers;
using LEDTabelam.Maui.Models;
using LEDTabelam.Maui.Services;
using LEDTabelam.Maui.ViewModels;

namespace LEDTabelam.Maui;

/// <summary>
/// Ana sayfa - HD2020 benzeri 4 bölgeli layout
/// Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 15.1-15.8
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly IKeyboardShortcutService _keyboardShortcutService;
    private readonly IFontLoader _fontLoader;

    public MainPage(MainViewModel viewModel, IKeyboardShortcutService keyboardShortcutService, IFontLoader fontLoader)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MainPage: DI constructor called");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("MainPage: InitializeComponent completed");
            BindingContext = viewModel;
            _keyboardShortcutService = keyboardShortcutService;
            _fontLoader = fontLoader;

            // Wire up keyboard shortcuts to ViewModel commands
            SetupKeyboardShortcuts(viewModel);

            // Initialize the keyboard helper for platform-specific handling
            KeyboardHelper.Initialize(_keyboardShortcutService, this);
            
            // Fontları yükle
            _ = LoadFontsAsync();
            
            System.Diagnostics.Debug.WriteLine("MainPage: Constructor completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainPage DI Constructor Error: {ex}");
            throw;
        }
    }

    private async Task LoadFontsAsync()
    {
        try
        {
            await _fontLoader.LoadDefaultFontsAsync();
            System.Diagnostics.Debug.WriteLine("✅ MainPage: Fontlar yüklendi");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ MainPage: Font yükleme hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Parameterless constructor for design-time (should not be used at runtime)
    /// WARNING: This constructor creates a non-functional ViewModel for design-time only!
    /// </summary>
    [Obsolete("Design-time only. Use DI constructor at runtime.")]
    public MainPage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("⚠️ MainPage: Parameterless constructor called - THIS SHOULD NOT HAPPEN AT RUNTIME!");
            InitializeComponent();
            // Create a minimal ViewModel for design-time preview only
            // Commands will NOT work with this constructor!
            BindingContext = new MainViewModel();
            _keyboardShortcutService = new KeyboardShortcutService();
            _fontLoader = new FontLoader();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainPage Constructor Error: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Klavye kısayollarını ViewModel komutlarına bağlar
    /// Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7, 15.8
    /// </summary>
    private void SetupKeyboardShortcuts(MainViewModel viewModel)
    {
        // Temel kısayollar (Requirements: 15.1, 15.2, 15.3)
        _keyboardShortcutService.NewProjectCommand = viewModel.NewProjectCommand;
        _keyboardShortcutService.OpenProjectCommand = viewModel.OpenProjectCommand;
        _keyboardShortcutService.SaveProjectCommand = viewModel.SaveProjectCommand;

        // Düzenleme kısayolları (Requirements: 15.4, 15.5, 15.6, 15.7)
        _keyboardShortcutService.UndoCommand = viewModel.UndoCommand;
        _keyboardShortcutService.RedoCommand = viewModel.RedoCommand;
        _keyboardShortcutService.DeleteCommand = viewModel.DeleteSelectedCommand;
        _keyboardShortcutService.CopyCommand = viewModel.CopyCommand;
        _keyboardShortcutService.PasteCommand = viewModel.PasteCommand;
        _keyboardShortcutService.CutCommand = viewModel.CutCommand;

        // Önizleme kısayolu (Requirement: 15.8)
        _keyboardShortcutService.StartPreviewCommand = viewModel.StartPreviewCommand;
    }

    /// <summary>
    /// Klavye tuşu basıldığında çağrılır
    /// Platform-specific keyboard handling için override edilebilir
    /// </summary>
    public bool HandleKeyPress(string key, bool isCtrlPressed, bool isShiftPressed, bool isAltPressed)
    {
        var modifiers = KeyModifiers.None;
        if (isCtrlPressed) modifiers |= KeyModifiers.Control;
        if (isShiftPressed) modifiers |= KeyModifiers.Shift;
        if (isAltPressed) modifiers |= KeyModifiers.Alt;

        return _keyboardShortcutService.HandleKeyPress(key, modifiers);
    }

    #region TreeView Event Handlers

    /// <summary>
    /// TreeView düğümü çift tıklama işleyicisi
    /// Requirement: 3.4
    /// </summary>
    private void OnTreeViewNodeDoubleClicked(object? sender, object node)
    {
        var vm = BindingContext as MainViewModel;
        if (vm == null) return;

        // Çift tıklama ile düzenleme moduna geç
        if (node is ContentItem content)
        {
            vm.TreeView.SelectItem(content);
            // Editor paneline odaklan
            vm.StatusMessage = $"Düzenleniyor: {content.Name}";
        }
        else if (node is ProgramNode program)
        {
            // Programı genişlet/daralt
            program.IsExpanded = !program.IsExpanded;
        }
        else if (node is ScreenNode screen)
        {
            // Ekranı genişlet/daralt
            screen.IsExpanded = !screen.IsExpanded;
        }
    }

    /// <summary>
    /// TreeView ekle isteği işleyicisi
    /// </summary>
    private void OnTreeViewAddRequested(object? sender, EventArgs e)
    {
        if (e is not AddRequestedEventArgs args) return;
        
        var vm = BindingContext as MainViewModel;
        if (vm == null) return;

        var action = args.ActionType;
        
        if (action == (string)Application.Current!.Resources["MenuAddScreen"])
        {
            vm.AddScreenCommand.Execute(null);
        }
        else if (action == (string)Application.Current.Resources["MenuAddProgram"])
        {
            vm.AddProgramCommand.Execute(null);
        }
        else if (action == (string)Application.Current.Resources["MenuAddText"])
        {
            vm.AddTextContentCommand.Execute(null);
        }
        else if (action == (string)Application.Current.Resources["MenuAddClock"])
        {
            vm.AddClockContentCommand.Execute(null);
        }
        else if (action == (string)Application.Current.Resources["MenuAddDate"])
        {
            vm.AddDateContentCommand.Execute(null);
        }
        else if (action == (string)Application.Current.Resources["MenuAddCountdown"])
        {
            vm.AddCountdownContentCommand.Execute(null);
        }
    }

    #endregion

    #region Preview Event Handlers

    /// <summary>
    /// Önizleme tam ekran modu değişikliği işleyicisi
    /// Requirement: 4.8
    /// </summary>
    private void OnPreviewFullscreenChanged(object? sender, bool isFullscreen)
    {
        // TODO: Implement fullscreen mode
        // This would typically involve:
        // 1. Hiding other panels (TreeView, Properties, Editor)
        // 2. Expanding the preview panel to fill the entire window
        // 3. Optionally entering true fullscreen mode via platform-specific APIs
        
        var vm = BindingContext as MainViewModel;
        if (vm != null)
        {
            vm.StatusMessage = isFullscreen 
                ? (string)Application.Current!.Resources["PreviewFullscreen"]
                : (string)Application.Current!.Resources["StatusReady"];
        }
    }

    #endregion

    #region Menu Event Handlers

    private async void OnFileMenuClicked(object? sender, EventArgs e)
    {
        var action = await DisplayActionSheet(
            (string)Application.Current!.Resources["MenuFile"],
            (string)Application.Current.Resources["ButtonCancel"],
            null,
            (string)Application.Current.Resources["MenuFileNew"],
            (string)Application.Current.Resources["MenuFileOpen"],
            (string)Application.Current.Resources["MenuFileSave"],
            (string)Application.Current.Resources["MenuFileSaveAs"],
            (string)Application.Current.Resources["MenuFileExport"],
            (string)Application.Current.Resources["MenuFileExit"]);

        if (action == null) return;

        var vm = BindingContext as MainViewModel;
        if (vm == null) return;

        if (action == (string)Application.Current.Resources["MenuFileNew"])
        {
            await vm.NewProjectCommand.ExecuteAsync(null);
        }
        else if (action == (string)Application.Current.Resources["MenuFileOpen"])
        {
            await vm.OpenProjectCommand.ExecuteAsync(null);
        }
        else if (action == (string)Application.Current.Resources["MenuFileSave"])
        {
            await vm.SaveProjectCommand.ExecuteAsync(null);
        }
        else if (action == (string)Application.Current.Resources["MenuFileSaveAs"])
        {
            await vm.SaveProjectAsCommand.ExecuteAsync(null);
        }
        else if (action == (string)Application.Current.Resources["MenuFileExport"])
        {
            await ShowExportMenuAsync(vm);
        }
        else if (action == (string)Application.Current.Resources["MenuFileExit"])
        {
            Application.Current.Quit();
        }
    }

    /// <summary>
    /// Dışa aktarma alt menüsünü gösterir
    /// Requirement: 13.5
    /// </summary>
    private async Task ShowExportMenuAsync(MainViewModel vm)
    {
        var exportPng = "PNG olarak dışa aktar";
        var exportGif = "GIF olarak dışa aktar";
        var exportWebP = "WebP olarak dışa aktar";
        
        var action = await DisplayActionSheet(
            (string)Application.Current!.Resources["MenuFileExport"],
            (string)Application.Current.Resources["ButtonCancel"],
            null,
            exportPng,
            exportGif,
            exportWebP);

        if (action == null) return;

        if (action == exportPng)
        {
            await vm.ExportPngCommand.ExecuteAsync(null);
        }
        else if (action == exportGif)
        {
            await vm.ExportGifCommand.ExecuteAsync(null);
        }
        else if (action == exportWebP)
        {
            await vm.ExportWebPCommand.ExecuteAsync(null);
        }
    }

    private async void OnSettingsMenuClicked(object? sender, EventArgs e)
    {
        var action = await DisplayActionSheet(
            (string)Application.Current!.Resources["MenuSettings"],
            (string)Application.Current.Resources["ButtonCancel"],
            null,
            (string)Application.Current.Resources["MenuSettingsDisplay"],
            (string)Application.Current.Resources["MenuSettingsConnection"],
            (string)Application.Current.Resources["MenuSettingsLanguage"]);

        // TODO: Implement settings actions
    }

    private async void OnAddMenuClicked(object? sender, EventArgs e)
    {
        var action = await DisplayActionSheet(
            (string)Application.Current!.Resources["MenuAdd"],
            (string)Application.Current.Resources["ButtonCancel"],
            null,
            (string)Application.Current.Resources["MenuAddScreen"],
            (string)Application.Current.Resources["MenuAddProgram"],
            (string)Application.Current.Resources["MenuAddText"],
            (string)Application.Current.Resources["MenuAddClock"],
            (string)Application.Current.Resources["MenuAddDate"],
            (string)Application.Current.Resources["MenuAddCountdown"]);

        if (action == null) return;

        var vm = BindingContext as MainViewModel;
        if (vm == null) return;

        if (action == (string)Application.Current.Resources["MenuAddScreen"])
        {
            vm.AddScreenCommand.Execute(null);
        }
        else if (action == (string)Application.Current.Resources["MenuAddProgram"])
        {
            vm.AddProgramCommand.Execute(null);
        }
        else if (action == (string)Application.Current.Resources["MenuAddText"])
        {
            vm.AddTextContentCommand.Execute(null);
        }
        else if (action == (string)Application.Current.Resources["MenuAddClock"])
        {
            vm.AddClockContentCommand.Execute(null);
        }
        else if (action == (string)Application.Current.Resources["MenuAddDate"])
        {
            vm.AddDateContentCommand.Execute(null);
        }
        else if (action == (string)Application.Current.Resources["MenuAddCountdown"])
        {
            vm.AddCountdownContentCommand.Execute(null);
        }
    }

    private async void OnHelpMenuClicked(object? sender, EventArgs e)
    {
        var action = await DisplayActionSheet(
            (string)Application.Current!.Resources["MenuHelp"],
            (string)Application.Current.Resources["ButtonCancel"],
            null,
            (string)Application.Current.Resources["MenuHelpManual"],
            (string)Application.Current.Resources["MenuHelpAbout"]);

        if (action == (string)Application.Current.Resources["MenuHelpAbout"])
        {
            await DisplayAlert(
                (string)Application.Current.Resources["MenuHelpAbout"],
                $"{Application.Current.Resources["AppTitle"]}\n{Application.Current.Resources["AppVersion"]}",
                (string)Application.Current.Resources["ButtonOK"]);
        }
    }

    #endregion
}
