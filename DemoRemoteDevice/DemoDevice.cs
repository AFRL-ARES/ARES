using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace DemoRemoteDevice;

public class DemoDevice : IDisposable
{
  private readonly Task _temperatureUpdater;
  private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
  private readonly AresStruct _settings = new AresStruct();

  public DemoDevice(ILogger<DemoDevice> logger)
  {
    logger.LogInformation("Demo device initialized");
    _temperatureUpdater = Task.Factory.StartNew(async () =>
    {
      while (!_cancellationTokenSource.Token.IsCancellationRequested)
      {
        Temperature = Random.Shared.Next(10, 100);
        await Task.Delay(250);
      }
    });

    StateSchema.AddEntry("Temperature", AresDataType.Number);
  }

  public int Temperature { get; set; } = 50;

  public AresStruct Settings => _settings;

  public AresStructSchema StateSchema = new AresStructSchema();

  public void Dispose()
  {
    _cancellationTokenSource.Cancel();
    _temperatureUpdater.Wait();
    _temperatureUpdater.Dispose();
    _cancellationTokenSource.Dispose();
  }
}
