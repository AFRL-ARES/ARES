using Ares.Core.Device.State.Export.StateGetters;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.DataManagement.DataMappers;

public class DeviceStateDatasetGenerator(IDeviceStateGetter _deviceStateGetter)
{
  private const string TimestampColumnName = "Timestamp";
  private const string DynamicTimestampColumnName = "Data.Timestamp";

  public async ValueTask<AresDataset[]> GenerateAsync(DeviceStateRequestFilter filter, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var stateMaps = await _deviceStateGetter.GetStates<DeviceState>(filter, cancellationToken);
    var datasets = new List<AresDataset>();

    foreach(var stateMap in stateMaps)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var states = stateMap.Value.OrderBy(state => state.Timestamp).ToArray();
      var columns = CreateColumns(states);
      var dataset = new AresDataset
      {
        Name = stateMap.Key
      };
      dataset.Columns.AddRange(columns);
      dataset.Rows.AddRange(CreateRows(states, filter, cancellationToken));
      datasets.Add(dataset);
    }

    return datasets.ToArray();
  }

  private static IEnumerable<AresDataColumn> CreateColumns(IEnumerable<DeviceState> states)
  {
    var columns = states
      .SelectMany(state => state.Data?.Fields ?? [])
      .Select(field => KeyValuePair.Create(GetColumnName(field.Key), field.Value))
      .GroupBy(field => field.Key)
      .OrderBy(group => group.Key)
      .Select(group => new AresDataColumn
      {
        Name = group.Key,
        Schema = group.First().Value.ToAresValueSchema()
      })
      .ToArray();

    foreach(var column in columns)
    {
      column.Schema.Optional = true;
    }

    return
    [
      new AresDataColumn
      {
        Name = TimestampColumnName,
        Schema = new AresValueSchema { Type = AresDataType.Timestamp }
      },
      .. columns
    ];
  }

  private static IEnumerable<AresDataRow> CreateRows(DeviceState[] states, DeviceStateRequestFilter filter, CancellationToken cancellationToken)
  {
    var interval = filter.Interval?.ToTimeSpan() ?? default;
    if(interval.TotalMilliseconds < 1)
    {
      return states.Select(state => CreateRow(state, state.Timestamp, cancellationToken)).ToArray();
    }

    if(states.Length == 0)
    {
      return [];
    }

    var startTime = filter.Start ?? states.First().Timestamp;
    var endTime = filter.End ?? states.Last().Timestamp;
    if(startTime > endTime)
    {
      return [];
    }

    var rows = new List<AresDataRow>();
    for(var timestamp = startTime.ToDateTime(); timestamp <= endTime.ToDateTime(); timestamp += interval)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var rowTimestamp = Timestamp.FromDateTime(timestamp);
      var state = states.LastOrDefault(state => state.Timestamp <= rowTimestamp);
      if(state is not null)
      {
        rows.Add(CreateRow(state, rowTimestamp, cancellationToken));
      }
    }

    return rows;
  }

  private static AresDataRow CreateRow(DeviceState state, Timestamp timestamp, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var data = new AresStruct();
    data.Fields[TimestampColumnName] = AresValueHelper.CreateTimestamp(timestamp);

    foreach(var field in state.Data?.Fields ?? [])
    {
      cancellationToken.ThrowIfCancellationRequested();
      data.Fields[GetColumnName(field.Key)] = field.Value.Clone();
    }

    return new AresDataRow
    {
      Data = data
    };
  }

  private static string GetColumnName(string fieldName)
  {
    return fieldName == TimestampColumnName ? DynamicTimestampColumnName : fieldName;
  }
}
