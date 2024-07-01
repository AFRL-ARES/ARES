using Ares.Device.Serial;

namespace AlicatMFC;

public interface IMfcConnection : IAresSerialConnection
{
  IEnumerable<char> UnusedIds { get; }

  bool ReserveId(char id);

  void ReleaseId(char id);
}
