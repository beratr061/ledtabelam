using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using LEDTabelam.Services;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for ZoneManager
/// Feature: led-tabelam, Property 5: Zone Width Normalization
/// Validates: Requirements 17.1, 17.2, 17.4
/// </summary>
public class ZoneManagerPropertyTests
{
    private const double Tolerance = 0.001;

    #region Generators

    public static Gen<Zone> GenZone()
    {
        return from index in Gen.Choose(0, 10)
               from widthPercent in Gen.Choose(1, 100).Select(x => (double)x)
               from contentType in Gen.Elements(Enum.GetValues<ZoneContentType>())
               from content in Gen.Elements("Test", "Content", "Zone", "Text", "")
               from hAlign in Gen.Elements(Enum.GetValues<HorizontalAlignment>())
               from vAlign in Gen.Elements(Enum.GetValues<VerticalAlignment>())
               from isScrolling in Arb.Generate<bool>()
               from scrollSpeed in Gen.Choose(1, 100)
               select new Zone
               {
                   Index = index,
                   WidthPercent = widthPercent,
                   ContentType = contentType,
                   Content = content,
                   HAlign = hAlign,
                   VAlign = vAlign,
                   IsScrolling = isScrolling,
                   ScrollSpeed = scrollSpeed
               };
    }

    public static Gen<List<Zone>> GenZoneList()
    {
        return from count in Gen.Choose(1, 10)
               from zones in Gen.ListOf(count, GenZone())
               select new List<Zone>(zones);
    }

    public static Gen<List<double>> GenWidthPercentList()
    {
        return from count in Gen.Choose(1, 10)
               from widths in Gen.ListOf(count, Gen.Choose(1, 100).Select(x => (double)x))
               select new List<double>(widths);
    }

    #endregion

    #region Arbitraries

    public class ZoneArbitraries
    {
        public static Arbitrary<Zone> ZoneArb() => Arb.From(GenZone());
        public static Arbitrary<List<Zone>> ZoneListArb() => Arb.From(GenZoneList());
        public static Arbitrary<List<double>> WidthPercentListArb() => Arb.From(GenWidthPercentList());
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Property 5: Zone Width Normalization
    /// For any set of zones with arbitrary width percentages, after normalization 
    /// the sum of all zone widths should equal exactly 100%.
    /// Validates: Requirements 17.1, 17.2, 17.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ZoneArbitraries) })]
    public bool ZoneWidthNormalization_SumEquals100(List<Zone> zones)
    {
        if (zones == null || zones.Count == 0)
            return true; // Empty list is trivially normalized

        var manager = new ZoneManager();
        
        // Add all zones
        foreach (var zone in zones)
        {
            manager.AddZone(zone);
        }

        // Get zones after normalization (AddZone calls NormalizeZoneWidths)
        var normalizedZones = manager.GetZones();
        var totalWidth = normalizedZones.Sum(z => z.WidthPercent);

        return Math.Abs(totalWidth - 100.0) < Tolerance;
    }

    /// <summary>
    /// Property 5: Zone Width Normalization - After removing a zone
    /// Additionally, removing a zone should trigger re-normalization maintaining the 100% total.
    /// Validates: Requirements 17.1, 17.2, 17.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ZoneArbitraries) })]
    public bool ZoneWidthNormalization_AfterRemoval_SumEquals100(List<Zone> zones)
    {
        if (zones == null || zones.Count < 2)
            return true; // Need at least 2 zones to test removal

        var manager = new ZoneManager();
        
        // Add all zones
        foreach (var zone in zones)
        {
            manager.AddZone(zone);
        }

        // Remove the first zone
        manager.RemoveZone(0);

        // Check normalization after removal
        var remainingZones = manager.GetZones();
        if (remainingZones.Count == 0)
            return true;

        var totalWidth = remainingZones.Sum(z => z.WidthPercent);
        return Math.Abs(totalWidth - 100.0) < Tolerance;
    }

    /// <summary>
    /// Property 5: Zone Width Normalization - After adding a zone
    /// Adding a zone should trigger re-normalization maintaining the 100% total.
    /// Validates: Requirements 17.1, 17.2, 17.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ZoneArbitraries) })]
    public bool ZoneWidthNormalization_AfterAddition_SumEquals100(List<Zone> initialZones, Zone newZone)
    {
        if (initialZones == null || initialZones.Count == 0)
            return true;

        var manager = new ZoneManager();
        
        // Add initial zones
        foreach (var zone in initialZones)
        {
            manager.AddZone(zone);
        }

        // Add new zone
        manager.AddZone(newZone);

        // Check normalization after addition
        var allZones = manager.GetZones();
        var totalWidth = allZones.Sum(z => z.WidthPercent);

        return Math.Abs(totalWidth - 100.0) < Tolerance;
    }

    /// <summary>
    /// Property: Zone count is preserved after normalization
    /// Validates: Requirements 17.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ZoneArbitraries) })]
    public bool ZoneCountPreservedAfterNormalization(List<Zone> zones)
    {
        if (zones == null)
            return true;

        var manager = new ZoneManager();
        
        foreach (var zone in zones)
        {
            manager.AddZone(zone);
        }

        return manager.ZoneCount == zones.Count;
    }

    /// <summary>
    /// Property: All zone widths are positive after normalization
    /// Validates: Requirements 17.2
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ZoneArbitraries) })]
    public bool AllZoneWidthsPositiveAfterNormalization(List<Zone> zones)
    {
        if (zones == null || zones.Count == 0)
            return true;

        var manager = new ZoneManager();
        
        foreach (var zone in zones)
        {
            manager.AddZone(zone);
        }

        var normalizedZones = manager.GetZones();
        return normalizedZones.All(z => z.WidthPercent > 0);
    }

    /// <summary>
    /// Property: Zone indices are sequential after operations
    /// Validates: Requirements 17.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ZoneArbitraries) })]
    public bool ZoneIndicesAreSequential(List<Zone> zones)
    {
        if (zones == null || zones.Count == 0)
            return true;

        var manager = new ZoneManager();
        
        foreach (var zone in zones)
        {
            manager.AddZone(zone);
        }

        var orderedZones = manager.GetZones();
        for (int i = 0; i < orderedZones.Count; i++)
        {
            if (orderedZones[i].Index != i)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Property: IsNormalized returns true after NormalizeZoneWidths
    /// Validates: Requirements 17.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ZoneArbitraries) })]
    public bool IsNormalizedReturnsTrueAfterNormalization(List<Zone> zones)
    {
        if (zones == null || zones.Count == 0)
            return true;

        var manager = new ZoneManager();
        
        foreach (var zone in zones)
        {
            manager.AddZone(zone);
        }

        return manager.IsNormalized();
    }

    #endregion
}
