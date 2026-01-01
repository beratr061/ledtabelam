using LEDTabelam.Models;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Program ve Ara Durak model testleri
/// Requirements: 2.2, 5.2, 6.2, 6.5
/// </summary>
public class ProgramModelTests
{
    #region TabelaProgram Tests

    [Fact]
    public void TabelaProgram_DefaultValues_AreCorrect()
    {
        // Requirements: 2.2, 3.2, 3.5
        var program = new TabelaProgram();
        
        Assert.Equal("Program 1", program.Name);
        Assert.Equal(5, program.DurationSeconds);
        Assert.Equal(ProgramTransitionType.Direct, program.Transition);
        Assert.Equal(300, program.TransitionDurationMs);
        Assert.NotNull(program.Items);
        Assert.Empty(program.Items);
        Assert.False(program.IsActive);
    }

    [Fact]
    public void TabelaProgram_Name_CanBeSet()
    {
        var program = new TabelaProgram();
        program.Name = "Test Program";
        
        Assert.Equal("Test Program", program.Name);
    }

    [Fact]
    public void TabelaProgram_Name_NullBecomesDefault()
    {
        var program = new TabelaProgram();
        program.Name = null!;
        
        Assert.Equal("Program 1", program.Name);
    }

    [Fact]
    public void TabelaProgram_Items_NullBecomesEmptyCollection()
    {
        var program = new TabelaProgram();
        program.Items = null!;
        
        Assert.NotNull(program.Items);
        Assert.Empty(program.Items);
    }

    [Fact]
    public void TabelaProgram_Id_CanBeSet()
    {
        var program = new TabelaProgram();
        program.Id = 42;
        
        Assert.Equal(42, program.Id);
    }

    [Fact]
    public void TabelaProgram_DurationSeconds_CanBeSet()
    {
        var program = new TabelaProgram();
        program.DurationSeconds = 30;
        
        Assert.Equal(30, program.DurationSeconds);
    }

    [Fact]
    public void TabelaProgram_Transition_CanBeSet()
    {
        var program = new TabelaProgram();
        program.Transition = ProgramTransitionType.Fade;
        
        Assert.Equal(ProgramTransitionType.Fade, program.Transition);
    }

    [Fact]
    public void TabelaProgram_TransitionDurationMs_CanBeSet()
    {
        var program = new TabelaProgram();
        program.TransitionDurationMs = 500;
        
        Assert.Equal(500, program.TransitionDurationMs);
    }

    [Fact]
    public void TabelaProgram_IsActive_CanBeSet()
    {
        var program = new TabelaProgram();
        program.IsActive = true;
        
        Assert.True(program.IsActive);
    }

    #endregion

    #region IntermediateStopSettings Tests

    [Fact]
    public void IntermediateStopSettings_DefaultValues_AreCorrect()
    {
        // Requirements: 5.2, 6.2, 6.5
        var settings = new IntermediateStopSettings();
        
        Assert.False(settings.IsEnabled);
        Assert.NotNull(settings.Stops);
        Assert.Empty(settings.Stops);
        Assert.Equal(2.0, settings.DurationSeconds);
        Assert.Equal(StopAnimationType.Direct, settings.Animation);
        Assert.Equal(200, settings.AnimationDurationMs);
        Assert.False(settings.AutoCalculateDuration);
    }

    [Fact]
    public void IntermediateStopSettings_IsEnabled_CanBeSet()
    {
        var settings = new IntermediateStopSettings();
        settings.IsEnabled = true;
        
        Assert.True(settings.IsEnabled);
    }

    [Fact]
    public void IntermediateStopSettings_Stops_NullBecomesEmptyCollection()
    {
        var settings = new IntermediateStopSettings();
        settings.Stops = null!;
        
        Assert.NotNull(settings.Stops);
        Assert.Empty(settings.Stops);
    }

    [Fact]
    public void IntermediateStopSettings_DurationSeconds_CanBeSet()
    {
        var settings = new IntermediateStopSettings();
        settings.DurationSeconds = 5.0;
        
        Assert.Equal(5.0, settings.DurationSeconds);
    }

    [Fact]
    public void IntermediateStopSettings_Animation_CanBeSet()
    {
        var settings = new IntermediateStopSettings();
        settings.Animation = StopAnimationType.Fade;
        
        Assert.Equal(StopAnimationType.Fade, settings.Animation);
    }

    [Fact]
    public void IntermediateStopSettings_AnimationDurationMs_CanBeSet()
    {
        var settings = new IntermediateStopSettings();
        settings.AnimationDurationMs = 300;
        
        Assert.Equal(300, settings.AnimationDurationMs);
    }

    [Fact]
    public void IntermediateStopSettings_AutoCalculateDuration_CanBeSet()
    {
        var settings = new IntermediateStopSettings();
        settings.AutoCalculateDuration = true;
        
        Assert.True(settings.AutoCalculateDuration);
    }

    #endregion

    #region IntermediateStop Tests

    [Fact]
    public void IntermediateStop_DefaultValues_AreCorrect()
    {
        var stop = new IntermediateStop();
        
        Assert.Equal(0, stop.Order);
        Assert.Equal(string.Empty, stop.StopName);
    }

    [Fact]
    public void IntermediateStop_ParameterizedConstructor_SetsValues()
    {
        var stop = new IntermediateStop(3, "Merkez");
        
        Assert.Equal(3, stop.Order);
        Assert.Equal("Merkez", stop.StopName);
    }

    [Fact]
    public void IntermediateStop_StopName_NullBecomesEmpty()
    {
        var stop = new IntermediateStop();
        stop.StopName = null!;
        
        Assert.Equal(string.Empty, stop.StopName);
    }

    [Fact]
    public void IntermediateStop_Order_CanBeSet()
    {
        var stop = new IntermediateStop();
        stop.Order = 5;
        
        Assert.Equal(5, stop.Order);
    }

    [Fact]
    public void IntermediateStop_StopName_CanBeSet()
    {
        var stop = new IntermediateStop();
        stop.StopName = "Otogar";
        
        Assert.Equal("Otogar", stop.StopName);
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void ProgramTransitionType_HasAllExpectedValues()
    {
        // Requirements: 3.1
        Assert.Equal(6, Enum.GetValues<ProgramTransitionType>().Length);
        Assert.True(Enum.IsDefined(ProgramTransitionType.Direct));
        Assert.True(Enum.IsDefined(ProgramTransitionType.Fade));
        Assert.True(Enum.IsDefined(ProgramTransitionType.SlideLeft));
        Assert.True(Enum.IsDefined(ProgramTransitionType.SlideRight));
        Assert.True(Enum.IsDefined(ProgramTransitionType.SlideUp));
        Assert.True(Enum.IsDefined(ProgramTransitionType.SlideDown));
    }

    [Fact]
    public void StopAnimationType_HasAllExpectedValues()
    {
        // Requirements: 6.1
        Assert.Equal(4, Enum.GetValues<StopAnimationType>().Length);
        Assert.True(Enum.IsDefined(StopAnimationType.Direct));
        Assert.True(Enum.IsDefined(StopAnimationType.Fade));
        Assert.True(Enum.IsDefined(StopAnimationType.SlideUp));
        Assert.True(Enum.IsDefined(StopAnimationType.SlideDown));
    }

    #endregion
}
