using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using LEDTabelam.ViewModels;
using LEDTabelam.Services;

namespace LEDTabelam.Views;

public partial class ProgramEditor : UserControl
{
    public ProgramEditor()
    {
        InitializeComponent();
    }

    private ProgramEditorViewModel? ViewModel => DataContext as ProgramEditorViewModel;

    // Sembol Kategori Değişikliği
    private void OnSymbolCategoryChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is string category)
        {
            if (ViewModel != null)
            {
                ViewModel.SelectedCategory = category;
            }
        }
    }

    // Sembol Seçimi
    private void OnSymbolSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string symbolName && ViewModel != null)
        {
            var symbol = ViewModel.AvailableSymbols.FirstOrDefault(s => s.Name == symbolName);
            if (symbol != null)
            {
                ViewModel.SelectedSymbol = symbol;
            }
        }
    }

    // Renk Butonları
    private void OnColorRed(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedColor(Color.FromRgb(255, 0, 0));
    private void OnColorGreen(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedColor(Color.FromRgb(0, 255, 0));
    private void OnColorAmber(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedColor(Color.FromRgb(255, 176, 0));
    private void OnColorWhite(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedColor(Color.FromRgb(255, 255, 255));
    private void OnColorCyan(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedColor(Color.FromRgb(0, 255, 255));
    private void OnColorMagenta(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedColor(Color.FromRgb(255, 0, 255));
    private void OnColorYellow(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedColor(Color.FromRgb(255, 255, 0));
    private void OnColorOrange(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedColor(Color.FromRgb(255, 128, 0));

    // Hizalama Butonları
    private void OnAlignLeft(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedHAlign(Models.HorizontalAlignment.Left);
    private void OnAlignCenterH(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedHAlign(Models.HorizontalAlignment.Center);
    private void OnAlignRight(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedHAlign(Models.HorizontalAlignment.Right);
    private void OnAlignTop(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedVAlign(Models.VerticalAlignment.Top);
    private void OnAlignCenterV(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedVAlign(Models.VerticalAlignment.Center);
    private void OnAlignBottom(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedVAlign(Models.VerticalAlignment.Bottom);

    // Hızlı Pozisyon Butonları
    private void OnPosTopLeft(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.X = 0;
            ViewModel.SelectedItem.Y = 0;
        }
    }

    private void OnPosTopRight(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.X = ViewModel.DisplayWidth - ViewModel.SelectedItem.Width;
            ViewModel.SelectedItem.Y = 0;
        }
    }

    private void OnPosBottomLeft(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.X = 0;
            ViewModel.SelectedItem.Y = ViewModel.DisplayHeight - ViewModel.SelectedItem.Height;
        }
    }

    private void OnPosBottomRight(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.X = ViewModel.DisplayWidth - ViewModel.SelectedItem.Width;
            ViewModel.SelectedItem.Y = ViewModel.DisplayHeight - ViewModel.SelectedItem.Height;
        }
    }

    private void OnFullWidth(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.X = 0;
            ViewModel.SelectedItem.Width = ViewModel.DisplayWidth;
        }
    }
}
