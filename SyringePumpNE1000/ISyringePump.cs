using Ares.Device.Serial;
using Ares.SyringePump.Ne1000.Messaging;
using SyringePumpNE1000.Commands.Responses;
using UnitsNet;

namespace SyringePumpNE1000;

public interface ISyringePump : ISerialDevice<ISyringePumpConnection>, IAsyncDisposable
{
  Task SetPhase(int phase);
  Task SetPhaseFunction(Ares.SyringePump.Ne1000.Messaging.Commands function);
  Task SetDiameter(Length diameter);
  Task<Length> GetDiameter();
  Task<PhaseFunctionResponse> QueryPhaseFunction();
  Task SetProgramFunctionRate(Speed rate);
  Task<PhaseFunctionRateResponse> GetProgramFunctionRate();
  Task SetProgramFunctionVolumeToBeDispensed(Volume volume);
  Task GetProgramFunctionVolumeToBeDispensed();
  Task SetProgramFunctionPumpingDirection(Direction direction);
  Task<Direction> GetProgramFunctionPumpingDirection();
  Task StartPumpingProgram();
  Task PurgePump();
  Task StopPumpingProgram();
  Task<VolumeDispensedResponse> GetVolumeDispensed();
  Task ClearVolumeDispensed(Direction direction);
  Task SetAddress(int address);
  Task<int> GetAddress();
  Task<int> QueryPhase();
  Task<StateResponse> GetCurrentState();
  uint AssumedAddress { get; }
  IObservable<StateResponse> InternalStateStream { get; }
}
