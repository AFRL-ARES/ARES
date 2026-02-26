using Ares.Toolkit.Serial;

namespace AlicatMFCRemastered;

public interface IMfcConnection : IAresSerialConnection
{
  IEnumerable<char> UnusedIds { get; }

  bool ReserveId(char id);

  void ReleaseId(char id);
}
