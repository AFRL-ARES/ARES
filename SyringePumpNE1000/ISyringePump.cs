using Ares.Device.Serial;
using Ares.SyringePump.Ne1000.Messaging;
using UnitsNet;

namespace SyringePumpNE1000;

public interface ISyringePump : ISerialDevice<ISyringePumpConnection>, IDisposable
{
  Task SetPhase(int phase);
  Task SetPhaseFunction(Ares.SyringePump.Ne1000.Messaging.Commands function);
  Task SetDiameter(Length diameter);
  Task GetDiameter();
  Task QueryPhaseFunction();
  Task SetProgramFunctionRate(Speed rate);
  Task GetProgramFunctionRate();
  Task SetProgramFunctionVolumeToBeDispensed(Volume volume);
  Task GetProgramFunctionVolumeToBeDispensed();
  Task SetProgramFunctionPumpingDirection(Direction direction);
  Task GetProgramFunctionPumpingDirection();
  Task StartPumpingProgram();
  Task PurgePump();
  Task StopPumpingProgram();
  Task GetVolumeDispensed();
  Task ClearVolumeDispensed(Direction direction);
  Task SetAddress(int address);
  Task GetAddress();
  Task QueryPhase();
  StateResponse GetCurrentState();
  StateResponse GetUpdatedState();
  uint AssumedAddress { get; }
  IObservable<StateResponse> StateStream { get; }
}
