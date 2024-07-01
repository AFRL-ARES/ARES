using System.Globalization;
using Ares.Messages.DeviceStates;
using CsvHelper;
using CsvHelper.Configuration;
using ARESMessaging.DeviceStateLogging;
using Google.Protobuf.Collections;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.StreamProviders;

/// <summary>
/// Provides a csv stream containing all the states for a single device type.
/// </summary>
/// <typeparam name="TState"></typeparam>
/// <typeparam name="TStateMap">Describes how the data maps to a csv format</typeparam>
public abstract class SingleDeviceTypeStateStreamProviderBase<TState, TStateMap> : IDeviceStateStreamProvider
  where TStateMap : ClassMap
  where TState : IDeviceState
{
  readonly IDeviceStateGetter<TState> _deviceStateGetter;
  public SingleDeviceTypeStateStreamProviderBase(IDeviceStateGetter<TState> deviceStateGetter)
  {
    _deviceStateGetter = deviceStateGetter;
  }

  /// <summary>
  /// </summary>
  /// <param name="request"></param>
  /// <returns>the state streams for all the requested devices that provide states of type <see cref="TState"/></returns>
  public async Task<IEnumerable<DeviceStateStream>> GetStream(StateRequest request)
  {
    var stateMaps = await _deviceStateGetter.GetStates(request);
    var exports = new List<DeviceStateStream>();

    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      NewLine = Environment.NewLine
    };

    foreach (var map in stateMaps)
    {
      var stream = new MemoryStream();
      var writer = new StreamWriter(stream);
      using (var csv = new CsvWriter(writer, config, true))
      {
        csv.Context.TypeConverterCache.AddConverter<RepeatedField<string>>(new StringCollectionConverter());
        csv.Context.RegisterClassMap<TStateMap>();
        await csv.WriteRecordsAsync(map.Value.OrderBy(val => val.Timestamp));
        await csv.FlushAsync();
        stream.Seek(0, SeekOrigin.Begin);
        exports.Add(new DeviceStateStream(Path.ChangeExtension(map.Key, "csv"), stream));
      }
    }
    return exports;
  }
}
