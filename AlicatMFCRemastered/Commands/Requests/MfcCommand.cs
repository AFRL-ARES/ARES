using Ares.Toolkit.Serial.Commands;
using System.Text;

namespace AlicatMFCRemastered.Commands.Requests;

public abstract class MfcCommand : SerialCommand
{
  readonly string _firmware;

  protected MfcCommand(char id, string firmware)
  {
    _firmware = firmware;
    MfcId = id;
  }

  public char MfcId { get; init; }

  protected abstract string SerializeToString();

  protected override byte[] Serialize()
  {
    var id = _firmware.StartsWith("GP", StringComparison.InvariantCultureIgnoreCase) ? $"{MfcId}$$" : $"{MfcId}";
    var serialString = $"{id}{SerializeToString()}\r";
    //serialString = serialString.Insert(serialString.Length - 1, "$$");
    var serialData = Encoding.ASCII.GetBytes(serialString.ToCharArray());
    return serialData;
  }
}
