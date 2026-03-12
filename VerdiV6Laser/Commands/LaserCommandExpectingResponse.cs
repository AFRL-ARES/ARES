using Ares.Toolkit.Serial.Commands;
using System.Text;
using VerdiV6Laser.Responses;

namespace VerdiV6Laser.Commands;

public abstract class LaserCommandExpectingResponse<T> : SerialCommandWithResponse<T> where T : SerialResponse
{
  protected LaserCommandExpectingResponse(SerialResponseParser<T> parser) : base(parser)
  {
  }

  protected abstract string SerializeToString();

  protected override byte[] Serialize()
  {
    var commandString = SerializeToString();
    return Encoding.ASCII.GetBytes(commandString);
  }
}
