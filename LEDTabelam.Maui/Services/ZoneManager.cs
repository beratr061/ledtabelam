using System;
using System.Collections.Generic;
using System.Linq;
using LEDTabelam.Maui.Models;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Zone (bölge) yönetimi servisi implementasyonu
/// </summary>
public class ZoneManager : IZoneManager
{
    private readonly List<Zone> _zones;
    private const double Tolerance = 0.001;

    public event Action? ZonesChanged;

    public ZoneManager() : this(new List<Zone>())
    {
    }

    public ZoneManager(List<Zone> zones)
    {
        _zones = zones ?? new List<Zone>();
        ReindexZones();
    }

    public int ZoneCount => _zones.Count;

    public List<Zone> GetZones()
    {
        return _zones.OrderBy(z => z.Index).ToList();
    }

    public Zone? GetZone(int index)
    {
        return _zones.FirstOrDefault(z => z.Index == index);
    }

    public void AddZone(Zone zone)
    {
        if (zone == null)
            throw new ArgumentNullException(nameof(zone));

        zone.Index = _zones.Count;
        _zones.Add(zone);
        NormalizeZoneWidths();
        ZonesChanged?.Invoke();
    }

    public bool RemoveZone(int index)
    {
        var zone = _zones.FirstOrDefault(z => z.Index == index);
        if (zone == null)
            return false;

        _zones.Remove(zone);
        ReindexZones();

        if (_zones.Count > 0)
            NormalizeZoneWidths();

        ZonesChanged?.Invoke();
        return true;
    }

    public void UpdateZoneWidth(int index, double widthPercent)
    {
        var zone = _zones.FirstOrDefault(z => z.Index == index);
        if (zone == null)
            throw new ArgumentException($"Zone bulunamadı: {index}", nameof(index));

        if (widthPercent < 0)
            throw new ArgumentOutOfRangeException(nameof(widthPercent), "Genişlik negatif olamaz.");

        zone.WidthPercent = widthPercent;
        ZonesChanged?.Invoke();
    }

    public void NormalizeZoneWidths()
    {
        if (_zones.Count == 0)
            return;

        var totalWidth = _zones.Sum(z => z.WidthPercent);

        if (totalWidth < Tolerance)
        {
            var equalWidth = 100.0 / _zones.Count;
            foreach (var zone in _zones)
                zone.WidthPercent = equalWidth;
            return;
        }

        var scaleFactor = 100.0 / totalWidth;
        foreach (var zone in _zones)
            zone.WidthPercent = zone.WidthPercent * scaleFactor;

        var normalizedTotal = _zones.Sum(z => z.WidthPercent);
        if (Math.Abs(normalizedTotal - 100.0) > Tolerance && _zones.Count > 0)
        {
            var lastZone = _zones.OrderBy(z => z.Index).Last();
            lastZone.WidthPercent += (100.0 - normalizedTotal);
        }
    }

    public void Clear()
    {
        _zones.Clear();
        ZonesChanged?.Invoke();
    }

    public void LoadZones(List<Zone> zones)
    {
        _zones.Clear();

        if (zones != null)
        {
            foreach (var zone in zones)
                _zones.Add(zone);
            ReindexZones();
        }

        ZonesChanged?.Invoke();
    }

    private void ReindexZones()
    {
        var orderedZones = _zones.OrderBy(z => z.Index).ToList();
        for (int i = 0; i < orderedZones.Count; i++)
            orderedZones[i].Index = i;
    }

    public static Zone CreateDefaultZone()
    {
        return new Zone
        {
            Index = 0,
            WidthPercent = 100,
            ContentType = ZoneContentType.Text,
            Content = string.Empty,
            HAlign = Models.HorizontalAlignment.Center,
            VAlign = Models.VerticalAlignment.Center,
            IsScrolling = false,
            ScrollSpeed = 20
        };
    }

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
                HAlign = Models.HorizontalAlignment.Center,
                VAlign = Models.VerticalAlignment.Center
            },
            new Zone
            {
                Index = 1,
                WidthPercent = 70,
                ContentType = ZoneContentType.ScrollingText,
                Content = string.Empty,
                HAlign = Models.HorizontalAlignment.Left,
                VAlign = Models.VerticalAlignment.Center,
                IsScrolling = true,
                ScrollSpeed = 20
            },
            new Zone
            {
                Index = 2,
                WidthPercent = 15,
                ContentType = ZoneContentType.Text,
                Content = string.Empty,
                HAlign = Models.HorizontalAlignment.Center,
                VAlign = Models.VerticalAlignment.Center
            }
        };
    }

    public bool IsNormalized()
    {
        if (_zones.Count == 0)
            return true;

        var total = _zones.Sum(z => z.WidthPercent);
        return Math.Abs(total - 100.0) < Tolerance;
    }

    public void SplitZone(int index, double splitPercent)
    {
        var zone = _zones.FirstOrDefault(z => z.Index == index);
        if (zone == null)
            throw new ArgumentException($"Zone bulunamadı: {index}", nameof(index));

        if (splitPercent <= 0 || splitPercent >= 100)
            throw new ArgumentOutOfRangeException(nameof(splitPercent), "Bölme yüzdesi 0-100 arasında olmalıdır.");

        var originalWidth = zone.WidthPercent;
        var leftWidth = originalWidth * (splitPercent / 100.0);
        var rightWidth = originalWidth - leftWidth;

        zone.WidthPercent = leftWidth;

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

        foreach (var z in _zones.Where(z => z.Index > index))
            z.Index++;

        _zones.Add(newZone);
        ZonesChanged?.Invoke();
    }

    public void MergeZones(int leftIndex)
    {
        var leftZone = _zones.FirstOrDefault(z => z.Index == leftIndex);
        var rightZone = _zones.FirstOrDefault(z => z.Index == leftIndex + 1);

        if (leftZone == null || rightZone == null)
            throw new ArgumentException("Birleştirilecek zone'lar bulunamadı.", nameof(leftIndex));

        leftZone.WidthPercent += rightZone.WidthPercent;
        _zones.Remove(rightZone);
        ReindexZones();
        ZonesChanged?.Invoke();
    }
}
