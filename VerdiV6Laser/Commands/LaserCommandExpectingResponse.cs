using Ares.Device.Serial.Commands;
using System.Text;

namespace VerdiV6Laser.Commands
{
  public abstract class LaserCommandExpectingResponse<T> : SerialCommandWithResponse<T> where T : CommandResponse
  {
    protected LaserCommandExpectingResponse(SerialResponseParser<T> parser) : base(parser)
    {
    }

    protected abstract string SerializeToString();

    protected override byte[] Serialize()
    {
      var commandString = SerializeToString();
      var serialCommand = Encoding.ASCII.GetBytes(commandString.ToCharArray());
      return serialCommand;
    }
  }
}
