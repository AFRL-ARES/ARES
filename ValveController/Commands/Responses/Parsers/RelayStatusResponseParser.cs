using Ares.Device.Serial.Commands;

namespace ValveController.Commands.Responses.Parsers;
public class RelayStatusResponseParser : SerialResponseParser<RelayStatusResponse>
{
  public override bool TryParseResponse(byte[] buffer, out RelayStatusResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    if (buffer != null && buffer.Length == 1)
    {
      var responseInteger = (int)buffer[0];
      ArraySegment<byte> bytes = new ArraySegment<byte>(buffer, 0, 1);

      switch (responseInteger)
      {
        case 0:
          response = new RelayStatusResponse() { RelayOneOn = false, RelayTwoOn = false };
          dataToRemove = bytes;
          return true;

        case 1:
          response = new RelayStatusResponse() { RelayOneOn = true, RelayTwoOn = false };
          dataToRemove = bytes;
          return true;

        case 2:
          response = new RelayStatusResponse() { RelayOneOn = false, RelayTwoOn = true };
          dataToRemove = bytes;
          return true;

        case 3:
          response = new RelayStatusResponse() { RelayOneOn = true, RelayTwoOn = true };
          dataToRemove = bytes;
          return true;

        default:
          break;

      }
    }

    response = null;
    dataToRemove = null;
    throw new Exception("Unexpected Response from Relay Board");
  }
}
