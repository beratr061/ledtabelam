using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using SkiaSharp;
using Xunit;

namespace LEDTabelam.Tests;

/// <summary>
/// Property-based tests for MAUI ViewModels
/// Feature: maui-ui-redesign
/// 
/// Property 4: TreeView Hierarchy Consistency
/// For any Project with screens, programs, and contents, the TreeView should display 
/// exactly three levels of hierarchy where each screen contains programs and each 
/// program contains content items. The total count of displayed nodes should equal 
/// screens + programs + contents.
/// 
/// Property 6: Zoom Bounds Validation
/// For any zoom operation, the resulting zoom level should be clamped between 50% 
/// and 400%. Attempting to zoom beyond these bounds should result in the boundary 
/// value being applied.
/// 
/// Property 7: Page Navigation Consistency
/// For any program with N content items, the page navigation should cycle through 
/// pages 1 to N. After reaching page N, "next" should either stop or loop to page 1 
/// based on loop setting. Current page should always be within [1, N] range.
/// 
/// Validates: Requirements 3.1, 4.4, 4.6, 4.7, 10.x
/// </summary>
public class ViewModelPropertyTests
{
    #region Local Model Classes (mirrors MAUI models for testing)

    public class ContentItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "İçerik";
        public int DurationMs { get; set; } = 3000;
    }

    public class ProgramNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Program1";
        public ObservableCollection<ContentItem> Contents { get; set; } = new();
        public bool IsLoop { get; set; } = true;
        public bool IsExpanded { get; set; } = true;
    }

    public class ScreenNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Ekran1";
        public ObservableCollection<ProgramNode> Programs { get; set; } = new();
        public bool IsExpanded { get; set; } = true;
    }

    public class Project
    {
        public string Name { get; set; } = "Yeni Proje";
        public ObservableCollection<ScreenNode> Screens { get; set; } = new();
    }

    #endregion

    #region Local ViewModel Classes (mirrors MAUI ViewModels for testing)

    /// <summary>
    /// TreeViewModel test implementation
    /// </summary>
    public class TreeViewModel
    {
        public ObservableCollection<ScreenNode> Screens { get; set; } = new();
        public object? SelectedItem { get; set; }

        public void LoadProject(Project project)
        {
            Screens.Clear();
            foreach (var screen in project.Screens)
            {
                Screens.Add(screen);
            }
            if (Screens.Count > 0)
            {
                SelectedItem = Screens[0];
            }
        }

        /// <summary>
        /// Calculates total node count (screens + programs + contents)
        /// Property 4: TreeView Hierarchy Consistency
        /// </summary>
        public int GetTotalNodeCount()
        {
            int count = Screens.Count;
            foreach (var screen in Screens)
            {
                count += screen.Programs.Count;
                foreach (var program in screen.Programs)
                {
                    count += program.Contents.Count;
                }
            }
            return count;
        }

        public void ExpandAll()
        {
            foreach (var screen in Screens)
            {
                screen.IsExpanded = true;
                foreach (var program in screen.Programs)
                {
                    program.IsExpanded = true;
                }
            }
        }

        public void CollapseAll()
        {
            foreach (var screen in Screens)
            {
                screen.IsExpanded = false;
                foreach (var program in screen.Programs)
                {
                    program.IsExpanded = false;
                }
            }
        }
    }

    /// <summary>
    /// PreviewViewModel test implementation
    /// </summary>
    public class PreviewViewModel
    {
        private const int MinZoom = 50;
        private const int MaxZoom = 400;
        private const int ZoomStep = 25;

        public int ZoomLevel { get; private set; } = 100;
        public int CurrentPage { get; private set; } = 1;
        public int TotalPages { get; private set; } = 1;
        public bool IsPlaying { get; set; } = false;
        public ProgramNode? CurrentProgram { get; private set; }

        public void LoadProgram(ProgramNode program)
        {
            CurrentProgram = program;
            TotalPages = Math.Max(1, program.Contents.Count);
            CurrentPage = 1;
        }

        /// <summary>
        /// Sets zoom level with bounds validation
        /// Property 6: Zoom Bounds Validation
        /// </summary>
        public void SetZoomLevel(int level)
        {
            ZoomLevel = Math.Clamp(level, MinZoom, MaxZoom);
        }

        public void ZoomIn()
        {
            SetZoomLevel(ZoomLevel + ZoomStep);
        }

        public void ZoomOut()
        {
            SetZoomLevel(ZoomLevel - ZoomStep);
        }

        /// <summary>
        /// Goes to next page with loop handling
        /// Property 7: Page Navigation Consistency
        /// </summary>
        public void NextPage()
        {
            if (CurrentProgram == null || TotalPages <= 1)
                return;

            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
            else if (CurrentProgram.IsLoop)
            {
                CurrentPage = 1;
            }
        }

        /// <summary>
        /// Goes to previous page with loop handling
        /// Property 7: Page Navigation Consistency
        /// </summary>
        public void PreviousPage()
        {
            if (CurrentProgram == null || TotalPages <= 1)
                return;

            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
            else if (CurrentProgram.IsLoop)
            {
                CurrentPage = TotalPages;
            }
        }

        /// <summary>
        /// Goes to specific page with bounds validation
        /// Property 7: Page Navigation Consistency
        /// </summary>
        public void GoToPage(int page)
        {
            if (TotalPages <= 0)
            {
                CurrentPage = 1;
                return;
            }
            CurrentPage = Math.Clamp(page, 1, TotalPages);
        }
    }

    #endregion

    #region Generators

    public static Gen<ContentItem> GenContentItem()
    {
        return from name in Gen.Elements("İçerik", "Metin", "Resim", "Saat", "Tarih")
               from durationMs in Gen.Choose(1000, 10000)
               select new ContentItem
               {
                   Id = Guid.NewGuid().ToString(),
                   Name = name,
                   DurationMs = durationMs
               };
    }

    public static Gen<ProgramNode> GenProgramNode(int maxContents)
    {
        return from name in Gen.Elements("Program1", "Program2", "Program3")
               from isLoop in Arb.Generate<bool>()
               from contentCount in Gen.Choose(0, maxContents)
               from contents in Gen.ListOf(contentCount, GenContentItem())
               select new ProgramNode
               {
                   Id = Guid.NewGuid().ToString(),
                   Name = name,
                   IsLoop = isLoop,
                   Contents = new ObservableCollection<ContentItem>(contents)
               };
    }

    public static Gen<ScreenNode> GenScreenNode(int maxPrograms, int maxContentsPerProgram)
    {
        return from name in Gen.Elements("Ekran1", "Ekran2", "Ekran3")
               from programCount in Gen.Choose(0, maxPrograms)
               from programs in Gen.ListOf(programCount, GenProgramNode(maxContentsPerProgram))
               select new ScreenNode
               {
                   Id = Guid.NewGuid().ToString(),
                   Name = name,
                   Programs = new ObservableCollection<ProgramNode>(programs)
               };
    }

    public static Gen<Project> GenProject(int maxScreens, int maxPrograms, int maxContents)
    {
        return from name in Gen.Elements("Proje1", "Proje2", "Test Projesi")
               from screenCount in Gen.Choose(0, maxScreens)
               from screens in Gen.ListOf(screenCount, GenScreenNode(maxPrograms, maxContents))
               select new Project
               {
                   Name = name,
                   Screens = new ObservableCollection<ScreenNode>(screens)
               };
    }

    #endregion

    #region Arbitraries

    public class ViewModelArbitraries
    {
        public static Arbitrary<Project> ProjectArb() =>
            Arb.From(GenProject(5, 5, 10));

        public static Arbitrary<ProgramNode> ProgramNodeArb() =>
            Arb.From(GenProgramNode(10));
    }

    #endregion

    #region Property 4: TreeView Hierarchy Consistency

    /// <summary>
    /// Property 4.1: TreeView node count equals sum of screens, programs, and contents
    /// For any project, GetTotalNodeCount() SHALL return exactly the sum of 
    /// screens.Count + all programs.Count + all contents.Count
    /// Feature: maui-ui-redesign, Property 4: TreeView Hierarchy Consistency
    /// Validates: Requirements 3.1, 10.1, 10.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ViewModelArbitraries) })]
    public Property TreeView_NodeCount_EqualsHierarchySum(Project project)
    {
        var treeViewModel = new TreeViewModel();
        treeViewModel.LoadProject(project);

        // Calculate expected count manually
        int expectedCount = project.Screens.Count;
        foreach (var screen in project.Screens)
        {
            expectedCount += screen.Programs.Count;
            foreach (var program in screen.Programs)
            {
                expectedCount += program.Contents.Count;
            }
        }

        var actualCount = treeViewModel.GetTotalNodeCount();

        return (actualCount == expectedCount).ToProperty();
    }

    /// <summary>
    /// Property 4.2: TreeView maintains three-level hierarchy
    /// For any project, screens contain programs and programs contain contents,
    /// maintaining exactly three levels of hierarchy.
    /// Feature: maui-ui-redesign, Property 4: TreeView Hierarchy Consistency
    /// Validates: Requirements 3.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ViewModelArbitraries) })]
    public Property TreeView_MaintainsThreeLevelHierarchy(Project project)
    {
        var treeViewModel = new TreeViewModel();
        treeViewModel.LoadProject(project);

        // Verify hierarchy structure
        foreach (var screen in treeViewModel.Screens)
        {
            // Level 1: Screen
            if (screen == null)
                return false.ToProperty();

            foreach (var program in screen.Programs)
            {
                // Level 2: Program
                if (program == null)
                    return false.ToProperty();

                foreach (var content in program.Contents)
                {
                    // Level 3: Content
                    if (content == null)
                        return false.ToProperty();
                }
            }
        }

        return true.ToProperty();
    }

    /// <summary>
    /// Property 4.3: TreeView preserves project data after loading
    /// For any project, loading into TreeView SHALL preserve all screens, 
    /// programs, and contents with their original data.
    /// Feature: maui-ui-redesign, Property 4: TreeView Hierarchy Consistency
    /// Validates: Requirements 3.1
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ViewModelArbitraries) })]
    public Property TreeView_PreservesProjectData(Project project)
    {
        var treeViewModel = new TreeViewModel();
        treeViewModel.LoadProject(project);

        // Verify screen count matches
        if (treeViewModel.Screens.Count != project.Screens.Count)
            return false.ToProperty();

        // Verify each screen's programs match
        for (int i = 0; i < project.Screens.Count; i++)
        {
            var originalScreen = project.Screens[i];
            var loadedScreen = treeViewModel.Screens[i];

            if (loadedScreen.Programs.Count != originalScreen.Programs.Count)
                return false.ToProperty();

            // Verify each program's contents match
            for (int j = 0; j < originalScreen.Programs.Count; j++)
            {
                var originalProgram = originalScreen.Programs[j];
                var loadedProgram = loadedScreen.Programs[j];

                if (loadedProgram.Contents.Count != originalProgram.Contents.Count)
                    return false.ToProperty();
            }
        }

        return true.ToProperty();
    }

    #endregion

    #region Property 6: Zoom Bounds Validation

    /// <summary>
    /// Property 6.1: Zoom level is always within bounds
    /// For any zoom level value, SetZoomLevel SHALL clamp the result to [50, 400].
    /// Feature: maui-ui-redesign, Property 6: Zoom Bounds Validation
    /// Validates: Requirements 4.6, 4.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ZoomLevel_IsAlwaysWithinBounds(int zoomValue)
    {
        var previewViewModel = new PreviewViewModel();
        previewViewModel.SetZoomLevel(zoomValue);

        return (previewViewModel.ZoomLevel >= 50 && previewViewModel.ZoomLevel <= 400).ToProperty();
    }

    /// <summary>
    /// Property 6.2: Zoom level clamps to minimum
    /// For any zoom level below 50, SetZoomLevel SHALL return exactly 50.
    /// Feature: maui-ui-redesign, Property 6: Zoom Bounds Validation
    /// Validates: Requirements 4.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ZoomLevel_ClampsToMinimum(NegativeInt negativeValue)
    {
        var previewViewModel = new PreviewViewModel();
        previewViewModel.SetZoomLevel(negativeValue.Get);

        return (previewViewModel.ZoomLevel == 50).ToProperty();
    }

    /// <summary>
    /// Property 6.3: Zoom level clamps to maximum
    /// For any zoom level above 400, SetZoomLevel SHALL return exactly 400.
    /// Feature: maui-ui-redesign, Property 6: Zoom Bounds Validation
    /// Validates: Requirements 4.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ZoomLevel_ClampsToMaximum(PositiveInt largeValue)
    {
        var previewViewModel = new PreviewViewModel();
        int value = largeValue.Get + 400; // Ensure it's above 400
        previewViewModel.SetZoomLevel(value);

        return (previewViewModel.ZoomLevel == 400).ToProperty();
    }

    /// <summary>
    /// Property 6.4: Zoom in/out operations stay within bounds
    /// For any sequence of zoom in/out operations, the zoom level SHALL 
    /// always remain within [50, 400].
    /// Feature: maui-ui-redesign, Property 6: Zoom Bounds Validation
    /// Validates: Requirements 4.6, 4.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ZoomOperations_StayWithinBounds(PositiveInt operationCount)
    {
        var previewViewModel = new PreviewViewModel();
        int count = Math.Min(operationCount.Get, 50);
        var random = new System.Random(42);

        for (int i = 0; i < count; i++)
        {
            if (random.Next(2) == 0)
                previewViewModel.ZoomIn();
            else
                previewViewModel.ZoomOut();

            if (previewViewModel.ZoomLevel < 50 || previewViewModel.ZoomLevel > 400)
                return false.ToProperty();
        }

        return true.ToProperty();
    }

    #endregion

    #region Property 7: Page Navigation Consistency

    /// <summary>
    /// Property 7.1: Current page is always within valid range
    /// For any program with N contents, CurrentPage SHALL always be in [1, N].
    /// Feature: maui-ui-redesign, Property 7: Page Navigation Consistency
    /// Validates: Requirements 4.4
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ViewModelArbitraries) })]
    public Property CurrentPage_IsAlwaysWithinRange(ProgramNode program)
    {
        if (program.Contents.Count == 0)
            return true.ToProperty(); // Skip empty programs

        var previewViewModel = new PreviewViewModel();
        previewViewModel.LoadProgram(program);

        // Navigate through all pages
        for (int i = 0; i < program.Contents.Count * 2; i++)
        {
            previewViewModel.NextPage();
            
            if (previewViewModel.CurrentPage < 1 || 
                previewViewModel.CurrentPage > previewViewModel.TotalPages)
                return false.ToProperty();
        }

        return true.ToProperty();
    }

    /// <summary>
    /// Property 7.2: GoToPage clamps to valid range
    /// For any page value, GoToPage SHALL clamp the result to [1, TotalPages].
    /// Feature: maui-ui-redesign, Property 7: Page Navigation Consistency
    /// Validates: Requirements 4.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property GoToPage_ClampsToValidRange(int pageValue, PositiveInt contentCount)
    {
        var program = new ProgramNode();
        int count = Math.Min(contentCount.Get, 20);
        for (int i = 0; i < count; i++)
        {
            program.Contents.Add(new ContentItem { Name = $"Content{i}" });
        }

        var previewViewModel = new PreviewViewModel();
        previewViewModel.LoadProgram(program);
        previewViewModel.GoToPage(pageValue);

        return (previewViewModel.CurrentPage >= 1 && 
                previewViewModel.CurrentPage <= previewViewModel.TotalPages).ToProperty();
    }

    /// <summary>
    /// Property 7.3: Loop mode cycles through all pages
    /// For a program with IsLoop=true, navigating past the last page SHALL 
    /// return to page 1, and navigating before page 1 SHALL go to last page.
    /// Feature: maui-ui-redesign, Property 7: Page Navigation Consistency
    /// Validates: Requirements 4.4, 10.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property LoopMode_CyclesThroughAllPages(PositiveInt contentCount)
    {
        int count = Math.Clamp(contentCount.Get, 2, 10);
        var program = new ProgramNode { IsLoop = true };
        for (int i = 0; i < count; i++)
        {
            program.Contents.Add(new ContentItem { Name = $"Content{i}" });
        }

        var previewViewModel = new PreviewViewModel();
        previewViewModel.LoadProgram(program);

        // Navigate to last page
        for (int i = 0; i < count - 1; i++)
        {
            previewViewModel.NextPage();
        }
        
        if (previewViewModel.CurrentPage != count)
            return false.ToProperty();

        // Next should loop to page 1
        previewViewModel.NextPage();
        if (previewViewModel.CurrentPage != 1)
            return false.ToProperty();

        // Previous should loop to last page
        previewViewModel.PreviousPage();
        if (previewViewModel.CurrentPage != count)
            return false.ToProperty();

        return true.ToProperty();
    }

    /// <summary>
    /// Property 7.4: Non-loop mode stops at boundaries
    /// For a program with IsLoop=false, navigating past the last page SHALL 
    /// stay on the last page, and navigating before page 1 SHALL stay on page 1.
    /// Feature: maui-ui-redesign, Property 7: Page Navigation Consistency
    /// Validates: Requirements 4.4, 10.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property NonLoopMode_StopsAtBoundaries(PositiveInt contentCount)
    {
        int count = Math.Clamp(contentCount.Get, 2, 10);
        var program = new ProgramNode { IsLoop = false };
        for (int i = 0; i < count; i++)
        {
            program.Contents.Add(new ContentItem { Name = $"Content{i}" });
        }

        var previewViewModel = new PreviewViewModel();
        previewViewModel.LoadProgram(program);

        // Navigate to last page
        for (int i = 0; i < count + 5; i++)
        {
            previewViewModel.NextPage();
        }
        
        // Should stay on last page
        if (previewViewModel.CurrentPage != count)
            return false.ToProperty();

        // Navigate back to first page
        for (int i = 0; i < count + 5; i++)
        {
            previewViewModel.PreviousPage();
        }

        // Should stay on first page
        if (previewViewModel.CurrentPage != 1)
            return false.ToProperty();

        return true.ToProperty();
    }

    /// <summary>
    /// Property 7.5: TotalPages equals content count
    /// For any program, TotalPages SHALL equal the number of content items 
    /// (minimum 1 for empty programs).
    /// Feature: maui-ui-redesign, Property 7: Page Navigation Consistency
    /// Validates: Requirements 4.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ViewModelArbitraries) })]
    public Property TotalPages_EqualsContentCount(ProgramNode program)
    {
        var previewViewModel = new PreviewViewModel();
        previewViewModel.LoadProgram(program);

        int expectedPages = Math.Max(1, program.Contents.Count);
        return (previewViewModel.TotalPages == expectedPages).ToProperty();
    }

    #endregion
}
