using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Tecan.Sila2;
using Tecan.Sila2.DynamicClient;

namespace Ares.Core.Device.Sila;

public sealed class SilaDevice : AresDevice, IAsyncDisposable
{
  private readonly SilaClient _silaClient;
  private readonly ServerData _serverData;
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly ConcurrentDictionary<string, AresValue> _currentPropertyValues = new();

  private DeviceCommandDescriptor[] _commands = [];
  private CancellationTokenSource _stateStreamCts = new();
  private DevicePollingSettings _pollingSettings = new();
  private Task? _monitoringTask;

  public string Address { get; private set; }
  private IEnumerable<Feature> DeviceFeatures { get; set; } = [];

  public override IObservable<AresStruct> StateStream => _stateSubject.AsObservable();

  public SilaDevice(ServerData serverData, DeviceConnectionInfo connectionInfo, SilaClient client) : base(connectionInfo)
  {
    _serverData = serverData ?? throw new ArgumentNullException(nameof(serverData));
    _silaClient = client ?? throw new ArgumentNullException(nameof(client));

    DeviceFeatures = _serverData.Features.ToArray();

    Description = serverData.Info.Description;
    Version = serverData.Info.Version;
    Type = serverData.Info.Type;
    Address = serverData.Address;
  }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    BuildStateSchema();

    Status = new DeviceOperationalStatus()
    {
      OperationalState = OperationalState.Active,
      Message = $"Connected to SiLA Device {Name}!"
    };

    await StartPropertyMonitoring();
    return true;
  }

  private void BuildStateSchema()
  {
    var schema = new AresStructSchema();
    foreach(var feature in DeviceFeatures)
    {
      foreach(var property in (feature.Items ?? []).OfType<FeatureProperty>())
      {
        var key = $"{feature.Identifier}.{property.Identifier}";
        var fieldSchema = property.DataType is not null
            ? SilaDataConverter.ToAresValueSchema(property.DataType)
            : AresSchemaBuilder.Entry(AresDataType.Any).Build();

        if(!string.IsNullOrWhiteSpace(property.Description))
          fieldSchema.Description = property.Description;

        schema.Fields[key] = fieldSchema;
      }
    }
    StateSchema = schema;
  }

  private async Task StartPropertyMonitoring()
  {
    if(_monitoringTask is not null)
      return;

    _stateStreamCts = new CancellationTokenSource();
    var token = _stateStreamCts.Token;

    var monitorTasks = new List<Task>();

    foreach(var feature in DeviceFeatures)
    {
      if(feature.Identifier.Contains("SiLAService"))
        continue;

      var properties = (feature.Items ?? []).OfType<FeatureProperty>();
      var context = new FeatureContext(feature, _serverData, _silaClient.ExecutionManager);

      foreach(var property in properties)
      {
        if(property.Observable == FeaturePropertyObservable.Yes)
          monitorTasks.Add(MonitorObservableProperty(property, context, token));
        else
          monitorTasks.Add(MonitorUnobservableProperty(property, context, token));
      }
    }

    // We assign the execution to _monitoringTask without awaiting it here, 
    // so it runs in the background.
    _monitoringTask = Task.WhenAll(monitorTasks);
    await Task.CompletedTask;
  }

  private async Task MonitorObservableProperty(FeatureProperty property, FeatureContext context, CancellationToken token)
  {
    var client = new PropertyClient(property, context);

    try
    {
      _ = client.Subscribe((update) => UpdatePropertyState(context.Feature, property, update), token);
    }
    catch(Exception ex)
    {
      Console.Error.WriteLine($"Failed to subscribe to observable property {property.Identifier}: {ex.Message}");
    }

    await Task.CompletedTask;
  }

  private async Task MonitorUnobservableProperty(FeatureProperty property, FeatureContext context, CancellationToken token)
  {
    var client = new PropertyClient(property, context);
    var pollingInterval = TimeSpan.FromMilliseconds(_pollingSettings.IntervalMs > 0 ? _pollingSettings.IntervalMs : 5000);

    while(!token.IsCancellationRequested)
    {
      try
      {
        var value = await Task.Run(() => client.RequestValue(), token);
        UpdatePropertyState(context.Feature, property, value);
      }
      catch(OperationCanceledException)
      {
        break;
      }
      catch(Exception ex)
      {
        Console.Error.WriteLine($"Error polling unobservable property {property.Identifier}: {ex.Message}");
      }

      try
      {
        await Task.Delay(pollingInterval, token);
      }
      catch(TaskCanceledException)
      {
        break;
      }
    }
  }

  private void UpdatePropertyState(Feature feature, FeatureProperty property, DynamicObjectProperty value)
  {
    var key = $"{feature.Identifier}.{property.Identifier}";
    var aresValue = SilaDataConverter.ToAresValue(value);
    _currentPropertyValues[key] = aresValue;

    var newState = new AresStruct();
    foreach(var kvp in _currentPropertyValues)
    {
      newState.Fields[kvp.Key] = kvp.Value;
    }

    _stateSubject.OnNext(newState);
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    token.ThrowIfCancellationRequested();

    if(_silaClient.ExecutionManager is null)
      return CreateFailedResult("SiLA client is not initialized.");

    if(!TryResolveCommand(command, out var feature, out var featureCommand))
      return CreateFailedResult($"Unknown SiLA command '{command}'.");

    var context = new FeatureContext(feature, _serverData, _silaClient.ExecutionManager);

    try
    {
      if(featureCommand.Observable == FeatureCommandObservable.Yes)
      {
        if(featureCommand.IntermediateResponse?.Length > 0)
        {
          var observableClient = new IntermediateObservableCommandClient(featureCommand, context);
          var request = BuildRequest(observableClient.CreateRequest(), featureCommand, arguments);
          var observableCommand = observableClient.Invoke(request);

          using var cancellationRegistration = token.Register(observableCommand.Cancel);
          var response = await observableCommand.Response.WaitAsync(token);
          return CreateSuccessResult(featureCommand, response);
        }
        else
        {
          var trackedObservableClient = new ObservableCommandClient(featureCommand, context);
          var trackedRequest = BuildRequest(trackedObservableClient.CreateRequest(), featureCommand, arguments);
          var trackedObservableCommand = trackedObservableClient.Invoke(trackedRequest);

          using var trackedCancellationRegistration = token.Register(trackedObservableCommand.Cancel);
          var trackedResponse = await trackedObservableCommand.Response.WaitAsync(token);
          return CreateSuccessResult(featureCommand, trackedResponse);
        }
      }

      var nonObservableClient = new NonObservableCommandClient(featureCommand, context);
      var nonObservableRequest = BuildRequest(nonObservableClient.CreateRequest(), featureCommand, arguments);
      var nonObservableResponse = await nonObservableClient.InvokeAsync(nonObservableRequest).WaitAsync(token);

      return CreateSuccessResult(featureCommand, nonObservableResponse);
    }
    catch(OperationCanceledException)
    {
      throw;
    }
    catch(Exception ex)
    {
      return CreateFailedResult(ex.Message);
    }
  }

  public override Task<AresStruct> GetSettings() => Task.FromResult(new AresStruct());

  public override Task<AresStruct> GetState() => Task.FromResult(_stateSubject.Value);

  public override Task UpdateSettings(AresStruct settings) => Task.CompletedTask;

  protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    _commands =
    [
        .. DeviceFeatures
                .SelectMany(feature => (feature.Items ?? []).OfType<FeatureCommand>()
                .Select(command => BuildCommandDescriptor(feature, command)))
    ];

    return Task.FromResult(_commands.ToList());
  }

  public async ValueTask DisposeAsync()
  {
    await _stateStreamCts.CancelAsync();

    if(_monitoringTask is not null)
    {
      try
      {
        await _monitoringTask;
      }
      catch
      {
        // Ignore exceptions thrown during task cancellation cleanup
      }
    }

    _stateStreamCts.Dispose();
    _stateSubject.Dispose();
  }

  private static DeviceCommandDescriptor BuildCommandDescriptor(Feature feature, FeatureCommand command)
  {
    return new DeviceCommandDescriptor
    {
      Name = GetCommandDescriptorName(feature, command),
      Description = command.Description ?? string.Empty,
      InputSchema = BuildStructSchema(command.Parameter),
      OutputSchema = BuildOutputSchema(command.Response)
    };
  }

  private static DynamicRequest BuildRequest(
      DynamicRequest request,
      FeatureCommand command,
      IEnumerable<DeviceCommandArgument> arguments)
  {
    var requestValue = new DynamicObject();

    foreach(var parameter in command.Parameter ?? [])
    {
      var argument = arguments.FirstOrDefault(arg => arg.ArgName == parameter.Identifier);
      if(argument is null)
        continue;

      requestValue.Elements.Add(new DynamicObjectProperty(parameter)
      {
        Value = SilaDataConverter.ToSilaProperty(parameter.Identifier, argument.ArgValue).Value
      });
    }

    request.Value = requestValue;
    request.Validate();
    return request;
  }

  private static CommandResult CreateSuccessResult(FeatureCommand command, DynamicObjectProperty? response)
  {
    return new CommandResult
    {
      Success = true,
      Result = HasResponse(command)
            ? response is not null
                ? SilaDataConverter.ToAresValue(response)
                : AresValueHelper.CreateNull()
            : AresValueHelper.CreateUnit()
    };
  }

  private static CommandResult CreateFailedResult(string error)
  {
    return new CommandResult
    {
      Success = false,
      Error = error
    };
  }

  private bool TryResolveCommand(string descriptorCommandName, out Feature feature, out FeatureCommand command)
  {
    foreach(var currentFeature in DeviceFeatures)
    {
      foreach(var currentCommand in (currentFeature.Items ?? []).OfType<FeatureCommand>())
      {
        if(string.Equals(GetCommandDescriptorName(currentFeature, currentCommand), descriptorCommandName, StringComparison.Ordinal))
        {
          feature = currentFeature;
          command = currentCommand;
          return true;
        }
      }
    }

    feature = null!;
    command = null!;
    return false;
  }

  private static string GetCommandDescriptorName(Feature feature, FeatureCommand command) => $"{feature.Identifier}.{command.Identifier}";

  private static bool HasResponse(FeatureCommand command) => command.Response?.Length > 0;

  private static AresStructSchema BuildStructSchema(IEnumerable<SiLAElement>? elements)
  {
    var schema = new AresStructSchema();

    foreach(var element in elements ?? [])
    {
      var fieldSchema = element.DataType is not null
          ? SilaDataConverter.ToAresValueSchema(element.DataType)
          : AresSchemaBuilder.Entry(AresDataType.Any).Build();

      if(!string.IsNullOrWhiteSpace(element.Description))
        fieldSchema.Description = element.Description;

      schema.Fields[element.Identifier] = fieldSchema;
    }

    return schema;
  }

  private static AresValueSchema? BuildOutputSchema(IEnumerable<SiLAElement>? responses)
  {
    var responseSchema = BuildStructSchema(responses);
    if(responseSchema.Fields.Count == 0)
      return null;

    return AresSchemaBuilder.Entry(AresDataType.Struct)
        .WithStructSchema(responseSchema)
        .Build();
  }
}