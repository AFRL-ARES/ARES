using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands.Responses;

public class CurrentLimit : SerialResponse
{
  public CurrentLimit(uint limit)
  {
    Limit = limit;
  }

  public uint Limit { get; set; }
}
