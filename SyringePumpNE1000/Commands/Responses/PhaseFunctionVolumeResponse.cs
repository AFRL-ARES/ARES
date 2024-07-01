using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Responses
{
  internal class PhaseFunctionVolumeResponse : Response
  {
    public PhaseFunctionVolumeResponse(int address, StatusPrompt status, CommandError? error) : base(address, status, error)
    {
    }
    public PhaseFunctionVolumeResponse(int address, StatusPrompt status, Volume volume, VolumeUnit systemVolumeUnit) : base(address, status)
    {
      Volume = volume;
      SystemVolumeUnit = systemVolumeUnit;
    }
    public Volume Volume { get; }
    public VolumeUnit SystemVolumeUnit { get; }
  }
}
