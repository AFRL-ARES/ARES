using Ares.Device.Serial.Commands;

namespace Ares.Device.Serial.Simulation;

public abstract class AresSimConnection : AresSerialConnection
{
  protected AresSimConnection(SerialPortConnectionInfo connectionInfo, string portName, SerialConnectionOptions? connectionOptions = null) : base(connectionInfo, portName, connectionOptions)
  {
  }

  protected override void Open(string portName)
  {
    // if (!portName.StartsWith("SIM"))
    //   throw new InvalidOperationException(
    //     $"Tried opening simulated port of type {GetType().Name} with port name {portName}. Simulated ports may only open on ports starting with SIM");

    IsOpen = true;
  }

  public abstract void SendInternally(byte[] bytes);

  protected override void SendOutboundMessage(SerialCommand command)
  {
    SendInternally(command.SerializedData);
  }

  protected internal override void CloseCore()
  {
    IsOpen = false;
  }
}
