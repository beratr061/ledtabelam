using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Tabela profili (Metrobüs, Belediye Otobüsü, Tramvay vb.)
/// Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6
/// </summary>
public class Profile : ReactiveObject
{
    private string _name = string.Empty;
    private DisplaySettings _settings = new();
    private string _fontName = string.Empty;
    private List<Zone> _defaultZones = new();
    private Dictionary<int, TabelaSlot> _slots = new();
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _modifiedAt = DateTime.UtcNow;

    /// <summary>
    /// Profil adı (örn: "Metrobüs Tabelaları", "Belediye Otobüsü")
    /// </summary>
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// Görüntüleme ayarları
    /// </summary>
    public DisplaySettings Settings
    {
        get => _settings;
        set => this.RaiseAndSetIfChanged(ref _settings, value);
    }

    /// <summary>
    /// Kullanılan font adı
    /// </summary>
    public string FontName
    {
        get => _fontName;
        set => this.RaiseAndSetIfChanged(ref _fontName, value);
    }

    /// <summary>
    /// Varsayılan zone düzeni
    /// </summary>
    public List<Zone> DefaultZones
    {
        get => _defaultZones;
        set => this.RaiseAndSetIfChanged(ref _defaultZones, value);
    }

    /// <summary>
    /// Tabela slotları (1-999)
    /// </summary>
    public Dictionary<int, TabelaSlot> Slots
    {
        get => _slots;
        set => this.RaiseAndSetIfChanged(ref _slots, value);
    }

    /// <summary>
    /// Profil oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => this.RaiseAndSetIfChanged(ref _createdAt, value);
    }

    /// <summary>
    /// Son değişiklik tarihi
    /// </summary>
    public DateTime ModifiedAt
    {
        get => _modifiedAt;
        set => this.RaiseAndSetIfChanged(ref _modifiedAt, value);
    }

    /// <summary>
    /// Belirtilen slot numarasındaki slot'u döndürür
    /// </summary>
    public TabelaSlot? GetSlot(int slotNumber)
    {
        return Slots.TryGetValue(slotNumber, out var slot) ? slot : null;
    }

    /// <summary>
    /// Belirtilen slot numarasına slot atar
    /// </summary>
    public void SetSlot(int slotNumber, TabelaSlot slot)
    {
        if (slotNumber < 1 || slotNumber > 999)
            throw new ArgumentOutOfRangeException(nameof(slotNumber), "Slot numarası 1-999 arasında olmalıdır.");

        slot.SlotNumber = slotNumber;
        Slots[slotNumber] = slot;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Tanımlı slot sayısını döndürür
    /// </summary>
    public int DefinedSlotCount => Slots.Count(s => s.Value.IsDefined);
}
