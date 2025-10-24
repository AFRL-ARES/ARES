using AlicatMFC.Commands.Requests;
using Ares.Alicat.Mfc.Messaging;
using Ares.Device.Serial;
using UnitsNet;

namespace AlicatMFC;

public interface IMassFlowController : ISerialDevice<IMfcConnection>, IAsyncDisposable
{
  char AssumedId { get; }
  bool HasValve { get; }
  IObservable<MfcState?> StateStream { get; }

  Task Start();
  Task<bool> QueryManufacturerInfo();
  Task ChangeHardwareUnitId(char targetId);
  Task CancelValveHold();
  Task ChooseDifferentGas(int gasNumber);
  Task<bool> QueryGasListInfo();
  Task<bool> QueryDataFrameFormat();

  Task SetSetpointSource(SetpointSource source);
  Task<SetpointSource> GetSetpointSource();
  Task StartUpdateLoop(TimeSpan interval);
  Task DeleteComposerMix(int mixNumber);
  Task HoldValvesAtCurrentPosition();
  Task HoldValvesClosed();
  Task NewComposerMix(MfcGasComposition composerMix);
  Task NewSetpoint(StandardVolumeFlow setpoint);
  Task TareAbsolutePressureWithBarometer();
  Task TareFlow();
}
