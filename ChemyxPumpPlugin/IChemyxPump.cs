using Ares.Device.Serial;
using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin;

public interface IChemyxPump : ISerialDevice<IChemyxPumpConnection>, IAsyncDisposable
{
  public Task Start(int? pump = null, int mode = 0);

  public Task Stop(int? pump = null);

  public Task Pause(int? pump = null);

  public Task<int?> GetStatus(int? pump = null);

  public Task<double?> GetDispensedVolume(int? pump = null);

  public Task<TimeSpan?> GetElapsedTime(int? pump = null);

  public Task<LimitParameterResponse?> ReadLimitParameter(int? pump = null, int program = 0);

  public Task<double?> SetDiameter(double diameter, int? pump = null);

  public Task<double?> SetRate(double rate, int? pump = null);

  public Task<double?> SetVolume(double volume, int? pump = null);

  public Task<double?> SetUnits(int units, int? pump = null);

  public Task<TimeSpan?> SetDelay(TimeSpan delay, int? pump = null);

  public Task<(double rate, TimeSpan time)?> SetTime(TimeSpan time, int? pump = null);

  Task<ViewParametersResponse?> ViewParameters();

  bool DualPump { get; set; }
}
