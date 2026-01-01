using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using LEDTabelam.Models;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for Profile program collection management
/// Feature: program-ve-ara-durak
/// </summary>
public class ProfilePropertyTests
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
    /// Generates a list of TabelaPrograms with unique IDs
    /// </summary>
    public static Gen<List<TabelaProgram>> GenTabelaProgramList()
    {
        return Gen.Choose(1, 10).SelectMany(count =>
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
    /// Generates a Profile with at least one program
    /// </summary>
    public static Gen<Profile> GenProfileWithPrograms()
    {
        return from programs in GenTabelaProgramList()
               select CreateProfileWithPrograms(programs);
    }

    private static Profile CreateProfileWithPrograms(List<TabelaProgram> programs)
    {
        var profile = new Profile { Name = "Test Profile" };
        foreach (var program in programs)
        {
            profile.Programs.Add(program);
        }
        return profile;
    }

    #endregion

    #region Arbitraries

    public class ProfileArbitraries
    {
        public static Arbitrary<Profile> ProfileWithProgramsArb() =>
            Arb.From(GenProfileWithPrograms());

        public static Arbitrary<List<TabelaProgram>> TabelaProgramListArb() =>
            Arb.From(GenTabelaProgramList());
    }

    #endregion


    #region Property 1: Program Koleksiyonu Minimum Boyut Invariantı

    /// <summary>
    /// Property 1: Program Koleksiyonu Minimum Boyut Invariantı
    /// For any Profile, the Programs collection SHALL always contain at least one program.
    /// Attempting to remove the last program SHALL be rejected.
    /// Feature: program-ve-ara-durak, Property 1: Program Koleksiyonu Minimum Boyut Invariantı
    /// Validates: Requirements 1.8
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProfileArbitraries) })]
    public Property RemovingLastProgramIsRejected(Profile profile)
    {
        // Ensure profile has exactly one program
        while (profile.Programs.Count > 1)
        {
            profile.Programs.RemoveAt(profile.Programs.Count - 1);
        }

        // Try to remove the last program
        var lastProgram = profile.Programs[0];
        var result = profile.RemoveProgram(lastProgram);

        // Should be rejected (return false) and program should still exist
        return (!result && profile.Programs.Count == 1).ToProperty();
    }

    /// <summary>
    /// Property 1.2: EnsureMinimumProgram creates program when empty
    /// For any empty Profile, calling EnsureMinimumProgram SHALL result in exactly one program.
    /// Feature: program-ve-ara-durak, Property 1: Program Koleksiyonu Minimum Boyut Invariantı
    /// Validates: Requirements 1.8
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EnsureMinimumProgramCreatesOneWhenEmpty()
    {
        var profile = new Profile { Name = "Empty Profile" };
        
        profile.EnsureMinimumProgram();
        
        return (profile.Programs.Count == 1).ToProperty();
    }

    /// <summary>
    /// Property 1.3: EnsureMinimumProgram does not add when programs exist
    /// For any Profile with programs, calling EnsureMinimumProgram SHALL not change the count.
    /// Feature: program-ve-ara-durak, Property 1: Program Koleksiyonu Minimum Boyut Invariantı
    /// Validates: Requirements 1.8
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProfileArbitraries) })]
    public Property EnsureMinimumProgramDoesNotAddWhenProgramsExist(Profile profile)
    {
        var initialCount = profile.Programs.Count;
        
        profile.EnsureMinimumProgram();
        
        return (profile.Programs.Count == initialCount).ToProperty();
    }

    /// <summary>
    /// Property 1.4: RemoveProgram succeeds when more than one program exists
    /// For any Profile with N > 1 programs, removing a program SHALL succeed and result in N-1 programs.
    /// Feature: program-ve-ara-durak, Property 1: Program Koleksiyonu Minimum Boyut Invariantı
    /// Validates: Requirements 1.7, 1.8
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProfileArbitraries) })]
    public Property RemoveProgramSucceedsWhenMoreThanOne(Profile profile)
    {
        if (profile.Programs.Count <= 1)
            return true.ToProperty();

        var initialCount = profile.Programs.Count;
        var programToRemove = profile.Programs[0];
        
        var result = profile.RemoveProgram(programToRemove);
        
        return (result && profile.Programs.Count == initialCount - 1).ToProperty();
    }

    #endregion


    #region Property 2: Program ID Benzersizliği

    /// <summary>
    /// Property 2: Program ID Benzersizliği
    /// For any set of programs in a Profile, all program IDs SHALL be unique.
    /// No two programs can have the same ID.
    /// Feature: program-ve-ara-durak, Property 2: Program ID Benzersizliği
    /// Validates: Requirements 1.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProfileArbitraries) })]
    public Property AllProgramIdsAreUnique(Profile profile)
    {
        var ids = profile.Programs.Select(p => p.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();
        
        return (ids.Count == uniqueIds.Count).ToProperty();
    }

    /// <summary>
    /// Property 2.2: AddProgram assigns unique ID
    /// For any Profile, adding a new program SHALL assign an ID that is unique among all programs.
    /// Feature: program-ve-ara-durak, Property 2: Program ID Benzersizliği
    /// Validates: Requirements 1.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProfileArbitraries) })]
    public Property AddProgramAssignsUniqueId(Profile profile)
    {
        var existingIds = profile.Programs.Select(p => p.Id).ToHashSet();
        
        var newProgram = profile.AddProgram("New Program");
        
        // New ID should not have existed before
        return (!existingIds.Contains(newProgram.Id)).ToProperty();
    }

    /// <summary>
    /// Property 2.3: Multiple AddProgram calls maintain uniqueness
    /// For any Profile, adding multiple programs SHALL result in all unique IDs.
    /// Feature: program-ve-ara-durak, Property 2: Program ID Benzersizliği
    /// Validates: Requirements 1.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property MultipleAddProgramMaintainsUniqueness(PositiveInt addCount)
    {
        var profile = new Profile { Name = "Test Profile" };
        profile.EnsureMinimumProgram();
        
        var count = System.Math.Min(addCount.Get, 20); // Limit to reasonable number
        for (int i = 0; i < count; i++)
        {
            profile.AddProgram($"Program {i + 2}");
        }
        
        var ids = profile.Programs.Select(p => p.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();
        
        return (ids.Count == uniqueIds.Count).ToProperty();
    }

    /// <summary>
    /// Property 2.4: GetProgramById returns correct program
    /// For any Profile with programs, GetProgramById SHALL return the program with matching ID or null.
    /// Feature: program-ve-ara-durak, Property 2: Program ID Benzersizliği
    /// Validates: Requirements 1.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProfileArbitraries) })]
    public Property GetProgramByIdReturnsCorrectProgram(Profile profile)
    {
        if (profile.Programs.Count == 0)
            return true.ToProperty();

        var randomProgram = profile.Programs[0];
        var foundProgram = profile.GetProgramById(randomProgram.Id);
        
        return (foundProgram != null && foundProgram.Id == randomProgram.Id).ToProperty();
    }

    /// <summary>
    /// Property 2.5: GetProgramById returns null for non-existent ID
    /// For any Profile, GetProgramById with non-existent ID SHALL return null.
    /// Feature: program-ve-ara-durak, Property 2: Program ID Benzersizliği
    /// Validates: Requirements 1.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ProfileArbitraries) })]
    public Property GetProgramByIdReturnsNullForNonExistentId(Profile profile)
    {
        var maxId = profile.Programs.Count > 0 ? profile.Programs.Max(p => p.Id) : 0;
        var nonExistentId = maxId + 1000;
        
        var foundProgram = profile.GetProgramById(nonExistentId);
        
        return (foundProgram == null).ToProperty();
    }

    #endregion
}
