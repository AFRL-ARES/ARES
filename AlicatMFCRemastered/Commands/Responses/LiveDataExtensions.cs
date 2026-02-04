using Ares.Alicat.Mfc.Messaging;
using UnitsNet;
using UnitsNet.Units;

namespace AlicatMFCRemastered.Commands.Responses;

public static class LiveDataExtensions
{
  public static MfcLiveDataResponse ToProto(this LiveDataResponse internalResponse)
  {
    var response = new MfcLiveDataResponse
    {
      AbsolutePressure = internalResponse.AbsolutePressure is not null ? new UnitValue { Unit = Pressure.GetAbbreviation(PressureUnit.PoundForcePerSquareInch), Value = internalResponse.AbsolutePressure.Value.PoundsForcePerSquareInch } : default,
      Gas = internalResponse.Gas,
      Id = internalResponse.Id.ToString(),
      MassFlow = internalResponse.MassFlow is not null ? new UnitValue { Unit = StandardVolumeFlow.GetAbbreviation(StandardVolumeFlowUnit.StandardLiterPerMinute), Value = internalResponse.MassFlow.Value.StandardLitersPerMinute } : default,
      Setpoint = internalResponse.Setpoint is not null ? new UnitValue { Unit = StandardVolumeFlow.GetAbbreviation(StandardVolumeFlowUnit.StandardLiterPerMinute), Value = internalResponse.Setpoint.Value.StandardLitersPerMinute } : default,
      Temperature = internalResponse.Temperature is not null ? new UnitValue { Unit = Temperature.GetAbbreviation(TemperatureUnit.DegreeCelsius), Value = internalResponse.Temperature.Value.DegreesCelsius } : default,
      VolumetricFlow = internalResponse.VolumetricFlow is not null ? new UnitValue { Unit = VolumeFlow.GetAbbreviation(VolumeFlowUnit.CubicCentimeterPerMinute), Value = internalResponse.VolumetricFlow.Value.CubicCentimetersPerMinute } : default,
    };

    if(internalResponse.ValveDrive.HasValue)
      response.ValveDrive = internalResponse.ValveDrive.Value;

    response.StatusCodes.AddRange(internalResponse.StatusCodes.Select(code => code.ToProto()));
    return response;
  }
}
