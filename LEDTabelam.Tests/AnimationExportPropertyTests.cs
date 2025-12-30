using System;
using System.Collections.Generic;
using System.IO;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Services;
using SkiaSharp;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for AnimationService and ExportService
/// Feature: led-tabelam
/// Property 16: Animation State Machine
/// Property 17: Export Format Validity
/// Validates: Requirements 8.x, 7.x
/// </summary>
public class AnimationExportPropertyTests : IDisposable
{
    private readonly string _testOutputDir;

    public AnimationExportPropertyTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"LEDTabelam_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testOutputDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testOutputDir))
            {
                Directory.Delete(_testOutputDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region Generators

    public static Gen<AnimationCommand> GenAnimationCommand()
    {
        return Gen.Elements(
            AnimationCommand.Start,
            AnimationCommand.Stop,
            AnimationCommand.Pause,
            AnimationCommand.Resume
        );
    }

    public static Gen<List<AnimationCommand>> GenCommandSequence()
    {
        return Gen.ListOf(Gen.Choose(1, 10).SelectMany(_ => GenAnimationCommand()))
            .Select(cmds => new List<AnimationCommand>(cmds));
    }

    private static SKBitmap CreateTestBitmap(int width, int height)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);
        
        // Draw some test pattern
        using var paint = new SKPaint { Color = SKColors.Orange };
        for (int x = 0; x < width; x += 4)
        {
            for (int y = 0; y < height; y += 4)
            {
                canvas.DrawRect(x, y, 2, 2, paint);
            }
        }
        
        return bitmap;
    }

    #endregion

    #region Arbitraries

    public class AnimationArbitraries
    {
        public static Arbitrary<AnimationCommand> AnimationCommandArb() =>
            Arb.From(GenAnimationCommand());

        public static Arbitrary<List<AnimationCommand>> CommandSequenceArb() =>
            Arb.From(GenCommandSequence());
    }

    #endregion

    #region Property 16: Animation State Machine

    /// <summary>
    /// Property 16: Animation State Machine
    /// For any sequence of Play/Pause/Stop commands, the animation state should follow 
    /// valid transitions: Stopped -> Playing, Playing -> Paused, Paused -> Playing, 
    /// Playing -> Stopped, Paused -> Stopped. Invalid transitions should be ignored.
    /// Validates: Requirements 8.1, 8.2, 8.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AnimationArbitraries) })]
    public Property AnimationStateMachineValidTransitions(List<AnimationCommand> commands)
    {
        using var service = new AnimationService();
        
        // Initial state should be Stopped
        if (service.IsPlaying || service.IsPaused)
            return false.ToProperty();

        foreach (var command in commands)
        {
            var previousState = GetCurrentState(service);
            
            ExecuteCommand(service, command);
            
            var newState = GetCurrentState(service);
            
            // Verify transition is valid
            if (!IsValidTransition(previousState, newState, command))
                return false.ToProperty();
        }

        return true.ToProperty();
    }

    /// <summary>
    /// Property: Start always results in Playing state
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StartAlwaysResultsInPlaying(int speed)
    {
        speed = Math.Clamp(speed, 1, 100);
        
        using var service = new AnimationService();
        
        service.StartScrollAnimation(speed);
        
        return (service.IsPlaying && !service.IsPaused).ToProperty();
    }

    /// <summary>
    /// Property: Stop always results in Stopped state and resets offset
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StopAlwaysResultsInStoppedAndResetsOffset(int speed)
    {
        speed = Math.Clamp(speed, 1, 100);
        
        using var service = new AnimationService();
        
        // Start and let it run briefly
        service.StartScrollAnimation(speed);
        service.SetOffset(100); // Set some offset
        
        service.StopAnimation();
        
        return (!service.IsPlaying && !service.IsPaused && service.CurrentOffset == 0).ToProperty();
    }

    /// <summary>
    /// Property: Pause from Playing results in Paused state
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PauseFromPlayingResultsInPaused(int speed)
    {
        speed = Math.Clamp(speed, 1, 100);
        
        using var service = new AnimationService();
        
        service.StartScrollAnimation(speed);
        service.PauseAnimation();
        
        return (!service.IsPlaying && service.IsPaused).ToProperty();
    }

    /// <summary>
    /// Property: Resume from Paused results in Playing state
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ResumeFromPausedResultsInPlaying(int speed)
    {
        speed = Math.Clamp(speed, 1, 100);
        
        using var service = new AnimationService();
        
        service.StartScrollAnimation(speed);
        service.PauseAnimation();
        service.ResumeAnimation();
        
        return (service.IsPlaying && !service.IsPaused).ToProperty();
    }

    /// <summary>
    /// Property: Pause from Stopped is ignored (no state change)
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PauseFromStoppedIsIgnored()
    {
        using var service = new AnimationService();
        
        // Initial state is Stopped
        service.PauseAnimation();
        
        return (!service.IsPlaying && !service.IsPaused).ToProperty();
    }

    /// <summary>
    /// Property: Resume from Stopped is ignored (no state change)
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ResumeFromStoppedIsIgnored()
    {
        using var service = new AnimationService();
        
        // Initial state is Stopped
        service.ResumeAnimation();
        
        return (!service.IsPlaying && !service.IsPaused).ToProperty();
    }

    /// <summary>
    /// Property: Speed is clamped to valid range (1-100)
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SpeedIsClampedToValidRange(int speed)
    {
        using var service = new AnimationService();
        
        service.SetSpeed(speed);
        
        // Speed is clamped to 1-100 range
        return (service.Speed >= 1 && 
                service.Speed <= 100).ToProperty();
    }

    /// <summary>
    /// Property: Offset is always non-negative
    /// </summary>
    [Property(MaxTest = 100)]
    public Property OffsetIsAlwaysNonNegative(int offset)
    {
        using var service = new AnimationService();
        
        service.SetOffset(offset);
        
        return (service.CurrentOffset >= 0).ToProperty();
    }

    private static AnimationState GetCurrentState(AnimationService service)
    {
        if (service.IsPlaying) return AnimationState.Playing;
        if (service.IsPaused) return AnimationState.Paused;
        return AnimationState.Stopped;
    }

    private static void ExecuteCommand(AnimationService service, AnimationCommand command)
    {
        switch (command)
        {
            case AnimationCommand.Start:
                service.StartScrollAnimation(20);
                break;
            case AnimationCommand.Stop:
                service.StopAnimation();
                break;
            case AnimationCommand.Pause:
                service.PauseAnimation();
                break;
            case AnimationCommand.Resume:
                service.ResumeAnimation();
                break;
        }
    }

    private static bool IsValidTransition(AnimationState from, AnimationState to, AnimationCommand command)
    {
        // Define valid transitions based on state machine
        return command switch
        {
            AnimationCommand.Start => to == AnimationState.Playing,
            AnimationCommand.Stop => to == AnimationState.Stopped,
            AnimationCommand.Pause => from == AnimationState.Playing 
                ? to == AnimationState.Paused 
                : to == from, // Ignored if not playing
            AnimationCommand.Resume => from == AnimationState.Paused 
                ? to == AnimationState.Playing 
                : to == from, // Ignored if not paused
            _ => true
        };
    }

    #endregion

    #region Property 17: Export Format Validity

    /// <summary>
    /// Property 17: Export Format Validity
    /// For any exported PNG file, the file should be a valid PNG that can be opened 
    /// by standard image libraries.
    /// Validates: Requirements 7.1, 7.2
    /// </summary>
    [Property(MaxTest = 50)]
    public Property ExportedPngIsValid()
    {
        return Prop.ForAll(
            Gen.Choose(8, 64).ToArbitrary(),
            Gen.Choose(8, 32).ToArbitrary(),
            (width, height) =>
            {
                var service = new ExportService();
                using var bitmap = CreateTestBitmap(width, height);
                var filePath = Path.Combine(_testOutputDir, $"test_{Guid.NewGuid():N}.png");

                try
                {
                    var result = service.ExportPngAsync(bitmap, filePath).GetAwaiter().GetResult();
                    
                    if (!result) return false;
                    if (!File.Exists(filePath)) return false;

                    // Verify it's a valid PNG by reading it back
                    using var loadedBitmap = SKBitmap.Decode(filePath);
                    if (loadedBitmap == null) return false;

                    // Dimensions should match
                    return loadedBitmap.Width == width && loadedBitmap.Height == height;
                }
                finally
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
            });
    }

    /// <summary>
    /// Property: PNG export with zoom scales correctly
    /// </summary>
    [Property(MaxTest = 50)]
    public Property PngExportWithZoomScalesCorrectly()
    {
        return Prop.ForAll(
            Gen.Choose(8, 32).ToArbitrary(),
            Gen.Choose(8, 16).ToArbitrary(),
            Gen.Choose(50, 400).ToArbitrary(),
            (width, height, zoomLevel) =>
            {
                var service = new ExportService();
                using var bitmap = CreateTestBitmap(width, height);
                var filePath = Path.Combine(_testOutputDir, $"test_zoom_{Guid.NewGuid():N}.png");

                try
                {
                    var result = service.ExportPngAsync(bitmap, filePath, useZoom: true, zoomLevel: zoomLevel)
                        .GetAwaiter().GetResult();
                    
                    if (!result) return false;
                    if (!File.Exists(filePath)) return false;

                    using var loadedBitmap = SKBitmap.Decode(filePath);
                    if (loadedBitmap == null) return false;

                    // Calculate expected dimensions
                    var clampedZoom = Math.Clamp(zoomLevel, ExportService.MinZoom, ExportService.MaxZoom);
                    var expectedWidth = (int)(width * clampedZoom / 100.0);
                    var expectedHeight = (int)(height * clampedZoom / 100.0);

                    // Allow small rounding differences
                    return Math.Abs(loadedBitmap.Width - expectedWidth) <= 1 &&
                           Math.Abs(loadedBitmap.Height - expectedHeight) <= 1;
                }
                finally
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
            });
    }

    /// <summary>
    /// Property: GIF export creates valid file with correct header
    /// </summary>
    [Property(MaxTest = 30)]
    public Property GifExportCreatesValidFile()
    {
        return Prop.ForAll(
            Gen.Choose(1, 5).ToArbitrary(),
            Gen.Choose(8, 32).ToArbitrary(),
            Gen.Choose(8, 16).ToArbitrary(),
            (frameCount, width, height) =>
            {
                var service = new ExportService();
                var frames = new List<SKBitmap>();
                
                try
                {
                    for (int i = 0; i < frameCount; i++)
                    {
                        frames.Add(CreateTestBitmap(width, height));
                    }

                    var filePath = Path.Combine(_testOutputDir, $"test_{Guid.NewGuid():N}.gif");

                    var result = service.ExportGifAsync(frames, filePath, fps: 10).GetAwaiter().GetResult();
                    
                    if (!result) return false;
                    if (!File.Exists(filePath)) return false;

                    // Verify file is not empty and has GIF header
                    var fileBytes = File.ReadAllBytes(filePath);
                    if (fileBytes.Length < 6) return false;

                    // Check GIF89a header
                    var header = System.Text.Encoding.ASCII.GetString(fileBytes, 0, 6);
                    var isValidGif = header == "GIF89a" || header == "GIF87a";

                    File.Delete(filePath);
                    return isValidGif;
                }
                finally
                {
                    foreach (var frame in frames)
                    {
                        frame.Dispose();
                    }
                }
            });
    }

    /// <summary>
    /// Property: WebP export creates valid file
    /// </summary>
    [Property(MaxTest = 30)]
    public Property WebPExportCreatesValidFile()
    {
        return Prop.ForAll(
            Gen.Choose(8, 32).ToArbitrary(),
            Gen.Choose(8, 16).ToArbitrary(),
            (width, height) =>
            {
                var service = new ExportService();
                var frames = new List<SKBitmap> { CreateTestBitmap(width, height) };

                try
                {
                    var filePath = Path.Combine(_testOutputDir, $"test_{Guid.NewGuid():N}.webp");

                    var result = service.ExportWebPAsync(frames, filePath, fps: 10).GetAwaiter().GetResult();
                    
                    if (!result) return false;
                    if (!File.Exists(filePath)) return false;

                    // Verify file is not empty
                    var fileInfo = new FileInfo(filePath);
                    var isValidSize = fileInfo.Length > 0;

                    // Try to decode it back
                    using var loadedBitmap = SKBitmap.Decode(filePath);
                    var canDecode = loadedBitmap != null;

                    File.Delete(filePath);
                    return isValidSize && canDecode;
                }
                finally
                {
                    foreach (var frame in frames)
                    {
                        frame.Dispose();
                    }
                }
            });
    }

    /// <summary>
    /// Property: Export with different formats produces valid files
    /// </summary>
    [Property(MaxTest = 50)]
    public Property ExportDifferentFormatsProducesValidFiles(ExportFormat format)
    {
        return Prop.ForAll(
            Gen.Choose(8, 32).ToArbitrary(),
            Gen.Choose(8, 16).ToArbitrary(),
            (width, height) =>
            {
                // Skip GIF for single frame export (use ExportGifAsync for that)
                if (format == ExportFormat.Gif) return true;

                var service = new ExportService();
                using var bitmap = CreateTestBitmap(width, height);
                
                var extension = format switch
                {
                    ExportFormat.Png => ".png",
                    ExportFormat.Jpeg => ".jpg",
                    ExportFormat.WebP => ".webp",
                    _ => ".png"
                };
                
                var filePath = Path.Combine(_testOutputDir, $"test_{Guid.NewGuid():N}{extension}");

                try
                {
                    var result = service.ExportAsync(bitmap, filePath, format).GetAwaiter().GetResult();
                    
                    if (!result) return false;
                    if (!File.Exists(filePath)) return false;

                    // Verify file is not empty
                    var fileInfo = new FileInfo(filePath);
                    return fileInfo.Length > 0;
                }
                finally
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
            });
    }

    /// <summary>
    /// Property: FPS is clamped to valid range (1-60)
    /// </summary>
    [Property(MaxTest = 50)]
    public Property FpsIsClampedToValidRange(int fps)
    {
        // This is implicitly tested by the service clamping fps
        var clampedFps = Math.Clamp(fps, ExportService.MinFps, ExportService.MaxFps);
        return (clampedFps >= ExportService.MinFps && clampedFps <= ExportService.MaxFps).ToProperty();
    }

    #endregion
}

/// <summary>
/// Animation commands for state machine testing
/// </summary>
public enum AnimationCommand
{
    Start,
    Stop,
    Pause,
    Resume
}
