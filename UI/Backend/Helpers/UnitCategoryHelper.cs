using UnitsNet;

namespace UI.Backend.Helpers;

public class UnitCategoryHelper
{
  private readonly Dictionary<string, QuantityInfo> _cachedLookups = new();

  public UnitCategoryHelper()
  {
    UnitCategories = Quantity.Infos.Select(i => i.Name).ToArray();
    Task.Run(BuildLookup);
  }

  public string[] UnitCategories { get; }

  private void BuildLookup()
  {
    foreach (var quantityInfo in Quantity.Infos)
    foreach (var unit in quantityInfo.UnitInfos)
      // var humanUnit = unit.Name.Humanize();
      _cachedLookups.TryAdd(unit.Name, quantityInfo);
  }

  public static string[] GetTypes(string unitCategory)
  {
    var selection = Quantity.Infos.FirstOrDefault(i => i.Name.Equals(unitCategory));
    if (selection == null)
      return Array.Empty<string>();

    var units = selection.UnitInfos;
    return units.Select(i => i.Name).ToArray();
  }

  public string GetCategoryForUnit(string unit)
    => _cachedLookups.ContainsKey(unit) ? _cachedLookups[unit].Name : "";
}
