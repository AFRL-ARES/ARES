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
    var response = await Connection.Send(new PumpStatusCommand(pump ?? DefaultPumpIndex), TimeSpan.FromSeconds(5));
    if(response is null)
      return null;

    return response.Status;
  }

  public async Task<double?> GetDispensedVolume(int? pump = null)
  {
    var response = await Connection.Send(new DispensedVolumeCommand(pump ?? DefaultPumpIndex), TimeSpan.FromSeconds(5));
    return response.Value;
  }

  public async Task<TimeSpan?> GetElapsedTime(int? pump = null)
  {
    var response = await Connection.Send(new ElapsedTimeCommand(pump ?? DefaultPumpIndex), TimeSpan.FromSeconds(5));
    if(response?.Value is not double minutes)
    {
      return null;
    }
    var timeSpan = TimeSpan.FromMinutes(minutes);
    return timeSpan;
  }

  public async Task<LimitParameterResponse?> ReadLimitParameter(int? pump = null, int program = 0)
  {
    var response = await Connection.Send(new ReadLimitParameterCommand(pump ?? DefaultPumpIndex, program), TimeSpan.FromSeconds(5));
    if(response is null)
      return null;

    return response;
  }

  public async Task<double?> SetDiameter(double diameter, int? pump = null)
  {
    var response = await Connection.Send(new SetDiameterCommand(pump ?? DefaultPumpIndex, diameter), TimeSpan.FromSeconds(5));
    return response.Value;
  }

  public async Task<double?> SetRate(double rate, int? pump = null)
  {
    var response = await Connection.Send(new SetRateCommand(pump ?? DefaultPumpIndex, rate), TimeSpan.FromSeconds(5));
    return response.Value;
  }

  public async Task<double?> SetVolume(double volume, int? pump = null)
  {
    var response = await Connection.Send(new SetVolumeCommand(pump ?? DefaultPumpIndex, volume), TimeSpan.FromSeconds(5));
    return response.Value;
  }

  public async Task<double?> SetUnits(int units, int? pump = null)
  {
    var response = await Connection.Send(new SetUnitsCommand(pump ?? DefaultPumpIndex, units), TimeSpan.FromSeconds(5));
    return response.Value;
  }

  public async Task<TimeSpan?> SetDelay(TimeSpan delay, int? pump = null)
  {
    var response = await Connection.Send(new SetDelayCommand(pump ?? DefaultPumpIndex, delay.TotalSeconds), TimeSpan.FromSeconds(5));
    if(response?.Value is not double responseSeconds)
    {
      return null;
    }

    var responseTime = TimeSpan.FromSeconds(responseSeconds);
    return responseTime;
  }

  public async Task<(double rate, TimeSpan time)?> SetTime(TimeSpan time, int? pump = null)
  {
    var totalMinutes = time.TotalMinutes;
    var response = await Connection.Send(new SetTimeCommand(pump ?? DefaultPumpIndex, totalMinutes), TimeSpan.FromSeconds(5));
    if(response is null || !response.Rate.HasValue || !response.Time.HasValue)
      return null;

    var responseTimespan = TimeSpan.FromMinutes(response.Time.Value);
    return (response.Rate.Value, responseTimespan);
  }

  public async Task<ViewParametersResponse?> ViewParameters()
    => await Connection.Send(new ViewParameterCommand(), TimeSpan.FromSeconds(5));

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
