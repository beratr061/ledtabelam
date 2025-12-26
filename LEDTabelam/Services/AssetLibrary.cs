using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Dahili varlık kütüphanesi implementasyonu
/// Requirements: 18.1, 18.2, 18.3, 18.4, 18.5
/// </summary>
public class AssetLibrary : IAssetLibrary
{
    private readonly ISvgRenderer _svgRenderer;
    private readonly List<AssetCategory> _categories;
    private readonly Dictionary<string, AssetInfo> _assets;
    private readonly string _userAssetsPath;

    public AssetLibrary(ISvgRenderer svgRenderer)
    {
        _svgRenderer = svgRenderer ?? throw new ArgumentNullException(nameof(svgRenderer));
        _categories = new List<AssetCategory>();
        _assets = new Dictionary<string, AssetInfo>(StringComparer.OrdinalIgnoreCase);
        _userAssetsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LEDTabelam", "UserAssets");

        InitializeBuiltInCategories();
        InitializeBuiltInAssets();
        LoadUserAssets();
    }

    private void InitializeBuiltInCategories()
    {
        _categories.AddRange(new[]
        {
            new AssetCategory { Name = "flags", DisplayName = "Bayraklar", IsBuiltIn = true },
            new AssetCategory { Name = "accessibility", DisplayName = "Erişilebilirlik", IsBuiltIn = true },
            new AssetCategory { Name = "arrows", DisplayName = "Yön Okları", IsBuiltIn = true },
            new AssetCategory { Name = "transport", DisplayName = "Ulaşım Sembolleri", IsBuiltIn = true },
            new AssetCategory { Name = "user", DisplayName = "Kullanıcı İkonları", IsBuiltIn = false }
        });
    }

    private void InitializeBuiltInAssets()
    {
        // Türk Bayrağı (16px pixel-perfect)
        AddBuiltInAsset("turkish_flag", "Türk Bayrağı", "flags", GetTurkishFlagSvg());

        // Erişilebilirlik İkonları
        AddBuiltInAsset("wheelchair", "Engelli İkonu", "accessibility", GetWheelchairSvg());
        AddBuiltInAsset("hearing", "İşitme Engelli", "accessibility", GetHearingSvg());
        AddBuiltInAsset("visual", "Görme Engelli", "accessibility", GetVisualSvg());

        // Yön Okları
        AddBuiltInAsset("arrow_left", "Sol Ok", "arrows", GetArrowLeftSvg());
        AddBuiltInAsset("arrow_right", "Sağ Ok", "arrows", GetArrowRightSvg());
        AddBuiltInAsset("arrow_up", "Yukarı Ok", "arrows", GetArrowUpSvg());
        AddBuiltInAsset("arrow_down", "Aşağı Ok", "arrows", GetArrowDownSvg());

        // Ulaşım Sembolleri
        AddBuiltInAsset("bus", "Otobüs", "transport", GetBusSvg());
        AddBuiltInAsset("metro", "Metro", "transport", GetMetroSvg());
        AddBuiltInAsset("tram", "Tramvay", "transport", GetTramSvg());
        AddBuiltInAsset("ferry", "Vapur", "transport", GetFerrySvg());
    }

    private void AddBuiltInAsset(string name, string displayName, string category, string svgContent)
    {
        _assets[name] = new AssetInfo
        {
            Name = name,
            DisplayName = displayName,
            Category = category,
            SvgContent = svgContent,
            IsBuiltIn = true,
            Has16px = true,
            Has32px = true
        };
    }


    private void LoadUserAssets()
    {
        if (!Directory.Exists(_userAssetsPath))
            return;

        foreach (var file in Directory.GetFiles(_userAssetsPath, "*.svg"))
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var content = File.ReadAllText(file);
                _assets[name] = new AssetInfo
                {
                    Name = name,
                    DisplayName = name,
                    Category = "user",
                    SvgContent = content,
                    FilePath = file,
                    IsBuiltIn = false,
                    Has16px = true,
                    Has32px = true
                };
            }
            catch
            {
                // Hatalı dosyaları atla
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<AssetCategory> GetCategories() => _categories.AsReadOnly();

    /// <inheritdoc/>
    public IReadOnlyList<AssetInfo> GetAssetsByCategory(string categoryName)
    {
        return _assets.Values
            .Where(a => a.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public IReadOnlyList<AssetInfo> GetAllAssets() => _assets.Values.ToList().AsReadOnly();

    /// <inheritdoc/>
    public AssetInfo? GetAsset(string assetName)
    {
        return _assets.TryGetValue(assetName, out var asset) ? asset : null;
    }

    /// <inheritdoc/>
    public SKBitmap? RenderAsset(string assetName, int size, SKColor tintColor)
    {
        var asset = GetAsset(assetName);
        if (asset == null)
            return null;

        // Boyutu 16 veya 32'ye yuvarla
        int targetSize = size <= 24 ? 16 : 32;

        try
        {
            return _svgRenderer.RenderSvgFromContent(asset.SvgContent, targetSize, tintColor);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public bool AddUserAsset(string name, string category, string svgPath)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(svgPath))
            return false;

        if (!File.Exists(svgPath))
            return false;

        try
        {
            // Kullanıcı varlıkları klasörünü oluştur
            Directory.CreateDirectory(_userAssetsPath);

            var content = File.ReadAllText(svgPath);
            var destPath = Path.Combine(_userAssetsPath, $"{name}.svg");
            File.WriteAllText(destPath, content);

            _assets[name] = new AssetInfo
            {
                Name = name,
                DisplayName = name,
                Category = string.IsNullOrWhiteSpace(category) ? "user" : category,
                SvgContent = content,
                FilePath = destPath,
                IsBuiltIn = false,
                Has16px = true,
                Has32px = true
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public bool RemoveUserAsset(string assetName)
    {
        if (!_assets.TryGetValue(assetName, out var asset))
            return false;

        if (asset.IsBuiltIn)
            return false; // Dahili varlıklar silinemez

        try
        {
            if (!string.IsNullOrEmpty(asset.FilePath) && File.Exists(asset.FilePath))
                File.Delete(asset.FilePath);

            _assets.Remove(assetName);
            return true;
        }
        catch
        {
            return false;
        }
    }


    #region Built-in SVG Content

    // Pixel-perfect 16x16 SVG ikonları
    // Tüm ikonlar siyah/beyaz olarak tasarlanmış, LED rengine boyanabilir

    private static string GetTurkishFlagSvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <rect width=""16"" height=""16"" fill=""white""/>
  <circle cx=""6"" cy=""8"" r=""4"" fill=""white""/>
  <circle cx=""7"" cy=""8"" r=""3"" fill=""black""/>
  <polygon points=""10,8 12,6 11,8 12,10"" fill=""white""/>
</svg>";

    private static string GetWheelchairSvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <circle cx=""8"" cy=""3"" r=""2"" fill=""white""/>
  <path d=""M7 5 L7 9 L5 9 L5 11 L9 11 L9 13 L13 13 L13 11 L11 11 L11 9 L9 9 L9 5 Z"" fill=""white""/>
  <circle cx=""6"" cy=""13"" r=""2"" fill=""none"" stroke=""white"" stroke-width=""1""/>
</svg>";

    private static string GetHearingSvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <path d=""M4 6 Q4 2 8 2 Q12 2 12 6 L12 8 Q12 10 10 10 L10 12 Q10 14 8 14"" fill=""none"" stroke=""white"" stroke-width=""2""/>
  <circle cx=""8"" cy=""14"" r=""1"" fill=""white""/>
</svg>";

    private static string GetVisualSvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <ellipse cx=""8"" cy=""8"" rx=""7"" ry=""4"" fill=""none"" stroke=""white"" stroke-width=""1""/>
  <circle cx=""8"" cy=""8"" r=""2"" fill=""white""/>
  <line x1=""2"" y1=""14"" x2=""14"" y2=""2"" stroke=""white"" stroke-width=""2""/>
</svg>";

    private static string GetArrowLeftSvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <polygon points=""2,8 8,2 8,5 14,5 14,11 8,11 8,14"" fill=""white""/>
</svg>";

    private static string GetArrowRightSvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <polygon points=""14,8 8,2 8,5 2,5 2,11 8,11 8,14"" fill=""white""/>
</svg>";

    private static string GetArrowUpSvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <polygon points=""8,2 2,8 5,8 5,14 11,14 11,8 14,8"" fill=""white""/>
</svg>";

    private static string GetArrowDownSvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <polygon points=""8,14 2,8 5,8 5,2 11,2 11,8 14,8"" fill=""white""/>
</svg>";

    private static string GetBusSvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <rect x=""2"" y=""2"" width=""12"" height=""10"" rx=""1"" fill=""white""/>
  <rect x=""3"" y=""3"" width=""4"" height=""3"" fill=""black""/>
  <rect x=""9"" y=""3"" width=""4"" height=""3"" fill=""black""/>
  <circle cx=""5"" cy=""13"" r=""1"" fill=""white""/>
  <circle cx=""11"" cy=""13"" r=""1"" fill=""white""/>
  <rect x=""2"" y=""11"" width=""12"" height=""2"" fill=""white""/>
</svg>";

    private static string GetMetroSvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <rect x=""3"" y=""2"" width=""10"" height=""10"" rx=""2"" fill=""white""/>
  <rect x=""4"" y=""3"" width=""8"" height=""4"" fill=""black""/>
  <circle cx=""5"" cy=""10"" r=""1"" fill=""black""/>
  <circle cx=""11"" cy=""10"" r=""1"" fill=""black""/>
  <line x1=""4"" y1=""13"" x2=""2"" y2=""15"" stroke=""white"" stroke-width=""1""/>
  <line x1=""12"" y1=""13"" x2=""14"" y2=""15"" stroke=""white"" stroke-width=""1""/>
</svg>";

    private static string GetTramSvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <rect x=""4"" y=""4"" width=""8"" height=""9"" rx=""1"" fill=""white""/>
  <rect x=""5"" y=""5"" width=""6"" height=""3"" fill=""black""/>
  <circle cx=""6"" cy=""11"" r=""1"" fill=""black""/>
  <circle cx=""10"" cy=""11"" r=""1"" fill=""black""/>
  <line x1=""6"" y1=""4"" x2=""4"" y2=""1"" stroke=""white"" stroke-width=""1""/>
  <line x1=""10"" y1=""4"" x2=""12"" y2=""1"" stroke=""white"" stroke-width=""1""/>
  <line x1=""2"" y1=""1"" x2=""14"" y2=""1"" stroke=""white"" stroke-width=""1""/>
</svg>";

    private static string GetFerrySvg() => @"
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16"">
  <path d=""M2 10 L4 6 L12 6 L14 10 Z"" fill=""white""/>
  <rect x=""6"" y=""3"" width=""4"" height=""3"" fill=""white""/>
  <path d=""M1 11 Q4 13 8 11 Q12 13 15 11 L15 12 Q12 14 8 12 Q4 14 1 12 Z"" fill=""white""/>
  <path d=""M1 13 Q4 15 8 13 Q12 15 15 13 L15 14 Q12 16 8 14 Q4 16 1 14 Z"" fill=""white""/>
</svg>";

    #endregion
}
