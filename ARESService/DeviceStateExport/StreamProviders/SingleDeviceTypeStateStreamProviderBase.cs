using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ares.Messages.DeviceState;
using CsvHelper;
using CsvHelper.Configuration;
using AresService.DeviceStateExport.StateGetters;
using AresMessaging.DeviceStateLogging;
using Google.Protobuf.Collections;

namespace AresService.DeviceStateExport.StreamProviders;

/// <summary>
/// Provides a csv stream containing all the states for a single device type.
/// </summary>
/// <typeparam name="TState"></typeparam>
/// <typeparam name="TStateMap">Describes how the data maps to a csv format</typeparam>
public abstract class SingleDeviceTypeStateStreamProviderBase<TState, TStateMap> : IDeviceStateStreamProvider
  where TStateMap : ClassMap
  where TState : class, IDeviceState
{
  readonly IDeviceStateGetter _deviceStateGetter;
  public SingleDeviceTypeStateStreamProviderBase(IDeviceStateGetter deviceStateGetter)
  {
    _deviceStateGetter = deviceStateGetter;
  }

  /// <summary>
  /// </summary>
  /// <param name="request"></param>
  /// <returns>the state streams for all the requested devices that provide states of type <see cref="TState"/></returns>
  public async Task<IEnumerable<DeviceStateStream>> GetStream(StateRequestFilter request)
  {
    var stateMaps = await _deviceStateGetter.GetStates<TState>(request);
    var exports = new List<DeviceStateStream>();

    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      NewLine = Environment.NewLine
    };

    foreach(var map in stateMaps)
    {
      var stream = new MemoryStream();
      var writer = new StreamWriter(stream);
      using(var csv = new CsvWriter(writer, config, true))
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
