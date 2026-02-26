using UnitsNet;
using UnitsNet.Units;

namespace Parsers.AlicatMFCRemastered;

internal class MfcUnitParser
{
  private readonly UnitParser _parser;

  static MfcUnitParser()
  {
    var mfcUnitCache = UnitAbbreviationsCache.CreateDefault();
    mfcUnitCache.MapUnitToAbbreviation(TemperatureUnit.DegreeCelsius, "C");
    mfcUnitCache.MapUnitToAbbreviation(TemperatureUnit.DegreeCelsius, "`C");
    mfcUnitCache.MapUnitToAbbreviation(TemperatureUnit.DegreeFahrenheit, "F");
    // PSIA should just be absolute PSI, so I believe the unit can just be PSI
    mfcUnitCache.MapUnitToAbbreviation(PressureUnit.PoundForcePerSquareInch, "PSIA");
    mfcUnitCache.MapUnitToAbbreviation(VolumeFlowUnit.CubicCentimeterPerMinute, "CCM");
    mfcUnitCache.MapUnitToAbbreviation(StandardVolumeFlowUnit.StandardLiterPerMinute, "SLPM");
    Parser = new MfcUnitParser(new UnitParser(mfcUnitCache));
  }

  public MfcUnitParser(UnitParser parser)
  {
    _parser = parser;
  }

  public static MfcUnitParser Parser { get; }

  public bool TryParse(string unitAbbreviation, Type unitType, out Enum? unitEnum)
    => _parser.TryParse(unitAbbreviation, unitType, out unitEnum);

  public bool TryParse<TUnitType>(string unitAbbreviation, out TUnitType unitEnum) where TUnitType : struct, Enum
    => _parser.TryParse(unitAbbreviation, out unitEnum);
}
