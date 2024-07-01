using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.Device.Serial.Commands;

namespace SyringePumpNE1000.Commands.Requests
{
  internal class RequestWithStreamedResponse<TResponse> : SerialCommandWithStreamedResponse<TResponse> where TResponse : SerialResponse
  {
    public RequestWithStreamedResponse(SerialResponseParser<TResponse> parser) : base(parser)
    {
    }

    protected override byte[] Serialize()
    {
      throw new NotImplementedException();
    }
  }
}
