using LEDTabelam.Maui.ViewModels;

namespace LEDTabelam.Maui.Controls;

/// <summary>
/// Özellikler paneli - Seçili içeriğin efekt, süre ve görünüm özelliklerini düzenler
/// Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9
/// </summary>
public partial class PropertiesPanel : ContentView
{
    public PropertiesPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// "Hemen Göster" label'ına tıklandığında checkbox'ı toggle eder
    /// Requirement: 5.6
    /// </summary>
    private void OnShowImmediatelyLabelTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is PropertiesViewModel vm)
        {
            vm.ShowImmediately = !vm.ShowImmediately;
        }
    }

    /// <summary>
    /// "Süreli" label'ına tıklandığında checkbox'ı toggle eder
    /// Requirement: 5.7
    /// </summary>
    private void OnTimedLabelTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is PropertiesViewModel vm)
        {
            vm.IsTimed = !vm.IsTimed;
        }
    }

    /// <summary>
    /// Arka plan renk alanına tıklandığında renk seçici açar
    /// Requirement: 5.3
    /// </summary>
    private async void OnBackgroundColorTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is PropertiesViewModel vm)
        {
            // Platform-specific color picker would be implemented here
            // For now, we'll use a simple action sheet with preset colors
            await ShowColorPickerAsync(vm);
        }
    }

    /// <summary>
    /// Preset renk butonuna tıklandığında ilgili rengi seçer
    /// Requirement: 5.3
    /// </summary>
    private void OnPresetColorClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && BindingContext is PropertiesViewModel vm)
        {
            var colorId = button.ClassId;
            
            vm.BackgroundColor = colorId switch
            {
                "Transparent" => Colors.Transparent,
                "LedBackground" => Color.FromArgb("#0A0A0A"),
                "Gray900" => Color.FromArgb("#1A1A1A"),
                "Gray700" => Color.FromArgb("#333333"),
                _ => Colors.Transparent
            };
        }
    }

    /// <summary>
    /// Renk seçici dialog'unu gösterir
    /// </summary>
    private async Task ShowColorPickerAsync(PropertiesViewModel vm)
    {
        var page = this.GetParentPage();
        if (page == null) return;

        var colors = new Dictionary<string, Color>
        {
            { "Şeffaf", Colors.Transparent },
            { "Siyah", Colors.Black },
            { "Koyu Gri", Color.FromArgb("#1A1A1A") },
            { "Gri", Color.FromArgb("#333333") },
            { "Açık Gri", Color.FromArgb("#555555") },
            { "Amber", Color.FromArgb("#FFBF00") },
            { "Yeşil", Color.FromArgb("#00FF00") },
            { "Kırmızı", Color.FromArgb("#FF0000") },
            { "Mavi", Color.FromArgb("#0080FF") },
            { "Beyaz", Colors.White }
        };

        var colorNames = colors.Keys.ToArray();
        var result = await page.DisplayActionSheet(
            "Arka Plan Rengi Seçin",
            "İptal",
            null,
            colorNames);

        if (result != null && result != "İptal" && colors.TryGetValue(result, out var selectedColor))
        {
            vm.BackgroundColor = selectedColor;
        }
    }

    /// <summary>
    /// ContentView'ın parent Page'ini bulur
    /// </summary>
    private Page? GetParentPage()
    {
        Element? element = this;
        while (element != null)
        {
            if (element is Page page)
                return page;
            element = element.Parent;
        }
        return null;
    }
}
