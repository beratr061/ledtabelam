using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Dahili varlık kütüphanesi implementasyonu
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
        LoadPiskelAssets();
        LoadUserAssets();
    }

    private void InitializeBuiltInCategories()
    {
        _categories.AddRange(new[]
        {
            new AssetCategory { Name = "transport", DisplayName = "Ulaşım", IsBuiltIn = true },
            new AssetCategory { Name = "arrows", DisplayName = "Yön Okları", IsBuiltIn = true },
            new AssetCategory { Name = "flags", DisplayName = "Bayraklar", IsBuiltIn = true },
            new AssetCategory { Name = "accessibility", DisplayName = "Erişilebilirlik", IsBuiltIn = true },
            new AssetCategory { Name = "other", DisplayName = "Diğer", IsBuiltIn = true },
            new AssetCategory { Name = "user", DisplayName = "Kullanıcı İkonları", IsBuiltIn = false }
        });
    }

    private void LoadPiskelAssets()
    {
        var piskelJsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "piskel");
        if (Directory.Exists(piskelJsonPath))
        {
            foreach (var file in Directory.GetFiles(piskelJsonPath, "*.json"))
                LoadPiskelJsonFile(file);
        }

        var iconsPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons");
        if (Directory.Exists(iconsPath))
        {
            foreach (var file in Directory.GetFiles(iconsPath, "*.c"))
                LoadPiskelCFile(file);
        }
    }

    private void LoadPiskelJsonFile(string file)
    {
        try
        {
            var json = File.ReadAllText(file);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.GetProperty("name").GetString() ?? Path.GetFileNameWithoutExtension(file);
            var displayName = root.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? name : name;
            var category = root.TryGetProperty("category", out var cat) ? cat.GetString() ?? "transport" : "transport";
            var width = root.GetProperty("width").GetInt32();
            var height = root.GetProperty("height").GetInt32();

            var pixelsElement = root.GetProperty("pixels");
            var pixels = new int[height][];
            int rowIndex = 0;
            foreach (var row in pixelsElement.EnumerateArray())
            {
                var rowData = new List<int>();
                foreach (var pixel in row.EnumerateArray())
                    rowData.Add(pixel.GetInt32());
                pixels[rowIndex++] = rowData.ToArray();
            }

            _assets[name] = new AssetInfo
            {
                Name = name,
                DisplayName = displayName,
                Category = category,
                IsBuiltIn = true,
                Has16px = true,
                Has32px = true,
                IsBitmap = true,
                BitmapWidth = width,
                BitmapHeight = height,
                BitmapPixels = pixels,
                FilePath = file
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Piskel JSON yükleme hatası ({file}): {ex.Message}");
        }
    }

    private void LoadPiskelCFile(string file)
    {
        try
        {
            var content = File.ReadAllText(file);
            var name = Path.GetFileNameWithoutExtension(file);

            int width = 0, height = 0;

            var widthMatch = Regex.Match(content, @"#define\s+[^\s]+WIDTH\s+(\d+)");
            if (widthMatch.Success)
                width = int.Parse(widthMatch.Groups[1].Value);

            var heightMatch = Regex.Match(content, @"#define\s+[^\s]+HEIGHT\s+(\d+)");
            if (heightMatch.Success)
                height = int.Parse(heightMatch.Groups[1].Value);

            if (width == 0 || height == 0)
            {
                Debug.WriteLine($"Piskel .c boyut bulunamadı: {file}");
                return;
            }

            var dataMatch = Regex.Match(content,
                @"uint32_t\s+\w+\[\d+\]\[\d+\]\s*=\s*\{\s*\{([^}]+)\}",
                RegexOptions.Singleline);

            if (!dataMatch.Success)
            {
                Debug.WriteLine($"Piskel .c veri bulunamadı: {file}");
                return;
            }

            var dataStr = dataMatch.Groups[1].Value;
            var hexMatches = Regex.Matches(dataStr, @"0x([0-9a-fA-F]+)");
            var pixelValues = new List<uint>();
            foreach (Match m in hexMatches)
                pixelValues.Add(Convert.ToUInt32(m.Groups[1].Value, 16));

            if (pixelValues.Count != width * height)
            {
                Debug.WriteLine($"Piskel .c piksel sayısı uyuşmuyor: {file}");
                return;
            }

            var pixels = new int[height][];
            var colors = new uint[height][];

            for (int y = 0; y < height; y++)
            {
                pixels[y] = new int[width];
                colors[y] = new uint[width];

                for (int x = 0; x < width; x++)
                {
                    var pixel = pixelValues[y * width + x];
                    pixels[y][x] = (pixel & 0xFF000000) != 0 ? 1 : 0;
                    colors[y][x] = pixel;
                }
            }

            var category = "other";
            var lowerName = name.ToLowerInvariant();
            if (lowerName.Contains("arrow") || lowerName.Contains("ok"))
                category = "arrows";
            else if (lowerName.Contains("flag") || lowerName.Contains("bayrak"))
                category = "flags";
            else if (lowerName.Contains("wheel") || lowerName.Contains("engel"))
                category = "accessibility";
            else if (lowerName.Contains("bus") || lowerName.Contains("metro") || lowerName.Contains("tram") ||
                     lowerName.Contains("ucak") || lowerName.Contains("vapur") || lowerName.Contains("tren"))
                category = "transport";

            var displayName = char.ToUpper(name[0]) + name.Substring(1);

            _assets[name] = new AssetInfo
            {
                Name = name,
                DisplayName = displayName,
                Category = category,
                IsBuiltIn = true,
                Has16px = true,
                Has32px = true,
                IsBitmap = true,
                BitmapWidth = width,
                BitmapHeight = height,
                BitmapPixels = pixels,
                BitmapColors = colors,
                IsMultiColor = true,
                FilePath = file
            };

            Debug.WriteLine($"Piskel .c yüklendi: {name} ({width}x{height})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Piskel .c yükleme hatası ({file}): {ex.Message}");
        }
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

    public IReadOnlyList<AssetCategory> GetCategories() => _categories.AsReadOnly();

    public IReadOnlyList<AssetInfo> GetAssetsByCategory(string categoryName)
    {
        return _assets.Values
            .Where(a => a.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<AssetInfo> GetAllAssets() => _assets.Values.ToList().AsReadOnly();

    public AssetInfo? GetAsset(string assetName)
    {
        return _assets.TryGetValue(assetName, out var asset) ? asset : null;
    }

    public SKBitmap? RenderAsset(string assetName, int size, SKColor tintColor)
    {
        var asset = GetAsset(assetName);
        if (asset == null)
            return null;

        if (asset.IsBitmap && asset.BitmapPixels != null)
            return RenderBitmapAsset(asset, size, tintColor);

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

    private SKBitmap? RenderBitmapAsset(AssetInfo asset, int targetSize, SKColor tintColor)
    {
        if (asset.BitmapPixels == null || asset.BitmapColors == null)
            return null;

        int srcWidth = asset.BitmapWidth;
        int srcHeight = asset.BitmapHeight;

        var bitmap = new SKBitmap(srcWidth, srcHeight, SKColorType.Rgba8888, SKAlphaType.Premul);

        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);

            for (int y = 0; y < srcHeight && y < asset.BitmapPixels.Length; y++)
            {
                var row = asset.BitmapPixels[y];
                var colorRow = asset.BitmapColors[y];

                for (int x = 0; x < srcWidth && x < row.Length; x++)
                {
                    if (row[x] != 0)
                    {
                        var argb = colorRow[x];
                        byte a = (byte)((argb >> 24) & 0xFF);
                        byte b = (byte)((argb >> 16) & 0xFF);
                        byte g = (byte)((argb >> 8) & 0xFF);
                        byte r = (byte)(argb & 0xFF);

                        using var paint = new SKPaint
                        {
                            Color = new SKColor(r, g, b, a),
                            IsAntialias = false,
                            Style = SKPaintStyle.Fill
                        };
                        canvas.DrawPoint(x, y, paint);
                    }
                }
            }
        }

        return bitmap;
    }

    public bool AddUserAsset(string name, string category, string svgPath)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(svgPath))
            return false;

        if (!File.Exists(svgPath))
            return false;

        try
        {
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

    public bool RemoveUserAsset(string assetName)
    {
        if (!_assets.TryGetValue(assetName, out var asset))
            return false;

        if (asset.IsBuiltIn)
            return false;

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
}
