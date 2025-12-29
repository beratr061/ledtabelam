using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace LEDTabelam.Views;

public partial class PreviewPanel : UserControl
{
    private bool _isPanning;
    private Point _lastPanPosition;
    private ScrollViewer? _scrollViewer;

    public PreviewPanel()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _scrollViewer = this.FindControl<ScrollViewer>("PreviewScrollViewer");
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastPanPosition = e.GetPosition(this);
            e.Pointer.Capture((IInputElement?)sender);
            
            // Cursor'u değiştir
            if (sender is Control control)
            {
                control.Cursor = new Cursor(StandardCursorType.SizeAll);
            }
        }
    }

    private void OnPreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning && _scrollViewer != null)
        {
            var currentPosition = e.GetPosition(this);
            var delta = _lastPanPosition - currentPosition;
            
            // Scroll pozisyonunu güncelle
            _scrollViewer.Offset = new Vector(
                _scrollViewer.Offset.X + delta.X,
                _scrollViewer.Offset.Y + delta.Y
            );
            
            _lastPanPosition = currentPosition;
        }
    }

    private void OnPreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            
            // Cursor'u geri al
            if (sender is Control control)
            {
                control.Cursor = new Cursor(StandardCursorType.Hand);
            }
        }
    }
}
