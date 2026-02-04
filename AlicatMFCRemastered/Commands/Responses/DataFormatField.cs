using UnitsNet.Units;

namespace AlicatMFCRemastered.Commands.Responses;

public enum DataFormatField
{
  Unknown,
  UnitId,
  Pressure,
  GaugePressure,
  BarometricPressure,
  Temperature,
  Volumetric,
  Mass,
  Setpoint,
  TotalizedMassFlow,
  TotalizedVolumetricFlow,
  Gas,
  DifferentialPressure,
  Status,
  Error,
  ValveDrive,
  StatusCodes
}
internal static class DataFormatFieldExtensions
{
  private static IDictionary<DataFormatField, string[]> _fieldsToStrings = new Dictionary<DataFormatField, string[]>()
  {
    { DataFormatField.Pressure, new[] {"Pressure", "AbsPress" } },
    { DataFormatField.Temperature, new[] {"Temperature", "FlowTemp" } },
    { DataFormatField.Volumetric, new[] { "Volumetric", "VoluFlow" } },
    { DataFormatField.Mass, new[] {"Mass", "MassFlow" } },
    { DataFormatField.Setpoint, new[] {"SetPoint", "MassFlowSetpt" } },
    { DataFormatField.UnitId, new[] {"Identifier"} },
  };
  public static DataFormatField ToDataFormatField(this string potentialFormatField)
  {
    var blah = _fieldsToStrings.Keys.FirstOrDefault(key => _fieldsToStrings[key].Contains(potentialFormatField));
    if (blah is not DataFormatField.Unknown)
      return blah;

    var found = Enum.TryParse<DataFormatField>(potentialFormatField.Replace(" ", ""), true, out var format);
    return found ? format : DataFormatField.Unknown;
  }

  public static Type? ToUnitType(this DataFormatField format)
  {
    return format switch
    {
      DataFormatField.Unknown => null,
      DataFormatField.UnitId => null,
      DataFormatField.Pressure => typeof(PressureUnit),
      DataFormatField.Temperature => typeof(TemperatureUnit),
      DataFormatField.Volumetric => typeof(VolumeFlowUnit),
      DataFormatField.Mass => typeof(StandardVolumeFlowUnit),
      DataFormatField.Setpoint => typeof(StandardVolumeFlowUnit),
      DataFormatField.TotalizedMassFlow => typeof(StandardVolumeFlowUnit),
      DataFormatField.Gas => null,
      DataFormatField.DifferentialPressure => typeof(PressureUnit),
      DataFormatField.GaugePressure => typeof(PressureUnit),
      DataFormatField.BarometricPressure => typeof(PressureUnit),
      DataFormatField.TotalizedVolumetricFlow => typeof(VolumeFlowUnit),
      DataFormatField.ValveDrive => null,
      DataFormatField.Status => null,
      DataFormatField.Error => null,
      DataFormatField.StatusCodes => null,
      _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };
  }

  public static string ToFriendlyString(this DataFormatField format)
  {
    switch (format)
    {
      case DataFormatField.Unknown:
        return "Unknown";
      case DataFormatField.UnitId:
        return "Unit Id";
      case DataFormatField.Pressure:
      case DataFormatField.GaugePressure:
      case DataFormatField.BarometricPressure:
      case DataFormatField.Temperature:
      case DataFormatField.Volumetric:
      case DataFormatField.Mass:
      case DataFormatField.Setpoint:
      case DataFormatField.TotalizedMassFlow:
      case DataFormatField.TotalizedVolumetricFlow:
      case DataFormatField.Gas:
      case DataFormatField.DifferentialPressure:
      case DataFormatField.Status:
      case DataFormatField.StatusCodes:
      case DataFormatField.Error:
        return format.ToString();
      default:
        throw new ArgumentOutOfRangeException(nameof(format), format, null);
    }
  }
}
