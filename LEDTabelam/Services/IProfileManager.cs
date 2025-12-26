using System.Collections.Generic;
using System.Threading.Tasks;
using LEDTabelam.Models;

namespace LEDTabelam.Services;

/// <summary>
/// Profil yönetimi servisi interface'i
/// Requirements: 9.1, 9.7, 9.8, 9.9, 9.10, 9.11
/// </summary>
public interface IProfileManager
{
    /// <summary>
    /// Tüm profilleri listeler
    /// </summary>
    /// <returns>Profil listesi</returns>
    Task<List<Profile>> GetAllProfilesAsync();

    /// <summary>
    /// Belirtilen isimli profili yükler
    /// </summary>
    /// <param name="name">Profil adı</param>
    /// <returns>Yüklenen profil veya null</returns>
    Task<Profile?> LoadProfileAsync(string name);

    /// <summary>
    /// Profili JSON formatında kaydeder
    /// </summary>
    /// <param name="profile">Kaydedilecek profil</param>
    Task SaveProfileAsync(Profile profile);

    /// <summary>
    /// Belirtilen isimli profili siler (onay sonrası)
    /// </summary>
    /// <param name="name">Silinecek profil adı</param>
    /// <returns>Silme başarılı ise true</returns>
    Task<bool> DeleteProfileAsync(string name);

    /// <summary>
    /// Profili kopyalar
    /// </summary>
    /// <param name="sourceName">Kaynak profil adı</param>
    /// <param name="newName">Yeni profil adı</param>
    /// <returns>Kopyalanan profil</returns>
    Task<Profile> DuplicateProfileAsync(string sourceName, string newName);

    /// <summary>
    /// Profili tek dosya olarak dışa aktarır
    /// </summary>
    /// <param name="profile">Dışa aktarılacak profil</param>
    /// <param name="filePath">Hedef dosya yolu</param>
    Task ExportProfileAsync(Profile profile, string filePath);

    /// <summary>
    /// Profili dosyadan içe aktarır
    /// </summary>
    /// <param name="filePath">Kaynak dosya yolu</param>
    /// <returns>İçe aktarılan profil</returns>
    Task<Profile> ImportProfileAsync(string filePath);

    /// <summary>
    /// Varsayılan profili oluşturur veya döndürür
    /// </summary>
    /// <returns>Varsayılan profil</returns>
    Task<Profile> GetOrCreateDefaultProfileAsync();

    /// <summary>
    /// Profil adının kullanılabilir olup olmadığını kontrol eder
    /// </summary>
    /// <param name="name">Kontrol edilecek ad</param>
    /// <returns>Ad kullanılabilir ise true</returns>
    Task<bool> IsProfileNameAvailableAsync(string name);
}
