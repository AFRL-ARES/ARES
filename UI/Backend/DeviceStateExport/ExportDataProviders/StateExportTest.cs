using AlicatMFC;
using CsvHelper.Configuration;
using UI.Backend.DeviceStateExport.StreamProviders.Mfc;

namespace UI.Backend.DeviceStateExport.ExportDataProviders;

public class StateExportTest<TMap> where TMap : ClassMap
{
  public StateExportTest(IEnumerable<StateExportItem> exportItems, DateTime timestamp, string deviceName)
  {
    DeviceName = deviceName;
    Timestamp = timestamp;
    ExportItems = exportItems;
    var blah = (TMap)Activator.CreateInstance<TMap>();
  }

  public IEnumerable<StateExportItem> ExportItems { get; }
  public DateTime Timestamp { get; }
  public string DeviceName { get; set; }
}
