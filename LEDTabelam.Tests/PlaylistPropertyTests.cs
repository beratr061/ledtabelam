using System;
using System.Collections.Generic;
using System.Threading;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using LEDTabelam.Services;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for PlaylistManager
/// Feature: led-tabelam
/// Property 12: Playlist Transition Completeness
/// Validates: Requirements 15.x
/// </summary>
public class PlaylistPropertyTests : IDisposable
{
    private readonly PlaylistManager _playlistManager;

    public PlaylistPropertyTests()
    {
        _playlistManager = new PlaylistManager();
    }

    public void Dispose()
    {
        _playlistManager.Dispose();
    }

    #region Generators

    public static Gen<string> GenNonEmptyText()
    {
        return Gen.Elements(
            "Test Message 1",
            "Güzergah Bilgisi",
            "Hat 34A",
            "Metrobüs",
            "Tramvay Hattı",
            "Sakarya Park - Kampüs"
        );
    }

    public static Gen<int> GenDuration()
    {
        return Gen.Choose(1, 10);
    }

    public static Gen<TransitionType> GenTransitionType()
    {
        return Gen.Elements(
            TransitionType.None,
            TransitionType.Fade,
            TransitionType.SlideLeft,
            TransitionType.SlideRight
        );
    }

    public static Gen<PlaylistItem> GenPlaylistItem()
    {
        return from text in GenNonEmptyText()
               from duration in GenDuration()
               from transition in GenTransitionType()
               select new PlaylistItem
               {
                   Text = text,
                   DurationSeconds = duration,
                   Transition = transition
               };
    }

    public static Gen<List<PlaylistItem>> GenPlaylistItems()
    {
        return Gen.ListOf(Gen.Choose(1, 5).SelectMany(_ => GenPlaylistItem()))
            .Select(items => new List<PlaylistItem>(items))
            .Where(list => list.Count > 0);
    }

    #endregion

    #region Arbitraries

    public class PlaylistArbitraries
    {
        public static Arbitrary<PlaylistItem> PlaylistItemArb() =>
            Arb.From(GenPlaylistItem());

        public static Arbitrary<List<PlaylistItem>> PlaylistItemsArb() =>
            Arb.From(GenPlaylistItems());

        public static Arbitrary<TransitionType> TransitionTypeArb() =>
            Arb.From(GenTransitionType());
    }

    #endregion

    #region Property 12: Playlist Transition Completeness

    /// <summary>
    /// Property 12: Playlist Transition Completeness
    /// For any playlist with N items and loop mode enabled, after N transitions 
    /// the display should return to the first item.
    /// Validates: Requirements 15.1, 15.3, 15.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaylistArbitraries) })]
    public Property PlaylistLoopReturnsToFirstItem(List<PlaylistItem> items)
    {
        if (items == null || items.Count == 0)
            return true.ToProperty();

        using var manager = new PlaylistManager();
        manager.IsLoopEnabled = true;

        // Add all items
        foreach (var item in items)
        {
            manager.AddItem(item.Text, item.DurationSeconds, item.Transition);
        }

        var n = manager.Count;
        
        // Start at first item
        manager.GoTo(0);
        
        // Navigate N times (should loop back to first)
        for (int i = 0; i < n; i++)
        {
            manager.Next();
        }

        // Should be back at first item (index 0)
        return (manager.CurrentIndex == 0).ToProperty();
    }

    /// <summary>
    /// Property: Each item is displayed for exactly its specified duration
    /// This is verified by checking the item's DurationSeconds property is preserved
    /// Validates: Requirements 15.2, 15.6
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaylistArbitraries) })]
    public Property ItemDurationIsPreserved(List<PlaylistItem> items)
    {
        if (items == null || items.Count == 0)
            return true.ToProperty();

        using var manager = new PlaylistManager();

        // Add all items with specific durations
        var expectedDurations = new List<int>();
        foreach (var item in items)
        {
            var addedItem = manager.AddItem(item.Text, item.DurationSeconds, item.Transition);
            expectedDurations.Add(item.DurationSeconds);
        }

        // Verify all durations are preserved
        for (int i = 0; i < manager.Count; i++)
        {
            var retrievedItem = manager.GetItem(i);
            if (retrievedItem == null || retrievedItem.DurationSeconds != expectedDurations[i])
                return false.ToProperty();
        }

        return true.ToProperty();
    }

    /// <summary>
    /// Property: Adding items increases count by exactly 1 each time
    /// Validates: Requirements 15.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AddingItemIncreasesCountByOne(NonEmptyString text)
    {
        // Skip whitespace-only strings as PlaylistManager rejects them
        var textValue = text.Get;
        if (string.IsNullOrWhiteSpace(textValue))
            return true.ToProperty();
            
        using var manager = new PlaylistManager();
        
        var initialCount = manager.Count;
        manager.AddItem(textValue);
        
        return (manager.Count == initialCount + 1).ToProperty();
    }

    /// <summary>
    /// Property: Removing items decreases count by exactly 1
    /// Validates: Requirements 15.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaylistArbitraries) })]
    public Property RemovingItemDecreasesCountByOne(List<PlaylistItem> items)
    {
        if (items == null || items.Count == 0)
            return true.ToProperty();

        using var manager = new PlaylistManager();

        foreach (var item in items)
        {
            manager.AddItem(item.Text, item.DurationSeconds, item.Transition);
        }

        var countBefore = manager.Count;
        var removed = manager.RemoveItem(0);

        return (removed && manager.Count == countBefore - 1).ToProperty();
    }

    /// <summary>
    /// Property: Moving items preserves total count
    /// Validates: Requirements 15.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaylistArbitraries) })]
    public Property MovingItemPreservesCount(List<PlaylistItem> items)
    {
        if (items == null || items.Count < 2)
            return true.ToProperty();

        using var manager = new PlaylistManager();

        foreach (var item in items)
        {
            manager.AddItem(item.Text, item.DurationSeconds, item.Transition);
        }

        var countBefore = manager.Count;
        manager.MoveItem(0, manager.Count - 1);

        return (manager.Count == countBefore).ToProperty();
    }

    /// <summary>
    /// Property: Item order is updated correctly after move
    /// Validates: Requirements 15.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaylistArbitraries) })]
    public Property ItemOrderIsUpdatedAfterMove(List<PlaylistItem> items)
    {
        if (items == null || items.Count < 2)
            return true.ToProperty();

        using var manager = new PlaylistManager();

        foreach (var item in items)
        {
            manager.AddItem(item.Text, item.DurationSeconds, item.Transition);
        }

        manager.MoveItem(0, manager.Count - 1);

        // Verify all orders are sequential (1, 2, 3, ...)
        for (int i = 0; i < manager.Count; i++)
        {
            var retrievedItem = manager.GetItem(i);
            if (retrievedItem == null || retrievedItem.Order != i + 1)
                return false.ToProperty();
        }

        return true.ToProperty();
    }

    /// <summary>
    /// Property: Loop mode disabled stops at last item
    /// Validates: Requirements 15.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaylistArbitraries) })]
    public Property LoopDisabledStopsAtLastItem(List<PlaylistItem> items)
    {
        if (items == null || items.Count == 0)
            return true.ToProperty();

        using var manager = new PlaylistManager();
        manager.IsLoopEnabled = false;

        foreach (var item in items)
        {
            manager.AddItem(item.Text, item.DurationSeconds, item.Transition);
        }

        var n = manager.Count;
        manager.GoTo(0);

        // Navigate N+1 times (should stop at last item)
        for (int i = 0; i < n + 1; i++)
        {
            manager.Next();
        }

        // Should be at last item
        return (manager.CurrentIndex == n - 1).ToProperty();
    }

    /// <summary>
    /// Property: Clear removes all items
    /// Validates: Requirements 15.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaylistArbitraries) })]
    public Property ClearRemovesAllItems(List<PlaylistItem> items)
    {
        if (items == null || items.Count == 0)
            return true.ToProperty();

        using var manager = new PlaylistManager();

        foreach (var item in items)
        {
            manager.AddItem(item.Text, item.DurationSeconds, item.Transition);
        }

        manager.Clear();

        return (manager.Count == 0 && manager.CurrentItem == null).ToProperty();
    }

    /// <summary>
    /// Property: GoTo sets correct current index
    /// Validates: Requirements 15.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaylistArbitraries) })]
    public Property GoToSetsCorrectIndex(List<PlaylistItem> items)
    {
        if (items == null || items.Count == 0)
            return true.ToProperty();

        using var manager = new PlaylistManager();

        foreach (var item in items)
        {
            manager.AddItem(item.Text, item.DurationSeconds, item.Transition);
        }

        // Test going to each valid index
        for (int i = 0; i < manager.Count; i++)
        {
            manager.GoTo(i);
            if (manager.CurrentIndex != i)
                return false.ToProperty();
        }

        return true.ToProperty();
    }

    /// <summary>
    /// Property: Previous from first item with loop goes to last
    /// Validates: Requirements 15.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaylistArbitraries) })]
    public Property PreviousFromFirstWithLoopGoesToLast(List<PlaylistItem> items)
    {
        if (items == null || items.Count == 0)
            return true.ToProperty();

        using var manager = new PlaylistManager();
        manager.IsLoopEnabled = true;

        foreach (var item in items)
        {
            manager.AddItem(item.Text, item.DurationSeconds, item.Transition);
        }

        manager.GoTo(0);
        manager.Previous();

        return (manager.CurrentIndex == manager.Count - 1).ToProperty();
    }

    /// <summary>
    /// Property: Transition type is preserved for each item
    /// Validates: Requirements 15.7
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaylistArbitraries) })]
    public Property TransitionTypeIsPreserved(List<PlaylistItem> items)
    {
        if (items == null || items.Count == 0)
            return true.ToProperty();

        using var manager = new PlaylistManager();

        var expectedTransitions = new List<TransitionType>();
        foreach (var item in items)
        {
            manager.AddItem(item.Text, item.DurationSeconds, item.Transition);
            expectedTransitions.Add(item.Transition);
        }

        // Verify all transitions are preserved
        for (int i = 0; i < manager.Count; i++)
        {
            var retrievedItem = manager.GetItem(i);
            if (retrievedItem == null || retrievedItem.Transition != expectedTransitions[i])
                return false.ToProperty();
        }

        return true.ToProperty();
    }

    #endregion
}
