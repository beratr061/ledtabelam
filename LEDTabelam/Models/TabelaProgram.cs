using System;
using System.Collections.ObjectModel;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Tabela programı - bir içerik konfigürasyonu
/// Her program kendi öğelerini içerir ve belirli bir süre ekranda kalır
/// Requirements: 1.3, 1.4, 2.1, 2.2, 3.2, 3.5
/// </summary>
public class TabelaProgram : ReactiveObject
{
    private int _id;
    private string _name = "Program 1";
    private int _durationSeconds = 5;
    private ProgramTransitionType _transition = ProgramTransitionType.Direct;
    private int _transitionDurationMs = 300;
    private ObservableCollection<TabelaItem> _items = new();
    private bool _isActive = false;

    /// <summary>
    /// Program benzersiz ID'si
    /// Requirements: 1.3
    /// </summary>
    public int Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    /// <summary>
    /// Program adı (kullanıcı tanımlı)
    /// Requirements: 1.3
    /// </summary>
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value ?? "Program 1");
    }

    /// <summary>
    /// Programın ekranda kalma süresi (saniye)
    /// Varsayılan: 5 saniye
    /// Geçerli aralık: 1 - 60 saniye
    /// Requirements: 2.1, 2.2, 2.3
    /// </summary>
    public int DurationSeconds
    {
        get => _durationSeconds;
        set => this.RaiseAndSetIfChanged(ref _durationSeconds, Math.Clamp(value, 1, 60));
    }

    /// <summary>
    /// Programlar arası geçiş efekti
    /// Varsayılan: Direct (kesme)
    /// Requirements: 3.2
    /// </summary>
    public ProgramTransitionType Transition
    {
        get => _transition;
        set => this.RaiseAndSetIfChanged(ref _transition, value);
    }

    /// <summary>
    /// Geçiş animasyonu süresi (milisaniye)
    /// Varsayılan: 300ms
    /// Geçerli aralık: 200 - 1000ms
    /// Requirements: 3.4, 3.5
    /// </summary>
    public int TransitionDurationMs
    {
        get => _transitionDurationMs;
        set => this.RaiseAndSetIfChanged(ref _transitionDurationMs, Math.Clamp(value, 200, 1000));
    }

    /// <summary>
    /// Programın içerdiği öğeler (metin, sembol vb.)
    /// Requirements: 1.4
    /// </summary>
    public ObservableCollection<TabelaItem> Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value ?? new ObservableCollection<TabelaItem>());
    }

    /// <summary>
    /// Program şu anda aktif mi (oynatılıyor mu)
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }
}
