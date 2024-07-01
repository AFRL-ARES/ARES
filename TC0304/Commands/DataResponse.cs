using Ares.Device.Serial.Commands;
using UnitsNet;

namespace TC0304.Commands;

public class DataResponse : SerialResponse
{
  public Temperature? T1Probe { get; init; }
  public Temperature? T2Probe { get; init; }
  public Temperature? T3Probe { get; init; }
  public Temperature? T4Probe { get; init; }
  public bool BatteryLow { get; init; }
  public bool Hold { get; init; }
  public Mode Mode { get; init; }
  public bool T1T2 { get; init; }
}
