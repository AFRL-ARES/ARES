using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses
{
  internal class PhaseNumberResponse : Response
  {
    public PhaseNumberResponse(int address, StatusPrompt status, CommandError? error) : base(address, status, error)
    {
    }
    public PhaseNumberResponse(int address, StatusPrompt status, int phase) : base(address, status)
    {
      Phase = phase;
    }
    public int Phase { get; }
  }
}
