using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Responses
{
  internal class VolumeDispensedResponse : Response
  {
    public Volume Infused { get; }
    public Volume Withdrawn { get; }
    public VolumeUnit SystemVolumeUnit { get; }

    public VolumeDispensedResponse(int address, StatusPrompt status, CommandError? error) : base(address, status, error)
    {
    }

    public VolumeDispensedResponse(int address, StatusPrompt status, Volume infused, Volume withdrawn, VolumeUnit systemVolumeUnit) : base(address, status)
    {
      Infused = infused;
      Withdrawn = withdrawn;
      SystemVolumeUnit = systemVolumeUnit;
    }
  }
}
