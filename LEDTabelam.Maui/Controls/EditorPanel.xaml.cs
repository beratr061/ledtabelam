using LEDTabelam.Maui.Models;
using LEDTabelam.Maui.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using HAlign = LEDTabelam.Maui.Models.HorizontalAlignment;
using VAlign = LEDTabelam.Maui.Models.VerticalAlignment;

namespace LEDTabelam.Maui.Controls;

/// <summary>
/// Düzenleyici paneli - Canvas tabanlı görsel düzenleme
/// Avalonia UnifiedEditor.axaml.cs'den port edildi
/// Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9
/// </summary>
public partial class EditorPanel : ContentView
{
    private double _zoomLevel = 3.0; // Varsayılan zoom %300
    private const double MinZoom = 1.0;
    private const double MaxZoom = 6.0;
    private const double ZoomStep = 0.5;
    
    private int _displayWidth = 128;
    private int _displayHeight = 32;
    
    // Drag state
    private bool _isDragging;
    private bool _isResizing;
    private ResizeMode _resizeMode = ResizeMode.None;
    private Point _dragStartPoint;
    private Point _itemStartPosition;
    private Size _itemStartSize;
    private ContentItem? _draggedItem;
    
    private const int SnapDistance = 3; // Mıknatıs mesafesi (piksel)
    
    private readonly Dictionary<string, View> _itemViews = new();

    private enum ResizeMode
    {
        None,
        TopLeft, Top, TopRight,
        Left, Right,
        BottomLeft, Bottom, BottomRight
    }

    public EditorPanel()
    {
        InitializeComponent();
        UpdateCanvasSize();
        BindingContextChanged += OnBindingContextChanged;
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (BindingContext is EditorViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            
            // Items değişikliğini dinle
            vm.ItemsChanged += (s, args) => RedrawZones();
            
            RedrawZones();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorViewModel.EditingContent) ||
            e.PropertyName == nameof(EditorViewModel.Items))
        {
            RedrawZones();
        }
        else if (e.PropertyName == nameof(EditorViewModel.PositionX) ||
                 e.PropertyName == nameof(EditorViewModel.PositionY) ||
                 e.PropertyName == nameof(EditorViewModel.ContentWidth) ||
                 e.PropertyName == nameof(EditorViewModel.ContentHeight))
        {
            // Sadece seçili öğenin pozisyonunu güncelle
            UpdateSelectedItemPosition();
        }
    }

    private void UpdateCanvasSize()
    {
        var scaledWidth = _displayWidth * _zoomLevel;
        var scaledHeight = _displayHeight * _zoomLevel;
        
        CanvasBorder.WidthRequest = scaledWidth;
        CanvasBorder.HeightRequest = scaledHeight;
        CanvasContainer.WidthRequest = scaledWidth;
        CanvasContainer.HeightRequest = scaledHeight;
        ItemsCanvas.WidthRequest = scaledWidth;
        ItemsCanvas.HeightRequest = scaledHeight;
        InputHandler.WidthRequest = scaledWidth;
        InputHandler.HeightRequest = scaledHeight;
        
        ZoomLabel.Text = $"{(int)(_zoomLevel * 100)}%";
        DisplaySizeLabel.Text = $"{_displayWidth} x {_displayHeight}";
    }

    /// <summary>
    /// Tüm öğeleri canvas üzerine çizer (Avalonia RedrawZones gibi)
    /// </summary>
    private void RedrawZones()
    {
        ItemsCanvas.Children.Clear();
        _itemViews.Clear();
        
        if (BindingContext is not EditorViewModel vm)
            return;
        
        // Tüm öğeleri çiz
        foreach (var item in vm.Items)
        {
            // Öğe boyutlarını tabela sınırlarına göre kısıtla
            ClampItemBounds(item);
            AddItemToCanvas(item, item == vm.EditingContent);
        }
    }

    /// <summary>
    /// Öğe boyutlarını tabela sınırlarına göre kısıtlar
    /// </summary>
    private void ClampItemBounds(ContentItem item)
    {
        item.X = Math.Max(0, Math.Min(item.X, _displayWidth - 1));
        item.Y = Math.Max(0, Math.Min(item.Y, _displayHeight - 1));
        item.Width = Math.Max(1, Math.Min(item.Width, _displayWidth - item.X));
        item.Height = Math.Max(1, Math.Min(item.Height, _displayHeight - item.Y));
    }

    private void AddItemToCanvas(ContentItem item, bool isSelected)
    {
        var scale = _zoomLevel;
        var x = item.X * scale;
        var y = item.Y * scale;
        var width = item.Width * scale;
        var height = item.Height * scale;
        
        var itemView = CreateItemView(item, isSelected);
        
        AbsoluteLayout.SetLayoutBounds(itemView, new Rect(x, y, width, height));
        AbsoluteLayout.SetLayoutFlags(itemView, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
        
        ItemsCanvas.Children.Add(itemView);
        _itemViews[item.Id] = itemView;
        
        // Seçili ise resize handle'ları ekle
        if (isSelected)
        {
            AddResizeHandles(item);
        }
    }

    private View CreateItemView(ContentItem item, bool isSelected)
    {
        var borderColor = isSelected ? Color.FromArgb("#0078D4") : Color.FromArgb("#808080");
        var borderThickness = isSelected ? 3 : 2;
        
        // Arka plan rengi (item rengine göre yarı saydam)
        Color bgColor;
        if (item is TextContent textContent)
        {
            var c = textContent.ForegroundColor;
            bgColor = Color.FromRgba(c.Red, c.Green, c.Blue, 0.3f);
        }
        else
        {
            bgColor = Color.FromRgba(0.5f, 0.5f, 0.5f, 0.3f);
        }
        
        var container = new Border
        {
            BackgroundColor = bgColor,
            Stroke = new SolidColorBrush(borderColor),
            StrokeThickness = borderThickness,
            StrokeShape = new RoundRectangle { CornerRadius = 2 },
            ClassId = item.Id
        };
        
        // İçerik grid'i
        var grid = new Grid();
        
        // İsim etiketi (sol üst)
        var nameLabel = new Label
        {
            Text = item.Name,
            TextColor = Colors.White,
            FontSize = 9,
            Margin = new Thickness(3, 1, 0, 0),
            VerticalOptions = LayoutOptions.Start,
            HorizontalOptions = LayoutOptions.Start
        };
        grid.Children.Add(nameLabel);
        
        // Tip ikonu (orta)
        var iconLabel = new Label
        {
            Text = GetItemTypeIcon(item.ContentType),
            TextColor = Colors.White,
            FontSize = 12,
            Opacity = 0.7,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        grid.Children.Add(iconLabel);
        
        container.Content = grid;
        return container;
    }

    private string GetItemTypeIcon(ContentType itemType)
    {
        return itemType switch
        {
            ContentType.Text => "T",
            ContentType.Image => "🖼",
            ContentType.Clock => "⏰",
            ContentType.Date => "📅",
            ContentType.Countdown => "⏱",
            _ => "?"
        };
    }

    /// <summary>
    /// Seçili öğe için 8 yönde resize handle'ları ekler
    /// </summary>
    private void AddResizeHandles(ContentItem item)
    {
        var scale = _zoomLevel;
        var handleSize = 8;
        var halfHandle = handleSize / 2;
        
        var left = item.X * scale;
        var top = item.Y * scale;
        var right = (item.X + item.Width) * scale;
        var bottom = (item.Y + item.Height) * scale;
        var centerX = (left + right) / 2;
        var centerY = (top + bottom) / 2;
        
        // 8 yönde resize handle
        AddResizeHandle(item, left - halfHandle, top - halfHandle, ResizeMode.TopLeft);
        AddResizeHandle(item, centerX - halfHandle, top - halfHandle, ResizeMode.Top);
        AddResizeHandle(item, right - halfHandle, top - halfHandle, ResizeMode.TopRight);
        AddResizeHandle(item, left - halfHandle, centerY - halfHandle, ResizeMode.Left);
        AddResizeHandle(item, right - halfHandle, centerY - halfHandle, ResizeMode.Right);
        AddResizeHandle(item, left - halfHandle, bottom - halfHandle, ResizeMode.BottomLeft);
        AddResizeHandle(item, centerX - halfHandle, bottom - halfHandle, ResizeMode.Bottom);
        AddResizeHandle(item, right - halfHandle, bottom - halfHandle, ResizeMode.BottomRight);
    }

    private void AddResizeHandle(ContentItem item, double x, double y, ResizeMode mode)
    {
        var handle = new Border
        {
            WidthRequest = 8,
            HeightRequest = 8,
            BackgroundColor = Colors.White,
            Stroke = new SolidColorBrush(Color.FromArgb("#0078D4")),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 2 },
            ClassId = $"resize_{mode}_{item.Id}"
        };
        
        AbsoluteLayout.SetLayoutBounds(handle, new Rect(x, y, 8, 8));
        AbsoluteLayout.SetLayoutFlags(handle, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
        
        ItemsCanvas.Children.Add(handle);
    }

    private void UpdateSelectedItemPosition()
    {
        if (BindingContext is not EditorViewModel vm || vm.EditingContent == null)
            return;
        
        // Tam yeniden çizim yerine sadece pozisyon güncelle
        if (_itemViews.TryGetValue(vm.EditingContent.Id, out var itemView))
        {
            var x = vm.PositionX * _zoomLevel;
            var y = vm.PositionY * _zoomLevel;
            var width = vm.ContentWidth * _zoomLevel;
            var height = vm.ContentHeight * _zoomLevel;
            
            AbsoluteLayout.SetLayoutBounds(itemView, new Rect(x, y, width, height));
        }
    }

    #region Zoom Controls

    private void OnZoomIn(object? sender, EventArgs e)
    {
        if (_zoomLevel < MaxZoom)
        {
            _zoomLevel = Math.Min(MaxZoom, _zoomLevel + ZoomStep);
            UpdateCanvasSize();
            RedrawZones();
        }
    }

    private void OnZoomOut(object? sender, EventArgs e)
    {
        if (_zoomLevel > MinZoom)
        {
            _zoomLevel = Math.Max(MinZoom, _zoomLevel - ZoomStep);
            UpdateCanvasSize();
            RedrawZones();
        }
    }

    private void OnZoomFit(object? sender, EventArgs e)
    {
        _zoomLevel = 3.0; // Varsayılan zoom %300
        UpdateCanvasSize();
        RedrawZones();
    }

    #endregion

    #region Canvas Interaction (Avalonia'dan port edildi)

    private void OnCanvasPointerPressed(object? sender, PointerEventArgs e)
    {
        if (BindingContext is not EditorViewModel vm) return;
        
        var position = e.GetPosition(ItemsCanvas);
        if (position == null) return;
        
        var point = position.Value;
        _dragStartPoint = point;
        
        UpdateCoordinateLabel(point);
        
        // Önce resize handle kontrolü
        var resizeResult = CheckResizeHandle(point);
        if (resizeResult.item != null)
        {
            _isResizing = true;
            _resizeMode = resizeResult.mode;
            _draggedItem = resizeResult.item;
            _itemStartPosition = new Point(resizeResult.item.X, resizeResult.item.Y);
            _itemStartSize = new Size(resizeResult.item.Width, resizeResult.item.Height);
            return;
        }
        
        // Sonra öğe kontrolü
        var hitItem = HitTestItem(point);
        if (hitItem != null)
        {
            vm.SelectItem(hitItem);
            _isDragging = true;
            _draggedItem = hitItem;
            _itemStartPosition = new Point(hitItem.X, hitItem.Y);
            RedrawZones();
        }
        else
        {
            // Boş alana tıklandı - seçimi temizle
            vm.ClearSelection();
            RedrawZones();
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (BindingContext is not EditorViewModel vm) return;
        
        var position = e.GetPosition(ItemsCanvas);
        if (position == null) return;
        
        var point = position.Value;
        UpdateCoordinateLabel(point);
        
        if (_isDragging && _draggedItem != null)
        {
            HandleDrag(point, vm);
        }
        else if (_isResizing && _draggedItem != null)
        {
            HandleResize(point, vm);
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerEventArgs e)
    {
        _isDragging = false;
        _isResizing = false;
        _draggedItem = null;
        _resizeMode = ResizeMode.None;
    }

    private void OnCanvasTapped(object? sender, TappedEventArgs e)
    {
        var position = e.GetPosition(ItemsCanvas);
        if (position != null)
        {
            UpdateCoordinateLabel(position.Value);
        }
    }

    private void HandleDrag(Point point, EditorViewModel vm)
    {
        if (_draggedItem == null) return;
        
        var delta = new Point(point.X - _dragStartPoint.X, point.Y - _dragStartPoint.Y);
        var scale = _zoomLevel;
        
        // Yeni pozisyonu hesapla (scale'e göre)
        var newX = (int)(_itemStartPosition.X + delta.X / scale);
        var newY = (int)(_itemStartPosition.Y + delta.Y / scale);
        
        // Sınırları kontrol et
        newX = Math.Max(0, Math.Min(newX, _displayWidth - _draggedItem.Width));
        newY = Math.Max(0, Math.Min(newY, _displayHeight - _draggedItem.Height));
        
        // Mıknatıs aktifse diğer öğelere snap
        if (SnapCheckBox.IsChecked)
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
            
            // ViewModel'i güncelle
            if (_draggedItem == vm.EditingContent)
            {
                vm.PositionX = newX;
                vm.PositionY = newY;
            }
        }
        
        RedrawZones();
        vm.RaisePreviewUpdate();
    }

    private void HandleResize(Point point, EditorViewModel vm)
    {
        if (_draggedItem == null) return;
        
        var delta = new Point(point.X - _dragStartPoint.X, point.Y - _dragStartPoint.Y);
        var scale = _zoomLevel;
        var deltaX = (int)(delta.X / scale);
        var deltaY = (int)(delta.Y / scale);
        
        int newX = (int)_itemStartPosition.X;
        int newY = (int)_itemStartPosition.Y;
        int newWidth = (int)_itemStartSize.Width;
        int newHeight = (int)_itemStartSize.Height;
        
        // Resize moduna göre hesapla
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
        newWidth = Math.Min(newWidth, _displayWidth - newX);
        newHeight = Math.Min(newHeight, _displayHeight - newY);
        
        // Mıknatıs aktifse uygula
        if (SnapCheckBox.IsChecked)
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
            
            // ViewModel'i güncelle
            if (_draggedItem == vm.EditingContent)
            {
                vm.PositionX = newX;
                vm.PositionY = newY;
                vm.ContentWidth = newWidth;
                vm.ContentHeight = newHeight;
            }
        }
        
        RedrawZones();
        vm.RaisePreviewUpdate();
    }

    #endregion

    #region Hit Testing

    private ContentItem? HitTestItem(Point point)
    {
        if (BindingContext is not EditorViewModel vm) return null;
        
        var scale = _zoomLevel;
        
        // Ters sırada kontrol et (üstteki öğeler önce)
        for (int i = vm.Items.Count - 1; i >= 0; i--)
        {
            var item = vm.Items[i];
            var rect = new Rect(item.X * scale, item.Y * scale, item.Width * scale, item.Height * scale);
            if (rect.Contains(point))
            {
                return item;
            }
        }
        
        return null;
    }

    private (ContentItem? item, ResizeMode mode) CheckResizeHandle(Point point)
    {
        if (BindingContext is not EditorViewModel vm || vm.EditingContent == null)
            return (null, ResizeMode.None);
        
        var item = vm.EditingContent;
        var scale = _zoomLevel;
        var handleSize = 12; // Hit area biraz daha büyük
        
        var left = item.X * scale;
        var top = item.Y * scale;
        var right = (item.X + item.Width) * scale;
        var bottom = (item.Y + item.Height) * scale;
        var centerX = (left + right) / 2;
        var centerY = (top + bottom) / 2;
        
        // Handle pozisyonlarını kontrol et
        if (IsNearPoint(point, left, top, handleSize)) return (item, ResizeMode.TopLeft);
        if (IsNearPoint(point, centerX, top, handleSize)) return (item, ResizeMode.Top);
        if (IsNearPoint(point, right, top, handleSize)) return (item, ResizeMode.TopRight);
        if (IsNearPoint(point, left, centerY, handleSize)) return (item, ResizeMode.Left);
        if (IsNearPoint(point, right, centerY, handleSize)) return (item, ResizeMode.Right);
        if (IsNearPoint(point, left, bottom, handleSize)) return (item, ResizeMode.BottomLeft);
        if (IsNearPoint(point, centerX, bottom, handleSize)) return (item, ResizeMode.Bottom);
        if (IsNearPoint(point, right, bottom, handleSize)) return (item, ResizeMode.BottomRight);
        
        return (null, ResizeMode.None);
    }

    private bool IsNearPoint(Point point, double x, double y, double tolerance)
    {
        return Math.Abs(point.X - x) <= tolerance && Math.Abs(point.Y - y) <= tolerance;
    }

    #endregion

    #region Snap & Overlap Prevention (Avalonia'dan port edildi)

    /// <summary>
    /// Diğer öğelerle çakışma kontrolü
    /// </summary>
    private bool WouldOverlap(ContentItem current, int x, int y, int width, int height)
    {
        if (BindingContext is not EditorViewModel vm) return false;
        
        foreach (var item in vm.Items)
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
    private (int x, int y) SnapToOtherItems(ContentItem current, int x, int y, int width, int height)
    {
        if (BindingContext is not EditorViewModel vm) return (x, y);
        
        int snappedX = x;
        int snappedY = y;
        
        // Tabela kenarlarına snap
        if (Math.Abs(x) <= SnapDistance) snappedX = 0;
        if (Math.Abs(y) <= SnapDistance) snappedY = 0;
        if (Math.Abs(x + width - _displayWidth) <= SnapDistance) snappedX = _displayWidth - width;
        if (Math.Abs(y + height - _displayHeight) <= SnapDistance) snappedY = _displayHeight - height;
        
        foreach (var item in vm.Items)
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
    private (int x, int y, int w, int h) SnapResizeToOtherItems(ContentItem current, int x, int y, int width, int height, ResizeMode mode)
    {
        if (BindingContext is not EditorViewModel vm) return (x, y, width, height);
        
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
            if (Math.Abs(x + width - _displayWidth) <= SnapDistance) snappedW = _displayWidth - x;
        }
        if (mode == ResizeMode.Top || mode == ResizeMode.TopLeft || mode == ResizeMode.TopRight)
        {
            if (Math.Abs(y) <= SnapDistance) { snappedH += snappedY; snappedY = 0; }
        }
        if (mode == ResizeMode.Bottom || mode == ResizeMode.BottomLeft || mode == ResizeMode.BottomRight)
        {
            if (Math.Abs(y + height - _displayHeight) <= SnapDistance) snappedH = _displayHeight - y;
        }
        
        foreach (var item in vm.Items)
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

    #region Helper Methods

    private void UpdateCoordinateLabel(Point position)
    {
        var x = (int)(position.X / _zoomLevel);
        var y = (int)(position.Y / _zoomLevel);
        CoordinateLabel.Text = $"X: {x}, Y: {y}";
    }

    #endregion

    #region Color Selection

    private void OnPresetForegroundColorClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && BindingContext is EditorViewModel vm)
        {
            var colorId = button.ClassId;
            
            vm.ForegroundColor = colorId switch
            {
                "Amber" => Color.FromArgb("#FFB000"),
                "Green" => Color.FromArgb("#00FF00"),
                "Red" => Color.FromArgb("#FF0000"),
                "Cyan" => Color.FromArgb("#00FFFF"),
                "Yellow" => Color.FromArgb("#FFFF00"),
                "White" => Colors.White,
                _ => Color.FromArgb("#FFB000")
            };
            
            RedrawZones();
        }
    }

    #endregion

    /// <summary>
    /// Ekran boyutunu ayarlar
    /// </summary>
    public void SetDisplaySize(int width, int height)
    {
        _displayWidth = width;
        _displayHeight = height;
        UpdateCanvasSize();
        RedrawZones();
    }
}
