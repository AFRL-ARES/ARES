using Ares.SyringePump.Ne1000.Messaging;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Responses;

public class VolumeDispensedResponse : Response
{
  public VolumeDispensedResponse(int address, StatusPrompt status, CommandError? error) : base(address, status, error)
  {
  }

  public VolumeDispensedResponse(int address, StatusPrompt status, Volume infused, Volume withdrawn, VolumeUnit systemVolumeUnit) : base(address, status)
  {
    Infused = infused;
    Withdrawn = withdrawn;
    SystemVolumeUnit = systemVolumeUnit;
  }

  public Volume Infused { get; }
  public Volume Withdrawn { get; }
  public VolumeUnit SystemVolumeUnit { get; }
}


