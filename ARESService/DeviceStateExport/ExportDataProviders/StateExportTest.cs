using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AresService.DeviceStateExport.ExportDataProviders;

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
