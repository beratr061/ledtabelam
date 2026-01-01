using System.Collections.Generic;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Dahili varlık kütüphanesi interface'i
/// </summary>
public interface IAssetLibrary
{
    IReadOnlyList<AssetCategory> GetCategories();
    IReadOnlyList<AssetInfo> GetAssetsByCategory(string categoryName);
    IReadOnlyList<AssetInfo> GetAllAssets();
    AssetInfo? GetAsset(string assetName);
    SKBitmap? RenderAsset(string assetName, int size, SKColor tintColor);
    bool AddUserAsset(string name, string category, string svgPath);
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
    public bool IsBitmap { get; set; } = false;
    public int BitmapWidth { get; set; }
    public int BitmapHeight { get; set; }
    public int[][]? BitmapPixels { get; set; }
    public uint[][]? BitmapColors { get; set; }
    public bool IsMultiColor { get; set; } = false;
}
