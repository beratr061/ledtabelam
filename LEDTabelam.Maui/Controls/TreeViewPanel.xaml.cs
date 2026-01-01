using LEDTabelam.Maui.Models;
using LEDTabelam.Maui.ViewModels;

namespace LEDTabelam.Maui.Controls;

/// <summary>
/// TreeView paneli - Ekran/Program/İçerik hiyerarşisini gösterir
/// Requirements: 3.1, 3.2, 3.3, 3.4, 3.7
/// </summary>
public partial class TreeViewPanel : ContentView
{
    /// <summary>
    /// Çift tıklama olayı - düzenleme moduna geçiş için
    /// </summary>
    public event EventHandler<object>? NodeDoubleClicked;

    /// <summary>
    /// Ekle butonu tıklama olayı
    /// </summary>
    public event EventHandler? AddRequested;

    public TreeViewPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Düğüm tıklama işleyicisi - seçim
    /// Requirement: 3.3
    /// </summary>
    private void OnNodeTapped(object? sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            // ViewModel'e seçimi bildir - görsel güncelleme SelectableNodeBehavior tarafından yapılır
            if (BindingContext is TreeViewModel viewModel && view.BindingContext is object node)
            {
                viewModel.SelectItemCommand.Execute(node);
            }
        }
    }

    /// <summary>
    /// Düğüm çift tıklama işleyicisi
    /// Requirement: 3.4
    /// </summary>
    private void OnNodeDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is View view && view.BindingContext is object node)
        {
            NodeDoubleClicked?.Invoke(this, node);
        }
    }

    /// <summary>
    /// Genişlet/Daralt toggle işleyicisi
    /// Requirement: 3.7
    /// </summary>
    private void OnExpandToggleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            // Parent'ın BindingContext'ini al (ScreenNode veya ProgramNode)
            var parent = view.Parent;
            while (parent != null)
            {
                if (parent.BindingContext is ScreenNode screen)
                {
                    screen.IsExpanded = !screen.IsExpanded;
                    break;
                }
                else if (parent.BindingContext is ProgramNode program)
                {
                    program.IsExpanded = !program.IsExpanded;
                    break;
                }
                parent = parent.Parent;
            }
        }
    }

    /// <summary>
    /// Ekle butonu tıklama işleyicisi
    /// </summary>
    private async void OnAddClicked(object? sender, EventArgs e)
    {
        // Context'e göre uygun ekleme seçeneklerini göster
        var viewModel = BindingContext as TreeViewModel;
        if (viewModel == null) return;

        string[] options;
        
        if (viewModel.SelectedItem is ContentItem)
        {
            // İçerik seçiliyse, aynı programa yeni içerik ekle
            options = new[]
            {
                (string)Application.Current!.Resources["MenuAddText"],
                (string)Application.Current.Resources["MenuAddClock"],
                (string)Application.Current.Resources["MenuAddDate"],
                (string)Application.Current.Resources["MenuAddCountdown"]
            };
        }
        else if (viewModel.SelectedItem is ProgramNode)
        {
            // Program seçiliyse, içerik veya yeni program ekle
            options = new[]
            {
                (string)Application.Current!.Resources["MenuAddText"],
                (string)Application.Current.Resources["MenuAddClock"],
                (string)Application.Current.Resources["MenuAddDate"],
                (string)Application.Current.Resources["MenuAddCountdown"],
                (string)Application.Current.Resources["MenuAddProgram"]
            };
        }
        else if (viewModel.SelectedItem is ScreenNode)
        {
            // Ekran seçiliyse, program veya yeni ekran ekle
            options = new[]
            {
                (string)Application.Current!.Resources["MenuAddProgram"],
                (string)Application.Current.Resources["MenuAddScreen"]
            };
        }
        else
        {
            // Hiçbir şey seçili değilse, ekran ekle
            options = new[]
            {
                (string)Application.Current!.Resources["MenuAddScreen"]
            };
        }

        var page = this.GetParentPage();
        if (page != null)
        {
            var action = await page.DisplayActionSheet(
                (string)Application.Current!.Resources["ContextAdd"],
                (string)Application.Current.Resources["ButtonCancel"],
                null,
                options);

            if (action != null && action != (string)Application.Current.Resources["ButtonCancel"])
            {
                AddRequested?.Invoke(this, new AddRequestedEventArgs(action));
            }
        }
    }

    /// <summary>
    /// Parent Page'i bulur
    /// </summary>
    private Page? GetParentPage()
    {
        Element? parent = this;
        while (parent != null)
        {
            if (parent is Page page)
                return page;
            parent = parent.Parent;
        }
        return null;
    }

    #region Context Menu Event Handlers

    /// <summary>
    /// Context menü - Ekran ekle
    /// Requirement: 3.6
    /// </summary>
    private void OnContextAddScreen(object? sender, EventArgs e)
    {
        AddRequested?.Invoke(this, new AddRequestedEventArgs(
            (string)Application.Current!.Resources["MenuAddScreen"]));
    }

    /// <summary>
    /// Context menü - Program ekle
    /// Requirement: 3.6
    /// </summary>
    private void OnContextAddProgram(object? sender, EventArgs e)
    {
        AddRequested?.Invoke(this, new AddRequestedEventArgs(
            (string)Application.Current!.Resources["MenuAddProgram"]));
    }

    /// <summary>
    /// Context menü - Metin ekle
    /// Requirement: 3.6
    /// </summary>
    private void OnContextAddText(object? sender, EventArgs e)
    {
        AddRequested?.Invoke(this, new AddRequestedEventArgs(
            (string)Application.Current!.Resources["MenuAddText"]));
    }

    /// <summary>
    /// Context menü - Saat ekle
    /// Requirement: 3.6
    /// </summary>
    private void OnContextAddClock(object? sender, EventArgs e)
    {
        AddRequested?.Invoke(this, new AddRequestedEventArgs(
            (string)Application.Current!.Resources["MenuAddClock"]));
    }

    /// <summary>
    /// Context menü - Tarih ekle
    /// Requirement: 3.6
    /// </summary>
    private void OnContextAddDate(object? sender, EventArgs e)
    {
        AddRequested?.Invoke(this, new AddRequestedEventArgs(
            (string)Application.Current!.Resources["MenuAddDate"]));
    }

    /// <summary>
    /// Context menü - Geri sayım ekle
    /// Requirement: 3.6
    /// </summary>
    private void OnContextAddCountdown(object? sender, EventArgs e)
    {
        AddRequested?.Invoke(this, new AddRequestedEventArgs(
            (string)Application.Current!.Resources["MenuAddCountdown"]));
    }

    /// <summary>
    /// Context menü - Çoğalt
    /// Requirement: 3.6
    /// </summary>
    private void OnContextDuplicate(object? sender, EventArgs e)
    {
        if (BindingContext is TreeViewModel viewModel)
        {
            viewModel.DuplicateSelectedCommand.Execute(null);
        }
    }

    /// <summary>
    /// Context menü - Yeniden adlandır
    /// Requirement: 3.6
    /// </summary>
    private async void OnContextRename(object? sender, EventArgs e)
    {
        if (BindingContext is not TreeViewModel viewModel || viewModel.SelectedItem == null)
            return;

        var page = GetParentPage();
        if (page == null) return;

        string currentName = viewModel.SelectedItem switch
        {
            ScreenNode screen => screen.Name,
            ProgramNode program => program.Name,
            ContentItem content => content.Name,
            _ => ""
        };

        var newName = await page.DisplayPromptAsync(
            (string)Application.Current!.Resources["ContextRename"],
            (string)Application.Current.Resources["ContextRename"],
            (string)Application.Current.Resources["ButtonOK"],
            (string)Application.Current.Resources["ButtonCancel"],
            initialValue: currentName);

        if (!string.IsNullOrWhiteSpace(newName))
        {
            switch (viewModel.SelectedItem)
            {
                case ScreenNode screen:
                    screen.Name = newName;
                    break;
                case ProgramNode program:
                    program.Name = newName;
                    break;
                case ContentItem content:
                    content.Name = newName;
                    break;
            }
        }
    }

    /// <summary>
    /// Context menü - Yukarı taşı
    /// Requirement: 3.6
    /// </summary>
    private void OnContextMoveUp(object? sender, EventArgs e)
    {
        if (BindingContext is TreeViewModel viewModel)
        {
            viewModel.MoveUpCommand.Execute(null);
        }
    }

    /// <summary>
    /// Context menü - Aşağı taşı
    /// Requirement: 3.6
    /// </summary>
    private void OnContextMoveDown(object? sender, EventArgs e)
    {
        if (BindingContext is TreeViewModel viewModel)
        {
            viewModel.MoveDownCommand.Execute(null);
        }
    }

    /// <summary>
    /// Context menü - Sil
    /// Requirement: 3.6
    /// </summary>
    private async void OnContextDelete(object? sender, EventArgs e)
    {
        if (BindingContext is not TreeViewModel viewModel || viewModel.SelectedItem == null)
            return;

        var page = GetParentPage();
        if (page == null) return;

        var confirm = await page.DisplayAlert(
            (string)Application.Current!.Resources["DialogConfirm"],
            (string)Application.Current.Resources["MessageDeleteConfirm"],
            (string)Application.Current.Resources["ButtonYes"],
            (string)Application.Current.Resources["ButtonNo"]);

        if (confirm)
        {
            viewModel.DeleteSelectedCommand.Execute(null);
        }
    }

    #endregion
}

/// <summary>
/// Ekle isteği event args
/// </summary>
public class AddRequestedEventArgs : EventArgs
{
    public string ActionType { get; }

    public AddRequestedEventArgs(string actionType)
    {
        ActionType = actionType;
    }
}
