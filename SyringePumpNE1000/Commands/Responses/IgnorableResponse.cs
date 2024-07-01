using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Responses
{
  internal class IgnorableResponse : Response
  {
    public IgnorableResponse(int address, StatusPrompt status, CommandError? error) : base(address, status, error)
    {
    }
    public IgnorableResponse(int address, StatusPrompt status, string content) : base(address, status)
    {
      Message = content;
    }
    public string Message { get; }
  }
}
