using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for Intermediate Stop collection management
/// Feature: program-ve-ara-durak
/// Property 6: Ara Durak Koleksiyonu Yönetimi
/// Validates: Requirements 4.5, 4.7, 4.8
/// </summary>
public class IntermediateStopPropertyTests
{
    #region Generators

    /// <summary>
    /// Generates valid stop names (Turkish bus stop names)
    /// </summary>
    public static Gen<string> GenStopName()
    {
        return Gen.Elements(
            "Merkez",
            "Otogar",
            "Hastane",
            "Üniversite",
            "Belediye",
            "Stadyum",
            "Terminal",
            "Çarşı",
            "Sanayi",
            "Köprü",
            "Park",
            "Okul"
        );
    }

    /// <summary>
    /// Generates a valid IntermediateStop
    /// </summary>
    public static Gen<IntermediateStop> GenIntermediateStop()
    {
        return from name in GenStopName()
               from order in Gen.Choose(0, 100)
               select new IntermediateStop(order, name);
    }

    /// <summary>
    /// Generates a non-empty list of IntermediateStops
    /// </summary>
    public static Gen<List<IntermediateStop>> GenIntermediateStopList()
    {
        return Gen.ListOf(GenIntermediateStop())
            .Select(stops => stops.ToList())
            .Where(list => list.Count > 0 && list.Count <= 10);
    }

    /// <summary>
    /// Generates a TabelaItem with intermediate stops enabled
    /// </summary>
    public static Gen<TabelaItem> GenTabelaItemWithStops()
    {
        return from stops in GenIntermediateStopList()
               select CreateTabelaItemWithStops(stops);
    }

    private static TabelaItem CreateTabelaItemWithStops(List<IntermediateStop> stops)
    {
        var item = new TabelaItem
        {
            ItemType = TabelaItemType.Text,
            Content = "Test Content"
        };
        item.IntermediateStops.IsEnabled = true;
        foreach (var stop in stops)
        {
            item.IntermediateStops.Stops.Add(stop);
        }
        return item;
    }

    #endregion

    #region Arbitraries

    public class IntermediateStopArbitraries
    {
        public static Arbitrary<IntermediateStop> IntermediateStopArb() =>
            Arb.From(GenIntermediateStop());

        public static Arbitrary<List<IntermediateStop>> IntermediateStopListArb() =>
            Arb.From(GenIntermediateStopList());

        public static Arbitrary<TabelaItem> TabelaItemWithStopsArb() =>
            Arb.From(GenTabelaItemWithStops());
    }

    #endregion

    #region Property 6: Ara Durak Koleksiyonu Yönetimi

    /// <summary>
    /// Property 6.1: Adding a stop increases collection size by 1
    /// For any TabelaItem with N intermediate stops, adding a new stop SHALL result in N+1 stops.
    /// Validates: Requirements 4.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IntermediateStopArbitraries) })]
    public Property AddingStopIncreasesCollectionSizeByOne(TabelaItem item, IntermediateStop newStop)
    {
        var initialCount = item.IntermediateStops.Stops.Count;
        
        item.IntermediateStops.Stops.Add(newStop);
        
        return (item.IntermediateStops.Stops.Count == initialCount + 1).ToProperty();
    }

    /// <summary>
    /// Property 6.2: Removing a stop decreases collection size by 1
    /// For any TabelaItem with N intermediate stops (N > 0), removing a stop SHALL result in N-1 stops.
    /// Validates: Requirements 4.8
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IntermediateStopArbitraries) })]
    public Property RemovingStopDecreasesCollectionSizeByOne(TabelaItem item)
    {
        if (item.IntermediateStops.Stops.Count == 0)
            return true.ToProperty();

        var initialCount = item.IntermediateStops.Stops.Count;
        
        item.IntermediateStops.Stops.RemoveAt(0);
        
        return (item.IntermediateStops.Stops.Count == initialCount - 1).ToProperty();
    }

    /// <summary>
    /// Property 6.3: Reordering preserves all stops
    /// For any TabelaItem with intermediate stops, reordering SHALL preserve all stop names.
    /// Validates: Requirements 4.7
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IntermediateStopArbitraries) })]
    public Property ReorderingPreservesAllStops(TabelaItem item)
    {
        if (item.IntermediateStops.Stops.Count < 2)
            return true.ToProperty();

        // Capture original stop names
        var originalNames = item.IntermediateStops.Stops
            .Select(s => s.StopName)
            .OrderBy(n => n)
            .ToList();

        // Simulate reordering by moving first item to last position
        var firstStop = item.IntermediateStops.Stops[0];
        item.IntermediateStops.Stops.RemoveAt(0);
        item.IntermediateStops.Stops.Add(firstStop);

        // Verify all names are still present
        var newNames = item.IntermediateStops.Stops
            .Select(s => s.StopName)
            .OrderBy(n => n)
            .ToList();

        return originalNames.SequenceEqual(newNames).ToProperty();
    }

    /// <summary>
    /// Property 6.4: Added stop is present in collection
    /// For any TabelaItem, after adding a stop, that stop SHALL be present in the collection.
    /// Validates: Requirements 4.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IntermediateStopArbitraries) })]
    public Property AddedStopIsPresentInCollection(IntermediateStop newStop)
    {
        var item = new TabelaItem { ItemType = TabelaItemType.Text };
        item.IntermediateStops.IsEnabled = true;
        
        item.IntermediateStops.Stops.Add(newStop);
        
        return item.IntermediateStops.Stops.Contains(newStop).ToProperty();
    }

    /// <summary>
    /// Property 6.5: HasIntermediateStops is true when enabled and has stops
    /// For any TabelaItem with IsEnabled=true and at least one stop, HasIntermediateStops SHALL be true.
    /// Validates: Requirements 4.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IntermediateStopArbitraries) })]
    public Property HasIntermediateStopsIsTrueWhenEnabledAndHasStops(TabelaItem item)
    {
        // Item is generated with IsEnabled=true and at least one stop
        return item.HasIntermediateStops.ToProperty();
    }

    /// <summary>
    /// Property 6.6: HasIntermediateStops is false when disabled
    /// For any TabelaItem with IsEnabled=false, HasIntermediateStops SHALL be false regardless of stop count.
    /// Validates: Requirements 4.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IntermediateStopArbitraries) })]
    public Property HasIntermediateStopsIsFalseWhenDisabled(List<IntermediateStop> stops)
    {
        var item = new TabelaItem { ItemType = TabelaItemType.Text };
        item.IntermediateStops.IsEnabled = false;
        
        foreach (var stop in stops)
        {
            item.IntermediateStops.Stops.Add(stop);
        }
        
        return (!item.HasIntermediateStops).ToProperty();
    }

    /// <summary>
    /// Property 6.7: HasIntermediateStops is false when no stops
    /// For any TabelaItem with IsEnabled=true but no stops, HasIntermediateStops SHALL be false.
    /// Validates: Requirements 4.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property HasIntermediateStopsIsFalseWhenNoStops()
    {
        var item = new TabelaItem { ItemType = TabelaItemType.Text };
        item.IntermediateStops.IsEnabled = true;
        // No stops added
        
        return (!item.HasIntermediateStops).ToProperty();
    }

    /// <summary>
    /// Property 6.8: Collection count is non-negative
    /// For any TabelaItem, the intermediate stops collection count SHALL always be >= 0.
    /// Validates: Requirements 4.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IntermediateStopArbitraries) })]
    public Property CollectionCountIsNonNegative(TabelaItem item)
    {
        return (item.IntermediateStops.Stops.Count >= 0).ToProperty();
    }

    #endregion
}
