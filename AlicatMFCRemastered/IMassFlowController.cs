using AlicatMFCRemastered.Enums;
using AlicatMFCRemastered.Models;
using UnitsNet;

namespace AlicatMFCRemastered;

public interface IMassFlowController : IAsyncDisposable
{
  char AssumedId { get; }
  bool HasValve { get; }
  Task Start();
  double? GetSetpoint();
  Task<bool> QueryManufacturerInfo();
  Task ChangeHardwareUnitId(char targetId);
  Task CancelValveHold();
  Task ChooseDifferentGas(int gasNumber);
  Task<bool> QueryGasListInfo();
  Task<bool> QueryDataFrameFormat();
  Task SetSetpointSource(MfcSetpointSourceEnum source);
  Task<MfcSetpointSourceEnum> GetSetpointSource();
  Task StartUpdateLoop(TimeSpan interval);
  Task DeleteComposerMix(int mixNumber);
  Task HoldValvesAtCurrentPosition();
  Task HoldValvesClosed();
  Task NewComposerMix(MfcGasComposition composerMix);
  Task NewSetpoint(StandardVolumeFlow setpoint);
  Task TareAbsolutePressureWithBarometer();
  Task TareFlow();
}
