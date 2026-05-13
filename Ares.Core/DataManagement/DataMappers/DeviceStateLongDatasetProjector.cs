using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace Ares.Core.DataManagement.DataMappers;

public static class DeviceStateLongDatasetProjector
{
  private const string TimestampColumnName = "Timestamp";
  private const string DeviceColumnName = "Device";
  private const string PropertyColumnName = "Property";
  private const string ValueColumnName = "Value";
  private const string UnitColumnName = "Unit";

  public static AresDataset Project(IEnumerable<AresDataset> deviceDatasets)
  {
    var longDataset = new AresDataset
    {
      Name = "Device State Long"
    };

    longDataset.Columns.AddRange(CreateColumns());
    var rows = new List<AresDataRow>();

    foreach(var deviceDataset in deviceDatasets)
    {
      var propertyColumns = deviceDataset.Columns
        .Where(column => column.Name != TimestampColumnName)
        .ToArray();

      foreach(var sourceRow in deviceDataset.Rows)
      {
        if(sourceRow.Data?.Fields.TryGetValue(TimestampColumnName, out var timestamp) != true)
          continue;

        foreach(var propertyColumn in propertyColumns)
        {
          if(sourceRow.Data.Fields.TryGetValue(propertyColumn.Name, out var value) != true)
            continue;

          rows.Add(CreateRow(timestamp, deviceDataset.Name, propertyColumn.Name, value));
        }
      }
    }

    longDataset.Rows.AddRange(rows.OrderBy(GetTimestamp));
    return longDataset;
  }

  private static IEnumerable<AresDataColumn> CreateColumns()
  {
    return
    [
      new AresDataColumn
      {
        Name = TimestampColumnName,
        Schema = new AresValueSchema { Type = AresDataType.Timestamp }
      },
      new AresDataColumn
      {
        Name = DeviceColumnName,
        Schema = new AresValueSchema { Type = AresDataType.String }
      },
      new AresDataColumn
      {
        Name = PropertyColumnName,
        Schema = new AresValueSchema { Type = AresDataType.String }
      },
      new AresDataColumn
      {
        Name = ValueColumnName,
        Schema = new AresValueSchema { Type = AresDataType.Any, Optional = true }
      },
      new AresDataColumn
      {
        Name = UnitColumnName,
        Schema = new AresValueSchema { Type = AresDataType.String, Optional = true }
      }
    ];
  }

  private static AresDataRow CreateRow(AresValue timestamp, string deviceName, string propertyName, AresValue value)
  {
    var rowData = new AresStruct();
    rowData.Fields[TimestampColumnName] = timestamp.Clone();
    rowData.Fields[DeviceColumnName] = AresValueHelper.CreateString(deviceName);
    rowData.Fields[PropertyColumnName] = AresValueHelper.CreateString(propertyName);

    if(value.KindCase == AresValue.KindOneofCase.QuantityValue)
    {
      rowData.Fields[ValueColumnName] = AresValueHelper.CreateNumber(value.QuantityValue.Scalar);
      rowData.Fields[UnitColumnName] = AresValueHelper.CreateString(value.QuantityValue.Unit);
    }
    else
    {
      rowData.Fields[ValueColumnName] = value.Clone();
    }

    return new AresDataRow
    {
      Data = rowData
    };
  }

  private static DateTime GetTimestamp(AresDataRow row)
  {
    return row.Data.Fields[TimestampColumnName].TimestampValue.ToDateTime();
  }
}
