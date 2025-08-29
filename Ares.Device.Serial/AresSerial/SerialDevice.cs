using System;
using System.Threading;
using System.Threading.Tasks;
using Ares.Datamodel.Device;

namespace Ares.Device.Serial;

public abstract class SerialDevice<TConnection> : AresDevice, ISerialDevice<TConnection> where TConnection : IAresSerialConnection
{
  protected SerialDevice(string name, TConnection connection) : base(name)
  {
    Connection = connection;
  }

  public TConnection Connection { get; }

  public override Task<bool> Activate(CancellationToken ct)
    => SerialActivate(ct);

  private async Task<bool> SerialActivate(CancellationToken ct)
  {
    if (!Connection.IsOpen)
    {
      try
      {
        Connection.AttemptOpen();
      }
      catch (Exception e)
      {
        Status = new DeviceOperationalStatus
        {
          OperationalState = OperationalState.Error,
          Message = $"Failed to open connection {Connection.Name}{Environment.NewLine}{e.Message}"
        };

        return false;
      }

      Status = new DeviceOperationalStatus
      {
        OperationalState = OperationalState.Error,
        Message = $"Successfully established connection {Connection.Name} but it failed to report as being open."
      };
    }

    try
    {
      var validationResult = await Validate();
      if (!validationResult.Success)
      {
        Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"{Name} connected but could not pass validation.{Environment.NewLine}{validationResult.Message}" };
        return false;
      }
    }
    catch (Exception e)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = e.Message };
      return false;
    }

    Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = $"Activated {Name}" };
    return true;
  }

  protected abstract Task<SerialDeviceValidationResult> Validate();
}
