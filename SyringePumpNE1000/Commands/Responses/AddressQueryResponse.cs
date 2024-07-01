using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses
{
  internal class AddressQueryResponse : Response
  {
    public AddressQueryResponse(int address, StatusPrompt status, int respondingAddress) : base(address, status, null)
    {
      RespondingAddress = respondingAddress;
    }

    public int RespondingAddress { get; }

  }
}
