namespace AlicatMFCRemasterd.Commands;

public enum MassFlowControllerCommand
{
  // Commands end with a carriage return <CR>
  ChangeUnitId,// [unit ID]@=[desired ID]
  TareFlow,// [unit ID]v
  TareAbsolutePressureWithBarometer,// [unit ID]pc
  PollLiveDataFrame,// [unit ID]
  BeginStreamingData,// [unit ID]@=@
  StopStreamingData,// @@=[desired unit ID]
  SetStreamingInterval,// [unit ID]w91=[# of ms]
  NewSetpoint,
  GetSetpoint,
  // NewSetpointFloat, // [unit ID]s[floating point #]
  // NewSetpointInteger, // [unit ID][integer]
  HoldValvesAtCurrentPosition,// [unit ID]hp
  HoldValvesAtGivenPosition,
  HoldValvesClosed,// [unit ID]hc
  CancelValveHold,// [unit ID]c
  QueryGasListInfo,// [unit ID]??g*
  ChooseDifferentGas,// [unit ID]g[Gas Number]
  NewComposerMix,// [unit ID]gm [Mix Name] [Mix #] [Gas1 %] [Gas1 #] [Gas2 %] [Gas2 #]...
  DeleteComposerMix,// [unit ID]gd [Mix #]
  QueryLiveDataInfo,// [unit ID]??d*
  ManufacturerInfo,// [unit ID]??m*
  FirmwareVersion,// [unit ID]??m9     OR    ave
  LockTheFrontDisplay,// [unit ID]l       (as in lock)
  UnlockTheDisplay// [unit ID]u
}
