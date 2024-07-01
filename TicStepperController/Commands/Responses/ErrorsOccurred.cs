using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands.Responses;
public class ErrorsOccurred : SerialResponse
{
  public bool SerialFraming { get; set; }
  public bool SerialRxOverrun { get; set; }
  public bool SerialFormat { get; set; }
  public bool SerialCrc { get; set; }
  public bool EncoderSkip { get; set; }
}
