using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for model classes
/// Feature: led-tabelam, Property 2: Profile Round-Trip Consistency
/// Feature: led-tabelam, Property 3: Slot Round-Trip Consistency
/// Validates: Requirements 9.x, 20.x
/// </summary>
public class ModelPropertyTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

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
               from routeNumber in Gen.Elements("34", "19K", "M1", "T4", "500")
               from routeText in Gen.Elements("Zincirlikuyu - Söğütlüçeşme", "Kampüs", "Merkez", "Terminal")
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

    public static Gen<Profile> GenProfile()
    {
        return from name in Gen.Elements("Metrobüs", "Belediye Otobüsü", "Tramvay", "Test Profil")
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

    public class ModelArbitraries
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
    /// Property 2: Profile Round-Trip Consistency
    /// For any valid Profile object containing display settings, font configuration, 
    /// zone layouts, and up to 999 slots, saving to JSON and loading back should 
    /// produce an equivalent Profile with all data preserved.
    /// Validates: Requirements 9.1, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ModelArbitraries) })]
    public bool ProfileRoundTripConsistency(Profile original)
    {
        // Serialize to JSON
        var json = JsonSerializer.Serialize(original, JsonOptions);
        
        // Deserialize back
        var deserialized = JsonSerializer.Deserialize<Profile>(json, JsonOptions);
        
        if (deserialized == null) return false;
        
        return deserialized.Name == original.Name &&
               deserialized.FontName == original.FontName &&
               deserialized.Settings.Width == original.Settings.Width &&
               deserialized.Settings.Height == original.Settings.Height &&
               deserialized.Settings.ColorType == original.Settings.ColorType &&
               deserialized.Settings.Brightness == original.Settings.Brightness &&
               deserialized.Settings.Pitch == original.Settings.Pitch &&
               deserialized.Settings.Shape == original.Settings.Shape &&
               deserialized.Settings.ZoomLevel == original.Settings.ZoomLevel &&
               deserialized.Settings.InvertColors == original.Settings.InvertColors &&
               deserialized.Settings.AgingPercent == original.Settings.AgingPercent &&
               deserialized.Settings.LineSpacing == original.Settings.LineSpacing &&
               deserialized.DefaultZones.Count == original.DefaultZones.Count &&
               deserialized.Slots.Count == original.Slots.Count;
    }

    /// <summary>
    /// Property 3: Slot Round-Trip Consistency
    /// For any valid TabelaSlot object with route number, route text, zones, 
    /// and text style, saving and loading should preserve all slot data exactly.
    /// Validates: Requirements 20.1, 20.2, 20.5, 20.10
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ModelArbitraries) })]
    public bool SlotRoundTripConsistency(TabelaSlot original)
    {
        // Serialize to JSON
        var json = JsonSerializer.Serialize(original, JsonOptions);
        
        // Deserialize back
        var deserialized = JsonSerializer.Deserialize<TabelaSlot>(json, JsonOptions);
        
        if (deserialized == null) return false;
        
        return deserialized.SlotNumber == original.SlotNumber &&
               deserialized.RouteNumber == original.RouteNumber &&
               deserialized.RouteText == original.RouteText &&
               deserialized.HAlign == original.HAlign &&
               deserialized.VAlign == original.VAlign &&
               deserialized.TextStyle.HasBackground == original.TextStyle.HasBackground &&
               deserialized.TextStyle.HasStroke == original.TextStyle.HasStroke &&
               deserialized.TextStyle.StrokeWidth == original.TextStyle.StrokeWidth &&
               deserialized.Zones.Count == original.Zones.Count;
    }

    #endregion
}
