using Ares.Core.Execution.VersionChecking;
using Ares.Datamodel;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Remote;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ares.Core.Visualization;

public class RemoteVisualizer : IRemoteVisualizer, IDisposable
{
  private readonly GrpcChannel _channel;
  private readonly IDatamodelVersionValidator _versionValidator;
  private readonly BehaviorSubject<State> _stateSubject = new(State.UnspecifiedState);
  private readonly List<VisualizationOption> _visualizationOptions = [];
  private Metadata? _latestReportedMetadata;
  private State _visualizerState = State.UnspecifiedState;

  public RemoteVisualizer(string name, Uri address, IDatamodelVersionValidator versionValidator, string? id = null)
  {
    _channel = GrpcChannel.ForAddress(address);
    _versionValidator = versionValidator;
    Name = name;
    Address = address;
    UniqueId = id ?? Guid.NewGuid().ToString();
  }

  public string Name { get; set; }
  public string Type { get; private set; } = string.Empty;
  public string Version { get; private set; } = "_._._";
  public string UniqueId { get; set; }
  public string Description { get; private set; } = string.Empty;
  public Uri Address { get; }
  public State VisualizerState
  {
    get => _visualizerState;
    private set
    {
      _visualizerState = value;
      _stateSubject.OnNext(value);
    }
  }

  public string StateMessage { get; private set; } = string.Empty;
  public IObservable<State> VisualizerStateObservable => _stateSubject.AsObservable();
  public AresStruct Settings { get; } = new();
  public IReadOnlyList<VisualizationOption> VisualizationOptions => new ReadOnlyCollection<VisualizationOption>(_visualizationOptions);

  public async Task Init()
  {
    await UpdateInfo();
    await VerifyDatamodelCompatibility();
    await UpdateState();
    await UpdateVisualizationOptions();
  }

  public Task Refresh()
    => UpdateState();

  public void UpdateSettings(AresStruct settings)
  {
    foreach(var setting in settings.Fields)
      Settings.Fields[setting.Key] = setting.Value;
  }

  public async Task<VisualizationOptionsResponse> GetVisualizationOptions(CancellationToken cancellationToken = default)
  {
    var client = GetClient();
    try
    {
      var response = await client.GetVisualizationOptionsAsync(new Empty(), cancellationToken: cancellationToken);
      _visualizationOptions.Clear();
      _visualizationOptions.AddRange(response.VisualizationOptions);
      return response;
    }
    catch(RpcException)
    {
      var cachedResponse = new VisualizationOptionsResponse();
      cachedResponse.VisualizationOptions.AddRange(_visualizationOptions);
      return cachedResponse;
    }
  }

  public async Task<GetUpdatedVisualResponse> RequestUpdatedVisualization(GetUpdatedVisualRequest request, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    return await GetClient().RequestUpdatedVisualizationAsync(request, cancellationToken: cancellationToken);
  }

  public async IAsyncEnumerable<GetUpdatedVisualResponse> StreamPlotlyVisualization(GetUpdatedVisualRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    using var call = GetClient().StreamPlotlyVisualization(request, cancellationToken: cancellationToken);
    while(await call.ResponseStream.MoveNext(cancellationToken))
      yield return call.ResponseStream.Current;
  }

  public Task<ConnectionStatus> GetConnectionStatus(CancellationToken cancellationToken = default)
    => GetClient().GetConnectionStatusAsync(new Empty(), cancellationToken: cancellationToken).ResponseAsync;

  internal async Task UpdateInfo()
  {
    var client = GetClient();
    try
    {
      using var call = client.GetInfoAsync(new Empty());
      _latestReportedMetadata = await call.ResponseHeadersAsync;
      var info = await call.ResponseAsync;
      Type = info.Name;
      Version = info.Version;
      Description = info.Description;
    }
    catch(RpcException)
    {
    }
  }

  internal Task UpdateInfo(VisualizerInfo info)
  {
    Type = info.Type;
    Version = info.Version;
    Description = info.Description;
    return Task.CompletedTask;
  }

  internal async Task UpdateState()
  {
    var client = GetClient();
    try
    {
      var state = await client.GetStateAsync(new Empty());
      VisualizerState = state.State;
      StateMessage = state.StateMessage;
    }
    catch(RpcException e)
    {
      VisualizerState = State.Inactive;
      StateMessage = $"Failed to connect to visualizer: {e.Message}";
    }
  }

  internal Task SetOfflineVisualizerStatus(string message)
  {
    VisualizerState = State.Inactive;
    StateMessage = message;
    return Task.CompletedTask;
  }

  internal async Task UpdateVisualizationOptions()
    => await GetVisualizationOptions();
  

  private async Task VerifyDatamodelCompatibility()
  {
    if(_latestReportedMetadata is not null)
      await _versionValidator.CheckDatamodelVersionValidity(_latestReportedMetadata, Name);
  }

  private RemoteVisualizerService.RemoteVisualizerServiceClient GetClient()
    => new(_channel);

  public void Dispose()
  {
    _stateSubject.Dispose();
    _channel.Dispose();
    GC.SuppressFinalize(this);
  }
}