using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses;

public class PhaseFunctionResponse : Response
{
  public PhaseFunctionResponse(int address, StatusPrompt status, Ares.SyringePump.Ne1000.Messaging.Commands function) : base(address, status)
  {
    Function = function;
  }
  public Ares.SyringePump.Ne1000.Messaging.Commands Function { get; }
}
