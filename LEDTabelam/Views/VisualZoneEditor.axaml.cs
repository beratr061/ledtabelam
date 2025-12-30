using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using LEDTabelam.Models;
using LEDTabelam.ViewModels;

namespace LEDTabelam.Views;

/// <summary>
/// Görsel bölge düzenleyici - Canvas üzerinde sürükle-bırak ile zone düzenleme
/// </summary>
public partial class VisualZoneEditor : UserControl
{
    private bool _isDragging;
    private bool _isResizing;
    private ResizeMode _resizeMode;
    private Point _dragStartPoint;
    private Point _itemStartPosition;
    private Size _itemStartSize;
    private TabelaItem? _draggedItem;
    private Canvas? _zoneCanvas;

    private enum ResizeMode
    {
        None,
        BottomRight,
        Right,
        Bottom
    }

    public VisualZoneEditor()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _zoneCanvas = this.FindControl<Canvas>("ZoneCanvas");
        RedrawZones();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.Items.CollectionChanged += OnItemsCollectionChanged;
            foreach (var item in ViewModel.Items)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
            RedrawZones();
        }
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TabelaItem item in e.NewItems)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }
        RedrawZones();
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        RedrawZones();
    }

    private VisualZoneEditorViewModel? ViewModel => DataContext as VisualZoneEditorViewModel;

    /// <summary>
    /// Zone'ları canvas üzerine çizer
    /// </summary>
    private void RedrawZones()
    {
        if (_zoneCanvas == null || ViewModel == null) return;

        _zoneCanvas.Children.Clear();
        var scale = ViewModel.ZoomLevel / 100.0;

        foreach (var item in ViewModel.Items)
        {
            // Zone kutusu
            var zoneBorder = new Border
            {
                Width = item.Width * scale,
                Height = item.Height * scale,
                Background = new SolidColorBrush(Color.FromArgb(80, item.Color.R, item.Color.G, item.Color.B)),
                BorderBrush = item.IsSelected 
                    ? new SolidColorBrush(Color.FromRgb(0, 120, 212)) 
                    : new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                BorderThickness = new Thickness(item.IsSelected ? 3 : 2),
                CornerRadius = new CornerRadius(2),
                Cursor = new Cursor(StandardCursorType.SizeAll),
                Tag = item
            };

            // İçerik
            var grid = new Grid();
            
            // İsim etiketi
            var nameText = new TextBlock
            {
                Text = item.Name,
                Foreground = Brushes.White,
                FontSize = 10,
                Margin = new Thickness(4, 2, 0, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };
            grid.Children.Add(nameText);

            // Tip ikonu
            var iconText = new TextBlock
            {
                Text = GetItemTypeIcon(item.ItemType),
                Foreground = Brushes.White,
                FontSize = 14,
                Opacity = 0.7,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            grid.Children.Add(iconText);

            zoneBorder.Child = grid;

            // Event handler'lar
            zoneBorder.PointerPressed += (s, e) => OnZoneBorderPressed(item, e);

            Canvas.SetLeft(zoneBorder, item.X * scale);
            Canvas.SetTop(zoneBorder, item.Y * scale);
            _zoneCanvas.Children.Add(zoneBorder);

            // Resize handle (sadece seçili ise)
            if (item.IsSelected)
            {
                var resizeHandle = new Border
                {
                    Width = 10,
                    Height = 10,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Cursor = new Cursor(StandardCursorType.BottomRightCorner),
                    Tag = item
                };
                resizeHandle.PointerPressed += (s, e) => OnResizeHandlePressed(item, e);

                Canvas.SetLeft(resizeHandle, (item.X + item.Width) * scale - 5);
                Canvas.SetTop(resizeHandle, (item.Y + item.Height) * scale - 5);
                _zoneCanvas.Children.Add(resizeHandle);
            }
        }
    }

    private string GetItemTypeIcon(TabelaItemType itemType)
    {
        return itemType switch
        {
            TabelaItemType.Text => "T",
            TabelaItemType.Symbol => "◆",
            TabelaItemType.Image => "🖼",
            TabelaItemType.Clock => "⏰",
            TabelaItemType.Date => "📅",
            _ => "?"
        };
    }

    #region Canvas Events

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel == null) return;

        var point = e.GetPosition(_zoneCanvas);
        
        // Canvas'a tıklandığında seçimi kaldır
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ViewModel.ClearSelection();
            ViewModel.UpdateMousePosition(point);
            RedrawZones();
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (ViewModel == null || _zoneCanvas == null) return;

        var point = e.GetPosition(_zoneCanvas);
        ViewModel.UpdateMousePosition(point);

        if (_isDragging && _draggedItem != null)
        {
            var delta = point - _dragStartPoint;
            var scale = ViewModel.ZoomLevel / 100.0;
            
            // Yeni pozisyonu hesapla (scale'e göre)
            var newX = (int)(_itemStartPosition.X + delta.X / scale);
            var newY = (int)(_itemStartPosition.Y + delta.Y / scale);
            
            // Sınırları kontrol et
            newX = Math.Max(0, Math.Min(newX, ViewModel.DisplayWidth - _draggedItem.Width));
            newY = Math.Max(0, Math.Min(newY, ViewModel.DisplayHeight - _draggedItem.Height));
            
            _draggedItem.X = newX;
            _draggedItem.Y = newY;
            RedrawZones();
        }
        else if (_isResizing && _draggedItem != null)
        {
            var delta = point - _dragStartPoint;
            var scale = ViewModel.ZoomLevel / 100.0;
            
            switch (_resizeMode)
            {
                case ResizeMode.BottomRight:
                    var newWidth = (int)(_itemStartSize.Width + delta.X / scale);
                    var newHeight = (int)(_itemStartSize.Height + delta.Y / scale);
                    _draggedItem.Width = Math.Max(10, Math.Min(newWidth, ViewModel.DisplayWidth - _draggedItem.X));
                    _draggedItem.Height = Math.Max(10, Math.Min(newHeight, ViewModel.DisplayHeight - _draggedItem.Y));
                    break;
                    
                case ResizeMode.Right:
                    var newW = (int)(_itemStartSize.Width + delta.X / scale);
                    _draggedItem.Width = Math.Max(10, Math.Min(newW, ViewModel.DisplayWidth - _draggedItem.X));
                    break;
                    
                case ResizeMode.Bottom:
                    var newH = (int)(_itemStartSize.Height + delta.Y / scale);
                    _draggedItem.Height = Math.Max(10, Math.Min(newH, ViewModel.DisplayHeight - _draggedItem.Y));
                    break;
            }
            RedrawZones();
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        _isResizing = false;
        _draggedItem = null;
        _resizeMode = ResizeMode.None;
        e.Pointer.Capture(null);
    }

    #endregion

    #region Zone Events

    private void OnZoneBorderPressed(TabelaItem item, PointerPressedEventArgs e)
    {
        if (ViewModel == null) return;
        
        e.Handled = true;
        
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // Öğeyi seç
            ViewModel.SelectItem(item);
            RedrawZones();
            
            // Sürüklemeyi başlat
            _isDragging = true;
            _draggedItem = item;
            _dragStartPoint = e.GetPosition(_zoneCanvas);
            _itemStartPosition = new Point(item.X, item.Y);
            
            e.Pointer.Capture(_zoneCanvas);
        }
    }

    private void OnResizeHandlePressed(TabelaItem item, PointerPressedEventArgs e)
    {
        if (ViewModel == null) return;
        
        e.Handled = true;
        
        _isResizing = true;
        _resizeMode = ResizeMode.BottomRight;
        _draggedItem = item;
        _dragStartPoint = e.GetPosition(_zoneCanvas);
        _itemStartSize = new Size(item.Width, item.Height);
        
        e.Pointer.Capture(_zoneCanvas);
    }

    #endregion

    #region Color Buttons

    private void OnColorRed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetSelectedColor(Color.FromRgb(255, 0, 0));
    }

    private void OnColorGreen(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetSelectedColor(Color.FromRgb(0, 255, 0));
    }

    private void OnColorAmber(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetSelectedColor(Color.FromRgb(255, 176, 0));
    }

    private void OnColorWhite(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetSelectedColor(Color.FromRgb(255, 255, 255));
    }

    private void OnColorCyan(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetSelectedColor(Color.FromRgb(0, 255, 255));
    }

    private void OnColorYellow(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetSelectedColor(Color.FromRgb(255, 255, 0));
    }

    private void SetSelectedColor(Color color)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.Color = color;
            RedrawZones();
        }
    }

    #endregion

    #region Alignment Buttons

    private void OnAlignLeft(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.HAlign = Models.HorizontalAlignment.Left;
            RedrawZones();
        }
    }

    private void OnAlignCenterH(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.HAlign = Models.HorizontalAlignment.Center;
            RedrawZones();
        }
    }

    private void OnAlignRight(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.HAlign = Models.HorizontalAlignment.Right;
            RedrawZones();
        }
    }

    private void OnAlignTop(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.VAlign = Models.VerticalAlignment.Top;
            RedrawZones();
        }
    }

    private void OnAlignCenterV(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.VAlign = Models.VerticalAlignment.Center;
            RedrawZones();
        }
    }

    private void OnAlignBottom(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.VAlign = Models.VerticalAlignment.Bottom;
            RedrawZones();
        }
    }

    #endregion
}
