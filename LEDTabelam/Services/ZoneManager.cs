using System;
using System.Collections.Generic;
using System.Linq;
using LEDTabelam.Models;

namespace LEDTabelam.Services;

/// <summary>
/// Zone (bölge) yönetimi servisi implementasyonu
/// Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 17.6, 17.7
/// </summary>
public class ZoneManager : IZoneManager
{
    private readonly List<Zone> _zones;
    private const double Tolerance = 0.001;

    /// <inheritdoc/>
    public event Action? ZonesChanged;

    public ZoneManager() : this(new List<Zone>())
    {
    }

    public ZoneManager(List<Zone> zones)
    {
        _zones = zones ?? new List<Zone>();
        ReindexZones();
    }

    /// <inheritdoc/>
    public int ZoneCount => _zones.Count;

    /// <inheritdoc/>
    public List<Zone> GetZones()
    {
        return _zones.OrderBy(z => z.Index).ToList();
    }

    /// <inheritdoc/>
    public Zone? GetZone(int index)
    {
        return _zones.FirstOrDefault(z => z.Index == index);
    }

    /// <inheritdoc/>
    public void AddZone(Zone zone)
    {
        if (zone == null)
        {
            throw new ArgumentNullException(nameof(zone));
        }

        // Yeni zone'a sıradaki indeksi ata
        zone.Index = _zones.Count;
        _zones.Add(zone);
        
        // Genişlikleri normalize et
        NormalizeZoneWidths();
        
        ZonesChanged?.Invoke();
    }

    /// <inheritdoc/>
    public bool RemoveZone(int index)
    {
        var zone = _zones.FirstOrDefault(z => z.Index == index);
        if (zone == null)
        {
            return false;
        }

        _zones.Remove(zone);
        ReindexZones();
        
        // Kalan zone'ların genişliklerini normalize et
        if (_zones.Count > 0)
        {
            NormalizeZoneWidths();
        }
        
        ZonesChanged?.Invoke();
        return true;
    }

    /// <inheritdoc/>
    public void UpdateZoneWidth(int index, double widthPercent)
    {
        var zone = _zones.FirstOrDefault(z => z.Index == index);
        if (zone == null)
        {
            throw new ArgumentException($"Zone bulunamadı: {index}", nameof(index));
        }

        if (widthPercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(widthPercent), "Genişlik negatif olamaz.");
        }

        zone.WidthPercent = widthPercent;
        ZonesChanged?.Invoke();
    }

    /// <inheritdoc/>
    public void NormalizeZoneWidths()
    {
        if (_zones.Count == 0)
        {
            return;
        }

        var totalWidth = _zones.Sum(z => z.WidthPercent);
        
        // Toplam sıfır veya çok küçükse, eşit dağıt
        if (totalWidth < Tolerance)
        {
            var equalWidth = 100.0 / _zones.Count;
            foreach (var zone in _zones)
            {
                zone.WidthPercent = equalWidth;
            }
            return;
        }

        // Oransal olarak %100'e normalize et
        var scaleFactor = 100.0 / totalWidth;
        foreach (var zone in _zones)
        {
            zone.WidthPercent = zone.WidthPercent * scaleFactor;
        }

        // Yuvarlama hatalarını düzelt - son zone'a kalan farkı ekle
        var normalizedTotal = _zones.Sum(z => z.WidthPercent);
        if (Math.Abs(normalizedTotal - 100.0) > Tolerance && _zones.Count > 0)
        {
            var lastZone = _zones.OrderBy(z => z.Index).Last();
            lastZone.WidthPercent += (100.0 - normalizedTotal);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _zones.Clear();
        ZonesChanged?.Invoke();
    }

    /// <inheritdoc/>
    public void LoadZones(List<Zone> zones)
    {
        _zones.Clear();
        
        if (zones != null)
        {
            foreach (var zone in zones)
            {
                _zones.Add(zone);
            }
            ReindexZones();
        }
        
        ZonesChanged?.Invoke();
    }

    /// <summary>
    /// Zone indekslerini yeniden sıralar
    /// </summary>
    private void ReindexZones()
    {
        var orderedZones = _zones.OrderBy(z => z.Index).ToList();
        for (int i = 0; i < orderedZones.Count; i++)
        {
            orderedZones[i].Index = i;
        }
    }

    /// <summary>
    /// Varsayılan zone yapılandırması oluşturur (tek zone, %100 genişlik)
    /// </summary>
    public static Zone CreateDefaultZone()
    {
        return new Zone
        {
            Index = 0,
            WidthPercent = 100,
            ContentType = ZoneContentType.Text,
            Content = string.Empty,
            HAlign = HorizontalAlignment.Center,
            VAlign = VerticalAlignment.Center,
            IsScrolling = false,
            ScrollSpeed = 20
        };
    }

    /// <summary>
    /// Tipik 3-zone layout oluşturur (logo, metin, hat numarası)
    /// Requirements: 17.1, 17.2
    /// </summary>
    public static List<Zone> CreateThreeZoneLayout()
    {
        return new List<Zone>
        {
            new Zone
            {
                Index = 0,
                WidthPercent = 15,
                ContentType = ZoneContentType.Image,
                Content = string.Empty,
                HAlign = HorizontalAlignment.Center,
                VAlign = VerticalAlignment.Center
            },
            new Zone
            {
                Index = 1,
                WidthPercent = 70,
                ContentType = ZoneContentType.ScrollingText,
                Content = string.Empty,
                HAlign = HorizontalAlignment.Left,
                VAlign = VerticalAlignment.Center,
                IsScrolling = true,
                ScrollSpeed = 20
            },
            new Zone
            {
                Index = 2,
                WidthPercent = 15,
                ContentType = ZoneContentType.Text,
                Content = string.Empty,
                HAlign = HorizontalAlignment.Center,
                VAlign = VerticalAlignment.Center
            }
        };
    }

    /// <summary>
    /// Zone genişliklerinin toplamının %100 olup olmadığını kontrol eder
    /// </summary>
    public bool IsNormalized()
    {
        if (_zones.Count == 0)
        {
            return true;
        }
        
        var total = _zones.Sum(z => z.WidthPercent);
        return Math.Abs(total - 100.0) < Tolerance;
    }

    /// <summary>
    /// Zone'ları belirtilen indekste böler
    /// </summary>
    /// <param name="index">Bölünecek zone indeksi</param>
    /// <param name="splitPercent">Bölme noktası (0-100 arası, zone genişliği içinde)</param>
    public void SplitZone(int index, double splitPercent)
    {
        var zone = _zones.FirstOrDefault(z => z.Index == index);
        if (zone == null)
        {
            throw new ArgumentException($"Zone bulunamadı: {index}", nameof(index));
        }

        if (splitPercent <= 0 || splitPercent >= 100)
        {
            throw new ArgumentOutOfRangeException(nameof(splitPercent), "Bölme yüzdesi 0-100 arasında olmalıdır.");
        }

        var originalWidth = zone.WidthPercent;
        var leftWidth = originalWidth * (splitPercent / 100.0);
        var rightWidth = originalWidth - leftWidth;

        // Mevcut zone'u sol parça olarak güncelle
        zone.WidthPercent = leftWidth;

        // Sağ parça için yeni zone oluştur
        var newZone = new Zone
        {
            Index = index + 1,
            WidthPercent = rightWidth,
            ContentType = zone.ContentType,
            Content = string.Empty,
            HAlign = zone.HAlign,
            VAlign = zone.VAlign,
            IsScrolling = zone.IsScrolling,
            ScrollSpeed = zone.ScrollSpeed
        };

        // Sonraki zone'ların indekslerini artır
        foreach (var z in _zones.Where(z => z.Index > index))
        {
            z.Index++;
        }

        _zones.Add(newZone);
        ZonesChanged?.Invoke();
    }

    /// <summary>
    /// İki bitişik zone'u birleştirir
    /// </summary>
    /// <param name="leftIndex">Sol zone indeksi</param>
    public void MergeZones(int leftIndex)
    {
        var leftZone = _zones.FirstOrDefault(z => z.Index == leftIndex);
        var rightZone = _zones.FirstOrDefault(z => z.Index == leftIndex + 1);

        if (leftZone == null || rightZone == null)
        {
            throw new ArgumentException("Birleştirilecek zone'lar bulunamadı.", nameof(leftIndex));
        }

        // Sol zone'un genişliğini artır
        leftZone.WidthPercent += rightZone.WidthPercent;

        // Sağ zone'u sil
        _zones.Remove(rightZone);
        ReindexZones();
        
        ZonesChanged?.Invoke();
    }
}
