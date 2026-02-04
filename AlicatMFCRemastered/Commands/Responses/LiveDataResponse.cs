using UnitsNet;

namespace AlicatMFCRemastered.Commands.Responses;

public class LiveDataResponse : CommandResponse
{
  public LiveDataResponse(
    char id,
    Pressure? absolutePressure,
    Pressure? gaugePressure,
    Pressure? barometricPressure,
    Pressure? differentialPressure,
    Temperature? temperature,
    VolumeFlow? volumetricFlow,
    VolumeFlow? totalizedVolumetricFlow,
    StandardVolumeFlow? massFlow,
    StandardVolumeFlow? setpoint,
    StandardVolumeFlow? totalizedMassFlow,
    double? valveDrive,
    string gas,
    IEnumerable<StatusCode> statusCodes)
    : base(id)
  {
    AbsolutePressure = absolutePressure;
    GaugePressure = gaugePressure;
    BarometricPressure = barometricPressure;
    DifferentialPressure = differentialPressure;
    Temperature = temperature;
    VolumetricFlow = volumetricFlow;
    TotalizedVolumetricFlow = totalizedVolumetricFlow;
    MassFlow = massFlow;
    Setpoint = setpoint;
    TotalizedMassFlow = totalizedMassFlow;
    ValveDrive = valveDrive;
    Gas = gas;
    StatusCodes = statusCodes.ToList();
  }

  public Pressure? AbsolutePressure { get; }
  public Pressure? GaugePressure { get; }
  public Pressure? BarometricPressure { get; }
  public Pressure? DifferentialPressure { get; }
  public Temperature? Temperature { get; }
  public VolumeFlow? VolumetricFlow { get; }
  public VolumeFlow? TotalizedVolumetricFlow { get; }
  public StandardVolumeFlow? MassFlow { get; }
  public StandardVolumeFlow? Setpoint { get; }
  public StandardVolumeFlow? TotalizedMassFlow { get; }
  public string Gas { get; } = "Errrrrorr";
  public IList<StatusCode> StatusCodes { get; } = new List<StatusCode>();
  public double? ValveDrive { get; }
}
