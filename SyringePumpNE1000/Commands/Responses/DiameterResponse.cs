using Ares.SyringePump.Ne1000.Messaging;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Responses;

internal class DiameterResponse : Response
{
  public DiameterResponse(int address, StatusPrompt status, CommandError? error) : base(address, status, error)
  {
  }
  public DiameterResponse(int address, StatusPrompt status, Length diameter) : base(address, status)
  {
    Diameter = diameter;
  }

  public Length Diameter { get; }
}
