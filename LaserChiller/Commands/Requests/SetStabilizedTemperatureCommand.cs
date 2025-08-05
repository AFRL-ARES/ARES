using Ares.Device.Serial.Commands;

namespace LaserChiller.Commands.Requests;

public class SetStabilizedTemperatureCommand : SerialCommand
{

  public readonly double _stabilizedTemperature;

  public SetStabilizedTemperatureCommand(double stabilizedTemperature)
  {
    _stabilizedTemperature = stabilizedTemperature;
  }

  protected override byte[] Serialize()
  {
    string formattedTempStr;

    if(_stabilizedTemperature < 0)
      formattedTempStr = "-";

    else
      formattedTempStr = "+";

    var decimalFormattedTemp = $"{_stabilizedTemperature:00.0}";
    formattedTempStr += decimalFormattedTemp.Replace(".", string.Empty);

    var temperatureBytes = formattedTempStr.ToCharArray().Select(c => (byte)c).ToArray();
    var checkSumBytes = new byte[] { 0x2E, 0x4D, temperatureBytes[0], temperatureBytes[1], temperatureBytes[2], temperatureBytes[3] };

    var checkSum = CalculateCheckSum(checkSumBytes);

    return new byte[] { 0x2E, 0x4D, temperatureBytes[0], temperatureBytes[1], temperatureBytes[2], temperatureBytes[3], checkSum[0], checkSum[1], 0x0D };
  }

  private static byte[] CalculateCheckSum(byte[] data)
  {
    var sum = data.Aggregate((l, r) => (byte)(l + r));
    var sumHexStr = $"{sum:X}";
    return sumHexStr.Select(chara => (byte)chara).ToArray();
  }
}
