using Ares.Device.Serial.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ares.Device.Serial.Commands
{
  public abstract class SerialCommandWithStreamedResponse<TCommandResponse> : SerialCommandWithResponse<TCommandResponse> where TCommandResponse : SerialResponse
  {
    protected SerialCommandWithStreamedResponse(SerialResponseParser<TCommandResponse> parser) : base(parser)
    {
    }
  }
}
