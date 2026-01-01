using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using LEDTabelam.Models;
using LEDTabelam.ViewModels;

namespace LEDTabelam.Views;

/// <summary>
/// Birleşik düzenleyici - Program düzenleyici ve görsel düzenleyici tek arayüzde
/// </summary>
public partial class UnifiedEditor : UserControl
{
    private bool _isDragging;
    private bool _isResizing;
    private ResizeMode _resizeMode;
    private Point _dragStartPoint;
    private Point _itemStartPosition;
    private Size _itemStartSize;
    private TabelaItem? _draggedItem;
    private Canvas? _zoneCanvas;
    
    private const int SnapDistance = 3; // Mıknatıs mesafesi (piksel)

    private enum ResizeMode
    {
        None,
        TopLeft,
        Top,
        TopRight,
        Left,
        Right,
        BottomLeft,
        Bottom,
        BottomRight
    }

    public UnifiedEditor()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnLoaded(RoutedEventArgs e)
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
            ViewModel.ZonesNeedRedraw += RedrawZones;
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

    private UnifiedEditorViewModel? ViewModel => DataContext as UnifiedEditorViewModel;

    /// <summary>
    /// Zone'ları canvas üzerine çizer
    /// </summary>
    private void RedrawZones()
    {
        if (_zoneCanvas == null || ViewModel == null) return;

        _zoneCanvas.Children.Clear();
        var scale = ViewModel.ZoomLevel / 100.0;
        var displayWidth = ViewModel.DisplayWidth;
        var displayHeight = ViewModel.DisplayHeight;

        foreach (var item in ViewModel.Items)
        {
            // Öğe boyutlarını tabela sınırlarına göre kısıtla
            ClampItemBounds(item, displayWidth, displayHeight);

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
                FontSize = 9,
                Margin = new Thickness(3, 1, 0, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };
            grid.Children.Add(nameText);

            // Tip ikonu
            var iconText = new TextBlock
            {
                Text = GetItemTypeIcon(item.ItemType),
                Foreground = Brushes.White,
                FontSize = 12,
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

            // Resize handle'ları (sadece seçili ise)
            if (item.IsSelected)
            {
                AddResizeHandles(item, scale);
            }
        }
    }

    /// <summary>
    /// Öğe boyutlarını tabela sınırlarına göre kısıtlar
    /// </summary>
    private void ClampItemBounds(TabelaItem item, int displayWidth, int displayHeight)
    {
        // X ve Y sınırları
        item.X = Math.Max(0, Math.Min(item.X, displayWidth - 1));
        item.Y = Math.Max(0, Math.Min(item.Y, displayHeight - 1));
        
        // Genişlik ve yükseklik sınırları (tabela dışına taşmasın)
        item.Width = Math.Max(1, Math.Min(item.Width, displayWidth - item.X));
        item.Height = Math.Max(1, Math.Min(item.Height, displayHeight - item.Y));
    }

    /// <summary>
    /// Seçili öğe için tüm kenarlardan resize handle'ları ekler
    /// </summary>
    private void AddResizeHandles(TabelaItem item, double scale)
    {
        if (_zoneCanvas == null) return;

        var handleSize = 8;
        var halfHandle = handleSize / 2;
        
        var left = item.X * scale;
        var top = item.Y * scale;
        var right = (item.X + item.Width) * scale;
        var bottom = (item.Y + item.Height) * scale;
        var centerX = (left + right) / 2;
        var centerY = (top + bottom) / 2;

        // 8 yönde resize handle
        AddResizeHandle(item, left - halfHandle, top - halfHandle, ResizeMode.TopLeft, StandardCursorType.TopLeftCorner);
        AddResizeHandle(item, centerX - halfHandle, top - halfHandle, ResizeMode.Top, StandardCursorType.TopSide);
        AddResizeHandle(item, right - halfHandle, top - halfHandle, ResizeMode.TopRight, StandardCursorType.TopRightCorner);
        AddResizeHandle(item, left - halfHandle, centerY - halfHandle, ResizeMode.Left, StandardCursorType.LeftSide);
        AddResizeHandle(item, right - halfHandle, centerY - halfHandle, ResizeMode.Right, StandardCursorType.RightSide);
        AddResizeHandle(item, left - halfHandle, bottom - halfHandle, ResizeMode.BottomLeft, StandardCursorType.BottomLeftCorner);
        AddResizeHandle(item, centerX - halfHandle, bottom - halfHandle, ResizeMode.Bottom, StandardCursorType.BottomSide);
        AddResizeHandle(item, right - halfHandle, bottom - halfHandle, ResizeMode.BottomRight, StandardCursorType.BottomRightCorner);
    }

    private void AddResizeHandle(TabelaItem item, double x, double y, ResizeMode mode, StandardCursorType cursorType)
    {
        if (_zoneCanvas == null) return;

        var handle = new Border
        {
            Width = 8,
            Height = 8,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            Cursor = new Cursor(cursorType),
            Tag = (item, mode)
        };
        handle.PointerPressed += OnResizeHandlePressed;

        Canvas.SetLeft(handle, x);
        Canvas.SetTop(handle, y);
        _zoneCanvas.Children.Add(handle);
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
            
            // Mıknatıs aktifse diğer öğelere snap
            if (ViewModel.IsSnapEnabled)
            {
                var (snappedX, snappedY) = SnapToOtherItems(_draggedItem, newX, newY, _draggedItem.Width, _draggedItem.Height);
                newX = snappedX;
                newY = snappedY;
            }
            
            // Çakışma varsa hareket etme
            if (!WouldOverlap(_draggedItem, newX, newY, _draggedItem.Width, _draggedItem.Height))
            {
                _draggedItem.X = newX;
                _draggedItem.Y = newY;
            }
            
            RedrawZones();
        }
        else if (_isResizing && _draggedItem != null)
        {
            var delta = point - _dragStartPoint;
            var scale = ViewModel.ZoomLevel / 100.0;
            var deltaX = (int)(delta.X / scale);
            var deltaY = (int)(delta.Y / scale);
            
            var displayWidth = ViewModel.DisplayWidth;
            var displayHeight = ViewModel.DisplayHeight;
            
            int newX = (int)_itemStartPosition.X;
            int newY = (int)_itemStartPosition.Y;
            int newWidth = (int)_itemStartSize.Width;
            int newHeight = (int)_itemStartSize.Height;
            
            switch (_resizeMode)
            {
                case ResizeMode.TopLeft:
                    newX = (int)_itemStartPosition.X + deltaX;
                    newY = (int)_itemStartPosition.Y + deltaY;
                    newWidth = (int)_itemStartSize.Width - deltaX;
                    newHeight = (int)_itemStartSize.Height - deltaY;
                    break;
                case ResizeMode.Top:
                    newY = (int)_itemStartPosition.Y + deltaY;
                    newHeight = (int)_itemStartSize.Height - deltaY;
                    break;
                case ResizeMode.TopRight:
                    newY = (int)_itemStartPosition.Y + deltaY;
                    newWidth = (int)_itemStartSize.Width + deltaX;
                    newHeight = (int)_itemStartSize.Height - deltaY;
                    break;
                case ResizeMode.Left:
                    newX = (int)_itemStartPosition.X + deltaX;
                    newWidth = (int)_itemStartSize.Width - deltaX;
                    break;
                case ResizeMode.Right:
                    newWidth = (int)_itemStartSize.Width + deltaX;
                    break;
                case ResizeMode.BottomLeft:
                    newX = (int)_itemStartPosition.X + deltaX;
                    newWidth = (int)_itemStartSize.Width - deltaX;
                    newHeight = (int)_itemStartSize.Height + deltaY;
                    break;
                case ResizeMode.Bottom:
                    newHeight = (int)_itemStartSize.Height + deltaY;
                    break;
                case ResizeMode.BottomRight:
                    newWidth = (int)_itemStartSize.Width + deltaX;
                    newHeight = (int)_itemStartSize.Height + deltaY;
                    break;
            }
            
            // Minimum boyut kontrolü
            if (newWidth < 5) { newWidth = 5; newX = (int)_itemStartPosition.X + (int)_itemStartSize.Width - 5; }
            if (newHeight < 5) { newHeight = 5; newY = (int)_itemStartPosition.Y + (int)_itemStartSize.Height - 5; }
            
            // Tabela sınırları kontrolü
            newX = Math.Max(0, newX);
            newY = Math.Max(0, newY);
            newWidth = Math.Min(newWidth, displayWidth - newX);
            newHeight = Math.Min(newHeight, displayHeight - newY);
            
            // Mıknatıs aktifse uygula
            if (ViewModel.IsSnapEnabled)
            {
                var (snappedX, snappedY, snappedW, snappedH) = SnapResizeToOtherItems(_draggedItem, newX, newY, newWidth, newHeight, _resizeMode);
                newX = snappedX;
                newY = snappedY;
                newWidth = snappedW;
                newHeight = snappedH;
            }
            
            // Çakışma kontrolü
            if (!WouldOverlap(_draggedItem, newX, newY, newWidth, newHeight))
            {
                _draggedItem.X = newX;
                _draggedItem.Y = newY;
                _draggedItem.Width = newWidth;
                _draggedItem.Height = newHeight;
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
            ViewModel.SelectItem(item);
            RedrawZones();
            
            _isDragging = true;
            _draggedItem = item;
            _dragStartPoint = e.GetPosition(_zoneCanvas);
            _itemStartPosition = new Point(item.X, item.Y);
            
            e.Pointer.Capture(_zoneCanvas);
        }
    }

    private void OnResizeHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel == null || sender is not Border handle) return;
        if (handle.Tag is not (TabelaItem item, ResizeMode mode)) return;
        
        e.Handled = true;
        
        _isResizing = true;
        _resizeMode = mode;
        _draggedItem = item;
        _dragStartPoint = e.GetPosition(_zoneCanvas);
        _itemStartPosition = new Point(item.X, item.Y);
        _itemStartSize = new Size(item.Width, item.Height);
        
        e.Pointer.Capture(_zoneCanvas);
    }

    #endregion

    #region Sembol Events

    private void OnSymbolCategoryChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is string category)
        {
            ViewModel?.SetSelectedCategory(category);
        }
    }

    private void OnSymbolSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string symbolName)
        {
            ViewModel?.SelectSymbol(symbolName);
        }
    }

    #endregion

    #region Color Buttons

    private void OnColorRed(object? sender, RoutedEventArgs e) => SetSelectedColor(Color.FromRgb(255, 0, 0));
    private void OnColorGreen(object? sender, RoutedEventArgs e) => SetSelectedColor(Color.FromRgb(0, 255, 0));
    private void OnColorAmber(object? sender, RoutedEventArgs e) => SetSelectedColor(Color.FromRgb(255, 176, 0));
    private void OnColorWhite(object? sender, RoutedEventArgs e) => SetSelectedColor(Color.FromRgb(255, 255, 255));
    private void OnColorCyan(object? sender, RoutedEventArgs e) => SetSelectedColor(Color.FromRgb(0, 255, 255));
    private void OnColorYellow(object? sender, RoutedEventArgs e) => SetSelectedColor(Color.FromRgb(255, 255, 0));

    private void SetSelectedColor(Color color)
    {
        if (ViewModel?.SelectedItem != null)
        {
            ViewModel.SelectedItem.Color = color;
            ViewModel.OnItemsChanged();
            RedrawZones();
        }
    }

    #endregion

    #region Border Color Buttons

    private void OnBorderColorRed(object? sender, RoutedEventArgs e) => SetSelectedBorderColor(Color.FromRgb(255, 0, 0));
    private void OnBorderColorGreen(object? sender, RoutedEventArgs e) => SetSelectedBorderColor(Color.FromRgb(0, 255, 0));
    private void OnBorderColorAmber(object? sender, RoutedEventArgs e) => SetSelectedBorderColor(Color.FromRgb(255, 176, 0));
    private void OnBorderColorWhite(object? sender, RoutedEventArgs e) => SetSelectedBorderColor(Color.FromRgb(255, 255, 255));
    private void OnBorderColorCyan(object? sender, RoutedEventArgs e) => SetSelectedBorderColor(Color.FromRgb(0, 255, 255));
    private void OnBorderColorYellow(object? sender, RoutedEventArgs e) => SetSelectedBorderColor(Color.FromRgb(255, 255, 0));

    private void SetSelectedBorderColor(Color color)
    {
        if (ViewModel?.SelectedItem?.Border != null)
        {
            ViewModel.SelectedItem.Border.Color = color;
            ViewModel.OnItemsChanged();
            RedrawZones();
        }
    }

    #endregion

    #region Alignment Buttons

    private void OnHAlignChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && ViewModel?.SelectedItem != null)
        {
            var align = combo.SelectedIndex switch
            {
                0 => Models.HorizontalAlignment.Left,
                1 => Models.HorizontalAlignment.Center,
                2 => Models.HorizontalAlignment.Right,
                _ => Models.HorizontalAlignment.Left
            };
            ViewModel.SelectedItem.HAlign = align;
            ViewModel.OnItemsChanged();
            RedrawZones();
        }
    }

    private void OnVAlignChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && ViewModel?.SelectedItem != null)
        {
            var align = combo.SelectedIndex switch
            {
                0 => Models.VerticalAlignment.Top,
                1 => Models.VerticalAlignment.Center,
                2 => Models.VerticalAlignment.Bottom,
                _ => Models.VerticalAlignment.Top
            };
            ViewModel.SelectedItem.VAlign = align;
            ViewModel.OnItemsChanged();
            RedrawZones();
        }
    }

    private void OnAlignLeft(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedHAlign(Models.HorizontalAlignment.Left);
    private void OnAlignCenterH(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedHAlign(Models.HorizontalAlignment.Center);
    private void OnAlignRight(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedHAlign(Models.HorizontalAlignment.Right);
    private void OnAlignTop(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedVAlign(Models.VerticalAlignment.Top);
    private void OnAlignCenterV(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedVAlign(Models.VerticalAlignment.Center);
    private void OnAlignBottom(object? sender, RoutedEventArgs e) => ViewModel?.SetSelectedVAlign(Models.VerticalAlignment.Bottom);

    #endregion

    #region Snap & Overlap Prevention

    /// <summary>
    /// Diğer öğelerle çakışma kontrolü
    /// </summary>
    private bool WouldOverlap(TabelaItem current, int x, int y, int width, int height)
    {
        if (ViewModel == null) return false;
        
        foreach (var item in ViewModel.Items)
        {
            if (item == current) continue;
            
            // Dikdörtgen çakışma kontrolü
            bool overlapsX = x < item.X + item.Width && x + width > item.X;
            bool overlapsY = y < item.Y + item.Height && y + height > item.Y;
            
            if (overlapsX && overlapsY)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Sürükleme sırasında diğer öğelere mıknatıslanma
    /// </summary>
    private (int x, int y) SnapToOtherItems(TabelaItem current, int x, int y, int width, int height)
    {
        if (ViewModel == null) return (x, y);
        
        int snappedX = x;
        int snappedY = y;
        
        // Tabela kenarlarına snap
        if (Math.Abs(x) <= SnapDistance) snappedX = 0;
        if (Math.Abs(y) <= SnapDistance) snappedY = 0;
        if (Math.Abs(x + width - ViewModel.DisplayWidth) <= SnapDistance) snappedX = ViewModel.DisplayWidth - width;
        if (Math.Abs(y + height - ViewModel.DisplayHeight) <= SnapDistance) snappedY = ViewModel.DisplayHeight - height;
        
        foreach (var item in ViewModel.Items)
        {
            if (item == current) continue;
            
            // Sol kenar -> diğerinin sağ kenarına snap
            if (Math.Abs(x - (item.X + item.Width)) <= SnapDistance)
                snappedX = item.X + item.Width;
            
            // Sağ kenar -> diğerinin sol kenarına snap
            if (Math.Abs(x + width - item.X) <= SnapDistance)
                snappedX = item.X - width;
            
            // Üst kenar -> diğerinin alt kenarına snap
            if (Math.Abs(y - (item.Y + item.Height)) <= SnapDistance)
                snappedY = item.Y + item.Height;
            
            // Alt kenar -> diğerinin üst kenarına snap
            if (Math.Abs(y + height - item.Y) <= SnapDistance)
                snappedY = item.Y - height;
            
            // Sol kenarlar hizalama
            if (Math.Abs(x - item.X) <= SnapDistance)
                snappedX = item.X;
            
            // Sağ kenarlar hizalama
            if (Math.Abs(x + width - (item.X + item.Width)) <= SnapDistance)
                snappedX = item.X + item.Width - width;
            
            // Üst kenarlar hizalama
            if (Math.Abs(y - item.Y) <= SnapDistance)
                snappedY = item.Y;
            
            // Alt kenarlar hizalama
            if (Math.Abs(y + height - (item.Y + item.Height)) <= SnapDistance)
                snappedY = item.Y + item.Height - height;
        }
        
        return (snappedX, snappedY);
    }

    /// <summary>
    /// Boyutlandırma sırasında diğer öğelere mıknatıslanma
    /// </summary>
    private (int x, int y, int w, int h) SnapResizeToOtherItems(TabelaItem current, int x, int y, int width, int height, ResizeMode mode)
    {
        if (ViewModel == null) return (x, y, width, height);
        
        int snappedX = x;
        int snappedY = y;
        int snappedW = width;
        int snappedH = height;
        
        // Tabela kenarlarına snap
        if (mode == ResizeMode.Left || mode == ResizeMode.TopLeft || mode == ResizeMode.BottomLeft)
        {
            if (Math.Abs(x) <= SnapDistance) { snappedW += snappedX; snappedX = 0; }
        }
        if (mode == ResizeMode.Right || mode == ResizeMode.TopRight || mode == ResizeMode.BottomRight)
        {
            if (Math.Abs(x + width - ViewModel.DisplayWidth) <= SnapDistance) snappedW = ViewModel.DisplayWidth - x;
        }
        if (mode == ResizeMode.Top || mode == ResizeMode.TopLeft || mode == ResizeMode.TopRight)
        {
            if (Math.Abs(y) <= SnapDistance) { snappedH += snappedY; snappedY = 0; }
        }
        if (mode == ResizeMode.Bottom || mode == ResizeMode.BottomLeft || mode == ResizeMode.BottomRight)
        {
            if (Math.Abs(y + height - ViewModel.DisplayHeight) <= SnapDistance) snappedH = ViewModel.DisplayHeight - y;
        }
        
        foreach (var item in ViewModel.Items)
        {
            if (item == current) continue;
            
            // Sol kenar boyutlandırma
            if (mode == ResizeMode.Left || mode == ResizeMode.TopLeft || mode == ResizeMode.BottomLeft)
            {
                if (Math.Abs(x - (item.X + item.Width)) <= SnapDistance)
                {
                    int diff = x - (item.X + item.Width);
                    snappedX = item.X + item.Width;
                    snappedW += diff;
                }
            }
            
            // Sağ kenar boyutlandırma
            if (mode == ResizeMode.Right || mode == ResizeMode.TopRight || mode == ResizeMode.BottomRight)
            {
                if (Math.Abs(x + width - item.X) <= SnapDistance)
                    snappedW = item.X - x;
            }
            
            // Üst kenar boyutlandırma
            if (mode == ResizeMode.Top || mode == ResizeMode.TopLeft || mode == ResizeMode.TopRight)
            {
                if (Math.Abs(y - (item.Y + item.Height)) <= SnapDistance)
                {
                    int diff = y - (item.Y + item.Height);
                    snappedY = item.Y + item.Height;
                    snappedH += diff;
                }
            }
            
            // Alt kenar boyutlandırma
            if (mode == ResizeMode.Bottom || mode == ResizeMode.BottomLeft || mode == ResizeMode.BottomRight)
            {
                if (Math.Abs(y + height - item.Y) <= SnapDistance)
                    snappedH = item.Y - y;
            }
        }
        
        return (snappedX, snappedY, Math.Max(5, snappedW), Math.Max(5, snappedH));
    }

    #endregion

    #region Çok Renkli Metin Events

    /// <summary>
    /// Çok renkli mod toggle
    /// </summary>
    private void OnColoredModeToggle(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem == null) 
        {
            System.Diagnostics.Debug.WriteLine("SelectedItem null!");
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"Mevcut durum: {ViewModel.SelectedItem.UseColoredSegments}");
        System.Diagnostics.Debug.WriteLine($"Content: '{ViewModel.SelectedItem.Content}'");
        
        if (ViewModel.SelectedItem.UseColoredSegments)
        {
            // Zaten açıksa kapat
            ViewModel.SelectedItem.ConvertToSingleColor();
            System.Diagnostics.Debug.WriteLine("Kapatıldı");
        }
        else
        {
            // Kapalıysa aç - önce içerik kontrolü
            if (string.IsNullOrEmpty(ViewModel.SelectedItem.Content))
            {
                System.Diagnostics.Debug.WriteLine("İçerik boş, segment oluşturulamaz!");
                return;
            }
            ViewModel.SelectedItem.ConvertToColoredSegments();
            System.Diagnostics.Debug.WriteLine($"Açıldı, segment sayısı: {ViewModel.SelectedItem.ColoredSegments.Count}");
        }
        
        ViewModel.OnItemsChanged();
        RedrawZones();
    }

    /// <summary>
    /// Gökkuşağı renkleri uygula
    /// </summary>
    private void OnApplyRainbow(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem == null) return;
        
        // Önce çok renkli moda geç
        if (!ViewModel.SelectedItem.UseColoredSegments)
        {
            ViewModel.SelectedItem.ConvertToColoredSegments();
        }
        
        // Gökkuşağı renkleri
        var rainbowColors = new[]
        {
            Color.FromRgb(255, 0, 0),     // Kırmızı
            Color.FromRgb(255, 127, 0),   // Turuncu
            Color.FromRgb(255, 255, 0),   // Sarı
            Color.FromRgb(0, 255, 0),     // Yeşil
            Color.FromRgb(0, 0, 255),     // Mavi
            Color.FromRgb(75, 0, 130),    // Indigo
            Color.FromRgb(148, 0, 211)    // Mor
        };
        
        for (int i = 0; i < ViewModel.SelectedItem.ColoredSegments.Count; i++)
        {
            ViewModel.SelectedItem.ColoredSegments[i].Color = rainbowColors[i % rainbowColors.Length];
        }
        
        ViewModel.OnItemsChanged();
        RedrawZones();
    }

    /// <summary>
    /// Kırmızı-Yeşil gradient uygula
    /// </summary>
    private void OnApplyGradientRG(object? sender, RoutedEventArgs e)
    {
        ApplyGradient(Color.FromRgb(255, 0, 0), Color.FromRgb(0, 255, 0));
    }

    /// <summary>
    /// Mavi-Sarı gradient uygula
    /// </summary>
    private void OnApplyGradientBY(object? sender, RoutedEventArgs e)
    {
        ApplyGradient(Color.FromRgb(0, 100, 255), Color.FromRgb(255, 255, 0));
    }

    private void ApplyGradient(Color startColor, Color endColor)
    {
        if (ViewModel?.SelectedItem == null) return;
        
        // Önce çok renkli moda geç
        if (!ViewModel.SelectedItem.UseColoredSegments)
        {
            ViewModel.SelectedItem.ConvertToColoredSegments();
        }
        
        int count = ViewModel.SelectedItem.ColoredSegments.Count;
        if (count == 0) return;
        
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0;
            var color = Color.FromRgb(
                (byte)(startColor.R + (endColor.R - startColor.R) * t),
                (byte)(startColor.G + (endColor.G - startColor.G) * t),
                (byte)(startColor.B + (endColor.B - startColor.B) * t)
            );
            ViewModel.SelectedItem.ColoredSegments[i].Color = color;
        }
        
        ViewModel.OnItemsChanged();
        RedrawZones();
    }

    /// <summary>
    /// Segment'e tıklandığında
    /// </summary>
    private void OnSegmentClick(object? sender, PointerPressedEventArgs e)
    {
        // Segment seçimi için (ileride kullanılabilir)
    }

    /// <summary>
    /// Segment rengini kırmızı yap
    /// </summary>
    private void OnSegmentColorRed(object? sender, RoutedEventArgs e) => SetSegmentColor(sender, Color.FromRgb(255, 0, 0));
    
    /// <summary>
    /// Segment rengini yeşil yap
    /// </summary>
    private void OnSegmentColorGreen(object? sender, RoutedEventArgs e) => SetSegmentColor(sender, Color.FromRgb(0, 255, 0));
    
    /// <summary>
    /// Segment rengini amber yap
    /// </summary>
    private void OnSegmentColorAmber(object? sender, RoutedEventArgs e) => SetSegmentColor(sender, Color.FromRgb(255, 176, 0));
    
    /// <summary>
    /// Segment rengini cyan yap
    /// </summary>
    private void OnSegmentColorCyan(object? sender, RoutedEventArgs e) => SetSegmentColor(sender, Color.FromRgb(0, 255, 255));
    
    /// <summary>
    /// Segment rengini sarı yap
    /// </summary>
    private void OnSegmentColorYellow(object? sender, RoutedEventArgs e) => SetSegmentColor(sender, Color.FromRgb(255, 255, 0));

    private void SetSegmentColor(object? sender, Color color)
    {
        if (sender is Button btn && btn.Tag is ColoredTextSegment segment)
        {
            segment.Color = color;
            ViewModel?.OnItemsChanged();
            RedrawZones();
        }
    }

    #endregion

    #region Program Events

    /// <summary>
    /// Program geçiş tipi değiştiğinde
    /// Requirements: 3.1
    /// </summary>
    private void OnProgramTransitionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && ViewModel?.SelectedProgram != null)
        {
            var transition = combo.SelectedIndex switch
            {
                0 => Models.ProgramTransitionType.Direct,
                1 => Models.ProgramTransitionType.Fade,
                2 => Models.ProgramTransitionType.SlideLeft,
                3 => Models.ProgramTransitionType.SlideRight,
                4 => Models.ProgramTransitionType.SlideUp,
                5 => Models.ProgramTransitionType.SlideDown,
                _ => Models.ProgramTransitionType.Direct
            };
            ViewModel.SelectedProgram.Transition = transition;
        }
    }

    #endregion

    #region Ara Durak Events
    // Requirements: 4.2, 4.3, 4.4, 5.6, 6.1

    /// <summary>
    /// Yeni durak ekleme butonuna tıklandığında
    /// Requirements: 4.3, 4.4
    /// </summary>
    private void OnAddStopClick(object? sender, RoutedEventArgs e)
    {
        var textBox = this.FindControl<TextBox>("NewStopNameTextBox");
        if (textBox == null || ViewModel == null) return;
        
        var stopName = textBox.Text?.Trim();
        if (string.IsNullOrEmpty(stopName))
        {
            // Boş isimle varsayılan isim kullan
            ViewModel.AddIntermediateStop();
        }
        else
        {
            ViewModel.AddIntermediateStop(stopName);
        }
        
        // TextBox'ı temizle
        textBox.Text = string.Empty;
        textBox.Focus();
    }

    /// <summary>
    /// Yeni durak TextBox'ında Enter tuşuna basıldığında
    /// Requirements: 4.3, 4.4
    /// </summary>
    private void OnNewStopKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnAddStopClick(sender, e);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Ara durak animasyon tipi değiştiğinde
    /// Requirements: 6.1
    /// </summary>
    private void OnStopAnimationChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && ViewModel?.SelectedItem?.IntermediateStops != null)
        {
            var animation = combo.SelectedIndex switch
            {
                0 => Models.StopAnimationType.Direct,
                1 => Models.StopAnimationType.Fade,
                2 => Models.StopAnimationType.SlideUp,
                3 => Models.StopAnimationType.SlideDown,
                _ => Models.StopAnimationType.Direct
            };
            ViewModel.SelectedItem.IntermediateStops.Animation = animation;
        }
    }

    #endregion

    #region Playback Events
    // Requirements: 7.1, 7.2, 7.3

    /// <summary>
    /// Play/Pause toggle butonuna tıklandığında
    /// Requirements: 7.1, 7.2, 7.3
    /// </summary>
    private void OnPlayPauseToggle(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        
        // Toggle: mevcut durumun tersini yap
        if (ViewModel.IsAnimationPlaying)
        {
            // Oynatılıyorsa duraklat
            ViewModel.PauseCommand.Execute().Subscribe();
        }
        else
        {
            // Duraklatılmışsa oynat
            ViewModel.PlayCommand.Execute().Subscribe();
        }
    }

    #endregion
}
