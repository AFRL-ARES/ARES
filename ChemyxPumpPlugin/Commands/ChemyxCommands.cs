using Ares.Toolkit.Serial.Commands;
using ChemyxPumpPlugin.Enums;
using ChemyxPumpPlugin.Responses;
using ChemyxPumpPlugin.Responses.Parsers;
using System.Text;

namespace ChemyxPumpPlugin.Commands;

public abstract class ChemyxCommandBase<TResponse>(string commandText, SerialResponseParser<TResponse> parser) 
  : SerialCommandWithResponse<TResponse>(parser) where TResponse : SerialResponse
{
  protected override byte[] Serialize()
  {
    var text = commandText.EndsWith('\r') ? commandText : $"{commandText}\r";
    return Encoding.ASCII.GetBytes(text);
  }
}

public class StartCommand(int? pump, int mode) 
  : ChemyxCommandBase<ChemyxResponse>(Build(pump, $"start {mode}"), new ChemyxResponseParser(Build(pump, $"start {mode}")))
{
  private static string Build(int? pumpIndex, string cmd) => pumpIndex.HasValue ? $"{pumpIndex.Value} {cmd}" : cmd;
}

public class StopCommand(int? pump) 
  : ChemyxCommandBase<ChemyxResponse>(Build(pump, "stop"), new ChemyxResponseParser(Build(pump, "stop")))
{
  private static string Build(int? pumpIndex, string cmd) => pumpIndex.HasValue ? $"{pumpIndex.Value} {cmd}" : cmd;
}

public class PauseCommand(int? pump) 
  : ChemyxCommandBase<ChemyxResponse>(Build(pump, "pause"), new ChemyxResponseParser(Build(pump, "pause")))
{
  private static string Build(int? pumpIndex, string cmd) => pumpIndex.HasValue ? $"{pumpIndex.Value} {cmd}" : cmd;
}

public class PumpStatusCommand(int pump) 
  : ChemyxCommandBase<PumpStatusResponse>($"{pump} pump status", new PumpStatusResponseParser($"{pump} pump status"));

public class DispensedVolumeCommand(int pump) 
  : ChemyxCommandBase<NumericResponse>($"{pump} dispensed volume", new NumericResponseParser($"{pump} dispensed volume"));

public class ElapsedTimeCommand(int pump) 
  : ChemyxCommandBase<NumericResponse>($"{pump} elapsed time", new NumericResponseParser($"{pump} elapsed time"));

public class ReadLimitParameterCommand(int pump, int program) 
  : ChemyxCommandBase<LimitParameterResponse>($"{pump} read limit parameter {program}", new LimitParameterResponseParser($"{pump} read limit parameter {program}"));

public class SetDiameterCommand(int pump, double diameter) 
  : ChemyxCommandBase<NumericResponse>($"{pump} set diameter {diameter}", new NumericResponseParser($"{pump} set diameter {diameter}"));

public class SetRateCommand(int pump, double rate) 
  : ChemyxCommandBase<NumericResponse>($"{pump} set rate {rate}", new NumericResponseParser($"{pump} set rate {rate}"));

public class SetVolumeCommand(int pump, double volume) 
  : ChemyxCommandBase<NumericResponse>($"{pump} set volume {volume}", new NumericResponseParser($"{pump} set volume {volume}"));

public class SetUnitsCommand(int pump, PumpUnits units) 
  : ChemyxCommandBase<NumericResponse>($"{pump} set unit {(int)units}", new NumericResponseParser($"{pump} set unit {(int)units}"));

public class SetDelayCommand(int pump, double seconds) 
  : ChemyxCommandBase<NumericResponse>($"{pump} set delay {seconds}", new NumericResponseParser($"{pump} set delay {seconds}"));

public class SetTimeCommand(int pump, double minutes) 
  : ChemyxCommandBase<SetTimeResponse>($"{pump} set time {minutes}", new SetTimeResponseParser($"{pump} set time {minutes}"));

public class ViewParameterCommand() 
  : ChemyxCommandBase<ViewParametersResponse>("view parameter", new ViewParametersResponseParser("view parameter"));
