using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LEDTabelam.Maui.Models;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Tabela slot yönetimi servisi implementasyonu
/// </summary>
public class SlotManager : ISlotManager
{
    private readonly Dictionary<int, TabelaSlot> _slots;
    private readonly JsonSerializerOptions _jsonOptions;

    public const int MinSlotNumber = 1;
    public const int MaxSlotNumber = 999;

    public event Action<int>? SlotChanged;

    public SlotManager() : this(new Dictionary<int, TabelaSlot>())
    {
    }

    public SlotManager(Dictionary<int, TabelaSlot> slots)
    {
        _slots = slots ?? new Dictionary<int, TabelaSlot>();
        _jsonOptions = CreateJsonOptions();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new MauiColorJsonConverter());
        return options;
    }

    public int DefinedSlotCount => _slots.Count(s => s.Value.IsDefined);

    public TabelaSlot? GetSlot(int slotNumber)
    {
        ValidateSlotNumber(slotNumber);
        return _slots.TryGetValue(slotNumber, out var slot) ? slot : null;
    }

    public void SetSlot(int slotNumber, TabelaSlot slot)
    {
        ValidateSlotNumber(slotNumber);

        if (slot == null)
            throw new ArgumentNullException(nameof(slot));

        slot.SlotNumber = slotNumber;
        _slots[slotNumber] = slot;
        SlotChanged?.Invoke(slotNumber);
    }

    public bool RemoveSlot(int slotNumber)
    {
        ValidateSlotNumber(slotNumber);

        if (_slots.Remove(slotNumber))
        {
            SlotChanged?.Invoke(slotNumber);
            return true;
        }
        return false;
    }

    public List<TabelaSlot> SearchSlots(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAllDefinedSlots();

        var normalizedQuery = query.Trim().ToLowerInvariant();

        return _slots.Values
            .Where(s => s.IsDefined &&
                       (s.Name.ToLowerInvariant().Contains(normalizedQuery) ||
                        s.Summary.ToLowerInvariant().Contains(normalizedQuery) ||
                        s.SlotNumber.ToString().Contains(normalizedQuery)))
            .OrderBy(s => s.SlotNumber)
            .ToList();
    }

    public List<TabelaSlot> GetAllDefinedSlots()
    {
        return _slots.Values
            .Where(s => s.IsDefined)
            .OrderBy(s => s.SlotNumber)
            .ToList();
    }

    public async Task ExportSlotsAsync(string filePath)
    {
        var definedSlots = GetAllDefinedSlots();
        var exportData = new SlotExportData
        {
            ExportedAt = DateTime.UtcNow,
            SlotCount = definedSlots.Count,
            Slots = definedSlots.ToDictionary(s => s.SlotNumber, s => s)
        };

        var json = JsonSerializer.Serialize(exportData, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task ExportSelectedSlotsAsync(IEnumerable<int> slotNumbers, string filePath)
    {
        var selectedSlots = slotNumbers
            .Where(n => _slots.ContainsKey(n) && _slots[n].IsDefined)
            .Select(n => _slots[n])
            .ToList();

        var exportData = new SlotExportData
        {
            ExportedAt = DateTime.UtcNow,
            SlotCount = selectedSlots.Count,
            Slots = selectedSlots.ToDictionary(s => s.SlotNumber, s => s)
        };

        var json = JsonSerializer.Serialize(exportData, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task ImportSlotsAsync(string filePath, bool overwrite = false)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Dosya bulunamadı.", filePath);

        var json = await File.ReadAllTextAsync(filePath);
        var importData = JsonSerializer.Deserialize<SlotExportData>(json, _jsonOptions);

        if (importData?.Slots == null)
            throw new InvalidOperationException("Geçersiz slot dosyası.");

        foreach (var kvp in importData.Slots)
        {
            var slotNumber = kvp.Key;
            var slot = kvp.Value;

            if (slotNumber < MinSlotNumber || slotNumber > MaxSlotNumber)
                continue;

            if (overwrite || !_slots.ContainsKey(slotNumber))
            {
                slot.SlotNumber = slotNumber;
                _slots[slotNumber] = slot;
                SlotChanged?.Invoke(slotNumber);
            }
        }
    }

    private static void ValidateSlotNumber(int slotNumber)
    {
        if (slotNumber < MinSlotNumber || slotNumber > MaxSlotNumber)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotNumber),
                $"Slot numarası {MinSlotNumber}-{MaxSlotNumber} arasında olmalıdır.");
        }
    }

    public void Clear()
    {
        _slots.Clear();
    }

    public void LoadSlots(Dictionary<int, TabelaSlot> slots)
    {
        _slots.Clear();
        if (slots != null)
        {
            foreach (var kvp in slots)
            {
                if (kvp.Key >= MinSlotNumber && kvp.Key <= MaxSlotNumber)
                    _slots[kvp.Key] = kvp.Value;
            }
        }
    }

    public Dictionary<int, TabelaSlot> GetSlotsDictionary()
    {
        return new Dictionary<int, TabelaSlot>(_slots);
    }
}

/// <summary>
/// Slot export/import için veri yapısı
/// </summary>
public class SlotExportData
{
    public DateTime ExportedAt { get; set; }
    public int SlotCount { get; set; }
    public Dictionary<int, TabelaSlot> Slots { get; set; } = new();
}
