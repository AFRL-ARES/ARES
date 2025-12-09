using Ares.Device.Serial;
using ChemyxPumpPlugin.Commands.Requests;
using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin;

public class ChemyxPump : SerialDevice<IChemyxPumpConnection>, IChemyxPump
{
  private const int DefaultPumpIndex = 1;

  public ChemyxPump(string name, bool dualPump, IChemyxPumpConnection connection) : base(name, connection)
  {
    DualPump = dualPump;
  }

  public async Task Start(int? pump = null, int mode = 0)
    => await Connection.Send(new StartCommand(pump, mode));

  public async Task Stop(int? pump = null)
    => await Connection.Send(new StopCommand(pump));

  public async Task Pause(int? pump = null)
    => await Connection.Send(new PauseCommand(pump));

  public async Task<int?> GetStatus(int? pump = null)
  {
    var response = await Connection.Send(new PumpStatusCommand(pump ?? DefaultPumpIndex), TimeSpan.FromSeconds(3));
    if(response is null)
      return null;

    return response.Status;
  }

  public async Task<double?> GetDispensedVolume(int? pump = null)
  {
    var response = await Connection.Send(new DispensedVolumeCommand(pump ?? DefaultPumpIndex), TimeSpan.FromSeconds(3));
    return response.Value;
  }

  public async Task<double?> GetElapsedTimeMinutes(int? pump = null)
  {
    var response = await Connection.Send(new ElapsedTimeCommand(pump ?? DefaultPumpIndex), TimeSpan.FromSeconds(3));
    return response.Value;
  }

  public async Task<LimitParameterResponse> ReadLimitParameter(int? pump = null, int program = 0)
  {
    var response = await Connection.Send(new ReadLimitParameterCommand(pump ?? DefaultPumpIndex, program), TimeSpan.FromSeconds(3));
    if(response is null)
      return null;

    return response;
  }

  public async Task<double?> SetDiameter(double diameter, int? pump = null)
  {
    var response = await Connection.Send(new SetDiameterCommand(pump ?? DefaultPumpIndex, diameter), TimeSpan.FromSeconds(3));
    return response.Value;
  }

  public async Task<double?> SetRate(double rate, int? pump = null)
  {
    var response = await Connection.Send(new SetRateCommand(pump ?? DefaultPumpIndex, rate), TimeSpan.FromSeconds(3));
    return response.Value;
  }

  public async Task<double?> SetVolume(double volume, int? pump = null)
  {
    var response = await Connection.Send(new SetVolumeCommand(pump ?? DefaultPumpIndex, volume), TimeSpan.FromSeconds(3));
    return response.Value;
  }

  public async Task<double?> SetUnits(int units, int? pump = null)
  {
    var response = await Connection.Send(new SetUnitsCommand(pump ?? DefaultPumpIndex, units), TimeSpan.FromSeconds(3));
    return response.Value;
  }

  public async Task<double?> SetDelay(double delayMinutes, int? pump = null)
  {
    var response = await Connection.Send(new SetDelayCommand(pump ?? DefaultPumpIndex, delayMinutes), TimeSpan.FromSeconds(3));
    return response.Value;
  }

  public async Task<(double rate, double time)?> SetTime(double minutes, int? pump = null)
  {
    var response = await Connection.Send(new SetTimeCommand(pump ?? DefaultPumpIndex, minutes), TimeSpan.FromSeconds(3));
    if(response is null || !response.Rate.HasValue || !response.Time.HasValue)
      return null;

    return (response.Rate.Value, response.Time.Value);
  }

  public async Task<ChemyxPumpResponse?> ViewParameters()
    => await Connection.Send(new ViewParameterCommand(), TimeSpan.FromSeconds(3));

  public override async Task EnterSafeMode(CancellationToken ct)
  {
    await Stop(null);
  }

  protected override async Task<SerialDeviceValidationResult> Validate()
  {
    try
    {
      var status = await GetStatus(DefaultPumpIndex);
      var valid = status.HasValue;
      return new SerialDeviceValidationResult(valid, valid ? string.Empty : "Unable to query pump status.");
    }
    catch(Exception ex)
    {
      return new SerialDeviceValidationResult(false, ex.Message);
    }
  }

  public ValueTask DisposeAsync()
  {
    throw new NotImplementedException();
  }

  public bool DualPump { get; set; } = false;
}
