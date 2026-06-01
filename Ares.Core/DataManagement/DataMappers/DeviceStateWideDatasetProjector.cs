using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.DataManagement.DataMappers;

public static class DeviceStateWideDatasetProjector
{
  private const string TimestampColumnName = "Timestamp";

  public static Task<AresDataset> ProjectAsync(IEnumerable<AresDataset> datasets, CancellationToken cancellationToken = default)
  {
    return Task.Run(() => Project(datasets, cancellationToken), cancellationToken);
  }

  public static AresDataset Project(IEnumerable<AresDataset> deviceDatasets, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var usedColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      TimestampColumnName
    };
    var deviceProjections = deviceDatasets
      .Select(dataset => CreateDeviceProjection(dataset, usedColumnNames, cancellationToken))
      .ToArray();
    var wideDataset = new AresDataset
    {
      Name = "Device State Wide"
    };

    wideDataset.Columns.Add(new AresDataColumn
    {
      Name = TimestampColumnName,
      Schema = new AresValueSchema { Type = AresDataType.Timestamp }
    });
    wideDataset.Columns.AddRange(deviceProjections.SelectMany(projection => projection.Columns.Select(column => column.OutputColumn)));
    wideDataset.Rows.AddRange(CreateRows(deviceProjections, cancellationToken));

    return wideDataset;
  }

  private static DeviceProjection CreateDeviceProjection(AresDataset dataset, HashSet<string> usedColumnNames, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var columns = dataset.Columns
      .Where(column => column.Name != TimestampColumnName)
      .Select(column =>
      {
        cancellationToken.ThrowIfCancellationRequested();

        var outputColumnName = GetUniqueColumnName($"{dataset.Name}.{column.Name}", usedColumnNames);
        var outputColumn = new AresDataColumn
        {
          Name = outputColumnName,
          Schema = column.Schema?.Clone() ?? new AresValueSchema { Type = AresDataType.Any }
        };
        outputColumn.Schema.Optional = true;
        return new ColumnProjection(column.Name, outputColumnName, outputColumn);
      })
      .ToArray();
    var rows = dataset.Rows
      .Select(row => TryCreateSourceRow(row, cancellationToken))
      .OfType<SourceRow>()
      .OrderBy(row => row.Timestamp)
      .ToArray();

    return new DeviceProjection(columns, rows);
  }

  private static IEnumerable<AresDataRow> CreateRows(DeviceProjection[] deviceProjections, CancellationToken cancellationToken)
  {
    var timestamps = deviceProjections
      .SelectMany(projection => projection.Rows.Select(row => row.Timestamp))
      .Distinct()
      .OrderBy(timestamp => timestamp)
      .ToArray();
    var cursors = new int[deviceProjections.Length];

    foreach(var timestamp in timestamps)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var data = new AresStruct();
      data.Fields[TimestampColumnName] = AresValueHelper.CreateTimestamp(Timestamp.FromDateTime(timestamp));

      for(var projectionIndex = 0; projectionIndex < deviceProjections.Length; projectionIndex++)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var projection = deviceProjections[projectionIndex];
        while(cursors[projectionIndex] < projection.Rows.Length && projection.Rows[cursors[projectionIndex]].Timestamp <= timestamp)
        {
          cursors[projectionIndex]++;
        }

        var latestRowIndex = cursors[projectionIndex] - 1;
        if(latestRowIndex < 0)
        {
          continue;
        }

        var latestRow = projection.Rows[latestRowIndex].Row;
        foreach(var column in projection.Columns)
        {
          cancellationToken.ThrowIfCancellationRequested();

          if(latestRow.Data?.Fields.TryGetValue(column.SourceColumnName, out var value) == true)
          {
            data.Fields[column.OutputColumnName] = value.Clone();
          }
        }
      }

      yield return new AresDataRow
      {
        Data = data
      };
    }
  }

  private static SourceRow? TryCreateSourceRow(AresDataRow row, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if(row.Data?.Fields.TryGetValue(TimestampColumnName, out var timestamp) != true)
    {
      return null;
    }

    return new SourceRow(timestamp.TimestampValue.ToDateTime(), row);
  }

  private static string GetUniqueColumnName(string columnName, HashSet<string> usedColumnNames)
  {
    if(usedColumnNames.Add(columnName))
    {
      return columnName;
    }

    var index = 2;
    while(!usedColumnNames.Add($"{columnName}-{index}"))
    {
      index++;
    }

    return $"{columnName}-{index}";
  }

  private record DeviceProjection(ColumnProjection[] Columns, SourceRow[] Rows);

  private record ColumnProjection(string SourceColumnName, string OutputColumnName, AresDataColumn OutputColumn);

  private record SourceRow(DateTime Timestamp, AresDataRow Row);
}
