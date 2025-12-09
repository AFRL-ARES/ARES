namespace ChemyxPumpPlugin.Commands.Responses;

public class LimitParameterResponse : ChemyxPumpResponse
{
  public LimitParameterResponse(string commandEcho, string[] responseLines, string raw, double? maxRate, double? minRate, double? maxVolume, double? minVolume) : base(commandEcho, responseLines, raw)
  {
    MaxRate = maxRate;
    MinRate = minRate;
    MaxVolume = maxVolume;
    MinVolume = minVolume;
  }

  public double? MaxRate { get; }
  public double? MinRate { get; }
  public double? MaxVolume { get; }
  public double? MinVolume { get; }
}
