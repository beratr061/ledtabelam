using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for boundary validation
/// Feature: program-ve-ara-durak
/// Validates: Requirements 2.3, 5.3, 3.4, 6.4
/// </summary>
public class BoundaryValidationPropertyTests
{
    #region Property 4: Program Süresi Sınır Kontrolü

    /// <summary>
    /// Property 4: Program Süresi Sınır Kontrolü
    /// For any program duration value, it SHALL be clamped to the range [1, 60] seconds.
    /// Values outside this range SHALL be adjusted to the nearest valid value.
    /// Validates: Requirements 2.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProgramDurationIsClampedToValidRange(int inputDuration)
    {
        var program = new TabelaProgram();
        program.DurationSeconds = inputDuration;

        var result = program.DurationSeconds;
        
        // Result should always be in [1, 60] range
        return (result >= 1 && result <= 60).ToProperty();
    }

    /// <summary>
    /// Property 4.2: Values below minimum are clamped to 1
    /// Validates: Requirements 2.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProgramDurationBelowMinimumIsClampedTo1()
    {
        return Prop.ForAll(
            Gen.Choose(int.MinValue, 0).ToArbitrary(),
            inputDuration =>
            {
                var program = new TabelaProgram();
                program.DurationSeconds = inputDuration;
                return program.DurationSeconds == 1;
            });
    }

    /// <summary>
    /// Property 4.3: Values above maximum are clamped to 60
    /// Validates: Requirements 2.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProgramDurationAboveMaximumIsClampedTo60()
    {
        return Prop.ForAll(
            Gen.Choose(61, 10000).ToArbitrary(),
            inputDuration =>
            {
                var program = new TabelaProgram();
                program.DurationSeconds = inputDuration;
                return program.DurationSeconds == 60;
            });
    }

    /// <summary>
    /// Property 4.4: Values within range are preserved
    /// Validates: Requirements 2.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProgramDurationWithinRangeIsPreserved()
    {
        return Prop.ForAll(
            Gen.Choose(1, 60).ToArbitrary(),
            inputDuration =>
            {
                var program = new TabelaProgram();
                program.DurationSeconds = inputDuration;
                return program.DurationSeconds == inputDuration;
            });
    }

    #endregion

    #region Property 7: Ara Durak Süresi Sınır Kontrolü

    /// <summary>
    /// Property 7: Ara Durak Süresi Sınır Kontrolü
    /// For any intermediate stop duration value, it SHALL be clamped to the range [0.5, 10] seconds.
    /// Validates: Requirements 5.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StopDurationIsClampedToValidRange()
    {
        return Prop.ForAll(
            Gen.Choose(-1000, 2000).Select(x => x / 100.0).ToArbitrary(), // -10.0 to 20.0
            inputDuration =>
            {
                var settings = new IntermediateStopSettings();
                settings.DurationSeconds = inputDuration;

                var result = settings.DurationSeconds;
                
                // Result should always be in [0.5, 10.0] range
                return result >= 0.5 && result <= 10.0;
            });
    }

    /// <summary>
    /// Property 7.2: Values below minimum are clamped to 0.5
    /// Validates: Requirements 5.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StopDurationBelowMinimumIsClampedTo05()
    {
        return Prop.ForAll(
            Gen.Choose(-1000, 49).Select(x => x / 100.0).ToArbitrary(), // -10.0 to 0.49
            inputDuration =>
            {
                var settings = new IntermediateStopSettings();
                settings.DurationSeconds = inputDuration;
                return Math.Abs(settings.DurationSeconds - 0.5) < 0.001;
            });
    }

    /// <summary>
    /// Property 7.3: Values above maximum are clamped to 10
    /// Validates: Requirements 5.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StopDurationAboveMaximumIsClampedTo10()
    {
        return Prop.ForAll(
            Gen.Choose(1001, 5000).Select(x => x / 100.0).ToArbitrary(), // 10.01 to 50.0
            inputDuration =>
            {
                var settings = new IntermediateStopSettings();
                settings.DurationSeconds = inputDuration;
                return Math.Abs(settings.DurationSeconds - 10.0) < 0.001;
            });
    }

    /// <summary>
    /// Property 7.4: Values within range are preserved
    /// Validates: Requirements 5.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StopDurationWithinRangeIsPreserved()
    {
        return Prop.ForAll(
            Gen.Choose(50, 1000).Select(x => x / 100.0).ToArbitrary(), // 0.5 to 10.0
            inputDuration =>
            {
                var settings = new IntermediateStopSettings();
                settings.DurationSeconds = inputDuration;
                return Math.Abs(settings.DurationSeconds - inputDuration) < 0.001;
            });
    }

    #endregion

    #region Property 12: Geçiş Süresi Sınır Kontrolü

    /// <summary>
    /// Property 12: Geçiş Süresi Sınır Kontrolü - Program Transition
    /// For any program transition duration, it SHALL be clamped to [200, 1000] ms.
    /// Validates: Requirements 3.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProgramTransitionDurationIsClampedToValidRange(int inputDuration)
    {
        var program = new TabelaProgram();
        program.TransitionDurationMs = inputDuration;

        var result = program.TransitionDurationMs;
        
        // Result should always be in [200, 1000] range
        return (result >= 200 && result <= 1000).ToProperty();
    }

    /// <summary>
    /// Property 12.2: Program transition below minimum is clamped to 200
    /// Validates: Requirements 3.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProgramTransitionBelowMinimumIsClampedTo200()
    {
        return Prop.ForAll(
            Gen.Choose(int.MinValue, 199).ToArbitrary(),
            inputDuration =>
            {
                var program = new TabelaProgram();
                program.TransitionDurationMs = inputDuration;
                return program.TransitionDurationMs == 200;
            });
    }

    /// <summary>
    /// Property 12.3: Program transition above maximum is clamped to 1000
    /// Validates: Requirements 3.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProgramTransitionAboveMaximumIsClampedTo1000()
    {
        return Prop.ForAll(
            Gen.Choose(1001, 10000).ToArbitrary(),
            inputDuration =>
            {
                var program = new TabelaProgram();
                program.TransitionDurationMs = inputDuration;
                return program.TransitionDurationMs == 1000;
            });
    }

    /// <summary>
    /// Property 12.4: Program transition within range is preserved
    /// Validates: Requirements 3.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProgramTransitionWithinRangeIsPreserved()
    {
        return Prop.ForAll(
            Gen.Choose(200, 1000).ToArbitrary(),
            inputDuration =>
            {
                var program = new TabelaProgram();
                program.TransitionDurationMs = inputDuration;
                return program.TransitionDurationMs == inputDuration;
            });
    }

    /// <summary>
    /// Property 12.5: Stop animation duration is clamped to [100, 500] ms
    /// Validates: Requirements 6.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StopAnimationDurationIsClampedToValidRange(int inputDuration)
    {
        var settings = new IntermediateStopSettings();
        settings.AnimationDurationMs = inputDuration;

        var result = settings.AnimationDurationMs;
        
        // Result should always be in [100, 500] range
        return (result >= 100 && result <= 500).ToProperty();
    }

    /// <summary>
    /// Property 12.6: Stop animation below minimum is clamped to 100
    /// Validates: Requirements 6.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StopAnimationBelowMinimumIsClampedTo100()
    {
        return Prop.ForAll(
            Gen.Choose(int.MinValue, 99).ToArbitrary(),
            inputDuration =>
            {
                var settings = new IntermediateStopSettings();
                settings.AnimationDurationMs = inputDuration;
                return settings.AnimationDurationMs == 100;
            });
    }

    /// <summary>
    /// Property 12.7: Stop animation above maximum is clamped to 500
    /// Validates: Requirements 6.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StopAnimationAboveMaximumIsClampedTo500()
    {
        return Prop.ForAll(
            Gen.Choose(501, 10000).ToArbitrary(),
            inputDuration =>
            {
                var settings = new IntermediateStopSettings();
                settings.AnimationDurationMs = inputDuration;
                return settings.AnimationDurationMs == 500;
            });
    }

    /// <summary>
    /// Property 12.8: Stop animation within range is preserved
    /// Validates: Requirements 6.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StopAnimationWithinRangeIsPreserved()
    {
        return Prop.ForAll(
            Gen.Choose(100, 500).ToArbitrary(),
            inputDuration =>
            {
                var settings = new IntermediateStopSettings();
                settings.AnimationDurationMs = inputDuration;
                return settings.AnimationDurationMs == inputDuration;
            });
    }

    #endregion
}
