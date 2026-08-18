using UnitsNet;
using UnitsNet.Units;

namespace AresScript;

internal static class UnitsNetAbbreviationExtensions
{
  private static readonly Lazy<bool> Registration = new(RegisterCustomAbbreviations);

  public static UnitParser Parser
  {
    get
    {
      EnsureRegistered();
      return UnitParser.Default;
    }
  }

  public static void EnsureRegistered()
  {
    _ = Registration.Value;
  }

  private static bool RegisterCustomAbbreviations()
  {
    UnitsNetSetup.Default.UnitAbbreviations.MapUnitToAbbreviation(TemperatureUnit.DegreeCelsius, "c");
    UnitsNetSetup.Default.UnitAbbreviations.MapUnitToAbbreviation(TemperatureUnit.DegreeFahrenheit, "f");
    return true;
  }
}
