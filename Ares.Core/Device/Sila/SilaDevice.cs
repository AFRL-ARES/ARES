using Ares.Datamodel;
using Ares.Datamodel.Device;
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
    //TEMPORARY TESTING CODE
    var feature = _serverData.Features.FirstOrDefault(f => f.Identifier == "GreetingProvider");
    var command = feature?.Items.OfType<FeatureCommand>().First();

    var context = new FeatureContext(feature, _serverData, _silaClient.ExecutionManager);
    var commandClient = new NonObservableCommandClient(command, context);
    var request = commandClient.CreateRequest();

    request.Value = new DynamicObject()
    {
      Elements =
      {
        new DynamicObjectProperty(command?.Parameter[0])
        {
          Value = "Arnas"
        }
      }
    };

    var response = commandClient.Invoke(request);

    if(response is not null)
    {
      Status = new DeviceOperationalStatus() { OperationalState = OperationalState.Active, Message = $"Connected to SiLA Device {Name}!" };
      return Task.FromResult(true);
    }


    else
    {
      Status = new DeviceOperationalStatus() { OperationalState = OperationalState.Error, Message = $"Could not connect to SiLA Device" };
      return Task.FromResult(false);
    }
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public override Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    throw new NotImplementedException();
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
      Name = $"{feature.Identifier}.{command.Identifier}",
      Description = command.Description ?? string.Empty,
      InputSchema = BuildStructSchema(command.Parameter),
      OutputSchema = BuildOutputSchema(command.Response)
    };
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
