using AlicatMFCRemastered.Commands.Responses;
using Ares.Toolkit.Serial.Commands;
using System.Text;

namespace AlicatMFCRemastered.Commands.Requests;

public abstract class MfcCommandExpectingResponse<T> : SerialCommandWithResponse<T> where T : CommandResponse
{
  readonly string _firmware;
  protected MfcCommandExpectingResponse(char id, SerialResponseParser<T> parser, string firmware) : base(parser)
  {
    _firmware = firmware;
    MfcId = id;
  }

  public char MfcId { get; }

  protected abstract string SerializeToString();

  protected override byte[] Serialize()
  {
    //var id = _firmware.StartsWith("GP", StringComparison.InvariantCultureIgnoreCase) ? $"{Id}$$" : $"{Id}";
    var serialString = $"{MfcId}{SerializeToString()}\r";
    //serialString = serialString.Insert(serialString.Length - 1, "$$");
    var serialData = Encoding.ASCII.GetBytes(serialString.ToCharArray());
    return serialData;
  }
}
