using System.Collections.Generic;
using LEDTabelam.Models;

namespace LEDTabelam.Services;

/// <summary>
/// Zone (bölge) yönetimi servisi interface'i
/// Requirements: 17.1, 17.4
/// </summary>
public interface IZoneManager
{
    /// <summary>
    /// Tüm zone'ları döndürür
    /// </summary>
    /// <returns>Zone listesi</returns>
    List<Zone> GetZones();

    /// <summary>
    /// Yeni zone ekler
    /// </summary>
    /// <param name="zone">Eklenecek zone</param>
    void AddZone(Zone zone);

    /// <summary>
    /// Belirtilen indeksteki zone'u siler
    /// </summary>
    /// <param name="index">Silinecek zone indeksi</param>
    /// <returns>Silme başarılı ise true</returns>
    bool RemoveZone(int index);

    /// <summary>
    /// Belirtilen indeksteki zone'un genişliğini günceller
    /// </summary>
    /// <param name="index">Zone indeksi</param>
    /// <param name="widthPercent">Yeni genişlik yüzdesi</param>
    void UpdateZoneWidth(int index, double widthPercent);

    /// <summary>
    /// Tüm zone genişliklerini normalize eder (toplam %100 olacak şekilde)
    /// </summary>
    void NormalizeZoneWidths();

    /// <summary>
    /// Belirtilen indeksteki zone'u döndürür
    /// </summary>
    /// <param name="index">Zone indeksi</param>
    /// <returns>Zone veya null</returns>
    Zone? GetZone(int index);

    /// <summary>
    /// Zone sayısını döndürür
    /// </summary>
    int ZoneCount { get; }

    /// <summary>
    /// Zone'ları temizler
    /// </summary>
    void Clear();

    /// <summary>
    /// Zone'ları yükler
    /// </summary>
    /// <param name="zones">Yüklenecek zone listesi</param>
    void LoadZones(List<Zone> zones);

    /// <summary>
    /// Zone değişikliklerini bildirir
    /// </summary>
    event System.Action? ZonesChanged;
}
