using System.Collections.Generic;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Dahili varlık kütüphanesi interface'i
/// Requirements: 18.1, 18.2, 18.3, 18.4, 18.5
/// </summary>
public interface IAssetLibrary
{
    /// <summary>
    /// Tüm kategorileri döndürür
    /// </summary>
    IReadOnlyList<AssetCategory> GetCategories();

    /// <summary>
    /// Belirtilen kategorideki tüm ikonları döndürür
    /// </summary>
    /// <param name="categoryName">Kategori adı</param>
    IReadOnlyList<AssetInfo> GetAssetsByCategory(string categoryName);

    /// <summary>
    /// Tüm ikonları döndürür
    /// </summary>
    IReadOnlyList<AssetInfo> GetAllAssets();

    /// <summary>
    /// İkon adına göre varlık bilgisini döndürür
    /// </summary>
    /// <param name="assetName">Varlık adı</param>
    AssetInfo? GetAsset(string assetName);

    /// <summary>
    /// İkonu belirtilen boyut ve renkle render eder
    /// </summary>
    /// <param name="assetName">Varlık adı</param>
    /// <param name="size">Hedef boyut (16 veya 32)</param>
    /// <param name="tintColor">Boyama rengi</param>
    SKBitmap? RenderAsset(string assetName, int size, SKColor tintColor);

    /// <summary>
    /// Kullanıcı ikonunu kütüphaneye ekler
    /// </summary>
    /// <param name="name">İkon adı</param>
    /// <param name="category">Kategori</param>
    /// <param name="svgPath">SVG dosya yolu</param>
    /// <returns>Ekleme başarılı ise true</returns>
    bool AddUserAsset(string name, string category, string svgPath);

    /// <summary>
    /// Kullanıcı ikonunu kütüphaneden kaldırır
    /// </summary>
    /// <param name="assetName">Varlık adı</param>
    /// <returns>Kaldırma başarılı ise true</returns>
    bool RemoveUserAsset(string assetName);
}

/// <summary>
/// Varlık kategorisi
/// </summary>
public class AssetCategory
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; } = true;
}

/// <summary>
/// Varlık bilgisi
/// </summary>
public class AssetInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SvgContent { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public bool IsBuiltIn { get; set; } = true;
    public bool Has16px { get; set; } = true;
    public bool Has32px { get; set; } = true;
}
