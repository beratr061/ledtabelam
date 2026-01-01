using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Tabela profili (Metrobüs, Belediye Otobüsü, Tramvay vb.)
/// </summary>
public partial class Profile : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private DisplaySettings _settings = new();

    [ObservableProperty]
    private string _fontName = string.Empty;

    [ObservableProperty]
    private List<Zone> _defaultZones = new();

    [ObservableProperty]
    private Dictionary<int, TabelaSlot> _slots = new();

    [ObservableProperty]
    private DateTime _createdAt = DateTime.UtcNow;

    [ObservableProperty]
    private DateTime _modifiedAt = DateTime.UtcNow;

    [ObservableProperty]
    private ObservableCollection<TabelaProgram> _programs = new();

    public TabelaSlot? GetSlot(int slotNumber)
    {
        return Slots.TryGetValue(slotNumber, out var slot) ? slot : null;
    }

    public void SetSlot(int slotNumber, TabelaSlot slot)
    {
        if (slotNumber < 1 || slotNumber > 999)
            throw new ArgumentOutOfRangeException(nameof(slotNumber), "Slot numarası 1-999 arasında olmalıdır.");

        slot.SlotNumber = slotNumber;
        Slots[slotNumber] = slot;
        ModifiedAt = DateTime.UtcNow;
    }

    public int DefinedSlotCount => Slots.Count(s => s.Value.IsDefined);

    public void EnsureMinimumProgram()
    {
        if (Programs.Count == 0)
        {
            Programs.Add(new TabelaProgram
            {
                Id = 1,
                Name = "Program 1"
            });
        }
    }

    public TabelaProgram? GetProgramById(int id)
    {
        return Programs.FirstOrDefault(p => p.Id == id);
    }

    public TabelaProgram AddProgram(string name = "Yeni Program")
    {
        var newId = Programs.Count > 0 ? Programs.Max(p => p.Id) + 1 : 1;
        var program = new TabelaProgram
        {
            Id = newId,
            Name = name
        };
        Programs.Add(program);
        ModifiedAt = DateTime.UtcNow;
        return program;
    }

    public bool RemoveProgram(TabelaProgram program)
    {
        if (Programs.Count <= 1)
            return false;

        var result = Programs.Remove(program);
        if (result)
            ModifiedAt = DateTime.UtcNow;
        return result;
    }

    public bool RemoveProgramById(int id)
    {
        var program = GetProgramById(id);
        if (program == null)
            return false;
        return RemoveProgram(program);
    }
}
