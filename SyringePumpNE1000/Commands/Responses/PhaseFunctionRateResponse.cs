using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Responses
{
  internal class PhaseFunctionRateResponse : Response
  {
    public PhaseFunctionRateResponse(int address, StatusPrompt status, CommandError? error = null) : base(address, status, error)
    {
    }
    public PhaseFunctionRateResponse(int address, StatusPrompt status) : base(address, status, null)
    {
    }
    public PhaseFunctionRateResponse(int address, StatusPrompt status, Speed rate, RateUnit systemRateUnit) : base(address, status)
    {
      Rate = rate;
      SystemRateUnit = systemRateUnit;
    }
    public Speed Rate { get; }
    public RateUnit SystemRateUnit { get; }
  }
}
