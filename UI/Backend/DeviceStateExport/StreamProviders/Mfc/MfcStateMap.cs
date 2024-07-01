using Ares.Messages.DeviceStates.Mfc;
using CsvHelper.Configuration;

namespace UI.Backend.DeviceStateExport.StreamProviders.Mfc;

public class MfcStateMap : ClassMap<MfcState>
{
  public MfcStateMap()
  {
    Map(m => m.Timestamp).Index(0).Name("Timestamp");
    Map(m => m.Gas).Index(1).Name("Gas");
    Map(m => m.MassFlow).Index(2).Name("Mass Flow (SCCM)");
    Map(m => m.Temperature).Index(3).Name("Temperature (°C)");
    Map(m => m.AbsolutePressure).Index(4).Name("Absolute Pressure (PSI)");
    Map(m => m.VolumetricFlow).Index(5).Name("Volumetric Flow (CCM)");
    Map(m => m.Setpoint).Index(6).Name("Setpoint (SCCM)");
    Map(m => m.StatusCodes).Index(7).Name("Status Codes");
  }
}
