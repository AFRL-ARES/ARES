using AlicatMFCRemastered.Enums;
using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using System.IO.Ports;

namespace AlicatMFCRemastered.Simulation;

public class SimMassFlowControllerConnection : AresSerialSimConnection, IMfcConnection
{
  private readonly IList<IAlicatSim> _alicatSims = new List<IAlicatSim>();

  public SimMassFlowControllerConnection(string portName) : base(new SerialPortConnectionInfo(0, Parity.None, 0, StopBits.None), portName, new SerialConnectionOptions { SendBuffer = TimeSpan.FromMilliseconds(150) })
  {
  }

  public override async ValueTask DisposeAsync()
  {
    await base.DisposeAsync();
    foreach(var alicatSim in _alicatSims)
      alicatSim.Dispose();
  }

  public IEnumerable<char> UnusedIds => Enumerable.Range('A', 26).Select(id => (char)id).Except(_alicatSims.Select(sim => sim.DeviceId));

  public bool ReserveId(char id)
    => UnusedIds.Contains(id);

  public void ReleaseId(char id)
  {
  }

  public void AddCat(char id, MfcTypeEnum mfcType)
  {
    var cat = _alicatSims.FirstOrDefault(sim => sim.DeviceId == id);
    if(cat is not null)
      throw new InvalidOperationException($"Simulated alicat with id of {id} already exists on simulated connection {Name}");


    cat = mfcType switch
    {
      MfcTypeEnum.Normal => new AlicatSim(AddDataReceived, id),
      MfcTypeEnum.Basis2 => new AlicatBasisSim(AddDataReceived, id),
      _ => throw new NotImplementedException(),
    };

    _alicatSims.Add(cat);
  }

  public void RemoveCat(char id)
  {
    var cat = _alicatSims.FirstOrDefault(sim => sim.DeviceId == id);
    if(cat is null)
      return;

    cat.Dispose();
    _alicatSims.Remove(cat);
  }

  public override void SendInternally(byte[] bytes)
  {
    foreach(var alicatSim in _alicatSims)
      alicatSim.SendCommand(bytes);
  }
}
