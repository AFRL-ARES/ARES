using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace Ares.Core.DataManagement.DataMappers;

public static class DeviceStateLongDatasetProjector
{
  private const string TimestampColumnName = "Timestamp";
  private const string CampaignColumnName = "Campaign";
  private const string ExperimentNumberColumnName = "Experiment Number";
  private const string StepNameColumnName = "Step Name";
  private const string DeviceColumnName = "Device";
  private const string PropertyColumnName = "Property";
  private const string ValueColumnName = "Value";
  private const string UnitColumnName = "Unit";

  public static Task<AresDataset> ProjectAsync(IEnumerable<AresDataset> dataset, CancellationToken cancellationToken = default)
  {
    return Task.Run(() => Project(dataset, cancellationToken), cancellationToken);
  }

  public static AresDataset Project(IEnumerable<AresDataset> deviceDatasets, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var longDataset = new AresDataset
    {
      Name = "Device State Long"
    };

    longDataset.Columns.AddRange(CreateColumns());
    var rows = new List<AresDataRow>();

    foreach(var deviceDataset in deviceDatasets)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var propertyColumns = deviceDataset.Columns
        .Where(column => !IsContextColumn(column.Name))
        .ToArray();

      foreach(var sourceRow in deviceDataset.Rows)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if(sourceRow.Data?.Fields.TryGetValue(TimestampColumnName, out var timestamp) != true)
          continue;

        foreach(var propertyColumn in propertyColumns)
        {
          cancellationToken.ThrowIfCancellationRequested();

          if(sourceRow.Data.Fields.TryGetValue(propertyColumn.Name, out var value) != true)
            continue;

          rows.Add(CreateRow(sourceRow.Data, timestamp, deviceDataset.Name, propertyColumn.Name, value));
        }
      }
    }

    cancellationToken.ThrowIfCancellationRequested();
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
      CreateOptionalColumn(CampaignColumnName, AresDataType.String),
      CreateOptionalColumn(ExperimentNumberColumnName, AresDataType.Int),
      CreateOptionalColumn(StepNameColumnName, AresDataType.String),
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

  private static AresDataRow CreateRow(AresStruct sourceData, AresValue timestamp, string deviceName, string propertyName, AresValue value)
  {
    var rowData = new AresStruct();
    rowData.Fields[TimestampColumnName] = timestamp.Clone();
    CopyOptionalField(sourceData, rowData, CampaignColumnName);
    CopyOptionalField(sourceData, rowData, ExperimentNumberColumnName);
    CopyOptionalField(sourceData, rowData, StepNameColumnName);
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

  private static AresDataColumn CreateOptionalColumn(string name, AresDataType type)
  {
    return new AresDataColumn
    {
      Name = name,
      Schema = new AresValueSchema { Type = type, Optional = true }
    };
  }

  private static void CopyOptionalField(AresStruct sourceData, AresStruct destinationData, string fieldName)
  {
    if(sourceData.Fields.TryGetValue(fieldName, out var value))
      destinationData.Fields[fieldName] = value.Clone();
  }

  private static bool IsContextColumn(string columnName)
  {
    return columnName is TimestampColumnName or CampaignColumnName or ExperimentNumberColumnName or StepNameColumnName;
  }

  private static DateTime GetTimestamp(AresDataRow row)
  {
    return row.Data.Fields[TimestampColumnName].TimestampValue.ToDateTime();
  }
}
