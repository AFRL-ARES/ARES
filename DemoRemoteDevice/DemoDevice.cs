using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace DemoRemoteDevice;

public class DemoDevice
{
  private readonly Task _temperatureUpdater;

  private readonly AresStruct _settings = new AresStruct();

  public DemoDevice(ILogger<DemoDevice> logger)
  {
    logger.LogInformation("Demo device initialized");
    _temperatureUpdater = Task.Factory.StartNew(async () =>
    {
      Temperature = Random.Shared.Next(10, 100);
      await Task.Delay(5000);
    });

    StateSchema.AddEntry("Temperature", AresDataType.Number);
  }

  public int Temperature { get; set; } = 50;

  public AresStruct Settings => _settings;

  public AresDataSchema StateSchema = new AresDataSchema();
}
