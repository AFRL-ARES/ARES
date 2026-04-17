using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Tecan.Sila2;
using Tecan.Sila2.DynamicClient;

namespace Ares.Core.Device.Sila;

public sealed class SilaDevice : AresDevice, IAsyncDisposable
{
  private readonly SilaClient _silaClient;
  private DeviceCommandDescriptor[] _commands = [];
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private CancellationTokenSource _stateStreamCts = new();
  private DevicePollingSettings _pollingSettings = new();
  private ServerData _serverData;

  public SilaDevice(ServerData serverData, DeviceConnectionInfo connectionInfo, SilaClient client) : base(connectionInfo)
  {
    _serverData = serverData;
    DeviceFeatures = _serverData.Features;
    _silaClient = client;

    Description = serverData.Info.Description;
    Version = serverData.Info.Version;
    Type = serverData.Info.Type;
  }

  public override IObservable<AresStruct> StateStream => _stateSubject.AsObservable();

  public override Task<bool> Activate(CancellationToken ct)
  {
    DeviceFeatures = _serverData.Features.ToArray();

    Status = new DeviceOperationalStatus() { OperationalState = OperationalState.Active, Message = $"Connected to SiLA Device {Name}!" };
    return Task.FromResult(true);
    
    //else
    //{
    //  Status = new DeviceOperationalStatus() { OperationalState = OperationalState.Error, Message = $"Could not connect to SiLA Device" };
    //  return Task.FromResult(false);
    //}
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
          // TODO: Surface observable state and intermediate responses once ARES exposes progress/intermediate result APIs.
          var response = await observableCommand.Response.WaitAsync(token);
          return CreateSuccessResult(featureCommand, response);
        }

        var trackedObservableClient = new ObservableCommandClient(featureCommand, context);
        var trackedRequest = BuildRequest(trackedObservableClient.CreateRequest(), featureCommand, arguments);
        var trackedObservableCommand = trackedObservableClient.Invoke(trackedRequest);

        using var trackedCancellationRegistration = token.Register(trackedObservableCommand.Cancel);
        var trackedResponse = await trackedObservableCommand.Response.WaitAsync(token);
        return CreateSuccessResult(featureCommand, trackedResponse);
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

  public override Task<AresStruct> GetSettings()
  {
    throw new NotImplementedException();
  }

  public override Task<AresStruct> GetState()
  {
    throw new NotImplementedException();
  }

  public override Task UpdateSettings(AresStruct settings)
  {
    return Task.FromResult(new AresStruct());
  }

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

  public ValueTask DisposeAsync()
  {
    return new ValueTask();
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

  private static string GetCommandDescriptorName(Feature feature, FeatureCommand command)
  {
    return $"{feature.Identifier}.{command.Identifier}";
  }

  private static bool HasResponse(FeatureCommand command)
  {
    return command.Response?.Length > 0;
  }

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

  private IEnumerable<Feature> DeviceFeatures { get; set; } = [];
}
