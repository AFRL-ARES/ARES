namespace SyringePumpNE1000;
public enum SyringePumpCommand
{
  QueryPhaseFunction,
  SetPhase,
  SetPhaseFunction,
  QueryPhase,
  SetDiameter,
  GetDiameter,
  SetProgramFunctionRate,
  GetProgramFunctionRate,
  SetProgramFunctionVolumeToBeDispensed,
  GetProgramFunctionVolumeToBeDispensed,
  SetProgramFunctionPumpingDirection,
  GetProgramFunctionPumpingDirection,
  StartPumpingProgram,
  PurgePump,
  StopPumpingProgram,
  GetVolumeDispensed,
  ClearVolumeDispensed
}
