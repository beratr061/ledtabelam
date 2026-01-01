using System;
using System.Collections.Generic;
using LEDTabelam.Maui.Models;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Zone (bölge) yönetimi servisi interface'i
/// </summary>
public interface IZoneManager
{
    /// <summary>
    /// Tüm zone'ları döndürür
    /// </summary>
    List<Zone> GetZones();

    /// <summary>
    /// Yeni zone ekler
    /// </summary>
    void AddZone(Zone zone);

    /// <summary>
    /// Belirtilen indeksteki zone'u siler
    /// </summary>
    bool RemoveZone(int index);

    /// <summary>
    /// Belirtilen indeksteki zone'un genişliğini günceller
    /// </summary>
    void UpdateZoneWidth(int index, double widthPercent);

    /// <summary>
    /// Tüm zone genişliklerini normalize eder
    /// </summary>
    void NormalizeZoneWidths();

    /// <summary>
    /// Belirtilen indeksteki zone'u döndürür
    /// </summary>
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
    void LoadZones(List<Zone> zones);

    /// <summary>
    /// Zone değişikliklerini bildirir
    /// </summary>
    event Action? ZonesChanged;
}
