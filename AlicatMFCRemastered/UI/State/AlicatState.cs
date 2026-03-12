using AlicatMFCRemastered.Enums;

namespace AlicatMFCRemastered.UI.State;

public class AlicatMfcState
{
  public string Id { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Firmware { get; set; } = string.Empty;
  public bool HasValve { get; set; }
  public MfcLiveData LiveData { get; set; } = new();
  public string ActiveGas { get; set; } = "Unknown";
  public List<MfcGasInfo> Gases { get; set; } = new();
  public List<MfcManufacturerEntry> ManufacturerInfo { get; set; } = new();
}

public class MfcLiveData
{
  public double? AbsolutePressure { get; set; }
  public double Temperature { get; set; }
  public double? VolumetricFlow { get; set; }
  public double MassFlow { get; set; }
  public double Setpoint { get; set; }
  public double? ValveDrive { get; set; }
  public List<string> StatusCodes { get; set; } = new();
}

public class MfcGasInfo
{
  public string Gas { get; set; } = string.Empty;
  public int Index { get; set; }
  public bool IsEndMarker { get; set; }
  public string Id { get; set; } = string.Empty;
}

public class MfcManufacturerEntry
{
  public int EntryNumber { get; set; }
  public string Category { get; set; } = string.Empty;
  public string Data { get; set; } = string.Empty;
}
