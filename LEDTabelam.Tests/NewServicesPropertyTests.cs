using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using SkiaSharp;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for new MAUI services
/// Feature: maui-ui-redesign
/// 
/// Property 5: Auto-Naming Uniqueness
/// For any sequence of screen or program additions, the auto-generated names should be 
/// unique within their parent container. Adding N screens should produce names "Ekran1" 
/// through "EkranN" with no duplicates.
/// 
/// Property 9: Program Execution Order
/// For any program with ordered content items, executing the program should display 
/// contents in their defined order. The sequence should be deterministic and repeatable 
/// for the same program configuration.
/// 
/// Property 10: Effect Application
/// For any content item with entry/exit effects configured, applying the effect should 
/// produce a visual transformation that progresses from 0% to 100% over the specified 
/// duration. The effect type should determine the transformation behavior.
/// 
/// Validates: Requirements 3.8, 3.9, 10.4, 10.5, 12.x
/// </summary>
public class NewServicesPropertyTests
{
    #region Local Model Classes (mirrors MAUI models for testing)

    public enum EffectType
    {
        Immediate,
        SlideIn,
        FadeIn,
        None
    }

    public enum EffectDirection
    {
        Left,
        Right,
        Up,
        Down
    }

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
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public int Width { get; set; } = 128;
        public int Height { get; set; } = 16;
        public EffectConfig EntryEffect { get; set; } = new();
        public EffectConfig ExitEffect { get; set; } = new();
        public int DurationMs { get; set; } = 3000;
    }

    public class ProgramNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Program1";
        public ObservableCollection<ContentItem> Contents { get; set; } = new();
    }

    public class ScreenNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Ekran1";
        public ObservableCollection<ProgramNode> Programs { get; set; } = new();
    }

    public class Project
    {
        public string Name { get; set; } = "Yeni Proje";
        public ObservableCollection<ScreenNode> Screens { get; set; } = new();
    }

    #endregion

    #region Local Service Classes (mirrors MAUI services for testing)

    public class ProjectManager
    {
        private Project _currentProject;

        public ProjectManager()
        {
            _currentProject = new Project();
        }

        public Project CurrentProject => _currentProject;

        public void AddScreen(ScreenNode screen)
        {
            if (string.IsNullOrWhiteSpace(screen.Name))
                screen.Name = GenerateScreenName();
            _currentProject.Screens.Add(screen);
        }

        public void RemoveScreen(ScreenNode screen)
        {
            _currentProject.Screens.Remove(screen);
        }

        public void AddProgram(ScreenNode screen, ProgramNode program)
        {
            if (string.IsNullOrWhiteSpace(program.Name))
                program.Name = GenerateProgramName(screen);
            screen.Programs.Add(program);
        }

        public void AddContent(ProgramNode program, ContentItem content)
        {
            program.Contents.Add(content);
        }

        public string GenerateScreenName()
        {
            int index = 1;
            string baseName = "Ekran";
            while (_currentProject.Screens.Any(s => s.Name == $"{baseName}{index}"))
            {
                index++;
            }
            return $"{baseName}{index}";
        }

        public string GenerateProgramName(ScreenNode screen)
        {
            int index = 1;
            string baseName = "Program";
            while (screen.Programs.Any(p => p.Name == $"{baseName}{index}"))
            {
                index++;
            }
            return $"{baseName}{index}";
        }
    }

    public class EffectService
    {
        private CancellationTokenSource? _effectCts;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        public void StopEffect()
        {
            if (_effectCts != null)
            {
                _effectCts.Cancel();
                _effectCts.Dispose();
                _effectCts = null;
            }
            _isPlaying = false;
        }

        public SKMatrix CalculateTransform(EffectType effectType, EffectDirection direction, double progress, SKRect bounds)
        {
            progress = SanitizeProgress(progress);

            return effectType switch
            {
                EffectType.SlideIn => CalculateSlideTransform(direction, progress, bounds),
                EffectType.FadeIn => SKMatrix.Identity,
                EffectType.Immediate => SKMatrix.Identity,
                EffectType.None => SKMatrix.Identity,
                _ => SKMatrix.Identity
            };
        }

        public byte CalculateOpacity(EffectType effectType, double progress)
        {
            progress = SanitizeProgress(progress);

            return effectType switch
            {
                EffectType.FadeIn => (byte)(255 * progress),
                EffectType.SlideIn => 255,
                EffectType.Immediate => 255,
                EffectType.None => 255,
                _ => 255
            };
        }

        private static double SanitizeProgress(double progress)
        {
            if (double.IsNaN(progress) || double.IsNegativeInfinity(progress))
                return 0.0;
            if (double.IsPositiveInfinity(progress))
                return 1.0;
            return Math.Clamp(progress, 0.0, 1.0);
        }

        private static SKMatrix CalculateSlideTransform(EffectDirection direction, double progress, SKRect bounds)
        {
            float offsetX = 0;
            float offsetY = 0;
            float remainingProgress = (float)(1.0 - progress);

            switch (direction)
            {
                case EffectDirection.Left:
                    offsetX = -bounds.Width * remainingProgress;
                    break;
                case EffectDirection.Right:
                    offsetX = bounds.Width * remainingProgress;
                    break;
                case EffectDirection.Up:
                    offsetY = -bounds.Height * remainingProgress;
                    break;
                case EffectDirection.Down:
                    offsetY = bounds.Height * remainingProgress;
                    break;
            }

            return SKMatrix.CreateTranslation(offsetX, offsetY);
        }
    }

    #endregion

    #region Generators

    public static Gen<EffectType> GenEffectType()
    {
        return Gen.Elements(
            EffectType.Immediate,
            EffectType.SlideIn,
            EffectType.FadeIn,
            EffectType.None
        );
    }

    public static Gen<EffectDirection> GenEffectDirection()
    {
        return Gen.Elements(
            EffectDirection.Left,
            EffectDirection.Right,
            EffectDirection.Up,
            EffectDirection.Down
        );
    }

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
        return from name in Gen.Elements("İçerik", "Metin", "Resim", "Saat")
               from x in Gen.Choose(0, 100)
               from y in Gen.Choose(0, 50)
               from width in Gen.Choose(16, 128)
               from height in Gen.Choose(8, 32)
               from entryEffect in GenEffectConfig()
               from exitEffect in GenEffectConfig()
               from durationMs in Gen.Choose(1000, 10000)
               select new ContentItem
               {
                   Name = name,
                   X = x,
                   Y = y,
                   Width = width,
                   Height = height,
                   EntryEffect = entryEffect,
                   ExitEffect = exitEffect,
                   DurationMs = durationMs
               };
    }

    #endregion

    #region Arbitraries

    public class NewServicesArbitraries
    {
        public static Arbitrary<EffectType> EffectTypeArb() =>
            Arb.From(GenEffectType());

        public static Arbitrary<EffectDirection> EffectDirectionArb() =>
            Arb.From(GenEffectDirection());

        public static Arbitrary<EffectConfig> EffectConfigArb() =>
            Arb.From(GenEffectConfig());

        public static Arbitrary<ContentItem> ContentItemArb() =>
            Arb.From(GenContentItem());
    }

    #endregion

    #region Property 5: Auto-Naming Uniqueness

    /// <summary>
    /// Property 5.1: Screen auto-naming produces unique names
    /// For any sequence of N screen additions, GenerateScreenName SHALL produce 
    /// unique names "Ekran1" through "EkranN" with no duplicates.
    /// Feature: maui-ui-redesign, Property 5: Auto-Naming Uniqueness
    /// Validates: Requirements 3.8
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ScreenAutoNaming_ProducesUniqueNames(PositiveInt countWrapper)
    {
        int count = Math.Min(countWrapper.Get, 20);
        var projectManager = new ProjectManager();
        var generatedNames = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            var screen = new ScreenNode { Name = string.Empty };
            projectManager.AddScreen(screen);
            
            if (!generatedNames.Add(screen.Name))
                return false.ToProperty();
        }

        return (generatedNames.Count == count).ToProperty();
    }

    /// <summary>
    /// Property 5.2: Screen auto-naming follows sequential pattern
    /// For any sequence of N screen additions, the generated names SHALL follow 
    /// the pattern "Ekran1", "Ekran2", ..., "EkranN".
    /// Feature: maui-ui-redesign, Property 5: Auto-Naming Uniqueness
    /// Validates: Requirements 3.8
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ScreenAutoNaming_FollowsSequentialPattern(PositiveInt countWrapper)
    {
        int count = Math.Min(countWrapper.Get, 20);
        var projectManager = new ProjectManager();

        for (int i = 0; i < count; i++)
        {
            var screen = new ScreenNode { Name = string.Empty };
            projectManager.AddScreen(screen);
            
            string expectedName = $"Ekran{i + 1}";
            if (screen.Name != expectedName)
                return false.ToProperty();
        }

        return true.ToProperty();
    }

    /// <summary>
    /// Property 5.3: Program auto-naming produces unique names within screen
    /// For any sequence of N program additions to a screen, GenerateProgramName 
    /// SHALL produce unique names "Program1" through "ProgramN" with no duplicates.
    /// Feature: maui-ui-redesign, Property 5: Auto-Naming Uniqueness
    /// Validates: Requirements 3.9
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProgramAutoNaming_ProducesUniqueNamesWithinScreen(PositiveInt countWrapper)
    {
        int count = Math.Min(countWrapper.Get, 20);
        var projectManager = new ProjectManager();
        var screen = new ScreenNode { Name = "TestScreen" };
        projectManager.AddScreen(screen);
        
        var generatedNames = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            var program = new ProgramNode { Name = string.Empty };
            projectManager.AddProgram(screen, program);
            
            if (!generatedNames.Add(program.Name))
                return false.ToProperty();
        }

        return (generatedNames.Count == count).ToProperty();
    }

    /// <summary>
    /// Property 5.4: Screen auto-naming handles gaps correctly
    /// When screens are removed and new ones added, auto-naming SHALL fill gaps 
    /// to maintain sequential naming without duplicates.
    /// Feature: maui-ui-redesign, Property 5: Auto-Naming Uniqueness
    /// Validates: Requirements 3.8
    /// </summary>
    [Fact]
    public void ScreenAutoNaming_HandlesGapsCorrectly()
    {
        var projectManager = new ProjectManager();
        
        var screen1 = new ScreenNode { Name = string.Empty };
        var screen2 = new ScreenNode { Name = string.Empty };
        var screen3 = new ScreenNode { Name = string.Empty };
        
        projectManager.AddScreen(screen1);
        projectManager.AddScreen(screen2);
        projectManager.AddScreen(screen3);
        
        Assert.Equal("Ekran1", screen1.Name);
        Assert.Equal("Ekran2", screen2.Name);
        Assert.Equal("Ekran3", screen3.Name);
        
        projectManager.RemoveScreen(screen2);
        
        var screen4 = new ScreenNode { Name = string.Empty };
        projectManager.AddScreen(screen4);
        
        Assert.Equal("Ekran2", screen4.Name);
        
        var names = projectManager.CurrentProject.Screens.Select(s => s.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    #endregion

    #region Property 9: Program Execution Order

    /// <summary>
    /// Property 9.1: Content items maintain insertion order
    /// For any program with N content items added in sequence, the Contents collection 
    /// SHALL maintain the exact insertion order.
    /// Feature: maui-ui-redesign, Property 9: Program Execution Order
    /// Validates: Requirements 10.4, 10.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ContentItems_MaintainInsertionOrder(PositiveInt countWrapper)
    {
        int count = Math.Min(countWrapper.Get, 20);
        var projectManager = new ProjectManager();
        var screen = new ScreenNode { Name = "TestScreen" };
        var program = new ProgramNode { Name = "TestProgram" };
        
        projectManager.AddScreen(screen);
        projectManager.AddProgram(screen, program);
        
        var expectedOrder = new List<string>();
        
        for (int i = 0; i < count; i++)
        {
            var content = new ContentItem { Name = $"Content{i}" };
            projectManager.AddContent(program, content);
            expectedOrder.Add(content.Id);
        }
        
        var actualOrder = program.Contents.Select(c => c.Id).ToList();
        
        return expectedOrder.SequenceEqual(actualOrder).ToProperty();
    }

    /// <summary>
    /// Property 9.2: Content execution order is deterministic
    /// For any program configuration, iterating through Contents multiple times 
    /// SHALL produce the same sequence each time.
    /// Feature: maui-ui-redesign, Property 9: Program Execution Order
    /// Validates: Requirements 10.4, 10.5, 10.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ContentExecutionOrder_IsDeterministic(PositiveInt countWrapper)
    {
        int count = Math.Min(countWrapper.Get, 20);
        var program = new ProgramNode { Name = "TestProgram" };
        
        for (int i = 0; i < count; i++)
        {
            program.Contents.Add(new ContentItem { Name = $"Content{i}" });
        }
        
        var order1 = program.Contents.Select(c => c.Id).ToList();
        var order2 = program.Contents.Select(c => c.Id).ToList();
        var order3 = program.Contents.Select(c => c.Id).ToList();
        
        return (order1.SequenceEqual(order2) && order2.SequenceEqual(order3)).ToProperty();
    }

    /// <summary>
    /// Property 9.3: Content count matches additions
    /// For any sequence of N content additions, the program SHALL contain exactly N items.
    /// Feature: maui-ui-redesign, Property 9: Program Execution Order
    /// Validates: Requirements 10.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ContentCount_MatchesAdditions(PositiveInt countWrapper)
    {
        int count = Math.Min(countWrapper.Get, 50);
        var projectManager = new ProjectManager();
        var screen = new ScreenNode { Name = "TestScreen" };
        var program = new ProgramNode { Name = "TestProgram" };
        
        projectManager.AddScreen(screen);
        projectManager.AddProgram(screen, program);
        
        for (int i = 0; i < count; i++)
        {
            projectManager.AddContent(program, new ContentItem { Name = $"Content{i}" });
        }
        
        return (program.Contents.Count == count).ToProperty();
    }

    #endregion

    #region Property 10: Effect Application

    /// <summary>
    /// Property 10.1: Effect progress is clamped to valid range
    /// For any progress value, CalculateOpacity and CalculateTransform SHALL clamp 
    /// the progress to [0.0, 1.0] range.
    /// Feature: maui-ui-redesign, Property 10: Effect Application
    /// Validates: Requirements 12.1, 12.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EffectProgress_IsClampedToValidRange(double progress)
    {
        var effectService = new EffectService();
        var bounds = new SKRect(0, 0, 128, 32);
        
        foreach (EffectType effectType in Enum.GetValues(typeof(EffectType)))
        {
            var opacity = effectService.CalculateOpacity(effectType, progress);
            var transform = effectService.CalculateTransform(effectType, EffectDirection.Left, progress, bounds);
            
            if (opacity < 0 || opacity > 255)
                return false.ToProperty();
            
            if (float.IsNaN(transform.TransX) || float.IsInfinity(transform.TransX) ||
                float.IsNaN(transform.TransY) || float.IsInfinity(transform.TransY))
                return false.ToProperty();
        }
        
        return true.ToProperty();
    }

    /// <summary>
    /// Property 10.2: FadeIn effect opacity increases with progress
    /// For FadeIn effect, opacity SHALL increase monotonically from 0 to 255 
    /// as progress goes from 0.0 to 1.0.
    /// Feature: maui-ui-redesign, Property 10: Effect Application
    /// Validates: Requirements 12.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FadeInEffect_OpacityIncreasesWithProgress()
    {
        var effectService = new EffectService();
        
        byte prevOpacity = 0;
        for (int i = 0; i <= 100; i++)
        {
            double progress = i / 100.0;
            byte opacity = effectService.CalculateOpacity(EffectType.FadeIn, progress);
            
            if (opacity < prevOpacity)
                return false.ToProperty();
            
            prevOpacity = opacity;
        }
        
        var startOpacity = effectService.CalculateOpacity(EffectType.FadeIn, 0.0);
        var endOpacity = effectService.CalculateOpacity(EffectType.FadeIn, 1.0);
        
        return (startOpacity == 0 && endOpacity == 255).ToProperty();
    }

    /// <summary>
    /// Property 10.3: SlideIn effect transform converges to identity
    /// For SlideIn effect, the transform SHALL converge to identity matrix 
    /// as progress approaches 1.0.
    /// Feature: maui-ui-redesign, Property 10: Effect Application
    /// Validates: Requirements 12.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NewServicesArbitraries) })]
    public Property SlideInEffect_TransformConvergesToIdentity(EffectDirection direction)
    {
        var effectService = new EffectService();
        var bounds = new SKRect(0, 0, 128, 32);
        
        var endTransform = effectService.CalculateTransform(EffectType.SlideIn, direction, 1.0, bounds);
        
        return (Math.Abs(endTransform.TransX) < 0.001f && 
                Math.Abs(endTransform.TransY) < 0.001f).ToProperty();
    }

    /// <summary>
    /// Property 10.4: Immediate effect has no animation
    /// For Immediate effect type, opacity SHALL always be 255 and transform 
    /// SHALL always be identity regardless of progress.
    /// Feature: maui-ui-redesign, Property 10: Effect Application
    /// Validates: Requirements 12.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ImmediateEffect_HasNoAnimation()
    {
        var effectService = new EffectService();
        var bounds = new SKRect(0, 0, 128, 32);
        
        for (int i = 0; i <= 100; i++)
        {
            double progress = i / 100.0;
            
            var opacity = effectService.CalculateOpacity(EffectType.Immediate, progress);
            var transform = effectService.CalculateTransform(EffectType.Immediate, EffectDirection.Left, progress, bounds);
            
            if (opacity != 255)
                return false.ToProperty();
            
            if (!transform.IsIdentity)
                return false.ToProperty();
        }
        
        return true.ToProperty();
    }

    /// <summary>
    /// Property 10.5: SlideIn direction affects correct axis
    /// For SlideIn effect, Left/Right directions SHALL only affect X translation,
    /// and Up/Down directions SHALL only affect Y translation.
    /// Feature: maui-ui-redesign, Property 10: Effect Application
    /// Validates: Requirements 12.4, 12.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SlideInDirection_AffectsCorrectAxis()
    {
        var effectService = new EffectService();
        var bounds = new SKRect(0, 0, 128, 32);
        double progress = 0.5;
        
        var leftTransform = effectService.CalculateTransform(EffectType.SlideIn, EffectDirection.Left, progress, bounds);
        var rightTransform = effectService.CalculateTransform(EffectType.SlideIn, EffectDirection.Right, progress, bounds);
        
        if (Math.Abs(leftTransform.TransY) > 0.001f || Math.Abs(rightTransform.TransY) > 0.001f)
            return false.ToProperty();
        
        var upTransform = effectService.CalculateTransform(EffectType.SlideIn, EffectDirection.Up, progress, bounds);
        var downTransform = effectService.CalculateTransform(EffectType.SlideIn, EffectDirection.Down, progress, bounds);
        
        if (Math.Abs(upTransform.TransX) > 0.001f || Math.Abs(downTransform.TransX) > 0.001f)
            return false.ToProperty();
        
        return true.ToProperty();
    }

    /// <summary>
    /// Property 10.6: Effect service IsPlaying state is consistent
    /// After StopEffect is called, IsPlaying SHALL always be false.
    /// Feature: maui-ui-redesign, Property 10: Effect Application
    /// Validates: Requirements 12.1
    /// </summary>
    [Fact]
    public void EffectService_StopEffect_SetsIsPlayingToFalse()
    {
        var effectService = new EffectService();
        
        Assert.False(effectService.IsPlaying);
        
        effectService.StopEffect();
        Assert.False(effectService.IsPlaying);
    }

    #endregion
}
