using System.Threading.Tasks;
using LEDTabelam.Maui.Models;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Proje yönetimi servisi interface'i
/// Ekran, program ve içerik hiyerarşisini yönetir
/// </summary>
public interface IProjectManager
{
    /// <summary>
    /// Mevcut açık proje
    /// </summary>
    Project CurrentProject { get; }

    /// <summary>
    /// Yeni bir proje oluşturur
    /// </summary>
    Task<Project> NewProjectAsync();

    /// <summary>
    /// Belirtilen dosyadan proje yükler
    /// </summary>
    Task<Project> LoadProjectAsync(string filePath);

    /// <summary>
    /// Projeyi belirtilen dosyaya kaydeder
    /// </summary>
    Task SaveProjectAsync(string filePath);

    /// <summary>
    /// Projeyi mevcut dosyasına kaydeder
    /// </summary>
    Task SaveProjectAsync();

    /// <summary>
    /// Projeye yeni ekran ekler
    /// </summary>
    void AddScreen(ScreenNode screen);

    /// <summary>
    /// Projeden ekran kaldırır
    /// </summary>
    void RemoveScreen(ScreenNode screen);

    /// <summary>
    /// Ekrana yeni program ekler
    /// </summary>
    void AddProgram(ScreenNode screen, ProgramNode program);

    /// <summary>
    /// Ekrandan program kaldırır
    /// </summary>
    void RemoveProgram(ProgramNode program);

    /// <summary>
    /// Programa yeni içerik ekler
    /// </summary>
    void AddContent(ProgramNode program, ContentItem content);

    /// <summary>
    /// Programdan içerik kaldırır
    /// </summary>
    void RemoveContent(ContentItem content);

    /// <summary>
    /// Otomatik ekran ismi oluşturur (Ekran1, Ekran2, ...)
    /// </summary>
    string GenerateScreenName();

    /// <summary>
    /// Otomatik program ismi oluşturur (Program1, Program2, ...)
    /// </summary>
    string GenerateProgramName(ScreenNode screen);

    /// <summary>
    /// Belirtilen ekranı bulur
    /// </summary>
    ScreenNode? FindScreen(string screenId);

    /// <summary>
    /// Belirtilen programı bulur
    /// </summary>
    ProgramNode? FindProgram(string programId);

    /// <summary>
    /// Belirtilen içeriği bulur
    /// </summary>
    ContentItem? FindContent(string contentId);

    /// <summary>
    /// Programın ait olduğu ekranı bulur
    /// </summary>
    ScreenNode? FindParentScreen(ProgramNode program);

    /// <summary>
    /// İçeriğin ait olduğu programı bulur
    /// </summary>
    ProgramNode? FindParentProgram(ContentItem content);
}
