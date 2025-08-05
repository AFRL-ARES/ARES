using Ares.SyringePump.Ne1000.Messaging;
using UnitsNet.Units;
using VolumeUnit = UnitsNet.Units.VolumeUnit;

namespace SyringePumpNE1000.Commands;

internal static class FormatHelper
{
  private static readonly string _referenceFloatString = "####.";

  public static string FormatToFloatString(double input)
  {
    // Maximum of 4 digits and 1 '.'. Maximum of 3 floating point digits (after '.')
    var inputMod = Math.Round(input, 3);
    var floatStr = $"{inputMod:0.###}";// 123.456 is 6 digits, invalid (unsafe)
    if(!floatStr.Contains('.'))
      floatStr += ".0";

    if(floatStr.Length <= _referenceFloatString.Length)
      return floatStr;

    var decimalIndex = floatStr.IndexOf('.');
    if(decimalIndex >= _referenceFloatString.Length - 1)
    {
      throw new Exception($"Tried formatting a floating point number greater than 999.9 for a Syringe Pump (incompatible). Value: {input}, Rounded: {inputMod}");
      //var maxFloat = 999.9f;
      //floatStr = $"{maxFloat:###.0}";
      //return floatStr;
    }

    var decimalsToTruncate = floatStr.Length - _referenceFloatString.Length;
    var decimalsToRound = 3 - decimalsToTruncate;
    inputMod = Math.Round(inputMod, decimalsToRound);
    floatStr = $"{inputMod:0.###}";
    return floatStr;
  }

  public static SpeedUnit ToUnitsNet(this RateUnit pumpRateUnit)
  {
    var unit = pumpRateUnit switch
    {
      RateUnit.Mh => SpeedUnit.MillimeterPerHour,
      RateUnit.Mm => SpeedUnit.MillimeterPerMinute,
      RateUnit.Um => SpeedUnit.MicrometerPerMinute,
      // TODO: Micrometers per Hour not supported in Units.Net, work around it if this becomes an issue
      RateUnit.UndefinedRateUnit => throw new InvalidOperationException(),
      RateUnit.Uh => throw new InvalidOperationException(),
      _ => throw new ArgumentOutOfRangeException()
    };
    return unit;
  }

  public static VolumeUnit ToUnitsNet(this Ares.SyringePump.Ne1000.Messaging.VolumeUnit pumpVolumeUnit)
  {
    var unit = pumpVolumeUnit switch
    {
      Ares.SyringePump.Ne1000.Messaging.VolumeUnit.UndefinedVolumeUnit => throw new InvalidOperationException(),
      Ares.SyringePump.Ne1000.Messaging.VolumeUnit.Ul => UnitsNet.Units.VolumeUnit.Microliter,
      Ares.SyringePump.Ne1000.Messaging.VolumeUnit.Ml => UnitsNet.Units.VolumeUnit.Milliliter,
      _ => throw new ArgumentOutOfRangeException()
    };
    return unit;
  }
}