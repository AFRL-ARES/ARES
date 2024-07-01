using AlicatMFC.Commands.Responses;
using Ares.Device.Serial.Commands;
using System.Text;

namespace AlicatMFC.Commands.Requests;

internal abstract class CommandWithStreamedResponse<T> : SerialCommandWithStreamedResponse<T> where T : CommandResponse
{
  readonly string _firmware;
  public CommandWithStreamedResponse(char id, SerialResponseParser<T> parser, string firmware) : base(parser)
  {
    _firmware = firmware;
    Id = id;
  }

  public char Id { get; }

  protected abstract string SerializeToString();

  protected override byte[] Serialize()
  {
    var id = _firmware.StartsWith("GP", StringComparison.InvariantCultureIgnoreCase) ? $"{Id}$$" : $"{Id}";
    var serialString = $"{id}{SerializeToString()}\r";
    //serialString = serialString.Insert(serialString.Length - 1, "$$");
    var serialData = Encoding.ASCII.GetBytes(serialString.ToCharArray());
    return serialData;
  }
}
