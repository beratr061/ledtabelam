using System;
using System.Windows.Input;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Klavye kısayolları servisi implementasyonu
/// Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7, 15.8
/// </summary>
public class KeyboardShortcutService : IKeyboardShortcutService
{
    /// <inheritdoc/>
    public ICommand? NewProjectCommand { get; set; }

    /// <inheritdoc/>
    public ICommand? OpenProjectCommand { get; set; }

    /// <inheritdoc/>
    public ICommand? SaveProjectCommand { get; set; }

    /// <inheritdoc/>
    public ICommand? UndoCommand { get; set; }

    /// <inheritdoc/>
    public ICommand? RedoCommand { get; set; }

    /// <inheritdoc/>
    public ICommand? DeleteCommand { get; set; }

    /// <inheritdoc/>
    public ICommand? CopyCommand { get; set; }

    /// <inheritdoc/>
    public ICommand? PasteCommand { get; set; }

    /// <inheritdoc/>
    public ICommand? CutCommand { get; set; }

    /// <inheritdoc/>
    public ICommand? StartPreviewCommand { get; set; }

    /// <inheritdoc/>
    public bool HandleKeyPress(string key, KeyModifiers modifiers)
    {
        // Normalize key to uppercase for comparison
        var normalizedKey = key?.ToUpperInvariant() ?? string.Empty;

        // Ctrl+N: Yeni proje (Requirement: 15.1)
        if (modifiers == KeyModifiers.Control && normalizedKey == "N")
        {
            return ExecuteCommand(NewProjectCommand);
        }

        // Ctrl+O: Proje aç (Requirement: 15.2)
        if (modifiers == KeyModifiers.Control && normalizedKey == "O")
        {
            return ExecuteCommand(OpenProjectCommand);
        }

        // Ctrl+S: Kaydet (Requirement: 15.3)
        if (modifiers == KeyModifiers.Control && normalizedKey == "S")
        {
            return ExecuteCommand(SaveProjectCommand);
        }

        // Ctrl+Z: Geri al (Requirement: 15.4)
        if (modifiers == KeyModifiers.Control && normalizedKey == "Z")
        {
            return ExecuteCommand(UndoCommand);
        }

        // Ctrl+Y: Yinele (Requirement: 15.5)
        if (modifiers == KeyModifiers.Control && normalizedKey == "Y")
        {
            return ExecuteCommand(RedoCommand);
        }

        // Delete: Sil (Requirement: 15.6)
        if (modifiers == KeyModifiers.None && (normalizedKey == "DELETE" || normalizedKey == "DEL"))
        {
            return ExecuteCommand(DeleteCommand);
        }

        // Ctrl+C: Kopyala (Requirement: 15.7)
        if (modifiers == KeyModifiers.Control && normalizedKey == "C")
        {
            return ExecuteCommand(CopyCommand);
        }

        // Ctrl+V: Yapıştır (Requirement: 15.7)
        if (modifiers == KeyModifiers.Control && normalizedKey == "V")
        {
            return ExecuteCommand(PasteCommand);
        }

        // Ctrl+X: Kes (Requirement: 15.7)
        if (modifiers == KeyModifiers.Control && normalizedKey == "X")
        {
            return ExecuteCommand(CutCommand);
        }

        // F5: Önizleme başlat (Requirement: 15.8)
        if (modifiers == KeyModifiers.None && normalizedKey == "F5")
        {
            return ExecuteCommand(StartPreviewCommand);
        }

        return false;
    }

    private static bool ExecuteCommand(ICommand? command)
    {
        if (command == null)
            return false;

        if (command.CanExecute(null))
        {
            command.Execute(null);
            return true;
        }

        return false;
    }
}
