using Ares.Device.Serial.Commands;
using UnitsNet;

namespace TC0304.Commands;

internal class DataResponseParser : SerialResponseParser<DataResponse>
{
  private const byte _firstByte = 2;
  private const byte _lastByte = 3;
  private const int _responseLength = 45;

  private const byte _batteryLowMask = 0b_0100_0001;
  private const byte _celsiusMask = 0b_1000_0000;
  private const byte _modeMask = 0b_0000_0110;
  private const byte _t1T2Mask = 0b_0000_1000;
  private const byte _holdMask = 0b_0010_0000;

  public override bool TryParseResponse(byte[] buffer, out DataResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    var bufferArray = buffer.ToArray();
    if (bufferArray.Length < _responseLength)
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    var firstOfLine = Array.IndexOf(bufferArray, _firstByte);
    var lastOfLine = Array.IndexOf(bufferArray, _lastByte, _responseLength - 1);
    if (firstOfLine == -1 || lastOfLine == -1)
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    if (lastOfLine - firstOfLine + 1 != _responseLength)
      throw new InvalidOperationException($"Malformed response from TC0304{Environment.NewLine}{string.Join(" ", bufferArray[firstOfLine..lastOfLine].Select(b => b.ToString("X")))}");

    var toRemove = new ArraySegment<byte>(bufferArray, firstOfLine, _responseLength);
    dataToRemove = toRemove;
    return TryParseLine(toRemove, out response);
  }

  private bool TryParseLine(ArraySegment<byte> bytes, out DataResponse? response)
  {
    var lineArr = bytes.ToArray();
    var infoByte = bytes[1];
    var batteryLow = (infoByte & _batteryLowMask) == _batteryLowMask;
    var mode = ModeExtensions.FromInt((infoByte & _modeMask) >> 1);
    var celsius = (infoByte & _celsiusMask) == _celsiusMask;
    var hold = (infoByte & _holdMask) == _holdMask;
    var t1T2 = (infoByte & _t1T2Mask) == _t1T2Mask;

    var t1 = GetTemperature(lineArr[7..9], celsius);
    var t2 = GetTemperature(lineArr[9..11], celsius);
    var t3 = GetTemperature(lineArr[11..13], celsius);
    var t4 = GetTemperature(lineArr[13..15], celsius);

    response = new DataResponse
    {
      BatteryLow = batteryLow,
      Hold = hold,
      Mode = mode,
      T1Probe = t1,
      T2Probe = t2,
      T3Probe = t3,
      T4Probe = t4,
      T1T2 = t1T2
    };

    return true;
  }

  private Temperature? GetTemperature(byte[] bytes, bool celsius)
  {
    var value = (bytes[0] << 8) | bytes[1];
    if (value == 0x_7FFF)
      return null;

    return celsius ? Temperature.FromDegreesCelsius((double)value / 10) : Temperature.FromDegreesFahrenheit((double)value / 10);
  }
}
