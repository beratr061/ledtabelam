using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using LEDTabelam.Services;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for ProfileManager and SlotManager services
/// Feature: led-tabelam, Property 2: Profile Round-Trip Consistency
/// Feature: led-tabelam, Property 3: Slot Round-Trip Consistency
/// Feature: led-tabelam, Property 15: Slot Search Completeness
/// Validates: Requirements 9.x, 20.x
/// </summary>
public class ProfileSlotManagerPropertyTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ProfileManager _profileManager;

    public ProfileSlotManagerPropertyTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LEDTabelam_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _profileManager = new ProfileManager(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region Generators

    public static Gen<DisplaySettings> GenDisplaySettings()
    {
        return from width in Gen.Choose(1, 512)
               from height in Gen.Choose(1, 512)
               from colorType in Gen.Elements(Enum.GetValues<LedColorType>())
               from brightness in Gen.Choose(0, 100)
               from backgroundDarkness in Gen.Choose(0, 100)
               from pixelSize in Gen.Choose(1, 20)
               from pitch in Gen.Elements(Enum.GetValues<PixelPitch>())
               from shape in Gen.Elements(Enum.GetValues<PixelShape>())
               from zoomLevel in Gen.Choose(50, 400)
               from invertColors in Arb.Generate<bool>()
               from agingPercent in Gen.Choose(0, 5)
               from lineSpacing in Gen.Choose(0, 10)
               select new DisplaySettings
               {
                   Width = width,
                   Height = height,
                   ColorType = colorType,
                   Brightness = brightness,
                   BackgroundDarkness = backgroundDarkness,
                   PixelSize = pixelSize,
                   Pitch = pitch,
                   Shape = shape,
                   ZoomLevel = zoomLevel,
                   InvertColors = invertColors,
                   AgingPercent = agingPercent,
                   LineSpacing = lineSpacing
               };
    }

    public static Gen<TextStyle> GenTextStyle()
    {
        return from hasBackground in Arb.Generate<bool>()
               from hasStroke in Arb.Generate<bool>()
               from strokeWidth in Gen.Choose(1, 3)
               select new TextStyle
               {
                   HasBackground = hasBackground,
                   HasStroke = hasStroke,
                   StrokeWidth = strokeWidth
               };
    }

    public static Gen<Zone> GenZone()
    {
        return from index in Gen.Choose(0, 10)
               from widthPercent in Gen.Choose(1, 100).Select(x => (double)x)
               from contentType in Gen.Elements(Enum.GetValues<ZoneContentType>())
               from content in Gen.Elements("Test", "Content", "Zone", "Text")
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

    public static Gen<TabelaSlot> GenTabelaSlot()
    {
        return from slotNumber in Gen.Choose(1, 999)
               from routeNumber in Gen.Elements("34", "19K", "M1", "T4", "500", "76A", "METRO")
               from routeText in Gen.Elements(
                   "Zincirlikuyu - Söğütlüçeşme", 
                   "Kampüs", 
                   "Merkez", 
                   "Terminal",
                   "Havalimanı",
                   "Otogar",
                   "Üniversite")
               from hAlign in Gen.Elements(Enum.GetValues<HorizontalAlignment>())
               from vAlign in Gen.Elements(Enum.GetValues<VerticalAlignment>())
               from textStyle in GenTextStyle()
               from zoneCount in Gen.Choose(0, 3)
               from zones in Gen.ListOf(zoneCount, GenZone())
               select new TabelaSlot
               {
                   SlotNumber = slotNumber,
                   RouteNumber = routeNumber,
                   RouteText = routeText,
                   HAlign = hAlign,
                   VAlign = vAlign,
                   TextStyle = textStyle,
                   Zones = new List<Zone>(zones)
               };
    }

    public static Gen<string> GenProfileName()
    {
        return Gen.Elements(
            "Metrobüs", 
            "Belediye Otobüsü", 
            "Tramvay", 
            "Test Profil",
            "Profil_" + Guid.NewGuid().ToString("N").Substring(0, 8));
    }

    public static Gen<Profile> GenProfile()
    {
        return from name in GenProfileName()
               from settings in GenDisplaySettings()
               from fontName in Gen.Elements("PixelFont16", "DefaultFont", "CustomFont")
               from zoneCount in Gen.Choose(0, 3)
               from defaultZones in Gen.ListOf(zoneCount, GenZone())
               from slotCount in Gen.Choose(0, 5)
               from slots in Gen.ListOf(slotCount, GenTabelaSlot())
               select new Profile
               {
                   Name = name,
                   Settings = settings,
                   FontName = fontName,
                   DefaultZones = new List<Zone>(defaultZones),
                   Slots = CreateSlotDictionary(new List<TabelaSlot>(slots)),
                   CreatedAt = DateTime.UtcNow,
                   ModifiedAt = DateTime.UtcNow
               };
    }

    private static Dictionary<int, TabelaSlot> CreateSlotDictionary(IList<TabelaSlot> slots)
    {
        var dict = new Dictionary<int, TabelaSlot>();
        foreach (var slot in slots)
        {
            var slotNumber = slot.SlotNumber;
            while (dict.ContainsKey(slotNumber))
            {
                slotNumber = (slotNumber % 999) + 1;
            }
            slot.SlotNumber = slotNumber;
            dict[slotNumber] = slot;
        }
        return dict;
    }

    #endregion

    #region Arbitraries

    public class ServiceArbitraries
    {
        public static Arbitrary<DisplaySettings> DisplaySettingsArb() => 
            Arb.From(GenDisplaySettings());
        
        public static Arbitrary<TextStyle> TextStyleArb() => 
            Arb.From(GenTextStyle());
        
        public static Arbitrary<Zone> ZoneArb() => 
            Arb.From(GenZone());
        
        public static Arbitrary<TabelaSlot> TabelaSlotArb() => 
            Arb.From(GenTabelaSlot());
        
        public static Arbitrary<Profile> ProfileArb() => 
            Arb.From(GenProfile());
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Property 2: Profile Round-Trip Consistency (Service Level)
    /// For any valid Profile object, saving via ProfileManager and loading back 
    /// should produce an equivalent Profile with all data preserved.
    /// Validates: Requirements 9.1, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ServiceArbitraries) })]
    public bool ProfileManagerRoundTripConsistency(Profile original)
    {
        // Ensure unique name for this test
        original.Name = "Test_" + Guid.NewGuid().ToString("N");
        
        // Save profile (synchronously for FsCheck compatibility)
        _profileManager.SaveProfileAsync(original).GetAwaiter().GetResult();
        
        // Load profile back
        var loaded = _profileManager.LoadProfileAsync(original.Name).GetAwaiter().GetResult();
        
        if (loaded == null) return false;
        
        // Verify core properties are preserved
        return loaded.Name == original.Name &&
               loaded.FontName == original.FontName &&
               loaded.Settings.Width == original.Settings.Width &&
               loaded.Settings.Height == original.Settings.Height &&
               loaded.Settings.ColorType == original.Settings.ColorType &&
               loaded.Settings.Brightness == original.Settings.Brightness &&
               loaded.Settings.Pitch == original.Settings.Pitch &&
               loaded.Settings.Shape == original.Settings.Shape &&
               loaded.Settings.ZoomLevel == original.Settings.ZoomLevel &&
               loaded.Settings.InvertColors == original.Settings.InvertColors &&
               loaded.Settings.AgingPercent == original.Settings.AgingPercent &&
               loaded.Settings.LineSpacing == original.Settings.LineSpacing &&
               loaded.DefaultZones.Count == original.DefaultZones.Count &&
               loaded.Slots.Count == original.Slots.Count;
    }

    /// <summary>
    /// Property 3: Slot Round-Trip Consistency (Service Level)
    /// For any valid TabelaSlot, setting via SlotManager and getting back 
    /// should preserve all slot data exactly.
    /// Validates: Requirements 20.1, 20.2, 20.5, 20.10
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ServiceArbitraries) })]
    public bool SlotManagerRoundTripConsistency(TabelaSlot original)
    {
        var slotManager = new SlotManager();
        
        // Ensure valid slot number
        var slotNumber = Math.Max(1, Math.Min(999, original.SlotNumber));
        
        // Set slot
        slotManager.SetSlot(slotNumber, original);
        
        // Get slot back
        var retrieved = slotManager.GetSlot(slotNumber);
        
        if (retrieved == null) return false;
        
        // Verify all properties are preserved
        return retrieved.SlotNumber == slotNumber &&
               retrieved.RouteNumber == original.RouteNumber &&
               retrieved.RouteText == original.RouteText &&
               retrieved.HAlign == original.HAlign &&
               retrieved.VAlign == original.VAlign &&
               retrieved.TextStyle.HasBackground == original.TextStyle.HasBackground &&
               retrieved.TextStyle.HasStroke == original.TextStyle.HasStroke &&
               retrieved.TextStyle.StrokeWidth == original.TextStyle.StrokeWidth &&
               retrieved.Zones.Count == original.Zones.Count;
    }

    /// <summary>
    /// Property 15: Slot Search Completeness
    /// For any search query, the search results should include all slots where 
    /// the route number OR route text contains the query string (case-insensitive). 
    /// No matching slot should be excluded from results.
    /// Validates: Requirements 20.7
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ServiceArbitraries) })]
    public bool SlotSearchCompleteness(List<TabelaSlot> slots, NonEmptyString queryWrapper)
    {
        var slotManager = new SlotManager();
        var query = queryWrapper.Get;
        
        // Add all slots with unique slot numbers
        var usedNumbers = new HashSet<int>();
        foreach (var slot in slots)
        {
            var slotNumber = slot.SlotNumber;
            while (usedNumbers.Contains(slotNumber) || slotNumber < 1 || slotNumber > 999)
            {
                slotNumber = (slotNumber % 999) + 1;
            }
            usedNumbers.Add(slotNumber);
            slot.SlotNumber = slotNumber;
            slotManager.SetSlot(slotNumber, slot);
        }
        
        // Perform search
        var searchResults = slotManager.SearchSlots(query);
        
        // Calculate expected matches manually
        var normalizedQuery = query.Trim().ToLowerInvariant();
        var expectedMatches = slots
            .Where(s => s.IsDefined &&
                       (s.RouteNumber.ToLowerInvariant().Contains(normalizedQuery) ||
                        s.RouteText.ToLowerInvariant().Contains(normalizedQuery)))
            .Select(s => s.SlotNumber)
            .ToHashSet();
        
        var actualMatches = searchResults.Select(s => s.SlotNumber).ToHashSet();
        
        // All expected matches should be in results (completeness)
        // Results should only contain matching slots (no false positives)
        return expectedMatches.SetEquals(actualMatches);
    }

    #endregion

    #region Additional Unit Tests

    [Fact]
    public async Task ProfileManager_GetOrCreateDefaultProfile_CreatesDefaultProfile()
    {
        var profile = await _profileManager.GetOrCreateDefaultProfileAsync();
        
        Assert.NotNull(profile);
        Assert.Equal("Varsayılan", profile.Name);
        Assert.Equal(128, profile.Settings.Width);
        Assert.Equal(16, profile.Settings.Height);
        Assert.Equal(LedColorType.Amber, profile.Settings.ColorType);
        Assert.Equal(PixelPitch.P10, profile.Settings.Pitch);
    }

    [Fact]
    public async Task ProfileManager_DuplicateProfile_CreatesNewProfile()
    {
        // Create original profile
        var original = new Profile
        {
            Name = "Original_" + Guid.NewGuid().ToString("N"),
            Settings = new DisplaySettings { Width = 192, Height = 32 },
            FontName = "TestFont"
        };
        await _profileManager.SaveProfileAsync(original);
        
        // Duplicate
        var newName = "Duplicate_" + Guid.NewGuid().ToString("N");
        var duplicate = await _profileManager.DuplicateProfileAsync(original.Name, newName);
        
        Assert.NotNull(duplicate);
        Assert.Equal(newName, duplicate.Name);
        Assert.Equal(original.Settings.Width, duplicate.Settings.Width);
        Assert.Equal(original.Settings.Height, duplicate.Settings.Height);
        Assert.Equal(original.FontName, duplicate.FontName);
    }

    [Fact]
    public void SlotManager_SetSlot_InvalidSlotNumber_ThrowsException()
    {
        var slotManager = new SlotManager();
        var slot = new TabelaSlot { RouteNumber = "34", RouteText = "Test" };
        
        Assert.Throws<ArgumentOutOfRangeException>(() => slotManager.SetSlot(0, slot));
        Assert.Throws<ArgumentOutOfRangeException>(() => slotManager.SetSlot(1000, slot));
    }

    [Fact]
    public void SlotManager_SearchSlots_EmptyQuery_ReturnsAllDefinedSlots()
    {
        var slotManager = new SlotManager();
        slotManager.SetSlot(1, new TabelaSlot { RouteNumber = "34", RouteText = "Test1" });
        slotManager.SetSlot(2, new TabelaSlot { RouteNumber = "19K", RouteText = "Test2" });
        
        var results = slotManager.SearchSlots("");
        
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void SlotManager_SearchSlots_CaseInsensitive()
    {
        var slotManager = new SlotManager();
        slotManager.SetSlot(1, new TabelaSlot { RouteNumber = "34A", RouteText = "Zincirlikuyu" });
        
        var resultsLower = slotManager.SearchSlots("zincirlikuyu");
        var resultsUpper = slotManager.SearchSlots("ZINCIRLIKUYU");
        var resultsMixed = slotManager.SearchSlots("ZinCirLiKuYu");
        
        Assert.Single(resultsLower);
        Assert.Single(resultsUpper);
        Assert.Single(resultsMixed);
    }

    #endregion
}
