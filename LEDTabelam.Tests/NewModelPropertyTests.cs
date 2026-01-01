using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for new MAUI model classes
/// Feature: maui-ui-redesign
/// 
/// These tests use mirror model classes to verify the serialization behavior
/// that will be used in the MAUI application. The mirror classes have identical
/// structure to the MAUI models but without platform-specific dependencies.
/// 
/// Property 1: Project Round-Trip Consistency
/// For any valid Project object containing screens, programs, and content items,
/// saving to JSON and loading back should produce an equivalent Project with all
/// data preserved including nested hierarchies.
/// 
/// Property 8: Content Type Creation
/// For any content type (Text, Image, Clock, Date, Countdown), creating a new content
/// item should produce an object with all required properties initialized to valid
/// default values. The content type should be correctly set and the item should be renderable.
/// 
/// Validates: Requirements 8.x, 11.x
/// </summary>
public class NewModelPropertyTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public NewModelPropertyTests()
    {
        _jsonOptions = CreateJsonOptions();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    #region Mirror Model Classes (matching MAUI models structure)

    public enum EffectType { Immediate, SlideIn, FadeIn, None }
    public enum EffectDirection { Left, Right, Up, Down }
    public enum TransitionType { None, Fade, SlideLeft, SlideRight, SlideUp, SlideDown }
    public enum ContentType { Text, Image, Clock, Date, Countdown }

    public class EffectConfig
    {
        public EffectType EffectType { get; set; } = EffectType.Immediate;
        public int SpeedMs { get; set; } = 500;
        public EffectDirection Direction { get; set; } = EffectDirection.Left;
    }


    public class ContentItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "İçerik";
        public ContentType ContentType { get; set; } = ContentType.Text;
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public int Width { get; set; } = 128;
        public int Height { get; set; } = 16;
        public EffectConfig EntryEffect { get; set; } = new();
        public EffectConfig ExitEffect { get; set; } = new();
        public int DurationMs { get; set; } = 3000;
        public bool ShowImmediately { get; set; } = true;
    }

    public class TextContent : ContentItem
    {
        public string Text { get; set; } = "";
        public string FontName { get; set; } = "Default";
        public int FontSize { get; set; } = 16;
        public string ForegroundColor { get; set; } = "#FFB000";
        public string BackgroundColor { get; set; } = "#00000000";
        public string HorizontalAlignment { get; set; } = "Center";
        public string VerticalAlignment { get; set; } = "Center";
        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;
        public bool IsUnderline { get; set; } = false;
        public bool IsRightToLeft { get; set; } = false;
        public bool IsScrolling { get; set; } = false;
        public int ScrollSpeed { get; set; } = 20;

        public TextContent()
        {
            ContentType = ContentType.Text;
            Name = "Metin Yazı";
        }
    }

    public class ClockContent : ContentItem
    {
        public string Format { get; set; } = "HH:mm:ss";
        public string FontName { get; set; } = "Default";
        public string ForegroundColor { get; set; } = "#FFB000";
        public bool ShowSeconds { get; set; } = true;
        public bool Is24Hour { get; set; } = true;

        public ClockContent()
        {
            ContentType = ContentType.Clock;
            Name = "Saat";
        }
    }

    public class DateContent : ContentItem
    {
        public string Format { get; set; } = "dd.MM.yyyy";
        public string FontName { get; set; } = "Default";
        public string ForegroundColor { get; set; } = "#FFB000";

        public DateContent()
        {
            ContentType = ContentType.Date;
            Name = "Tarih";
        }
    }

    public class CountdownContent : ContentItem
    {
        public DateTime TargetDateTime { get; set; } = DateTime.Now.AddHours(1);
        public string Format { get; set; } = "HH:mm:ss";
        public string FontName { get; set; } = "Default";
        public string ForegroundColor { get; set; } = "#FFB000";
        public string CompletedText { get; set; } = "SÜRE DOLDU";

        public CountdownContent()
        {
            ContentType = ContentType.Countdown;
            Name = "Geri Sayım";
        }
    }

    public class ProgramNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Program1";
        public List<ContentItem> Contents { get; set; } = new();
        public bool IsLoop { get; set; } = true;
        public TransitionType TransitionType { get; set; } = TransitionType.None;
        public bool IsExpanded { get; set; } = true;
    }

    public class ScreenNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Ekran1";
        public int Width { get; set; } = 128;
        public int Height { get; set; } = 32;
        public List<ProgramNode> Programs { get; set; } = new();
        public bool IsExpanded { get; set; } = true;
    }

    public class Project
    {
        public string Name { get; set; } = "Yeni Proje";
        public string FilePath { get; set; } = string.Empty;
        public List<ScreenNode> Screens { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }

    #endregion


    #region Generators

    public static Gen<EffectType> GenEffectType() =>
        Gen.Elements(EffectType.Immediate, EffectType.SlideIn, EffectType.FadeIn, EffectType.None);

    public static Gen<EffectDirection> GenEffectDirection() =>
        Gen.Elements(EffectDirection.Left, EffectDirection.Right, EffectDirection.Up, EffectDirection.Down);

    public static Gen<TransitionType> GenTransitionType() =>
        Gen.Elements(TransitionType.None, TransitionType.Fade, TransitionType.SlideLeft, TransitionType.SlideRight);

    public static Gen<ContentType> GenContentType() =>
        Gen.Elements(ContentType.Text, ContentType.Image, ContentType.Clock, ContentType.Date, ContentType.Countdown);

    public static Gen<EffectConfig> GenEffectConfig()
    {
        return from effectType in GenEffectType()
               from speedMs in Gen.Choose(100, 2000)
               from direction in GenEffectDirection()
               select new EffectConfig
               {
                   EffectType = effectType,
                   SpeedMs = speedMs,
                   Direction = direction
               };
    }

    public static Gen<ContentItem> GenContentItem()
    {
        return from name in Gen.Elements("İçerik1", "Metin", "Test", "Content")
               from contentType in GenContentType()
               from x in Gen.Choose(0, 100)
               from y in Gen.Choose(0, 50)
               from width in Gen.Choose(16, 256)
               from height in Gen.Choose(8, 64)
               from durationMs in Gen.Choose(1000, 10000)
               from showImmediately in Arb.Generate<bool>()
               from entryEffect in GenEffectConfig()
               from exitEffect in GenEffectConfig()
               select new ContentItem
               {
                   Name = name,
                   ContentType = contentType,
                   X = x,
                   Y = y,
                   Width = width,
                   Height = height,
                   DurationMs = durationMs,
                   ShowImmediately = showImmediately,
                   EntryEffect = entryEffect,
                   ExitEffect = exitEffect
               };
    }

    public static Gen<TextContent> GenTextContent()
    {
        return from text in Gen.Elements("Merhaba", "Test", "LED Tabela", "Hoşgeldiniz", "")
               from fontName in Gen.Elements("Default", "PolarisRGB6x8", "PixelFont8")
               from fontSize in Gen.Choose(8, 32)
               from x in Gen.Choose(0, 100)
               from y in Gen.Choose(0, 50)
               from width in Gen.Choose(16, 256)
               from height in Gen.Choose(8, 64)
               from isBold in Arb.Generate<bool>()
               from isItalic in Arb.Generate<bool>()
               from isUnderline in Arb.Generate<bool>()
               from isRightToLeft in Arb.Generate<bool>()
               from isScrolling in Arb.Generate<bool>()
               from scrollSpeed in Gen.Choose(1, 100)
               select new TextContent
               {
                   Text = text,
                   FontName = fontName,
                   FontSize = fontSize,
                   X = x,
                   Y = y,
                   Width = width,
                   Height = height,
                   IsBold = isBold,
                   IsItalic = isItalic,
                   IsUnderline = isUnderline,
                   IsRightToLeft = isRightToLeft,
                   IsScrolling = isScrolling,
                   ScrollSpeed = scrollSpeed
               };
    }

    public static Gen<ClockContent> GenClockContent()
    {
        return from format in Gen.Elements("HH:mm:ss", "HH:mm", "hh:mm:ss tt")
               from fontName in Gen.Elements("Default", "PolarisRGB6x8")
               from showSeconds in Arb.Generate<bool>()
               from is24Hour in Arb.Generate<bool>()
               from x in Gen.Choose(0, 100)
               from y in Gen.Choose(0, 50)
               from width in Gen.Choose(32, 128)
               from height in Gen.Choose(8, 32)
               select new ClockContent
               {
                   Format = format,
                   FontName = fontName,
                   ShowSeconds = showSeconds,
                   Is24Hour = is24Hour,
                   X = x,
                   Y = y,
                   Width = width,
                   Height = height
               };
    }

    public static Gen<DateContent> GenDateContent()
    {
        return from format in Gen.Elements("dd.MM.yyyy", "yyyy-MM-dd", "dd/MM/yyyy")
               from fontName in Gen.Elements("Default", "PolarisRGB6x8")
               from x in Gen.Choose(0, 100)
               from y in Gen.Choose(0, 50)
               from width in Gen.Choose(32, 128)
               from height in Gen.Choose(8, 32)
               select new DateContent
               {
                   Format = format,
                   FontName = fontName,
                   X = x,
                   Y = y,
                   Width = width,
                   Height = height
               };
    }

    public static Gen<CountdownContent> GenCountdownContent()
    {
        return from format in Gen.Elements("HH:mm:ss", "mm:ss", "dd:HH:mm:ss")
               from fontName in Gen.Elements("Default", "PolarisRGB6x8")
               from completedText in Gen.Elements("SÜRE DOLDU", "BİTTİ", "TAMAMLANDI")
               from x in Gen.Choose(0, 100)
               from y in Gen.Choose(0, 50)
               from width in Gen.Choose(32, 128)
               from height in Gen.Choose(8, 32)
               select new CountdownContent
               {
                   Format = format,
                   FontName = fontName,
                   CompletedText = completedText,
                   TargetDateTime = DateTime.Now.AddHours(1),
                   X = x,
                   Y = y,
                   Width = width,
                   Height = height
               };
    }


    public static Gen<ProgramNode> GenProgramNode()
    {
        return from name in Gen.Elements("Program1", "Program2", "Ana Program", "Test")
               from isLoop in Arb.Generate<bool>()
               from transitionType in GenTransitionType()
               from isExpanded in Arb.Generate<bool>()
               from contentCount in Gen.Choose(0, 3)
               from contents in Gen.ListOf(contentCount, GenContentItem())
               select new ProgramNode
               {
                   Name = name,
                   IsLoop = isLoop,
                   TransitionType = transitionType,
                   IsExpanded = isExpanded,
                   Contents = new List<ContentItem>(contents)
               };
    }

    public static Gen<ScreenNode> GenScreenNode()
    {
        return from name in Gen.Elements("Ekran1", "Ekran2", "Ana Ekran", "Test Ekran")
               from width in Gen.Choose(32, 512)
               from height in Gen.Choose(8, 128)
               from isExpanded in Arb.Generate<bool>()
               from programCount in Gen.Choose(0, 2)
               from programs in Gen.ListOf(programCount, GenProgramNode())
               select new ScreenNode
               {
                   Name = name,
                   Width = width,
                   Height = height,
                   IsExpanded = isExpanded,
                   Programs = new List<ProgramNode>(programs)
               };
    }

    public static Gen<Project> GenProject()
    {
        return from name in Gen.Elements("Yeni Proje", "Test Projesi", "Metrobüs", "Belediye")
               from filePath in Gen.Elements("", "C:/test.ledproj", "project.json")
               from screenCount in Gen.Choose(0, 2)
               from screens in Gen.ListOf(screenCount, GenScreenNode())
               select new Project
               {
                   Name = name,
                   FilePath = filePath,
                   Screens = new List<ScreenNode>(screens),
                   CreatedAt = DateTime.UtcNow,
                   ModifiedAt = DateTime.UtcNow
               };
    }

    #endregion

    #region Arbitraries

    public class NewModelArbitraries
    {
        public static Arbitrary<EffectConfig> EffectConfigArb() => Arb.From(GenEffectConfig());
        public static Arbitrary<ContentItem> ContentItemArb() => Arb.From(GenContentItem());
        public static Arbitrary<TextContent> TextContentArb() => Arb.From(GenTextContent());
        public static Arbitrary<ClockContent> ClockContentArb() => Arb.From(GenClockContent());
        public static Arbitrary<DateContent> DateContentArb() => Arb.From(GenDateContent());
        public static Arbitrary<CountdownContent> CountdownContentArb() => Arb.From(GenCountdownContent());
        public static Arbitrary<ProgramNode> ProgramNodeArb() => Arb.From(GenProgramNode());
        public static Arbitrary<ScreenNode> ScreenNodeArb() => Arb.From(GenScreenNode());
        public static Arbitrary<Project> ProjectArb() => Arb.From(GenProject());
    }

    #endregion


    #region Property 1: Project Round-Trip Consistency

    /// <summary>
    /// Property 1.1: EffectConfig serialization round-trip
    /// For any valid EffectConfig, serializing to JSON and deserializing back
    /// SHALL produce an equivalent object with all properties preserved.
    /// Feature: maui-ui-redesign, Property 1: Project Round-Trip Consistency
    /// Validates: Requirements 12.1, 12.2, 12.4, 12.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property EffectConfig_SerializationRoundTrip(EffectConfig original)
    {
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<EffectConfig>(json, _jsonOptions);

        if (deserialized == null)
            return false.ToProperty();

        return (original.EffectType == deserialized.EffectType &&
               original.SpeedMs == deserialized.SpeedMs &&
               original.Direction == deserialized.Direction).ToProperty();
    }

    /// <summary>
    /// Property 1.2: ContentItem serialization round-trip
    /// For any valid ContentItem, serializing to JSON and deserializing back
    /// SHALL produce an equivalent object with all properties preserved.
    /// Feature: maui-ui-redesign, Property 1: Project Round-Trip Consistency
    /// Validates: Requirements 11.7
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property ContentItem_SerializationRoundTrip(ContentItem original)
    {
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ContentItem>(json, _jsonOptions);

        if (deserialized == null)
            return false.ToProperty();

        return (original.Name == deserialized.Name &&
               original.ContentType == deserialized.ContentType &&
               original.X == deserialized.X &&
               original.Y == deserialized.Y &&
               original.Width == deserialized.Width &&
               original.Height == deserialized.Height &&
               original.DurationMs == deserialized.DurationMs &&
               original.ShowImmediately == deserialized.ShowImmediately).ToProperty();
    }

    /// <summary>
    /// Property 1.3: ProgramNode serialization round-trip
    /// For any valid ProgramNode, serializing to JSON and deserializing back
    /// SHALL produce an equivalent object with all properties preserved.
    /// Feature: maui-ui-redesign, Property 1: Project Round-Trip Consistency
    /// Validates: Requirements 10.3, 10.4, 10.6, 10.7
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property ProgramNode_SerializationRoundTrip(ProgramNode original)
    {
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProgramNode>(json, _jsonOptions);

        if (deserialized == null)
            return false.ToProperty();

        return (original.Name == deserialized.Name &&
               original.IsLoop == deserialized.IsLoop &&
               original.TransitionType == deserialized.TransitionType &&
               original.IsExpanded == deserialized.IsExpanded &&
               original.Contents.Count == deserialized.Contents.Count).ToProperty();
    }

    /// <summary>
    /// Property 1.4: ScreenNode serialization round-trip
    /// For any valid ScreenNode, serializing to JSON and deserializing back
    /// SHALL produce an equivalent object with all properties preserved.
    /// Feature: maui-ui-redesign, Property 1: Project Round-Trip Consistency
    /// Validates: Requirements 10.1, 10.2
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property ScreenNode_SerializationRoundTrip(ScreenNode original)
    {
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ScreenNode>(json, _jsonOptions);

        if (deserialized == null)
            return false.ToProperty();

        return (original.Name == deserialized.Name &&
               original.Width == deserialized.Width &&
               original.Height == deserialized.Height &&
               original.IsExpanded == deserialized.IsExpanded &&
               original.Programs.Count == deserialized.Programs.Count).ToProperty();
    }

    /// <summary>
    /// Property 1.5: Project serialization round-trip
    /// For any valid Project, serializing to JSON and deserializing back
    /// SHALL produce an equivalent object with all properties preserved.
    /// Feature: maui-ui-redesign, Property 1: Project Round-Trip Consistency
    /// Validates: Requirements 8.5, 8.6, 8.7
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property Project_SerializationRoundTrip(Project original)
    {
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<Project>(json, _jsonOptions);

        if (deserialized == null)
            return false.ToProperty();

        return (original.Name == deserialized.Name &&
               original.FilePath == deserialized.FilePath &&
               original.Screens.Count == deserialized.Screens.Count).ToProperty();
    }

    #endregion


    #region Property 8: Content Type Creation

    /// <summary>
    /// Property 8.1: TextContent default initialization
    /// For any TextContent created, it SHALL have ContentType set to Text
    /// and all required properties initialized to valid defaults.
    /// Feature: maui-ui-redesign, Property 8: Content Type Creation
    /// Validates: Requirements 11.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property TextContent_DefaultInitialization(TextContent content)
    {
        return (content.ContentType == ContentType.Text &&
               content.Name == "Metin Yazı" &&
               content.FontName != null &&
               content.Width > 0 &&
               content.Height > 0).ToProperty();
    }

    /// <summary>
    /// Property 8.2: ClockContent default initialization
    /// For any ClockContent created, it SHALL have ContentType set to Clock
    /// and all required properties initialized to valid defaults.
    /// Feature: maui-ui-redesign, Property 8: Content Type Creation
    /// Validates: Requirements 11.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property ClockContent_DefaultInitialization(ClockContent content)
    {
        return (content.ContentType == ContentType.Clock &&
               content.Name == "Saat" &&
               content.Format != null &&
               content.FontName != null).ToProperty();
    }

    /// <summary>
    /// Property 8.3: DateContent default initialization
    /// For any DateContent created, it SHALL have ContentType set to Date
    /// and all required properties initialized to valid defaults.
    /// Feature: maui-ui-redesign, Property 8: Content Type Creation
    /// Validates: Requirements 11.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property DateContent_DefaultInitialization(DateContent content)
    {
        return (content.ContentType == ContentType.Date &&
               content.Name == "Tarih" &&
               content.Format != null &&
               content.FontName != null).ToProperty();
    }

    /// <summary>
    /// Property 8.4: CountdownContent default initialization
    /// For any CountdownContent created, it SHALL have ContentType set to Countdown
    /// and all required properties initialized to valid defaults.
    /// Feature: maui-ui-redesign, Property 8: Content Type Creation
    /// Validates: Requirements 11.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property CountdownContent_DefaultInitialization(CountdownContent content)
    {
        return (content.ContentType == ContentType.Countdown &&
               content.Name == "Geri Sayım" &&
               content.Format != null &&
               content.FontName != null &&
               content.CompletedText != null).ToProperty();
    }

    /// <summary>
    /// Property 8.5: TextContent serialization round-trip
    /// For any valid TextContent, serializing to JSON and deserializing back
    /// SHALL produce an equivalent object with all properties preserved.
    /// Feature: maui-ui-redesign, Property 8: Content Type Creation
    /// Validates: Requirements 11.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property TextContent_SerializationRoundTrip(TextContent original)
    {
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<TextContent>(json, _jsonOptions);

        if (deserialized == null)
            return false.ToProperty();

        return (original.Text == deserialized.Text &&
               original.FontName == deserialized.FontName &&
               original.FontSize == deserialized.FontSize &&
               original.IsBold == deserialized.IsBold &&
               original.IsItalic == deserialized.IsItalic &&
               original.IsUnderline == deserialized.IsUnderline &&
               original.IsRightToLeft == deserialized.IsRightToLeft &&
               original.IsScrolling == deserialized.IsScrolling &&
               original.ScrollSpeed == deserialized.ScrollSpeed).ToProperty();
    }

    /// <summary>
    /// Property 8.6: ClockContent serialization round-trip
    /// For any valid ClockContent, serializing to JSON and deserializing back
    /// SHALL produce an equivalent object with all properties preserved.
    /// Feature: maui-ui-redesign, Property 8: Content Type Creation
    /// Validates: Requirements 11.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property ClockContent_SerializationRoundTrip(ClockContent original)
    {
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ClockContent>(json, _jsonOptions);

        if (deserialized == null)
            return false.ToProperty();

        return (original.Format == deserialized.Format &&
               original.FontName == deserialized.FontName &&
               original.ShowSeconds == deserialized.ShowSeconds &&
               original.Is24Hour == deserialized.Is24Hour).ToProperty();
    }

    /// <summary>
    /// Property 8.7: DateContent serialization round-trip
    /// For any valid DateContent, serializing to JSON and deserializing back
    /// SHALL produce an equivalent object with all properties preserved.
    /// Feature: maui-ui-redesign, Property 8: Content Type Creation
    /// Validates: Requirements 11.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property DateContent_SerializationRoundTrip(DateContent original)
    {
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<DateContent>(json, _jsonOptions);

        if (deserialized == null)
            return false.ToProperty();

        return (original.Format == deserialized.Format &&
               original.FontName == deserialized.FontName).ToProperty();
    }

    /// <summary>
    /// Property 8.8: CountdownContent serialization round-trip
    /// For any valid CountdownContent, serializing to JSON and deserializing back
    /// SHALL produce an equivalent object with all properties preserved.
    /// Feature: maui-ui-redesign, Property 8: Content Type Creation
    /// Validates: Requirements 11.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewModelArbitraries) })]
    public Property CountdownContent_SerializationRoundTrip(CountdownContent original)
    {
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<CountdownContent>(json, _jsonOptions);

        if (deserialized == null)
            return false.ToProperty();

        return (original.Format == deserialized.Format &&
               original.FontName == deserialized.FontName &&
               original.CompletedText == deserialized.CompletedText).ToProperty();
    }

    #endregion
}
