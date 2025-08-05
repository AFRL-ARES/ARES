using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses;

internal class PhaseFunctionDirectionResponse : Response
{
  public PhaseFunctionDirectionResponse(int address, StatusPrompt status, CommandError? error) : base(address, status, error)
  {
  }
  public PhaseFunctionDirectionResponse(int address, StatusPrompt status, Direction direction) : base(address, status)
  {
    Direction = direction;
  }
  public Direction Direction { get; }
}
