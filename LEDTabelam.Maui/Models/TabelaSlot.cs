using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Tabela slot tanımı (001-999 arası numaralı)
/// </summary>
public partial class TabelaSlot : ObservableObject
{
    [ObservableProperty]
    private int _slotNumber;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private List<TabelaItem> _items = new();

    [ObservableProperty]
    private int _panelWidth = 160;

    [ObservableProperty]
    private int _panelHeight = 24;

    public bool IsDefined => Items.Count > 0 || !string.IsNullOrEmpty(Name);

    public string Summary
    {
        get
        {
            if (!string.IsNullOrEmpty(Name))
                return Name;

            if (Items.Count == 0)
                return "(Boş)";

            foreach (var item in Items)
            {
                if (item.ItemType == TabelaItemType.Text && !string.IsNullOrEmpty(item.Content))
                {
                    var text = item.Content;
                    return text.Length > 30 ? text.Substring(0, 30) + "..." : text;
                }
            }

            return $"{Items.Count} öğe";
        }
    }
}
