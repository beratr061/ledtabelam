using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using LEDTabelam.Services;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for Profile serialization round-trip
/// Feature: program-ve-ara-durak
/// </summary>
public class SerializationPropertyTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public SerializationPropertyTests()
    {
        _jsonOptions = CreateJsonOptions();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new ColorJsonConverter());
        return options;
    }

    #region Generators

    /// <summary>
    /// Generates valid stop names
    /// </summary>
    public static Gen<string> GenStopName()
    {
        return Gen.Elements(
            "Kadıköy",
            "Üsküdar",
            "Beşiktaş",
            "Taksim",
            "Mecidiyeköy",
            "Levent",
            "Maslak",
            "Şişli",
            "Bakırköy",
            "Ataköy"
        );
    }

    /// <summary>
    /// Generates a valid IntermediateStop
    /// </summary>
    public static Gen<IntermediateStop> GenIntermediateStop(int order)
    {
        return from stopName in GenStopName()
               select new IntermediateStop(order, stopName);
    }

    /// <summary>
    /// Generates a list of IntermediateStops
    /// </summary>
    public static Gen<ObservableCollection<IntermediateStop>> GenIntermediateStopList()
    {
        return Gen.Choose(0, 5).SelectMany(count =>
        {
            if (count == 0)
                return Gen.Constant(new ObservableCollection<IntermediateStop>());

            var stops = new System.Collections.Generic.List<Gen<IntermediateStop>>();
            for (int i = 0; i < count; i++)
            {
                stops.Add(GenIntermediateStop(i));
            }
            return Gen.Sequence(stops).Select(s => new ObservableCollection<IntermediateStop>(s));
        });
    }

    /// <summary>
    /// Generates valid IntermediateStopSettings
    /// </summary>
    public static Gen<IntermediateStopSettings> GenIntermediateStopSettings()
    {
        return from isEnabled in Arb.Generate<bool>()
               from stops in GenIntermediateStopList()
               from duration in Gen.Choose(5, 100).Select(d => d / 10.0) // 0.5 to 10.0
               from animation in Gen.Elements(
                   StopAnimationType.Direct,
                   StopAnimationType.Fade,
                   StopAnimationType.SlideUp,
                   StopAnimationType.SlideDown)
               from animDuration in Gen.Choose(100, 500)
               from autoCalc in Arb.Generate<bool>()
               select new IntermediateStopSettings
               {
                   IsEnabled = isEnabled,
                   Stops = stops,
                   DurationSeconds = duration,
                   Animation = animation,
                   AnimationDurationMs = animDuration,
                   AutoCalculateDuration = autoCalc
               };
    }

    /// <summary>
    /// Generates valid program names
    /// </summary>
    public static Gen<string> GenProgramName()
    {
        return Gen.Elements(
            "Program 1",
            "Program 2",
            "Hat Bilgisi",
            "Bayram Mesajı",
            "Ara Duraklar",
            "Reklam",
            "Duyuru"
        );
    }

    /// <summary>
    /// Generates valid item content
    /// </summary>
    public static Gen<string> GenItemContent()
    {
        return Gen.Elements(
            "KADIKÖY",
            "ÜSKÜDAR",
            "500T",
            "METROBÜS",
            "İYİ YOLCULUKLAR",
            "HOŞGELDİNİZ"
        );
    }

    /// <summary>
    /// Generates a valid TabelaItem with optional IntermediateStops
    /// </summary>
    public static Gen<TabelaItem> GenTabelaItem(int id)
    {
        return from name in GenItemContent()
               from content in GenItemContent()
               from x in Gen.Choose(0, 100)
               from y in Gen.Choose(0, 20)
               from width in Gen.Choose(20, 100)
               from height in Gen.Choose(10, 24)
               from intermediateStops in GenIntermediateStopSettings()
               select new TabelaItem
               {
                   Id = id,
                   Name = name,
                   Content = content,
                   ItemType = TabelaItemType.Text,
                   X = x,
                   Y = y,
                   Width = width,
                   Height = height,
                   IntermediateStops = intermediateStops
               };
    }

    /// <summary>
    /// Generates a list of TabelaItems
    /// </summary>
    public static Gen<ObservableCollection<TabelaItem>> GenTabelaItemList()
    {
        return Gen.Choose(0, 3).SelectMany(count =>
        {
            if (count == 0)
                return Gen.Constant(new ObservableCollection<TabelaItem>());

            var items = new System.Collections.Generic.List<Gen<TabelaItem>>();
            for (int i = 0; i < count; i++)
            {
                items.Add(GenTabelaItem(i + 1));
            }
            return Gen.Sequence(items).Select(i => new ObservableCollection<TabelaItem>(i));
        });
    }

    /// <summary>
    /// Generates a valid TabelaProgram with unique ID
    /// </summary>
    public static Gen<TabelaProgram> GenTabelaProgram(int id)
    {
        return from name in GenProgramName()
               from duration in Gen.Choose(1, 60)
               from transition in Gen.Elements(
                   ProgramTransitionType.Direct,
                   ProgramTransitionType.Fade,
                   ProgramTransitionType.SlideLeft,
                   ProgramTransitionType.SlideRight,
                   ProgramTransitionType.SlideUp,
                   ProgramTransitionType.SlideDown)
               from transitionDuration in Gen.Choose(200, 1000)
               from items in GenTabelaItemList()
               select new TabelaProgram
               {
                   Id = id,
                   Name = name,
                   DurationSeconds = duration,
                   Transition = transition,
                   TransitionDurationMs = transitionDuration,
                   Items = items
               };
    }

    /// <summary>
    /// Generates a list of TabelaPrograms with unique IDs
    /// </summary>
    public static Gen<ObservableCollection<TabelaProgram>> GenTabelaProgramList()
    {
        return Gen.Choose(1, 5).SelectMany(count =>
        {
            var programs = new System.Collections.Generic.List<Gen<TabelaProgram>>();
            for (int i = 1; i <= count; i++)
            {
                programs.Add(GenTabelaProgram(i));
            }
            return Gen.Sequence(programs).Select(p => new ObservableCollection<TabelaProgram>(p));
        });
    }

    /// <summary>
    /// Generates a Profile with programs and intermediate stops
    /// </summary>
    public static Gen<Profile> GenProfileWithProgramsAndStops()
    {
        return from programs in GenTabelaProgramList()
               select CreateProfileWithPrograms(programs);
    }

    private static Profile CreateProfileWithPrograms(ObservableCollection<TabelaProgram> programs)
    {
        return new Profile
        {
            Name = "Test Profile",
            Settings = new DisplaySettings
            {
                PanelWidth = 160,
                PanelHeight = 24,
                ColorType = LedColorType.Amber,
                Pitch = PixelPitch.P10,
                Shape = PixelShape.Round,
                Brightness = 100,
                BackgroundDarkness = 100,
                PixelSize = 8
            },
            FontName = "PixelFont8",
            Programs = programs,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Arbitraries

    public class SerializationArbitraries
    {
        public static Arbitrary<Profile> ProfileWithProgramsAndStopsArb() =>
            Arb.From(GenProfileWithProgramsAndStops());

        public static Arbitrary<TabelaProgram> TabelaProgramArb() =>
            Arb.From(GenTabelaProgram(1));

        public static Arbitrary<IntermediateStopSettings> IntermediateStopSettingsArb() =>
            Arb.From(GenIntermediateStopSettings());
    }

    #endregion

    #region Property 11: Serializasyon Round-Trip

    /// <summary>
    /// Property 11: Serializasyon Round-Trip
    /// For any valid Profile with programs and intermediate stops, serializing to JSON 
    /// and deserializing back SHALL produce an equivalent Profile with all programs, 
    /// items, and stops preserved.
    /// Feature: program-ve-ara-durak, Property 11: Serializasyon Round-Trip
    /// Validates: Requirements 9.3, 9.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SerializationArbitraries) })]
    public Property ProfileSerializationRoundTrip(Profile original)
    {
        // Serialize to JSON
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        
        // Deserialize back
        var deserialized = JsonSerializer.Deserialize<Profile>(json, _jsonOptions);
        
        if (deserialized == null)
            return false.ToProperty();

        // Verify profile properties
        var profileMatch = original.Name == deserialized.Name &&
                          original.FontName == deserialized.FontName &&
                          original.Programs.Count == deserialized.Programs.Count;

        if (!profileMatch)
            return false.ToProperty();

        // Verify each program
        for (int i = 0; i < original.Programs.Count; i++)
        {
            var origProgram = original.Programs[i];
            var deserProgram = deserialized.Programs[i];

            var programMatch = origProgram.Id == deserProgram.Id &&
                              origProgram.Name == deserProgram.Name &&
                              origProgram.DurationSeconds == deserProgram.DurationSeconds &&
                              origProgram.Transition == deserProgram.Transition &&
                              origProgram.TransitionDurationMs == deserProgram.TransitionDurationMs &&
                              origProgram.Items.Count == deserProgram.Items.Count;

            if (!programMatch)
                return false.ToProperty();

            // Verify each item in the program
            for (int j = 0; j < origProgram.Items.Count; j++)
            {
                var origItem = origProgram.Items[j];
                var deserItem = deserProgram.Items[j];

                var itemMatch = origItem.Id == deserItem.Id &&
                               origItem.Name == deserItem.Name &&
                               origItem.Content == deserItem.Content &&
                               origItem.X == deserItem.X &&
                               origItem.Y == deserItem.Y &&
                               origItem.Width == deserItem.Width &&
                               origItem.Height == deserItem.Height;

                if (!itemMatch)
                    return false.ToProperty();

                // Verify IntermediateStopSettings
                var origStops = origItem.IntermediateStops;
                var deserStops = deserItem.IntermediateStops;

                var stopsMatch = origStops.IsEnabled == deserStops.IsEnabled &&
                                origStops.DurationSeconds == deserStops.DurationSeconds &&
                                origStops.Animation == deserStops.Animation &&
                                origStops.AnimationDurationMs == deserStops.AnimationDurationMs &&
                                origStops.AutoCalculateDuration == deserStops.AutoCalculateDuration &&
                                origStops.Stops.Count == deserStops.Stops.Count;

                if (!stopsMatch)
                    return false.ToProperty();

                // Verify each intermediate stop
                for (int k = 0; k < origStops.Stops.Count; k++)
                {
                    var origStop = origStops.Stops[k];
                    var deserStop = deserStops.Stops[k];

                    if (origStop.Order != deserStop.Order || origStop.StopName != deserStop.StopName)
                        return false.ToProperty();
                }
            }
        }

        return true.ToProperty();
    }

    /// <summary>
    /// Property 11.2: TabelaProgram serialization round-trip
    /// For any valid TabelaProgram, serializing to JSON and deserializing back 
    /// SHALL produce an equivalent program.
    /// Feature: program-ve-ara-durak, Property 11: Serializasyon Round-Trip
    /// Validates: Requirements 9.3, 9.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SerializationArbitraries) })]
    public Property TabelaProgramSerializationRoundTrip(TabelaProgram original)
    {
        // Serialize to JSON
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        
        // Deserialize back
        var deserialized = JsonSerializer.Deserialize<TabelaProgram>(json, _jsonOptions);
        
        if (deserialized == null)
            return false.ToProperty();

        // Verify program properties
        return (original.Id == deserialized.Id &&
                original.Name == deserialized.Name &&
                original.DurationSeconds == deserialized.DurationSeconds &&
                original.Transition == deserialized.Transition &&
                original.TransitionDurationMs == deserialized.TransitionDurationMs &&
                original.Items.Count == deserialized.Items.Count).ToProperty();
    }

    /// <summary>
    /// Property 11.3: IntermediateStopSettings serialization round-trip
    /// For any valid IntermediateStopSettings, serializing to JSON and deserializing back 
    /// SHALL produce equivalent settings.
    /// Feature: program-ve-ara-durak, Property 11: Serializasyon Round-Trip
    /// Validates: Requirements 9.3, 9.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SerializationArbitraries) })]
    public Property IntermediateStopSettingsSerializationRoundTrip(IntermediateStopSettings original)
    {
        // Serialize to JSON
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        
        // Deserialize back
        var deserialized = JsonSerializer.Deserialize<IntermediateStopSettings>(json, _jsonOptions);
        
        if (deserialized == null)
            return false.ToProperty();

        // Verify settings properties
        var settingsMatch = original.IsEnabled == deserialized.IsEnabled &&
                           original.DurationSeconds == deserialized.DurationSeconds &&
                           original.Animation == deserialized.Animation &&
                           original.AnimationDurationMs == deserialized.AnimationDurationMs &&
                           original.AutoCalculateDuration == deserialized.AutoCalculateDuration &&
                           original.Stops.Count == deserialized.Stops.Count;

        if (!settingsMatch)
            return false.ToProperty();

        // Verify each stop
        for (int i = 0; i < original.Stops.Count; i++)
        {
            if (original.Stops[i].Order != deserialized.Stops[i].Order ||
                original.Stops[i].StopName != deserialized.Stops[i].StopName)
                return false.ToProperty();
        }

        return true.ToProperty();
    }

    #endregion

    #region Backward Compatibility Tests

    /// <summary>
    /// Test: Loading profile without Programs should create default program
    /// For any profile JSON without Programs field, loading SHALL create a default program.
    /// Feature: program-ve-ara-durak, Property 11: Serializasyon Round-Trip
    /// Validates: Requirements 9.3, 9.4
    /// </summary>
    [Fact]
    public void LoadingProfileWithoutPrograms_CreatesDefaultProgram()
    {
        // Create a JSON without Programs field (simulating old profile format)
        var oldFormatJson = @"{
            ""name"": ""Old Profile"",
            ""fontName"": ""PixelFont8"",
            ""settings"": {
                ""panelWidth"": 160,
                ""panelHeight"": 24
            }
        }";

        var profile = JsonSerializer.Deserialize<Profile>(oldFormatJson, _jsonOptions);
        
        Assert.NotNull(profile);
        
        // EnsureMinimumProgram should be called after loading
        profile!.EnsureMinimumProgram();
        
        Assert.Single(profile.Programs);
        Assert.Equal("Program 1", profile.Programs[0].Name);
    }

    /// <summary>
    /// Test: Loading profile with empty Programs should create default program
    /// For any profile JSON with empty Programs array, loading SHALL create a default program.
    /// Feature: program-ve-ara-durak, Property 11: Serializasyon Round-Trip
    /// Validates: Requirements 9.3, 9.4
    /// </summary>
    [Fact]
    public void LoadingProfileWithEmptyPrograms_CreatesDefaultProgram()
    {
        // Create a JSON with empty Programs array
        var emptyProgramsJson = @"{
            ""name"": ""Empty Programs Profile"",
            ""fontName"": ""PixelFont8"",
            ""programs"": []
        }";

        var profile = JsonSerializer.Deserialize<Profile>(emptyProgramsJson, _jsonOptions);
        
        Assert.NotNull(profile);
        
        // EnsureMinimumProgram should be called after loading
        profile!.EnsureMinimumProgram();
        
        Assert.Single(profile.Programs);
    }

    #endregion
}
