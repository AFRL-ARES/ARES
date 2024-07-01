using Ares.Device.Serial.Commands;

namespace ValveController.Commands.Responses;
public class RelayStatusResponse : SerialResponse
{
  public bool RelayOneOn { get; init; }

  public bool RelayTwoOn { get; init; }

}
