using LEDTabelam.Maui.Models;
using LEDTabelam.Maui.ViewModels;

namespace LEDTabelam.Maui.Controls;

/// <summary>
/// TreeView düğümleri için sürükle-bırak davranışı
/// Öğe sırasını değiştirmeyi sağlar
/// Requirement: 3.5
/// </summary>
public class DragDropBehavior : Behavior<View>
{
    private View? _associatedView;
    private static object? _draggedItem;
    private static View? _draggedView;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        _associatedView = bindable;

        // Drag gesture ekle
        var dragGesture = new DragGestureRecognizer
        {
            CanDrag = true
        };
        dragGesture.DragStarting += OnDragStarting;
        bindable.GestureRecognizers.Add(dragGesture);

        // Drop gesture ekle
        var dropGesture = new DropGestureRecognizer
        {
            AllowDrop = true
        };
        dropGesture.DragOver += OnDragOver;
        dropGesture.Drop += OnDrop;
        bindable.GestureRecognizers.Add(dropGesture);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        base.OnDetachingFrom(bindable);
        
        // Gesture recognizer'ları temizle
        var toRemove = bindable.GestureRecognizers
            .Where(g => g is DragGestureRecognizer || g is DropGestureRecognizer)
            .ToList();
        
        foreach (var gesture in toRemove)
        {
            bindable.GestureRecognizers.Remove(gesture);
        }
        
        _associatedView = null;
    }

    private void OnDragStarting(object? sender, DragStartingEventArgs e)
    {
        if (_associatedView?.BindingContext == null) return;

        _draggedItem = _associatedView.BindingContext;
        _draggedView = _associatedView;
        
        // Görsel geri bildirim
        _associatedView.Opacity = 0.5;
        
        // Drag data'yı ayarla
        e.Data.Properties["DraggedItem"] = _draggedItem;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (_associatedView?.BindingContext == null || _draggedItem == null) return;

        var targetItem = _associatedView.BindingContext;
        
        // Aynı tip öğeler arasında sürüklemeye izin ver
        if (CanDrop(_draggedItem, targetItem))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            
            // Görsel geri bildirim - hedef vurgulama
            _associatedView.BackgroundColor = Colors.Blue.WithAlpha(0.15f);
        }
        else
        {
            e.AcceptedOperation = DataPackageOperation.None;
        }
    }

    private void OnDrop(object? sender, DropEventArgs e)
    {
        if (_associatedView?.BindingContext == null || _draggedItem == null) return;

        var targetItem = _associatedView.BindingContext;
        
        // Görsel geri bildirimi temizle
        _associatedView.BackgroundColor = Colors.Transparent;
        if (_draggedView != null)
        {
            _draggedView.Opacity = 1.0;
        }

        // Aynı öğeye bırakma kontrolü
        if (ReferenceEquals(_draggedItem, targetItem))
        {
            ResetDragState();
            return;
        }

        // Sıralama işlemini gerçekleştir
        if (CanDrop(_draggedItem, targetItem))
        {
            PerformReorder(_draggedItem, targetItem);
        }

        ResetDragState();
    }

    private static bool CanDrop(object draggedItem, object targetItem)
    {
        // Aynı tip öğeler arasında sürüklemeye izin ver
        return (draggedItem is ScreenNode && targetItem is ScreenNode) ||
               (draggedItem is ProgramNode && targetItem is ProgramNode) ||
               (draggedItem is ContentItem && targetItem is ContentItem);
    }

    private void PerformReorder(object draggedItem, object targetItem)
    {
        // TreeViewModel'i bul
        var treeViewModel = FindTreeViewModel();
        if (treeViewModel == null) return;

        switch (draggedItem)
        {
            case ScreenNode draggedScreen when targetItem is ScreenNode targetScreen:
                ReorderScreens(treeViewModel, draggedScreen, targetScreen);
                break;
            
            case ProgramNode draggedProgram when targetItem is ProgramNode targetProgram:
                ReorderPrograms(treeViewModel, draggedProgram, targetProgram);
                break;
            
            case ContentItem draggedContent when targetItem is ContentItem targetContent:
                ReorderContents(treeViewModel, draggedContent, targetContent);
                break;
        }
    }

    private void ReorderScreens(TreeViewModel viewModel, ScreenNode dragged, ScreenNode target)
    {
        var screens = viewModel.Screens;
        var draggedIndex = screens.IndexOf(dragged);
        var targetIndex = screens.IndexOf(target);
        
        if (draggedIndex >= 0 && targetIndex >= 0 && draggedIndex != targetIndex)
        {
            screens.Move(draggedIndex, targetIndex);
        }
    }

    private void ReorderPrograms(TreeViewModel viewModel, ProgramNode dragged, ProgramNode target)
    {
        // Aynı ekran içinde mi kontrol et
        ScreenNode? draggedParent = null;
        ScreenNode? targetParent = null;
        
        foreach (var screen in viewModel.Screens)
        {
            if (screen.Programs.Contains(dragged))
                draggedParent = screen;
            if (screen.Programs.Contains(target))
                targetParent = screen;
        }
        
        // Sadece aynı ekran içinde sıralama
        if (draggedParent != null && draggedParent == targetParent)
        {
            var programs = draggedParent.Programs;
            var draggedIndex = programs.IndexOf(dragged);
            var targetIndex = programs.IndexOf(target);
            
            if (draggedIndex >= 0 && targetIndex >= 0 && draggedIndex != targetIndex)
            {
                programs.Move(draggedIndex, targetIndex);
            }
        }
    }

    private void ReorderContents(TreeViewModel viewModel, ContentItem dragged, ContentItem target)
    {
        // Aynı program içinde mi kontrol et
        ProgramNode? draggedParent = null;
        ProgramNode? targetParent = null;
        
        foreach (var screen in viewModel.Screens)
        {
            foreach (var program in screen.Programs)
            {
                if (program.Contents.Contains(dragged))
                    draggedParent = program;
                if (program.Contents.Contains(target))
                    targetParent = program;
            }
        }
        
        // Sadece aynı program içinde sıralama
        if (draggedParent != null && draggedParent == targetParent)
        {
            var contents = draggedParent.Contents;
            var draggedIndex = contents.IndexOf(dragged);
            var targetIndex = contents.IndexOf(target);
            
            if (draggedIndex >= 0 && targetIndex >= 0 && draggedIndex != targetIndex)
            {
                contents.Move(draggedIndex, targetIndex);
            }
        }
    }

    private TreeViewModel? FindTreeViewModel()
    {
        var parent = _associatedView?.Parent;
        while (parent != null)
        {
            if (parent.BindingContext is TreeViewModel tvm)
                return tvm;
            parent = parent.Parent;
        }
        return null;
    }

    private static void ResetDragState()
    {
        if (_draggedView != null)
        {
            _draggedView.Opacity = 1.0;
        }
        _draggedItem = null;
        _draggedView = null;
    }
}
