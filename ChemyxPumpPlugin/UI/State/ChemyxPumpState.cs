namespace ChemyxPumpPlugin.UI.State;

public class ChemyxPumpState
{
  public string Name { get; set; } = string.Empty;
  public bool DualPump { get; set; }
  public List<PumpData> Pumps { get; set; } = new();
}

public class PumpData
{
  public int Index { get; set; }
  public string Status { get; set; } = "Unknown";
  public double Volume { get; set; }
  public string Time { get; set; } = "00:00:00";
  public double Diameter { get; set; }
  public double TargetVolume { get; set; }
  public double Rate { get; set; }
  public double Delay { get; set; }
  public string Units { get; set; } = string.Empty;
}
