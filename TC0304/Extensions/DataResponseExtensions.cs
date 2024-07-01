using TC0304.Commands;
using Tc0304.DataModel;
using UnitsNet;

namespace TC0304.Extensions;

public static class DataResponseExtensions
{
  public static Data ToProto(this DataResponse response)
  {
    var data = new Data
    {
      BatteryLow = response.BatteryLow,
      Mode = response.Mode.ToProto(),
      T1Probe = response.T1Probe?.DegreesCelsius,
      T2Probe = response.T2Probe?.DegreesCelsius,
      T3Probe = response.T3Probe?.DegreesCelsius,
      T4Probe = response.T4Probe?.DegreesCelsius,
      T1T2 = response.T1T2,
      Hold = response.Hold
    };

    return data;
  }

  public static DataResponse ToInternal(this Data data)
  {
    var response = new DataResponse
    {
      BatteryLow = data.BatteryLow,
      Mode = data.Mode.ToInternal(),
      T1Probe = data.T1Probe.HasValue ? Temperature.FromDegreesCelsius(data.T1Probe.Value) : null,
      T2Probe = data.T2Probe.HasValue ? Temperature.FromDegreesCelsius(data.T2Probe.Value) : null,
      T3Probe = data.T3Probe.HasValue ? Temperature.FromDegreesCelsius(data.T3Probe.Value) : null,
      T4Probe = data.T4Probe.HasValue ? Temperature.FromDegreesCelsius(data.T4Probe.Value) : null,
      T1T2 = data.T1T2,
      Hold = data.Hold
    };

    return response;
  }

  public static Commands.Mode ToInternal(this Tc0304.DataModel.Mode mode)
    => mode switch
    {
      Tc0304.DataModel.Mode.Normal => Commands.Mode.Normal,
      Tc0304.DataModel.Mode.Maximum => Commands.Mode.Maximum,
      Tc0304.DataModel.Mode.Minimum => Commands.Mode.Minimum,
      Tc0304.DataModel.Mode.MaxMin => Commands.Mode.MaxMin,
      _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

  public static Tc0304.DataModel.Mode ToProto(this Commands.Mode mode)
    => mode switch
    {
      Commands.Mode.Normal => Tc0304.DataModel.Mode.Normal,
      Commands.Mode.Maximum => Tc0304.DataModel.Mode.Maximum,
      Commands.Mode.Minimum => Tc0304.DataModel.Mode.Minimum,
      Commands.Mode.MaxMin => Tc0304.DataModel.Mode.MaxMin,
      _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };
}
