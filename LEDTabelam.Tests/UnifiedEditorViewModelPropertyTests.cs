using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using LEDTabelam.ViewModels;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for UnifiedEditorViewModel program management
/// Feature: program-ve-ara-durak
/// Property 3: Program Ekleme Koleksiyonu Büyütür
/// Validates: Requirements 1.1, 1.2, 1.4
/// </summary>
public class UnifiedEditorViewModelPropertyTests
{
    #region Generators

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
            "Duyuru",
            "Sefer Bilgisi"
        );
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
               select new TabelaProgram
               {
                   Id = id,
                   Name = name,
                   DurationSeconds = duration,
                   Transition = transition
               };
    }

    /// <summary>
    /// Generates a list of TabelaPrograms with unique IDs (1-5 programs)
    /// </summary>
    public static Gen<List<TabelaProgram>> GenTabelaProgramList()
    {
        return Gen.Choose(1, 5).SelectMany(count =>
        {
            var programs = new List<Gen<TabelaProgram>>();
            for (int i = 1; i <= count; i++)
            {
                programs.Add(GenTabelaProgram(i));
            }
            return Gen.Sequence(programs).Select(p => p.ToList());
        });
    }

    /// <summary>
    /// Generates a positive number of programs to add (1-10)
    /// </summary>
    public static Gen<int> GenAddCount()
    {
        return Gen.Choose(1, 10);
    }

    #endregion

    #region Arbitraries

    public class ViewModelArbitraries
    {
        public static Arbitrary<List<TabelaProgram>> TabelaProgramListArb() =>
            Arb.From(GenTabelaProgramList());

        public static Arbitrary<int> AddCountArb() =>
            Arb.From(GenAddCount());
    }

    #endregion

    #region Property 3: Program Ekleme Koleksiyonu Büyütür

    /// <summary>
    /// Property 3.1: Adding a program increases collection size by 1
    /// For any program collection with N programs, adding a new program SHALL result in N+1 programs.
    /// Feature: program-ve-ara-durak, Property 3: Program Ekleme Koleksiyonu Büyütür
    /// Validates: Requirements 1.1, 1.2
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ViewModelArbitraries) })]
    public Property AddingProgramIncreasesCollectionSizeByOne(List<TabelaProgram> initialPrograms)
    {
        // Create a fresh ViewModel for each test
        var viewModel = CreateViewModelWithPrograms(initialPrograms);
        var initialCount = viewModel.Programs.Count;
        
        // Add a new program
        viewModel.AddProgram();
        
        // Verify collection size increased by 1
        return (viewModel.Programs.Count == initialCount + 1).ToProperty();
    }

    /// <summary>
    /// Property 3.2: Added program is present in collection
    /// For any program collection, after adding a program, that program SHALL be present in the collection.
    /// Feature: program-ve-ara-durak, Property 3: Program Ekleme Koleksiyonu Büyütür
    /// Validates: Requirements 1.1, 1.2, 1.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ViewModelArbitraries) })]
    public Property AddedProgramIsPresentInCollection(List<TabelaProgram> initialPrograms)
    {
        var viewModel = CreateViewModelWithPrograms(initialPrograms);
        var countBefore = viewModel.Programs.Count;
        
        // Add a new program
        viewModel.AddProgram();
        
        // The last program should be the newly added one
        var lastProgram = viewModel.Programs.LastOrDefault();
        
        return (lastProgram != null && 
                viewModel.Programs.Contains(lastProgram) &&
                viewModel.Programs.Count == countBefore + 1).ToProperty();
    }

    /// <summary>
    /// Property 3.3: Added program becomes selected
    /// For any program collection, after adding a program, that program SHALL be selected.
    /// Feature: program-ve-ara-durak, Property 3: Program Ekleme Koleksiyonu Büyütür
    /// Validates: Requirements 1.2
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ViewModelArbitraries) })]
    public Property AddedProgramBecomesSelected(List<TabelaProgram> initialPrograms)
    {
        var viewModel = CreateViewModelWithPrograms(initialPrograms);
        
        // Add a new program
        viewModel.AddProgram();
        
        // The newly added program should be selected
        var lastProgram = viewModel.Programs.LastOrDefault();
        
        return (viewModel.SelectedProgram != null && 
                viewModel.SelectedProgram == lastProgram).ToProperty();
    }

    /// <summary>
    /// Property 3.4: Multiple AddProgram calls maintain correct count
    /// For any program collection with N programs, adding M programs SHALL result in N+M programs.
    /// Feature: program-ve-ara-durak, Property 3: Program Ekleme Koleksiyonu Büyütür
    /// Validates: Requirements 1.1, 1.2, 1.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ViewModelArbitraries) })]
    public Property MultipleAddProgramMaintainsCorrectCount(List<TabelaProgram> initialPrograms, int addCount)
    {
        var viewModel = CreateViewModelWithPrograms(initialPrograms);
        var initialCount = viewModel.Programs.Count;
        
        // Add multiple programs
        for (int i = 0; i < addCount; i++)
        {
            viewModel.AddProgram();
        }
        
        // Verify final count
        return (viewModel.Programs.Count == initialCount + addCount).ToProperty();
    }

    /// <summary>
    /// Property 3.5: Added program has valid Items collection
    /// For any program collection, after adding a program, the new program SHALL have a valid (non-null) Items collection.
    /// Feature: program-ve-ara-durak, Property 3: Program Ekleme Koleksiyonu Büyütür
    /// Validates: Requirements 1.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ViewModelArbitraries) })]
    public Property AddedProgramHasValidItemsCollection(List<TabelaProgram> initialPrograms)
    {
        var viewModel = CreateViewModelWithPrograms(initialPrograms);
        
        // Add a new program
        viewModel.AddProgram();
        
        // The newly added program should have a valid Items collection
        var lastProgram = viewModel.Programs.LastOrDefault();
        
        return (lastProgram != null && lastProgram.Items != null).ToProperty();
    }

    /// <summary>
    /// Property 3.6: All program IDs are unique after adding
    /// For any program collection, after adding a program, all program IDs SHALL be unique.
    /// Feature: program-ve-ara-durak, Property 3: Program Ekleme Koleksiyonu Büyütür
    /// Validates: Requirements 1.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ViewModelArbitraries) })]
    public Property AllProgramIdsAreUniqueAfterAdding(List<TabelaProgram> initialPrograms)
    {
        var viewModel = CreateViewModelWithPrograms(initialPrograms);
        
        // Add a new program
        viewModel.AddProgram();
        
        // All program IDs should be unique
        var ids = viewModel.Programs.Select(p => p.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();
        
        return (ids.Count == uniqueIds.Count).ToProperty();
    }

    /// <summary>
    /// Property 3.7: HasPrograms is true after adding program
    /// For any ViewModel, after adding a program, HasPrograms SHALL be true.
    /// Feature: program-ve-ara-durak, Property 3: Program Ekleme Koleksiyonu Büyütür
    /// Validates: Requirements 1.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property HasProgramsIsTrueAfterAddingProgram()
    {
        // Create ViewModel with empty programs (clear default)
        var viewModel = new UnifiedEditorViewModel();
        viewModel.Programs.Clear();
        
        // Add a program
        viewModel.AddProgram();
        
        return viewModel.HasPrograms.ToProperty();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a ViewModel with the given programs
    /// </summary>
    private static UnifiedEditorViewModel CreateViewModelWithPrograms(List<TabelaProgram> programs)
    {
        var viewModel = new UnifiedEditorViewModel();
        
        // Clear default programs and add the provided ones
        viewModel.Programs.Clear();
        foreach (var program in programs)
        {
            viewModel.Programs.Add(program);
        }
        
        // Select the first program if available
        if (viewModel.Programs.Count > 0)
        {
            viewModel.SelectedProgram = viewModel.Programs[0];
        }
        
        // Force update of the Programs property to trigger UpdateNextProgramId
        var currentPrograms = viewModel.Programs;
        viewModel.Programs = currentPrograms;
        
        return viewModel;
    }

    #endregion
}
