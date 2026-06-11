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
      .SelectMany(field => AresValueFlattener.Flatten(GetColumnName(field.Key), field.Value))
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
    var minimumSampleInterval = filter.Interval?.ToTimeSpan() ?? default;
    if(minimumSampleInterval.TotalMilliseconds < 1)
    {
      return states.Select(state => CreateRow(state, state.Timestamp, cancellationToken)).ToArray();
    }

    if(states.Length == 0)
    {
      return [];
    }

    var rows = new List<AresDataRow>();
    var lastIncludedTimestamp = states.First().Timestamp.ToDateTime();
    rows.Add(CreateRow(states.First(), states.First().Timestamp, cancellationToken));

    foreach(var state in states.Skip(1))
    {
      cancellationToken.ThrowIfCancellationRequested();
      var timestamp = state.Timestamp.ToDateTime();
      if(timestamp - lastIncludedTimestamp >= minimumSampleInterval)
      {
        rows.Add(CreateRow(state, state.Timestamp, cancellationToken));
        lastIncludedTimestamp = timestamp;
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
      foreach(var flattenedField in AresValueFlattener.Flatten(GetColumnName(field.Key), field.Value))
      {
        cancellationToken.ThrowIfCancellationRequested();
        data.Fields[flattenedField.Key] = flattenedField.Value.Clone();
      }
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
