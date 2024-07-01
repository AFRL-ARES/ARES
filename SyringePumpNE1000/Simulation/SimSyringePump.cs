namespace SyringePumpNE1000.Simulation;

public class SimSyringePump : SyringePump
{
  public SimSyringePump(SimSyringePumpConnection simConnection, string name) : base(name, 0, simConnection)
  {
  }
}
