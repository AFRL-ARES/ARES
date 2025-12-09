using Ares.Device.Serial;

namespace ChemyxPumpPlugin;

public class ChemyxPump : SerialDevice<ChemyxPumpConnection>
{
  public ChemyxPump(string name, ChemyxPumpConnection connection) : base(name, connection)
  {
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    throw new NotImplementedException();
  }

  protected override Task<SerialDeviceValidationResult> Validate()
  {
    throw new NotImplementedException();
  }
}
