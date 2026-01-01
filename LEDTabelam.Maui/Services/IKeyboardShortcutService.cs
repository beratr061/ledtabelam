using System;
using System.Windows.Input;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Klavye kısayolları servisi interface'i
/// Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7, 15.8
/// </summary>
public interface IKeyboardShortcutService
{
    /// <summary>
    /// Yeni proje komutu (Ctrl+N)
    /// Requirement: 15.1
    /// </summary>
    ICommand? NewProjectCommand { get; set; }

    /// <summary>
    /// Proje aç komutu (Ctrl+O)
    /// Requirement: 15.2
    /// </summary>
    ICommand? OpenProjectCommand { get; set; }

    /// <summary>
    /// Kaydet komutu (Ctrl+S)
    /// Requirement: 15.3
    /// </summary>
    ICommand? SaveProjectCommand { get; set; }

    /// <summary>
    /// Geri al komutu (Ctrl+Z)
    /// Requirement: 15.4
    /// </summary>
    ICommand? UndoCommand { get; set; }

    /// <summary>
    /// Yinele komutu (Ctrl+Y)
    /// Requirement: 15.5
    /// </summary>
    ICommand? RedoCommand { get; set; }

    /// <summary>
    /// Sil komutu (Delete)
    /// Requirement: 15.6
    /// </summary>
    ICommand? DeleteCommand { get; set; }

    /// <summary>
    /// Kopyala komutu (Ctrl+C)
    /// Requirement: 15.7
    /// </summary>
    ICommand? CopyCommand { get; set; }

    /// <summary>
    /// Yapıştır komutu (Ctrl+V)
    /// Requirement: 15.7
    /// </summary>
    ICommand? PasteCommand { get; set; }

    /// <summary>
    /// Kes komutu (Ctrl+X)
    /// Requirement: 15.7
    /// </summary>
    ICommand? CutCommand { get; set; }

    /// <summary>
    /// Önizleme başlat komutu (F5)
    /// Requirement: 15.8
    /// </summary>
    ICommand? StartPreviewCommand { get; set; }

    /// <summary>
    /// Klavye tuşu basıldığında çağrılır
    /// </summary>
    /// <param name="key">Basılan tuş</param>
    /// <param name="modifiers">Modifier tuşları (Ctrl, Shift, Alt)</param>
    /// <returns>Kısayol işlendiyse true</returns>
    bool HandleKeyPress(string key, KeyModifiers modifiers);
}

/// <summary>
/// Modifier tuşları
/// </summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Control = 1,
    Shift = 2,
    Alt = 4
}
