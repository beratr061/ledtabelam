using System.Collections.Generic;
using System.Threading.Tasks;
using LEDTabelam.Maui.Models;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Profil yönetimi servisi interface'i
/// </summary>
public interface IProfileManager
{
    /// <summary>
    /// Tüm profilleri listeler
    /// </summary>
    Task<List<Profile>> GetAllProfilesAsync();

    /// <summary>
    /// Belirtilen isimli profili yükler
    /// </summary>
    Task<Profile?> LoadProfileAsync(string name);

    /// <summary>
    /// Profili JSON formatında kaydeder
    /// </summary>
    Task SaveProfileAsync(Profile profile);

    /// <summary>
    /// Belirtilen isimli profili siler
    /// </summary>
    Task<bool> DeleteProfileAsync(string name);

    /// <summary>
    /// Profili kopyalar
    /// </summary>
    Task<Profile> DuplicateProfileAsync(string sourceName, string newName);

    /// <summary>
    /// Profili tek dosya olarak dışa aktarır
    /// </summary>
    Task ExportProfileAsync(Profile profile, string filePath);

    /// <summary>
    /// Profili dosyadan içe aktarır
    /// </summary>
    Task<Profile> ImportProfileAsync(string filePath);

    /// <summary>
    /// Varsayılan profili oluşturur veya döndürür
    /// </summary>
    Task<Profile> GetOrCreateDefaultProfileAsync();

    /// <summary>
    /// Profil adının kullanılabilir olup olmadığını kontrol eder
    /// </summary>
    Task<bool> IsProfileNameAvailableAsync(string name);
}
