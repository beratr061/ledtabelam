using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using LEDTabelam.ViewModels;

namespace LEDTabelam.Views;

public partial class SimpleTabelaEditor : UserControl
{
    private int _selectedColorTarget = 0; // 0: Hat Kodu, 1: Güzergah
    private Button? _lastColorButton;

    public SimpleTabelaEditor()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is SimpleTabelaViewModel vm)
        {
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(vm.HAlign))
                    UpdateHAlignButtons();
                else if (args.PropertyName == nameof(vm.VAlign))
                    UpdateVAlignButtons();
            };
            UpdateHAlignButtons();
            UpdateVAlignButtons();
        }
    }

    private SimpleTabelaViewModel? ViewModel => DataContext as SimpleTabelaViewModel;

    // Renk Seçim Butonları
    private void OnHatKoduColorClick(object? sender, RoutedEventArgs e)
    {
        _selectedColorTarget = 0;
        HighlightColorButton(sender as Button);
    }

    private void OnGuzergahColorClick(object? sender, RoutedEventArgs e)
    {
        _selectedColorTarget = 1;
        HighlightColorButton(sender as Button);
    }

    private void HighlightColorButton(Button? button)
    {
        if (_lastColorButton != null)
        {
            _lastColorButton.BorderThickness = new Avalonia.Thickness(0);
        }
        if (button != null)
        {
            button.BorderThickness = new Avalonia.Thickness(2);
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            _lastColorButton = button;
        }
    }

    // Hızlı Renk Seçimleri
    private void ApplyQuickColor(Color color)
    {
        if (ViewModel == null) return;
        
        if (_selectedColorTarget == 0)
            ViewModel.HatKoduRenk = color;
        else
            ViewModel.GuzergahRenk = color;
    }

    private void OnQuickColorRed(object? sender, RoutedEventArgs e) => ApplyQuickColor(Color.FromRgb(255, 0, 0));
    private void OnQuickColorGreen(object? sender, RoutedEventArgs e) => ApplyQuickColor(Color.FromRgb(0, 255, 0));
    private void OnQuickColorAmber(object? sender, RoutedEventArgs e) => ApplyQuickColor(Color.FromRgb(255, 176, 0));
    private void OnQuickColorWhite(object? sender, RoutedEventArgs e) => ApplyQuickColor(Color.FromRgb(255, 255, 255));
    private void OnQuickColorCyan(object? sender, RoutedEventArgs e) => ApplyQuickColor(Color.FromRgb(0, 255, 255));
    private void OnQuickColorMagenta(object? sender, RoutedEventArgs e) => ApplyQuickColor(Color.FromRgb(255, 0, 255));

    // Yatay Hizalama
    private void OnLeftAlignClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null) ViewModel.HAlign = Models.HorizontalAlignment.Left;
    }

    private void OnCenterHAlignClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null) ViewModel.HAlign = Models.HorizontalAlignment.Center;
    }

    private void OnRightAlignClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null) ViewModel.HAlign = Models.HorizontalAlignment.Right;
    }

    // Dikey Hizalama
    private void OnTopAlignClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null) ViewModel.VAlign = Models.VerticalAlignment.Top;
    }

    private void OnCenterVAlignClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null) ViewModel.VAlign = Models.VerticalAlignment.Center;
    }

    private void OnBottomAlignClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null) ViewModel.VAlign = Models.VerticalAlignment.Bottom;
    }

    // Hizalama butonlarını güncelle
    private void UpdateHAlignButtons()
    {
        if (ViewModel == null) return;
        
        var selectedBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
        var normalBrush = new SolidColorBrush(Color.FromRgb(61, 61, 61));

        BtnLeft.Background = ViewModel.HAlign == Models.HorizontalAlignment.Left ? selectedBrush : normalBrush;
        BtnCenterH.Background = ViewModel.HAlign == Models.HorizontalAlignment.Center ? selectedBrush : normalBrush;
        BtnRight.Background = ViewModel.HAlign == Models.HorizontalAlignment.Right ? selectedBrush : normalBrush;
    }

    private void UpdateVAlignButtons()
    {
        if (ViewModel == null) return;
        
        var selectedBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
        var normalBrush = new SolidColorBrush(Color.FromRgb(61, 61, 61));

        BtnTop.Background = ViewModel.VAlign == Models.VerticalAlignment.Top ? selectedBrush : normalBrush;
        BtnCenterV.Background = ViewModel.VAlign == Models.VerticalAlignment.Center ? selectedBrush : normalBrush;
        BtnBottom.Background = ViewModel.VAlign == Models.VerticalAlignment.Bottom ? selectedBrush : normalBrush;
    }
}
