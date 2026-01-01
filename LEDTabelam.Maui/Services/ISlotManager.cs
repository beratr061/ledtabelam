using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LEDTabelam.Maui.Models;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Tabela slot yönetimi servisi interface'i
/// </summary>
public interface ISlotManager
{
    /// <summary>
    /// Belirtilen slot numarasındaki slot'u döndürür
    /// </summary>
    TabelaSlot? GetSlot(int slotNumber);

    /// <summary>
    /// Belirtilen slot numarasına slot atar
    /// </summary>
    void SetSlot(int slotNumber, TabelaSlot slot);

    /// <summary>
    /// Belirtilen slot'u siler
    /// </summary>
    bool RemoveSlot(int slotNumber);

    /// <summary>
    /// Slotları arar
    /// </summary>
    List<TabelaSlot> SearchSlots(string query);

    /// <summary>
    /// Tüm tanımlı slotları döndürür
    /// </summary>
    List<TabelaSlot> GetAllDefinedSlots();

    /// <summary>
    /// Slotları JSON dosyasına dışa aktarır
    /// </summary>
    Task ExportSlotsAsync(string filePath);

    /// <summary>
    /// Slotları JSON dosyasından içe aktarır
    /// </summary>
    Task ImportSlotsAsync(string filePath, bool overwrite = false);

    /// <summary>
    /// Belirtilen slotları dışa aktarır
    /// </summary>
    Task ExportSelectedSlotsAsync(IEnumerable<int> slotNumbers, string filePath);

    /// <summary>
    /// Tanımlı slot sayısını döndürür
    /// </summary>
    int DefinedSlotCount { get; }

    /// <summary>
    /// Slot değişikliklerini bildirir
    /// </summary>
    event Action<int>? SlotChanged;
}
