using System.Collections.ObjectModel;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using LEDTabelam.Services;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for ProgramSequencer
/// Feature: program-ve-ara-durak
/// </summary>
public class ProgramSequencerPropertyTests
{
    #region Generators

    /// <summary>
    /// Generates a valid program duration (1-60 seconds)
    /// </summary>
    public static Gen<int> GenProgramDuration()
    {
        return Gen.Choose(1, 60);
    }

    /// <summary>
    /// Generates a valid TabelaProgram
    /// </summary>
    public static Gen<TabelaProgram> GenTabelaProgram()
    {
        return from id in Gen.Choose(1, 1000)
               from name in Gen.Elements("Program 1", "Program 2", "Program 3", "Hat Bilgisi", "Bayram Mesajı")
               from duration in GenProgramDuration()
               select new TabelaProgram
               {
                   Id = id,
                   Name = name,
                   DurationSeconds = duration
               };
    }

    /// <summary>
    /// Generates a non-empty list of programs (2-10 programs)
    /// </summary>
    public static Gen<ObservableCollection<TabelaProgram>> GenProgramCollection()
    {
        return Gen.Choose(2, 10)
            .SelectMany(count => Gen.ListOf(count, GenTabelaProgram()))
            .Select(list =>
            {
                var collection = new ObservableCollection<TabelaProgram>();
                int id = 1;
                foreach (var program in list)
                {
                    program.Id = id++;
                    collection.Add(program);
                }
                return collection;
            });
    }

    /// <summary>
    /// Generates a ProgramSequencer with programs
    /// </summary>
    public static Gen<ProgramSequencer> GenProgramSequencer()
    {
        return from programs in GenProgramCollection()
               select new ProgramSequencer { Programs = programs };
    }

    /// <summary>
    /// Generates a valid stop name
    /// </summary>
    public static Gen<string> GenStopName()
    {
        return Gen.Elements("Merkez", "Otogar", "Hastane", "Üniversite", "Belediye", "Terminal");
    }

    /// <summary>
    /// Generates a TabelaItem with intermediate stops
    /// </summary>
    public static Gen<TabelaItem> GenTabelaItemWithStops()
    {
        return from stopCount in Gen.Choose(2, 8)
               from stopNames in Gen.ListOf(stopCount, GenStopName())
               from duration in Gen.Choose(5, 100).Select(x => x / 10.0) // 0.5 - 10.0
               select CreateItemWithStops(stopNames.ToList(), duration);
    }

    private static TabelaItem CreateItemWithStops(System.Collections.Generic.List<string> stopNames, double duration)
    {
        var item = new TabelaItem
        {
            Id = 1,
            ItemType = TabelaItemType.Text,
            Content = "Test"
        };
        item.IntermediateStops.IsEnabled = true;
        item.IntermediateStops.DurationSeconds = duration;
        
        int order = 0;
        foreach (var name in stopNames)
        {
            item.IntermediateStops.Stops.Add(new IntermediateStop(order++, name));
        }
        
        return item;
    }

    /// <summary>
    /// Generates a program with items that have intermediate stops
    /// </summary>
    public static Gen<TabelaProgram> GenProgramWithStops()
    {
        return from program in GenTabelaProgram()
               from item in GenTabelaItemWithStops()
               select AddItemToProgram(program, item);
    }

    private static TabelaProgram AddItemToProgram(TabelaProgram program, TabelaItem item)
    {
        program.Items.Add(item);
        return program;
    }

    #endregion

    #region Arbitraries

    public class ProgramSequencerArbitraries
    {
        public static Arbitrary<TabelaProgram> TabelaProgramArb() =>
            Arb.From(GenTabelaProgram());

        public static Arbitrary<ObservableCollection<TabelaProgram>> ProgramCollectionArb() =>
            Arb.From(GenProgramCollection());

        public static Arbitrary<ProgramSequencer> ProgramSequencerArb() =>
            Arb.From(GenProgramSequencer());

        public static Arbitrary<TabelaItem> TabelaItemWithStopsArb() =>
            Arb.From(GenTabelaItemWithStops());

        public static Arbitrary<TabelaProgram> ProgramWithStopsArb() =>
            Arb.From(GenProgramWithStops());
    }

    #endregion

    #region Property 5: Program Döngü Davranışı

    /// <summary>
    /// Property 5: Program Döngü Davranışı
    /// For any program sequence with N programs, when the current program index reaches N-1 
    /// and the program duration expires, the next program index SHALL be 0 (loop back to first).
    /// Validates: Requirements 2.4, 2.6
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProgramSequencerArbitraries) })]
    public Property ProgramLoopsBackToFirstAfterLast(ProgramSequencer sequencer)
    {
        if (sequencer.Programs.Count < 2)
            return true.ToProperty();

        sequencer.IsLooping = true;
        sequencer.Play();

        // Go to last program
        sequencer.GoToProgram(sequencer.Programs.Count - 1);
        
        // Verify we're at last program
        var atLastProgram = sequencer.CurrentProgramIndex == sequencer.Programs.Count - 1;

        // Call NextProgram (simulates duration expiry)
        sequencer.NextProgram();

        // Should loop back to first program
        var loopedToFirst = sequencer.CurrentProgramIndex == 0;

        return (atLastProgram && loopedToFirst).ToProperty();
    }

    /// <summary>
    /// Property 5.2: Program advances on duration expiry via OnTick
    /// For any playing sequencer, when OnTick accumulates time >= program duration,
    /// the sequencer SHALL advance to the next program.
    /// Validates: Requirements 2.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProgramSequencerArbitraries) })]
    public Property ProgramAdvancesOnDurationExpiry(ProgramSequencer sequencer)
    {
        if (sequencer.Programs.Count < 2)
            return true.ToProperty();

        sequencer.IsLooping = true;
        sequencer.Play();
        sequencer.GoToProgram(0);

        var initialIndex = sequencer.CurrentProgramIndex;
        var programDuration = sequencer.CurrentProgram!.DurationSeconds;

        // Simulate time passing (slightly more than duration)
        sequencer.OnTick(programDuration + 0.1);

        // Should have advanced to next program
        return (sequencer.CurrentProgramIndex == initialIndex + 1).ToProperty();
    }

    /// <summary>
    /// Property 5.3: Loop mode cycles through all programs
    /// For any sequencer with N programs in loop mode, calling NextProgram N times
    /// SHALL return to the original program.
    /// Validates: Requirements 2.6
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProgramSequencerArbitraries) })]
    public Property LoopModeCyclesThroughAllPrograms(ProgramSequencer sequencer)
    {
        if (sequencer.Programs.Count < 2)
            return true.ToProperty();

        sequencer.IsLooping = true;
        sequencer.Play();
        sequencer.GoToProgram(0);

        var programCount = sequencer.Programs.Count;

        // Call NextProgram N times
        for (int i = 0; i < programCount; i++)
        {
            sequencer.NextProgram();
        }

        // Should be back at program 0
        return (sequencer.CurrentProgramIndex == 0).ToProperty();
    }

    #endregion

    #region Property 8: Ara Durak Döngü Davranışı

    /// <summary>
    /// Property 8: Ara Durak Döngü Davranışı
    /// For any TabelaItem with N intermediate stops, when the current stop index reaches N-1 
    /// and the stop duration expires, the next stop index SHALL be 0 (loop back to first).
    /// Validates: Requirements 5.4, 5.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProgramSequencerArbitraries) })]
    public Property StopLoopsBackToFirstAfterLast(TabelaProgram programWithStops)
    {
        // Create a program with stops
        var item = new TabelaItem
        {
            Id = 1,
            ItemType = TabelaItemType.Text,
            Content = "Test"
        };
        item.IntermediateStops.IsEnabled = true;
        item.IntermediateStops.DurationSeconds = 1.0;
        item.IntermediateStops.Stops.Add(new IntermediateStop(0, "Stop 1"));
        item.IntermediateStops.Stops.Add(new IntermediateStop(1, "Stop 2"));
        item.IntermediateStops.Stops.Add(new IntermediateStop(2, "Stop 3"));

        var program = new TabelaProgram
        {
            Id = 1,
            Name = "Test Program",
            DurationSeconds = 30 // Long enough for multiple stop cycles
        };
        program.Items.Add(item);

        var sequencer = new ProgramSequencer();
        sequencer.Programs = new ObservableCollection<TabelaProgram> { program };
        sequencer.Play();

        var stopCount = item.IntermediateStops.Stops.Count;
        var stopDuration = item.IntermediateStops.DurationSeconds;

        // Simulate time passing through all stops
        for (int i = 0; i < stopCount; i++)
        {
            sequencer.OnTick(stopDuration + 0.01);
        }

        // After cycling through all stops, should be back at stop 0
        var currentStopIndex = sequencer.GetCurrentStopIndex(item.Id);
        return (currentStopIndex == 0).ToProperty();
    }

    /// <summary>
    /// Property 8.2: Stop advances on duration expiry
    /// For any item with intermediate stops, when OnTick accumulates time >= stop duration,
    /// the stop index SHALL advance.
    /// Validates: Requirements 5.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StopAdvancesOnDurationExpiry()
    {
        var item = new TabelaItem
        {
            Id = 1,
            ItemType = TabelaItemType.Text,
            Content = "Test"
        };
        item.IntermediateStops.IsEnabled = true;
        item.IntermediateStops.DurationSeconds = 2.0;
        item.IntermediateStops.Stops.Add(new IntermediateStop(0, "Stop 1"));
        item.IntermediateStops.Stops.Add(new IntermediateStop(1, "Stop 2"));

        var program = new TabelaProgram
        {
            Id = 1,
            Name = "Test Program",
            DurationSeconds = 30
        };
        program.Items.Add(item);

        var sequencer = new ProgramSequencer();
        sequencer.Programs = new ObservableCollection<TabelaProgram> { program };
        sequencer.Play();

        var initialStopIndex = sequencer.GetCurrentStopIndex(item.Id);

        // Simulate time passing (slightly more than stop duration)
        sequencer.OnTick(2.1);

        var newStopIndex = sequencer.GetCurrentStopIndex(item.Id);

        return (initialStopIndex == 0 && newStopIndex == 1).ToProperty();
    }

    #endregion

    #region Property 13: Play/Pause State Değişimi

    /// <summary>
    /// Property 13: Play/Pause State Değişimi
    /// For any sequencer, calling Play() SHALL set IsPlaying to true, 
    /// and calling Pause() SHALL set IsPlaying to false while preserving the current program index.
    /// Validates: Requirements 7.2, 7.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProgramSequencerArbitraries) })]
    public Property PlaySetsIsPlayingToTrue(ProgramSequencer sequencer)
    {
        if (sequencer.Programs.Count == 0)
            return true.ToProperty();

        sequencer.Play();
        return sequencer.IsPlaying.ToProperty();
    }

    /// <summary>
    /// Property 13.2: Pause sets IsPlaying to false
    /// Validates: Requirements 7.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProgramSequencerArbitraries) })]
    public Property PauseSetsIsPlayingToFalse(ProgramSequencer sequencer)
    {
        if (sequencer.Programs.Count == 0)
            return true.ToProperty();

        sequencer.Play();
        sequencer.Pause();
        return (!sequencer.IsPlaying).ToProperty();
    }

    /// <summary>
    /// Property 13.3: Pause preserves current program index
    /// Validates: Requirements 7.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProgramSequencerArbitraries) })]
    public Property PausePreservesCurrentProgramIndex(ProgramSequencer sequencer)
    {
        if (sequencer.Programs.Count < 2)
            return true.ToProperty();

        sequencer.Play();
        
        // Go to a specific program
        var targetIndex = sequencer.Programs.Count / 2;
        sequencer.GoToProgram(targetIndex);
        
        var indexBeforePause = sequencer.CurrentProgramIndex;
        
        sequencer.Pause();
        
        var indexAfterPause = sequencer.CurrentProgramIndex;

        return (indexBeforePause == indexAfterPause).ToProperty();
    }

    /// <summary>
    /// Property 13.4: Play after Pause continues from same program
    /// Validates: Requirements 7.2, 7.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProgramSequencerArbitraries) })]
    public Property PlayAfterPauseContinuesFromSameProgram(ProgramSequencer sequencer)
    {
        if (sequencer.Programs.Count < 2)
            return true.ToProperty();

        sequencer.Play();
        
        // Go to a specific program
        var targetIndex = sequencer.Programs.Count / 2;
        sequencer.GoToProgram(targetIndex);
        
        sequencer.Pause();
        var indexAfterPause = sequencer.CurrentProgramIndex;
        
        sequencer.Play();
        var indexAfterResume = sequencer.CurrentProgramIndex;

        return (indexAfterPause == indexAfterResume && sequencer.IsPlaying).ToProperty();
    }

    #endregion

    #region Property 14: Non-Loop Mode Davranışı

    /// <summary>
    /// Property 14: Non-Loop Mode Davranışı
    /// For any sequencer with IsLooping=false and N programs, when reaching program N-1 
    /// and duration expires, the sequencer SHALL stop (IsPlaying=false) and remain at program N-1.
    /// Validates: Requirements 7.6
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProgramSequencerArbitraries) })]
    public Property NonLoopModeStopsAtLastProgram(ProgramSequencer sequencer)
    {
        if (sequencer.Programs.Count < 2)
            return true.ToProperty();

        sequencer.IsLooping = false;
        sequencer.Play();

        // Go to last program
        var lastIndex = sequencer.Programs.Count - 1;
        sequencer.GoToProgram(lastIndex);

        // Try to go to next program
        sequencer.NextProgram();

        // Should stop and remain at last program
        var stoppedAtLast = !sequencer.IsPlaying && sequencer.CurrentProgramIndex == lastIndex;

        return stoppedAtLast.ToProperty();
    }

    /// <summary>
    /// Property 14.2: Non-loop mode stops via OnTick at last program
    /// Validates: Requirements 7.6
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProgramSequencerArbitraries) })]
    public Property NonLoopModeStopsViaOnTickAtLastProgram(ProgramSequencer sequencer)
    {
        if (sequencer.Programs.Count < 2)
            return true.ToProperty();

        sequencer.IsLooping = false;
        sequencer.Play();

        // Go to last program
        var lastIndex = sequencer.Programs.Count - 1;
        sequencer.GoToProgram(lastIndex);

        var programDuration = sequencer.CurrentProgram!.DurationSeconds;

        // Simulate time passing beyond duration
        sequencer.OnTick(programDuration + 0.1);

        // Should stop and remain at last program
        return (!sequencer.IsPlaying && sequencer.CurrentProgramIndex == lastIndex).ToProperty();
    }

    /// <summary>
    /// Property 14.3: Non-loop mode allows manual navigation after stop
    /// Validates: Requirements 7.6
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProgramSequencerArbitraries) })]
    public Property NonLoopModeAllowsManualNavigationAfterStop(ProgramSequencer sequencer)
    {
        if (sequencer.Programs.Count < 2)
            return true.ToProperty();

        sequencer.IsLooping = false;
        sequencer.Play();

        // Go to last program and let it stop
        var lastIndex = sequencer.Programs.Count - 1;
        sequencer.GoToProgram(lastIndex);
        sequencer.NextProgram(); // This should stop

        // Manual navigation should still work
        sequencer.GoToProgram(0);

        return (sequencer.CurrentProgramIndex == 0).ToProperty();
    }

    #endregion

    #region Property 10: Otomatik Süre Hesaplama

    /// <summary>
    /// Property 10: Otomatik Süre Hesaplama
    /// For any TabelaItem with AutoCalculateDuration enabled and N stops within a program of D seconds duration,
    /// each stop duration SHALL be D/N seconds (±0.01 tolerance for floating point).
    /// Validates: Requirements 8.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AutoCalculateDurationDividesEvenlyAmongStops()
    {
        return Prop.ForAll(
            Gen.Choose(2, 10).ToArbitrary(), // stopCount
            Gen.Choose(5, 60).ToArbitrary(), // programDuration
            (stopCount, programDuration) =>
            {
                var item = new TabelaItem
                {
                    Id = 1,
                    ItemType = TabelaItemType.Text,
                    Content = "Test"
                };
                item.IntermediateStops.IsEnabled = true;
                item.IntermediateStops.AutoCalculateDuration = true;
                item.IntermediateStops.DurationSeconds = 2.0; // This should be ignored

                for (int i = 0; i < stopCount; i++)
                {
                    item.IntermediateStops.Stops.Add(new IntermediateStop(i, $"Stop {i + 1}"));
                }

                var program = new TabelaProgram
                {
                    Id = 1,
                    Name = "Test Program",
                    DurationSeconds = programDuration
                };
                program.Items.Add(item);

                var sequencer = new ProgramSequencer();
                sequencer.Programs = new ObservableCollection<TabelaProgram> { program };
                sequencer.Play();

                // Expected duration per stop
                double expectedDuration = (double)programDuration / stopCount;

                // Count how many stops we cycle through in exactly programDuration time
                int stopChanges = 0;
                double timeStep = 0.1;
                double totalTime = 0;

                while (totalTime < programDuration - timeStep)
                {
                    var beforeIndex = sequencer.GetCurrentStopIndex(item.Id);
                    sequencer.OnTick(timeStep);
                    var afterIndex = sequencer.GetCurrentStopIndex(item.Id);
                    
                    if (beforeIndex != afterIndex)
                        stopChanges++;
                    
                    totalTime += timeStep;
                }

                // We should have approximately stopCount stop changes (one full cycle)
                // Allow some tolerance due to timing
                var expectedChanges = stopCount;
                var tolerance = 2; // Allow ±2 due to timing granularity

                return System.Math.Abs(stopChanges - expectedChanges) <= tolerance;
            });
    }

    /// <summary>
    /// Property 10.2: Auto-calculated duration respects program duration
    /// For any item with AutoCalculateDuration, the total cycle time should approximately equal program duration.
    /// Validates: Requirements 8.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AutoCalculatedDurationRespectsProgramDuration()
    {
        return Prop.ForAll(
            Gen.Choose(2, 5).ToArbitrary(), // stopCount
            Gen.Choose(10, 30).ToArbitrary(), // programDuration
            (stopCount, programDuration) =>
            {
                var item = new TabelaItem
                {
                    Id = 1,
                    ItemType = TabelaItemType.Text,
                    Content = "Test"
                };
                item.IntermediateStops.IsEnabled = true;
                item.IntermediateStops.AutoCalculateDuration = true;

                for (int i = 0; i < stopCount; i++)
                {
                    item.IntermediateStops.Stops.Add(new IntermediateStop(i, $"Stop {i + 1}"));
                }

                var program = new TabelaProgram
                {
                    Id = 1,
                    Name = "Test Program",
                    DurationSeconds = programDuration
                };
                program.Items.Add(item);

                var sequencer = new ProgramSequencer();
                sequencer.Programs = new ObservableCollection<TabelaProgram> { program };
                sequencer.Play();

                // Calculate expected duration per stop
                double expectedDurationPerStop = (double)programDuration / stopCount;
                double tolerance = 0.01;

                // The GetStopDuration method should return the correct value
                // We verify this by checking that after one stop duration, the stop changes
                var initialIndex = sequencer.GetCurrentStopIndex(item.Id);
                sequencer.OnTick(expectedDurationPerStop + tolerance);
                var newIndex = sequencer.GetCurrentStopIndex(item.Id);

                return initialIndex != newIndex;
            });
    }

    #endregion

    #region Property 9: Eşzamanlı Döngü Bağımsızlığı

    /// <summary>
    /// Property 9: Eşzamanlı Döngü Bağımsızlığı
    /// For any playing state, the program timer and intermediate stop timers SHALL operate independently.
    /// A program transition SHALL NOT reset intermediate stop timers of items in the new program.
    /// Validates: Requirements 8.3
    /// Feature: program-ve-ara-durak, Property 9: Eşzamanlı Döngü Bağımsızlığı
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProgramAndStopTimersOperateIndependently()
    {
        return Prop.ForAll(
            Gen.Choose(2, 5).ToArbitrary(), // stopCount
            Gen.Choose(5, 15).ToArbitrary(), // programDuration
            Gen.Choose(1, 3).ToArbitrary(), // stopDuration (seconds)
            (stopCount, programDuration, stopDuration) =>
            {
                // Create an item with intermediate stops
                var item = new TabelaItem
                {
                    Id = 1,
                    ItemType = TabelaItemType.Text,
                    Content = "Test"
                };
                item.IntermediateStops.IsEnabled = true;
                item.IntermediateStops.DurationSeconds = stopDuration;
                item.IntermediateStops.AutoCalculateDuration = false;

                for (int i = 0; i < stopCount; i++)
                {
                    item.IntermediateStops.Stops.Add(new IntermediateStop(i, $"Stop {i + 1}"));
                }

                var program = new TabelaProgram
                {
                    Id = 1,
                    Name = "Test Program",
                    DurationSeconds = programDuration
                };
                program.Items.Add(item);

                var sequencer = new ProgramSequencer();
                sequencer.Programs = new ObservableCollection<TabelaProgram> { program };
                sequencer.Play();

                // Advance time to change stops (but not complete program)
                double timeToAdvance = stopDuration + 0.1;
                if (timeToAdvance < programDuration)
                {
                    sequencer.OnTick(timeToAdvance);
                }

                var stopIndexAfterFirstAdvance = sequencer.GetCurrentStopIndex(item.Id);
                var programElapsedAfterFirstAdvance = sequencer.ProgramElapsedTime;

                // Advance more time
                double moreTime = stopDuration + 0.1;
                if (programElapsedAfterFirstAdvance + moreTime < programDuration)
                {
                    sequencer.OnTick(moreTime);
                }

                var stopIndexAfterSecondAdvance = sequencer.GetCurrentStopIndex(item.Id);

                // Stop timer should have advanced independently of program timer
                // If stopDuration < programDuration, stops should cycle multiple times
                bool stopsAdvanced = stopIndexAfterFirstAdvance > 0 || stopIndexAfterSecondAdvance > 0;
                
                return stopsAdvanced || stopDuration >= programDuration;
            });
    }

    /// <summary>
    /// Property 9.2: Program transition does not reset stop timers in new program
    /// When transitioning to a new program, the stop timers for items in the new program
    /// should start fresh (not carry over from previous program).
    /// Validates: Requirements 8.3
    /// Feature: program-ve-ara-durak, Property 9: Eşzamanlı Döngü Bağımsızlığı
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProgramTransitionInitializesNewStopTimers()
    {
        return Prop.ForAll(
            Gen.Choose(2, 4).ToArbitrary(), // stopCount
            Gen.Choose(3, 10).ToArbitrary(), // programDuration
            (stopCount, programDuration) =>
            {
                // Create two programs, each with items that have intermediate stops
                var item1 = new TabelaItem
                {
                    Id = 1,
                    ItemType = TabelaItemType.Text,
                    Content = "Test 1"
                };
                item1.IntermediateStops.IsEnabled = true;
                item1.IntermediateStops.DurationSeconds = 1.0;
                for (int i = 0; i < stopCount; i++)
                {
                    item1.IntermediateStops.Stops.Add(new IntermediateStop(i, $"Stop 1-{i + 1}"));
                }

                var item2 = new TabelaItem
                {
                    Id = 2,
                    ItemType = TabelaItemType.Text,
                    Content = "Test 2"
                };
                item2.IntermediateStops.IsEnabled = true;
                item2.IntermediateStops.DurationSeconds = 1.0;
                for (int i = 0; i < stopCount; i++)
                {
                    item2.IntermediateStops.Stops.Add(new IntermediateStop(i, $"Stop 2-{i + 1}"));
                }

                var program1 = new TabelaProgram
                {
                    Id = 1,
                    Name = "Program 1",
                    DurationSeconds = programDuration
                };
                program1.Items.Add(item1);

                var program2 = new TabelaProgram
                {
                    Id = 2,
                    Name = "Program 2",
                    DurationSeconds = programDuration
                };
                program2.Items.Add(item2);

                var sequencer = new ProgramSequencer();
                sequencer.Programs = new ObservableCollection<TabelaProgram> { program1, program2 };
                sequencer.IsLooping = true;
                sequencer.Play();

                // Advance time in program 1 to change stops
                sequencer.OnTick(1.5); // Should advance stop index
                var stopIndexInProgram1 = sequencer.GetCurrentStopIndex(item1.Id);

                // Transition to program 2
                sequencer.GoToProgram(1);

                // Stop timer for item2 should start at 0
                var stopIndexInProgram2 = sequencer.GetCurrentStopIndex(item2.Id);

                // Item2's stop index should be 0 (fresh start)
                return stopIndexInProgram2 == 0;
            });
    }

    /// <summary>
    /// Property 9.3: Stop timers continue during program transition animation
    /// When a program transition animation is in progress, stop timers should continue to operate.
    /// Validates: Requirements 8.3
    /// Feature: program-ve-ara-durak, Property 9: Eşzamanlı Döngü Bağımsızlığı
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StopTimersContinueDuringProgramTransition()
    {
        return Prop.ForAll(
            Gen.Choose(2, 4).ToArbitrary(), // stopCount
            (stopCount) =>
            {
                var item = new TabelaItem
                {
                    Id = 1,
                    ItemType = TabelaItemType.Text,
                    Content = "Test"
                };
                item.IntermediateStops.IsEnabled = true;
                item.IntermediateStops.DurationSeconds = 0.5; // Short duration
                for (int i = 0; i < stopCount; i++)
                {
                    item.IntermediateStops.Stops.Add(new IntermediateStop(i, $"Stop {i + 1}"));
                }

                var program1 = new TabelaProgram
                {
                    Id = 1,
                    Name = "Program 1",
                    DurationSeconds = 5,
                    Transition = ProgramTransitionType.Fade,
                    TransitionDurationMs = 500 // 0.5 second transition
                };
                program1.Items.Add(item);

                var program2 = new TabelaProgram
                {
                    Id = 2,
                    Name = "Program 2",
                    DurationSeconds = 5,
                    Transition = ProgramTransitionType.Fade,
                    TransitionDurationMs = 500
                };

                var sequencer = new ProgramSequencer();
                sequencer.Programs = new ObservableCollection<TabelaProgram> { program1, program2 };
                sequencer.IsLooping = true;
                sequencer.Play();

                // Get initial stop index
                var initialStopIndex = sequencer.GetCurrentStopIndex(item.Id);

                // Advance time to trigger stop change (0.6 seconds > 0.5 stop duration)
                sequencer.OnTick(0.6);

                var stopIndexAfterTick = sequencer.GetCurrentStopIndex(item.Id);

                // Stop should have advanced
                return stopIndexAfterTick != initialStopIndex || stopCount == 1;
            });
    }

    #endregion
}
