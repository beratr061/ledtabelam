using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LEDTabelam.Maui.Models;

namespace LEDTabelam.Maui.ViewModels;

/// <summary>
/// TreeView paneli ViewModel'i
/// Ekran/Program/İçerik hiyerarşisini yönetir
/// Requirements: 3.1, 3.3, 3.5, 3.6, 3.7
/// </summary>
public partial class TreeViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ScreenNode> _screens = new();

    [ObservableProperty]
    private object? _selectedItem;

    [ObservableProperty]
    private bool _isExpanded = true;

    /// <summary>
    /// Seçili ekran (eğer seçili öğe ekran ise)
    /// </summary>
    public ScreenNode? SelectedScreen => SelectedItem as ScreenNode;

    /// <summary>
    /// Seçili program (eğer seçili öğe program ise)
    /// </summary>
    public ProgramNode? SelectedProgram => SelectedItem as ProgramNode;

    /// <summary>
    /// Seçili içerik (eğer seçili öğe içerik ise)
    /// </summary>
    public ContentItem? SelectedContent => SelectedItem as ContentItem;

    /// <summary>
    /// Projeyi yükler ve TreeView'ı günceller
    /// </summary>
    public void LoadProject(Project project)
    {
        if (project == null)
            return;

        Screens.Clear();
        foreach (var screen in project.Screens)
        {
            Screens.Add(screen);
        }
        
        // İlk ekranı seç
        if (Screens.Count > 0)
        {
            SelectedItem = Screens[0];
        }
    }

    /// <summary>
    /// Ekranları yeniler (proje değişikliklerinden sonra)
    /// </summary>
    public void RefreshScreens()
    {
        // ObservableCollection otomatik olarak UI'ı günceller
        // Ancak nested değişiklikler için manuel tetikleme gerekebilir
        OnPropertyChanged(nameof(Screens));
    }

    /// <summary>
    /// Belirtilen öğeyi seçer
    /// Requirement: 3.3
    /// </summary>
    [RelayCommand]
    public void SelectItem(object? item)
    {
        // Önce tüm öğelerin seçimini kaldır
        ClearAllSelections();
        
        SelectedItem = item;
        
        // Yeni seçili öğeyi işaretle
        if (item is ScreenNode screen)
        {
            screen.IsSelected = true;
        }
        else if (item is ProgramNode program)
        {
            program.IsSelected = true;
            ExpandParentsOf(program);
        }
        else if (item is ContentItem content)
        {
            content.IsSelected = true;
            ExpandParentsOf(content);
        }
    }

    /// <summary>
    /// Tüm öğelerin seçimini kaldırır
    /// </summary>
    private void ClearAllSelections()
    {
        foreach (var screen in Screens)
        {
            screen.IsSelected = false;
            foreach (var program in screen.Programs)
            {
                program.IsSelected = false;
                foreach (var content in program.Contents)
                {
                    content.IsSelected = false;
                }
            }
        }
    }

    /// <summary>
    /// Tüm düğümleri genişletir
    /// Requirement: 3.7
    /// </summary>
    [RelayCommand]
    public void ExpandAll()
    {
        foreach (var screen in Screens)
        {
            screen.IsExpanded = true;
            foreach (var program in screen.Programs)
            {
                program.IsExpanded = true;
            }
        }
        IsExpanded = true;
    }

    /// <summary>
    /// Tüm düğümleri daraltır
    /// Requirement: 3.7
    /// </summary>
    [RelayCommand]
    public void CollapseAll()
    {
        foreach (var screen in Screens)
        {
            screen.IsExpanded = false;
            foreach (var program in screen.Programs)
            {
                program.IsExpanded = false;
            }
        }
    }

    /// <summary>
    /// Seçili öğeyi siler
    /// Requirement: 3.6
    /// </summary>
    [RelayCommand]
    public void DeleteSelected()
    {
        if (SelectedItem == null)
            return;

        if (SelectedItem is ContentItem content)
        {
            DeleteContent(content);
        }
        else if (SelectedItem is ProgramNode program)
        {
            DeleteProgram(program);
        }
        else if (SelectedItem is ScreenNode screen)
        {
            DeleteScreen(screen);
        }
    }

    /// <summary>
    /// Seçili öğeyi kopyalar
    /// Requirement: 3.6
    /// </summary>
    [RelayCommand]
    public void DuplicateSelected()
    {
        if (SelectedItem == null)
            return;

        if (SelectedItem is ContentItem content)
        {
            DuplicateContent(content);
        }
        else if (SelectedItem is ProgramNode program)
        {
            DuplicateProgram(program);
        }
        else if (SelectedItem is ScreenNode screen)
        {
            DuplicateScreen(screen);
        }
    }

    /// <summary>
    /// Seçili öğeyi yukarı taşır
    /// Requirement: 3.5
    /// </summary>
    [RelayCommand]
    public void MoveUp()
    {
        if (SelectedItem == null)
            return;

        if (SelectedItem is ContentItem content)
        {
            MoveContentUp(content);
        }
        else if (SelectedItem is ProgramNode program)
        {
            MoveProgramUp(program);
        }
        else if (SelectedItem is ScreenNode screen)
        {
            MoveScreenUp(screen);
        }
    }

    /// <summary>
    /// Seçili öğeyi aşağı taşır
    /// Requirement: 3.5
    /// </summary>
    [RelayCommand]
    public void MoveDown()
    {
        if (SelectedItem == null)
            return;

        if (SelectedItem is ContentItem content)
        {
            MoveContentDown(content);
        }
        else if (SelectedItem is ProgramNode program)
        {
            MoveProgramDown(program);
        }
        else if (SelectedItem is ScreenNode screen)
        {
            MoveScreenDown(screen);
        }
    }

    /// <summary>
    /// Toplam düğüm sayısını hesaplar (screens + programs + contents)
    /// Property 4: TreeView Hierarchy Consistency için kullanılır
    /// </summary>
    public int GetTotalNodeCount()
    {
        int count = Screens.Count;
        foreach (var screen in Screens)
        {
            count += screen.Programs.Count;
            foreach (var program in screen.Programs)
            {
                count += program.Contents.Count;
            }
        }
        return count;
    }

    private void ExpandParentsOf(ContentItem content)
    {
        foreach (var screen in Screens)
        {
            foreach (var program in screen.Programs)
            {
                if (program.Contents.Contains(content))
                {
                    screen.IsExpanded = true;
                    program.IsExpanded = true;
                    return;
                }
            }
        }
    }

    private void ExpandParentsOf(ProgramNode program)
    {
        foreach (var screen in Screens)
        {
            if (screen.Programs.Contains(program))
            {
                screen.IsExpanded = true;
                return;
            }
        }
    }

    private void DeleteContent(ContentItem content)
    {
        foreach (var screen in Screens)
        {
            foreach (var program in screen.Programs)
            {
                if (program.Contents.Remove(content))
                {
                    // Bir önceki içeriği veya programı seç
                    var index = program.Contents.Count > 0 
                        ? Math.Min(program.Contents.Count - 1, 0) 
                        : -1;
                    
                    SelectedItem = index >= 0 ? program.Contents[index] : program;
                    return;
                }
            }
        }
    }

    private void DeleteProgram(ProgramNode program)
    {
        foreach (var screen in Screens)
        {
            if (screen.Programs.Remove(program))
            {
                // Bir önceki programı veya ekranı seç
                var index = screen.Programs.Count > 0 
                    ? Math.Min(screen.Programs.Count - 1, 0) 
                    : -1;
                
                SelectedItem = index >= 0 ? screen.Programs[index] : screen;
                return;
            }
        }
    }

    private void DeleteScreen(ScreenNode screen)
    {
        var index = Screens.IndexOf(screen);
        if (Screens.Remove(screen))
        {
            // Bir önceki ekranı seç
            if (Screens.Count > 0)
            {
                SelectedItem = Screens[Math.Max(0, index - 1)];
            }
            else
            {
                SelectedItem = null;
            }
        }
    }

    private void DuplicateContent(ContentItem content)
    {
        foreach (var screen in Screens)
        {
            foreach (var program in screen.Programs)
            {
                var index = program.Contents.IndexOf(content);
                if (index >= 0)
                {
                    var clone = CloneContent(content);
                    clone.Name = content.Name + " (Kopya)";
                    program.Contents.Insert(index + 1, clone);
                    SelectedItem = clone;
                    return;
                }
            }
        }
    }

    private void DuplicateProgram(ProgramNode program)
    {
        foreach (var screen in Screens)
        {
            var index = screen.Programs.IndexOf(program);
            if (index >= 0)
            {
                var clone = CloneProgram(program);
                clone.Name = program.Name + " (Kopya)";
                screen.Programs.Insert(index + 1, clone);
                SelectedItem = clone;
                return;
            }
        }
    }

    private void DuplicateScreen(ScreenNode screen)
    {
        var index = Screens.IndexOf(screen);
        if (index >= 0)
        {
            var clone = CloneScreen(screen);
            clone.Name = screen.Name + " (Kopya)";
            Screens.Insert(index + 1, clone);
            SelectedItem = clone;
        }
    }

    private void MoveContentUp(ContentItem content)
    {
        foreach (var screen in Screens)
        {
            foreach (var program in screen.Programs)
            {
                var index = program.Contents.IndexOf(content);
                if (index > 0)
                {
                    program.Contents.Move(index, index - 1);
                    return;
                }
            }
        }
    }

    private void MoveContentDown(ContentItem content)
    {
        foreach (var screen in Screens)
        {
            foreach (var program in screen.Programs)
            {
                var index = program.Contents.IndexOf(content);
                if (index >= 0 && index < program.Contents.Count - 1)
                {
                    program.Contents.Move(index, index + 1);
                    return;
                }
            }
        }
    }

    private void MoveProgramUp(ProgramNode program)
    {
        foreach (var screen in Screens)
        {
            var index = screen.Programs.IndexOf(program);
            if (index > 0)
            {
                screen.Programs.Move(index, index - 1);
                return;
            }
        }
    }

    private void MoveProgramDown(ProgramNode program)
    {
        foreach (var screen in Screens)
        {
            var index = screen.Programs.IndexOf(program);
            if (index >= 0 && index < screen.Programs.Count - 1)
            {
                screen.Programs.Move(index, index + 1);
                return;
            }
        }
    }

    private void MoveScreenUp(ScreenNode screen)
    {
        var index = Screens.IndexOf(screen);
        if (index > 0)
        {
            Screens.Move(index, index - 1);
        }
    }

    private void MoveScreenDown(ScreenNode screen)
    {
        var index = Screens.IndexOf(screen);
        if (index >= 0 && index < Screens.Count - 1)
        {
            Screens.Move(index, index + 1);
        }
    }

    private static ContentItem CloneContent(ContentItem source)
    {
        // Basit klon - gerçek implementasyonda IContentManager.CloneContent kullanılmalı
        return new ContentItem
        {
            Id = Guid.NewGuid().ToString(),
            Name = source.Name,
            ContentType = source.ContentType,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            DurationMs = source.DurationMs,
            ShowImmediately = source.ShowImmediately,
            EntryEffect = new EffectConfig
            {
                EffectType = source.EntryEffect.EffectType,
                SpeedMs = source.EntryEffect.SpeedMs,
                Direction = source.EntryEffect.Direction
            },
            ExitEffect = new EffectConfig
            {
                EffectType = source.ExitEffect.EffectType,
                SpeedMs = source.ExitEffect.SpeedMs,
                Direction = source.ExitEffect.Direction
            }
        };
    }

    private static ProgramNode CloneProgram(ProgramNode source)
    {
        var clone = new ProgramNode
        {
            Id = Guid.NewGuid().ToString(),
            Name = source.Name,
            IsLoop = source.IsLoop,
            TransitionType = source.TransitionType,
            IsExpanded = source.IsExpanded
        };

        foreach (var content in source.Contents)
        {
            clone.Contents.Add(CloneContent(content));
        }

        return clone;
    }

    private static ScreenNode CloneScreen(ScreenNode source)
    {
        var clone = new ScreenNode
        {
            Id = Guid.NewGuid().ToString(),
            Name = source.Name,
            Width = source.Width,
            Height = source.Height,
            IsExpanded = source.IsExpanded
        };

        foreach (var program in source.Programs)
        {
            clone.Programs.Add(CloneProgram(program));
        }

        return clone;
    }
}
