using AlicatMFCRemastered.Commands.Responses;
using Ares.Toolkit.Serial.Commands;
using System.Text;

namespace AlicatMFCRemastered.Commands.Requests;

internal abstract class MfcCommandWithStreamedResponse<T> : SerialCommandWithStreamedResponse<T> where T : CommandResponse
{
  readonly string _firmware;
  public MfcCommandWithStreamedResponse(char id, SerialResponseParser<T> parser, string firmware) : base(parser)
  {
    _firmware = firmware;
    AlicatId = id;
  }

  public char AlicatId { get; }

  protected abstract string SerializeToString();

  protected override byte[] Serialize()
  {
    var id = _firmware.StartsWith("GP", StringComparison.InvariantCultureIgnoreCase) ? $"{AlicatId}$$" : $"{AlicatId}";
    var serialString = $"{id}{SerializeToString()}\r";
    //serialString = serialString.Insert(serialString.Length - 1, "$$");
    var serialData = Encoding.ASCII.GetBytes(serialString.ToCharArray());
    return serialData;
  }
}
