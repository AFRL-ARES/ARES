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
      dataset.Rows.AddRange(states.Select(state => CreateRow(state, cancellationToken)));
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

  private static AresDataRow CreateRow(DeviceState state, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var data = new AresStruct();
    data.Fields[TimestampColumnName] = AresValueHelper.CreateTimestamp((Timestamp)state.Timestamp);

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
