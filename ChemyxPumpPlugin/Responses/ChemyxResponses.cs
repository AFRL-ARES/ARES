using Ares.Toolkit.Serial.Commands;
using ChemyxPumpPlugin.Enums;

namespace ChemyxPumpPlugin.Responses;

public class ChemyxResponse(string commandEcho, string[] responseLines, string raw) : SerialResponse
{
  public string CommandEcho { get; } = commandEcho;
  public string[] ResponseLines { get; } = responseLines;
  public string Raw { get; } = raw;
}

public class NumericResponse(string commandEcho, string[] responseLines, string raw, double? value) 
  : ChemyxResponse(commandEcho, responseLines, raw)
{
  public double? Value { get; } = value;
}

public class PumpStatusResponse(string commandEcho, string[] responseLines, string raw, PumpStatus? status) 
  : ChemyxResponse(commandEcho, responseLines, raw)
{
  public PumpStatus? Status { get; } = status;
}

public class LimitParameterResponse(string commandEcho, string[] responseLines, string raw, double maxVolume, double maxRate, double minRate, double minVolume) 
  : ChemyxResponse(commandEcho, responseLines, raw)
{
  public double MaxVolume { get; } = maxVolume;
  public double MaxRate { get; } = maxRate;
  public double MinRate { get; } = minRate;
  public double MinVolume { get; } = minVolume;
}

public class SetTimeResponse(string commandEcho, string[] responseLines, string raw, double? rate, double? time) 
  : ChemyxResponse(commandEcho, responseLines, raw)
{
  public double? Rate { get; } = rate;
  public double? Time { get; } = time;
}

public class SinglePumpParameters(double diameter, double volume, double rate, double delay, PumpUnits units)
{
  public double Diameter { get; } = diameter;
  public double Volume { get; } = volume;
  public double Rate { get; } = rate;
  public double Delay { get; } = delay;
  public PumpUnits Units { get; } = units;
}

public class ViewParametersResponse(string commandEcho, string[] responseLines, string raw, SinglePumpParameters pump1, SinglePumpParameters? pump2) 
  : ChemyxResponse(commandEcho, responseLines, raw)
{
  public SinglePumpParameters Pump1 { get; } = pump1;
  public SinglePumpParameters? Pump2 { get; } = pump2;
}
