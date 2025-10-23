namespace AlicatMFC.Simulation;
internal interface IAlicatSim : IDisposable
{
  char DeviceId { get; }
  void SendCommand(byte[] command);
}
