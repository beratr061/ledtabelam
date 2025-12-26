using System.Collections.Generic;
using System.Threading.Tasks;
using LEDTabelam.Models;

namespace LEDTabelam.Services;

/// <summary>
/// Tabela slot yönetimi servisi interface'i
/// Requirements: 20.3, 20.5, 20.7, 20.8
/// </summary>
public interface ISlotManager
{
    /// <summary>
    /// Belirtilen slot numarasındaki slot'u döndürür
    /// </summary>
    /// <param name="slotNumber">Slot numarası (1-999)</param>
    /// <returns>Slot veya null (tanımsız ise)</returns>
    TabelaSlot? GetSlot(int slotNumber);

    /// <summary>
    /// Belirtilen slot numarasına slot atar
    /// </summary>
    /// <param name="slotNumber">Slot numarası (1-999)</param>
    /// <param name="slot">Atanacak slot</param>
    void SetSlot(int slotNumber, TabelaSlot slot);

    /// <summary>
    /// Belirtilen slot'u siler
    /// </summary>
    /// <param name="slotNumber">Silinecek slot numarası</param>
    /// <returns>Silme başarılı ise true</returns>
    bool RemoveSlot(int slotNumber);

    /// <summary>
    /// Slotları arar (hat numarası veya güzergah adına göre)
    /// </summary>
    /// <param name="query">Arama sorgusu</param>
    /// <returns>Eşleşen slotların listesi</returns>
    List<TabelaSlot> SearchSlots(string query);

    /// <summary>
    /// Tüm tanımlı slotları döndürür
    /// </summary>
    /// <returns>Tanımlı slot listesi</returns>
    List<TabelaSlot> GetAllDefinedSlots();

    /// <summary>
    /// Slotları JSON dosyasına dışa aktarır
    /// </summary>
    /// <param name="filePath">Hedef dosya yolu</param>
    Task ExportSlotsAsync(string filePath);

    /// <summary>
    /// Slotları JSON dosyasından içe aktarır
    /// </summary>
    /// <param name="filePath">Kaynak dosya yolu</param>
    /// <param name="overwrite">Mevcut slotların üzerine yazılsın mı</param>
    Task ImportSlotsAsync(string filePath, bool overwrite = false);

    /// <summary>
    /// Belirtilen slotları dışa aktarır
    /// </summary>
    /// <param name="slotNumbers">Dışa aktarılacak slot numaraları</param>
    /// <param name="filePath">Hedef dosya yolu</param>
    Task ExportSelectedSlotsAsync(IEnumerable<int> slotNumbers, string filePath);

    /// <summary>
    /// Tanımlı slot sayısını döndürür
    /// </summary>
    int DefinedSlotCount { get; }

    /// <summary>
    /// Slot değişikliklerini bildirir
    /// </summary>
    event System.Action<int>? SlotChanged;
}
