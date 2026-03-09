namespace AlicatMFCRemastered.Simulation;

internal interface IAlicatSim : IDisposable
{
  char DeviceId { get; }
  Task SendCommand(byte[] command);
}
