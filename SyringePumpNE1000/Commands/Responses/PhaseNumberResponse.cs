using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses;

public class PhaseNumberResponse : Response
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
