using Avalonia.Media;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Tabela üzerindeki tek bir öğe (metin, sembol vb.)
/// HD2020 tarzı - her öğe için ayrı pozisyon ve ayarlar
/// </summary>
public class TabelaItem : ReactiveObject
{
    private int _id;
    private string _name = string.Empty;
    private string _content = string.Empty;
    private TabelaItemType _itemType = TabelaItemType.Text;
    
    // Pozisyon ve Boyut
    private int _x = 0;
    private int _y = 0;
    private int _width = 50;
    private int _height = 16;
    
    // Görünüm
    private Color _color = Color.FromRgb(255, 176, 0); // Amber
    private HorizontalAlignment _hAlign = HorizontalAlignment.Left;
    private VerticalAlignment _vAlign = VerticalAlignment.Center;
    private string _fontName = "PolarisRGB6x10M"; // Varsayılan font
    
    // Animasyon
    private bool _isScrolling = false;
    private ScrollDirection _scrollDirection = ScrollDirection.Left;
    private int _scrollSpeed = 20;
    
    // Durum
    private bool _isSelected = false;
    private bool _isVisible = true;

    /// <summary>
    /// Öğe ID'si
    /// </summary>
    public int Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    /// <summary>
    /// Öğe adı (kullanıcı tanımlı)
    /// </summary>
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// Öğe içeriği (metin veya dosya yolu)
    /// </summary>
    public string Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    /// <summary>
    /// Öğe tipi
    /// </summary>
    public TabelaItemType ItemType
    {
        get => _itemType;
        set => this.RaiseAndSetIfChanged(ref _itemType, value);
    }

    /// <summary>
    /// X pozisyonu (piksel)
    /// </summary>
    public int X
    {
        get => _x;
        set => this.RaiseAndSetIfChanged(ref _x, value);
    }

    /// <summary>
    /// Y pozisyonu (piksel)
    /// </summary>
    public int Y
    {
        get => _y;
        set => this.RaiseAndSetIfChanged(ref _y, value);
    }

    /// <summary>
    /// Genişlik (piksel)
    /// </summary>
    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    /// <summary>
    /// Yükseklik (piksel)
    /// </summary>
    public int Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    /// <summary>
    /// Metin/öğe rengi
    /// </summary>
    public Color Color
    {
        get => _color;
        set => this.RaiseAndSetIfChanged(ref _color, value);
    }

    /// <summary>
    /// Yatay hizalama
    /// </summary>
    public HorizontalAlignment HAlign
    {
        get => _hAlign;
        set => this.RaiseAndSetIfChanged(ref _hAlign, value);
    }

    /// <summary>
    /// Dikey hizalama
    /// </summary>
    public VerticalAlignment VAlign
    {
        get => _vAlign;
        set => this.RaiseAndSetIfChanged(ref _vAlign, value);
    }

    /// <summary>
    /// Font adı (her öğe için ayrı font)
    /// </summary>
    public string FontName
    {
        get => _fontName;
        set => this.RaiseAndSetIfChanged(ref _fontName, value);
    }

    /// <summary>
    /// Kayan yazı aktif mi
    /// </summary>
    public bool IsScrolling
    {
        get => _isScrolling;
        set => this.RaiseAndSetIfChanged(ref _isScrolling, value);
    }

    /// <summary>
    /// Kayma yönü
    /// </summary>
    public ScrollDirection ScrollDirection
    {
        get => _scrollDirection;
        set => this.RaiseAndSetIfChanged(ref _scrollDirection, value);
    }

    /// <summary>
    /// Kayma hızı (piksel/saniye)
    /// </summary>
    public int ScrollSpeed
    {
        get => _scrollSpeed;
        set => this.RaiseAndSetIfChanged(ref _scrollSpeed, value);
    }

    /// <summary>
    /// Seçili mi (UI için)
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    /// <summary>
    /// Görünür mü
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    /// <summary>
    /// Öğenin sağ kenarı (X + Width)
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    /// Öğenin alt kenarı (Y + Height)
    /// </summary>
    public int Bottom => Y + Height;
}

/// <summary>
/// Tabela öğe tipleri
/// </summary>
public enum TabelaItemType
{
    Text,
    Symbol,
    Image,
    Clock,
    Date
}

/// <summary>
/// Kayma yönleri
/// </summary>
public enum ScrollDirection
{
    Left,
    Right,
    Up,
    Down
}
