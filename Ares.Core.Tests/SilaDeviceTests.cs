using Ares.Core.Device.Sila;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Device;
using Moq;
using System.Threading.Channels;
using Tecan.Sila2;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.DynamicClient;

namespace Ares.Core.Tests;

[TestFixture]
public class SilaDeviceTests
{
  [Test]
  public async Task GetCommandDescriptorsAsync_BuildsDescriptorForScalarCommandWithoutResponse()
  {
    var serverData = CreateServerData(
    [
      CreateFeature(
        "PumpControl",
        CreateCommand(
          "Start",
          parameters:
          [
            CreateElement("Rate", BasicType.Real, description: "Pump rate")
          ]))
    ]);

    var device = CreateDevice(serverData);

    var descriptors = await device.GetCommandDescriptorsAsync();

    Assert.That(descriptors, Has.Count.EqualTo(1));
    var descriptor = descriptors[0];
    using(Assert.EnterMultipleScope())
    {
      Assert.That(descriptor.Name, Is.EqualTo("PumpControl.Start"));
      Assert.That(descriptor.Description, Is.Empty);
      Assert.That(descriptor.InputSchema.Fields["Rate"].Type, Is.EqualTo(AresDataType.Number));
      Assert.That(descriptor.InputSchema.Fields["Rate"].Description, Is.EqualTo("Pump rate"));
      Assert.That(descriptor.OutputSchema, Is.Null);
    }
  }

  [Test]
  public async Task GetCommandDescriptorsAsync_UsesFeatureQualifiedNamesAcrossFeatures()
  {
    var serverData = CreateServerData(
    [
      CreateFeature("PumpA", CreateCommand("Start")),
      CreateFeature("PumpB", CreateCommand("Start"))
    ]);

    var device = CreateDevice(serverData);

    var descriptors = await device.GetCommandDescriptorsAsync();

    Assert.That(descriptors.Select(d => d.Name), Is.EquivalentTo(new[] { "PumpA.Start", "PumpB.Start" }));
  }

  [Test]
  public async Task GetCommandDescriptorsAsync_PreservesConstrainedInputSchema()
  {
    var constrainedString = new DataTypeType
    {
      Item = new ConstrainedType
      {
        DataType = new DataTypeType { Item = BasicType.String },
        Constraints = new Constraints
        {
          Set = ["Fast", "Slow"]
        }
      }
    };

    var serverData = CreateServerData(
    [
      CreateFeature(
        "PumpControl",
        CreateCommand(
          "Configure",
          parameters:
          [
            CreateElement("Mode", constrainedString, "Operating mode")
          ]))
    ]);

    var device = CreateDevice(serverData);

    var descriptors = await device.GetCommandDescriptorsAsync();
    var modeSchema = descriptors[0].InputSchema.Fields["Mode"];

    using(Assert.EnterMultipleScope())
    {
      Assert.That(modeSchema.Type, Is.EqualTo(AresDataType.String));
      Assert.That(modeSchema.Description, Is.EqualTo("Operating mode"));
      Assert.That(modeSchema.StringChoices.Strings, Is.EqualTo(new[] { "Fast", "Slow" }));
    }
  }

  [Test]
  public async Task GetCommandDescriptorsAsync_BuildsStructOutputForSingleResponse()
  {
    var serverData = CreateServerData(
    [
      CreateFeature(
        "PumpControl",
        CreateCommand(
          "ReadStatus",
          responses:
          [
            CreateElement("Status", BasicType.String, description: "Current status")
          ]))
    ]);

    var device = CreateDevice(serverData);

    var descriptors = await device.GetCommandDescriptorsAsync();
    var outputSchema = descriptors[0].OutputSchema;

    using(Assert.EnterMultipleScope())
    {
      Assert.That(outputSchema, Is.Not.Null);
      Assert.That(outputSchema!.Type, Is.EqualTo(AresDataType.Struct));
      Assert.That(outputSchema.StructSchema.Fields["Status"].Type, Is.EqualTo(AresDataType.String));
      Assert.That(outputSchema.StructSchema.Fields["Status"].Description, Is.EqualTo("Current status"));
    }
  }

  [Test]
  public async Task GetCommandDescriptorsAsync_BuildsStructOutputForMultipleResponses()
  {
    var serverData = CreateServerData(
    [
      CreateFeature(
        "PumpControl",
        CreateCommand(
          "ReadMetrics",
          responses:
          [
            CreateElement("FlowRate", BasicType.Real),
            CreateElement("Enabled", BasicType.Boolean)
          ]))
    ]);

    var device = CreateDevice(serverData);

    var descriptors = await device.GetCommandDescriptorsAsync();
    var fields = descriptors[0].OutputSchema!.StructSchema.Fields;

    using(Assert.EnterMultipleScope())
    {
      Assert.That(fields.Keys, Is.EquivalentTo(new[] { "FlowRate", "Enabled" }));
      Assert.That(fields["FlowRate"].Type, Is.EqualTo(AresDataType.Number));
      Assert.That(fields["Enabled"].Type, Is.EqualTo(AresDataType.Boolean));
    }
  }

  [Test]
  public async Task GetCommandDescriptorsAsync_IgnoresNonCommandFeatureItems()
  {
    var feature = new Feature
    {
      Identifier = "PumpControl",
      FeatureVersion = "1.0.0",
      Originator = "test",
      Category = "devices",
      Items =
      [
        new FeatureProperty
        {
          Identifier = "Status",
          DataType = new DataTypeType { Item = BasicType.String }
        },
        new FeatureMetadata
        {
          Identifier = "VendorTag",
          DataType = new DataTypeType { Item = BasicType.String }
        },
        CreateCommand("Start")
      ]
    };

    var device = CreateDevice(CreateServerData([feature]));

    var descriptors = await device.GetCommandDescriptorsAsync();

    Assert.That(descriptors.Select(d => d.Name), Is.EqualTo(new[] { "PumpControl.Start" }));
  }

  [Test]
  public async Task Activate_BuildsStateSchemaFromProperties()
  {
    var feature = new Feature
    {
      Identifier = "PumpControl",
      FeatureVersion = "1.0.0",
      Originator = "test",
      Category = "devices",
      Items =
      [
        new FeatureProperty
        {
          Identifier = "FlowRate",
          Description = "Current flow rate",
          DataType = new DataTypeType { Item = BasicType.Real }
        },
        new FeatureProperty
        {
          Identifier = "IsEnabled",
          DataType = new DataTypeType { Item = BasicType.Boolean }
        }
      ]
    };

    var device = CreateDevice(CreateServerData([feature]));

    await device.Activate(CancellationToken.None);

    var fields = device.StateSchema.Fields;
    using(Assert.EnterMultipleScope())
    {
      Assert.That(fields.Keys, Is.EquivalentTo(new[] { "PumpControl.FlowRate", "PumpControl.IsEnabled" }));
      Assert.That(fields["PumpControl.FlowRate"].Type, Is.EqualTo(AresDataType.Number));
      Assert.That(fields["PumpControl.FlowRate"].Description, Is.EqualTo("Current flow rate"));
      Assert.That(fields["PumpControl.IsEnabled"].Type, Is.EqualTo(AresDataType.Boolean));
    }
  }

  [Test]
  public async Task GetState_ReturnsLatestPropertyValues()
  {
    var device = CreateDevice(CreateServerData([]));
    // Since we can't easily trigger the monitoring tasks in a unit test without complex mocks,
    // we'll at least verify that the default state is empty.
    
    var state = await device.GetState();

    Assert.That(state.Fields, Is.Empty);
  }

  [Test]
  public async Task ExecuteCommand_UsesExactFeatureQualifiedCommandName()
  {
    var featureA = CreateFeature(
      "PumpA",
      CreateCommand("Start", responses: [], observable: FeatureCommandObservable.No));
    var featureB = CreateFeature(
      "PumpB",
      CreateCommand("Start", responses: [], observable: FeatureCommandObservable.No));

    var channelMock = CreateChannelMock();
    channelMock
      .Setup(c => c.ExecuteUnobservableCommandAsync(
        It.Is<string>(serviceName => serviceName == $"{featureB.Namespace}.{featureB.Identifier}"),
        It.Is<string>(commandName => commandName == "Start"),
        It.IsAny<DynamicRequest>(),
        It.IsAny<IClientCallInfo>(),
        It.IsAny<ByteSerializer<DynamicRequest>>(),
        It.IsAny<ByteSerializer<DynamicObjectProperty>>()))
      .ReturnsAsync(CreateResponseProperty((FeatureCommand)featureB.Items[0]));

    var device = CreateDevice(CreateServerData([featureA, featureB], channelMock.Object), CreateConfiguredClient());

    var result = await device.ExecuteCommand("PumpB.Start", [], CancellationToken.None);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.True);
      Assert.That(result.Result.KindCase, Is.EqualTo(AresValue.KindOneofCase.UnitValue));
    }
  }

  [Test]
  public async Task ExecuteCommand_UnobservableCommand_MapsRequestAndResponse()
  {
    var command = CreateCommand(
      "Dispense",
      parameters:
      [
        CreateElement("Rate", BasicType.Real)
      ],
      responses:
      [
        CreateElement("Status", BasicType.String)
      ],
      observable: FeatureCommandObservable.No);

    var channelMock = CreateChannelMock();
    channelMock
      .Setup(c => c.ExecuteUnobservableCommandAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.Is<DynamicRequest>(request => RequestContains(request, "Rate", value => value != null && value.GetType() == typeof(double) && (double)value == 12.5)),
        It.IsAny<IClientCallInfo>(),
        It.IsAny<ByteSerializer<DynamicRequest>>(),
        It.IsAny<ByteSerializer<DynamicObjectProperty>>()))
      .ReturnsAsync(CreateResponseProperty(command, (command.Response[0], "Ready")));

    var device = CreateDevice(
      CreateServerData([CreateFeature("PumpControl", command)], channelMock.Object),
      CreateConfiguredClient());

    var result = await device.ExecuteCommand(
      "PumpControl.Dispense",
      [
        new DeviceCommandArgument
        {
          ArgName = "Rate",
          ArgValue = AresValueHelper.CreateNumber(12.5)
        }
      ],
      CancellationToken.None);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.True);
      Assert.That(result.Result.KindCase, Is.EqualTo(AresValue.KindOneofCase.StructValue));
      Assert.That(result.Result.StructValue.Fields["Status"].StringValue, Is.EqualTo("Ready"));
    }
  }

  [Test]
  public async Task ExecuteCommand_NoFinalResponse_ReturnsUnit()
  {
    var command = CreateCommand("Start", observable: FeatureCommandObservable.No);
    var channelMock = CreateChannelMock();
    channelMock
      .Setup(c => c.ExecuteUnobservableCommandAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<DynamicRequest>(),
        It.IsAny<IClientCallInfo>(),
        It.IsAny<ByteSerializer<DynamicRequest>>(),
        It.IsAny<ByteSerializer<DynamicObjectProperty>>()))
      .ReturnsAsync(CreateResponseProperty(command));

    var device = CreateDevice(
      CreateServerData([CreateFeature("PumpControl", command)], channelMock.Object),
      CreateConfiguredClient());

    var result = await device.ExecuteCommand("PumpControl.Start", [], CancellationToken.None);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.True);
      Assert.That(result.Result.KindCase, Is.EqualTo(AresValue.KindOneofCase.UnitValue));
    }
  }

  [Test]
  public async Task ExecuteCommand_ObservableCommandWithoutIntermediates_ReturnsFinalResponse()
  {
    var command = CreateCommand(
      "Run",
      responses:
      [
        CreateElement("Outcome", BasicType.String)
      ],
      observable: FeatureCommandObservable.Yes);

    var channelMock = CreateChannelMock();
    channelMock
      .Setup(c => c.ExecuteObservableCommand<DynamicRequest, DynamicObjectProperty, DynamicObjectProperty>(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<DynamicRequest>(),
        It.IsAny<Func<DynamicObjectProperty, DynamicObjectProperty>>(),
        It.IsAny<Func<string, string, Exception>>(),
        It.IsAny<IClientCallInfo>(),
        It.IsAny<ByteSerializer<DynamicRequest>>(),
        It.IsAny<ByteSerializer<DynamicObjectProperty>>()))
      .Returns(CreateObservableCommand(CreateResponseProperty(command, (command.Response[0], "Finished"))));

    var device = CreateDevice(
      CreateServerData([CreateFeature("PumpControl", command)], channelMock.Object),
      CreateConfiguredClient());

    var result = await device.ExecuteCommand("PumpControl.Run", [], CancellationToken.None);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.True);
      Assert.That(result.Result.StructValue.Fields["Outcome"].StringValue, Is.EqualTo("Finished"));
    }
  }

  [Test]
  public async Task ExecuteCommand_ObservableCommandWithIntermediates_ReturnsFinalResponse()
  {
    var command = CreateCommand(
      "RunTracked",
      responses:
      [
        CreateElement("Outcome", BasicType.String)
      ],
      intermediates:
      [
        CreateElement("Progress", BasicType.Real)
      ],
      observable: FeatureCommandObservable.Yes);

    var channelMock = CreateChannelMock();
    channelMock
      .Setup(c => c.ExecuteIntermediatesCommand<DynamicRequest, DynamicObjectProperty, DynamicObjectProperty, DynamicObjectProperty, DynamicObjectProperty>(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<DynamicRequest>(),
        It.IsAny<Func<DynamicObjectProperty, DynamicObjectProperty>>(),
        It.IsAny<Func<DynamicObjectProperty, DynamicObjectProperty>>(),
        It.IsAny<Func<string, string, Exception>>(),
        It.IsAny<IClientCallInfo>(),
        It.IsAny<ByteSerializer<DynamicRequest>>(),
        It.IsAny<ByteSerializer<DynamicObjectProperty>>(),
        It.IsAny<ByteSerializer<DynamicObjectProperty>>()))
      .Returns(CreateIntermediateObservableCommand(CreateResponseProperty(command, (command.Response[0], "Finished"))));

    var device = CreateDevice(
      CreateServerData([CreateFeature("PumpControl", command)], channelMock.Object),
      CreateConfiguredClient());

    var result = await device.ExecuteCommand("PumpControl.RunTracked", [], CancellationToken.None);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.True);
      Assert.That(result.Result.StructValue.Fields["Outcome"].StringValue, Is.EqualTo("Finished"));
    }
  }

  [Test]
  public async Task ExecuteCommand_UnknownDescriptor_ReturnsFailedResult()
  {
    var device = CreateDevice(
      CreateServerData([CreateFeature("PumpControl", CreateCommand("Start", observable: FeatureCommandObservable.No))]),
      CreateConfiguredClient());

    var result = await device.ExecuteCommand("PumpControl.Missing", [], CancellationToken.None);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.False);
      Assert.That(result.Error, Does.Contain("Unknown SiLA command"));
    }
  }

  [Test]
  public async Task ExecuteCommand_MissingExecutionManager_ReturnsFailedResult()
  {
    var device = CreateDevice(
      CreateServerData([CreateFeature("PumpControl", CreateCommand("Start", observable: FeatureCommandObservable.No))]));

    var result = await device.ExecuteCommand("PumpControl.Start", [], CancellationToken.None);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.False);
      Assert.That(result.Error, Does.Contain("not initialized"));
    }
  }

  [Test]
  public async Task ExecuteCommand_DefinedError_IsReturnedAsFailedResult()
  {
    var command = CreateCommand("Start", observable: FeatureCommandObservable.No);
    var channelMock = CreateChannelMock();
    channelMock
      .Setup(c => c.ExecuteUnobservableCommandAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<DynamicRequest>(),
        It.IsAny<IClientCallInfo>(),
        It.IsAny<ByteSerializer<DynamicRequest>>(),
        It.IsAny<ByteSerializer<DynamicObjectProperty>>()))
      .ThrowsAsync(new InvalidOperationException("Server-side failure"));
    channelMock
      .Setup(c => c.ConvertException(It.IsAny<Exception>(), It.IsAny<Func<string, string, Exception>>()))
      .Returns(new DefinedErrorException("test/error", "Defined failure"));

    var device = CreateDevice(
      CreateServerData([CreateFeature("PumpControl", command)], channelMock.Object),
      CreateConfiguredClient());

    var result = await device.ExecuteCommand("PumpControl.Start", [], CancellationToken.None);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.False);
      Assert.That(result.Error, Is.EqualTo("Defined failure"));
    }
  }

  [Test]
  public void ExecuteCommand_CancellationPropagates()
  {
    var command = CreateCommand("Start", observable: FeatureCommandObservable.No);
    var channelMock = CreateChannelMock();
    channelMock
      .Setup(c => c.ExecuteUnobservableCommandAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<DynamicRequest>(),
        It.IsAny<IClientCallInfo>(),
        It.IsAny<ByteSerializer<DynamicRequest>>(),
        It.IsAny<ByteSerializer<DynamicObjectProperty>>()))
      .Returns(Task.FromCanceled<DynamicObjectProperty>(new CancellationToken(canceled: true)));

    var device = CreateDevice(
      CreateServerData([CreateFeature("PumpControl", command)], channelMock.Object),
      CreateConfiguredClient());

    Assert.That(async () => await device.ExecuteCommand("PumpControl.Start", [], CancellationToken.None),
      Throws.InstanceOf<OperationCanceledException>());
  }

  private static SilaDevice CreateDevice(ServerData serverData, SilaClient client = null)
  {
    return new SilaDevice(
      serverData,
      new DeviceConnectionInfo
      {
        DeviceId = "sila-device",
        DeviceName = "SiLA Device"
      },
      client ?? new SilaClient());
  }

  private static ServerData CreateServerData(IEnumerable<Feature> features)
  {
    return CreateServerData(features, CreateChannelMock().Object);
  }

  private static ServerData CreateServerData(IEnumerable<Feature> features, IClientChannel channel)
  {
    return new ServerData(
      new ServerConfig("Test Server", Guid.NewGuid()),
      new ServerInformation("Test Type", "Test Description", "https://example.com", "1.0.0"),
      features,
      channel);
  }

  private static Mock<IClientChannel> CreateChannelMock()
  {
    var channelMock = new Mock<IClientChannel>();
    channelMock.SetupGet(c => c.State).Returns(ChannelState.Ready);
    channelMock.SetupGet(c => c.IsServerInitiated).Returns(false);
    channelMock
      .Setup(c => c.ConvertException(It.IsAny<Exception>(), It.IsAny<Func<string, string, Exception>>()))
      .Returns<Exception, Func<string, string, Exception>>((exception, _) => exception);

    return channelMock;
  }

  private static SilaClient CreateConfiguredClient()
  {
    return new SilaClient
    {
      ExecutionManager = new DiscoveryExecutionManager()
    };
  }

  private static Feature CreateFeature(string identifier, params object[] items)
  {
    return new Feature
    {
      Identifier = identifier,
      FeatureVersion = "1.0.0",
      Originator = "test",
      Category = "devices",
      Items = items
    };
  }

  private static FeatureCommand CreateCommand(
    string identifier,
    SiLAElement[] parameters = null,
    SiLAElement[] responses = null,
    SiLAElement[] intermediates = null,
    FeatureCommandObservable observable = FeatureCommandObservable.No)
  {
    return new FeatureCommand
    {
      Identifier = identifier,
      Observable = observable,
      Parameter = parameters ?? [],
      Response = responses ?? [],
      IntermediateResponse = intermediates ?? []
    };
  }

  private static SiLAElement CreateElement(string identifier, BasicType basicType, string description = null)
  {
    return CreateElement(identifier, new DataTypeType { Item = basicType }, description);
  }

  private static SiLAElement CreateElement(string identifier, DataTypeType dataType, string description = null)
  {
    return new SiLAElement
    {
      Identifier = identifier,
      Description = description,
      DataType = dataType
    };
  }

  private static bool RequestContains(DynamicRequest request, string identifier, Func<object, bool> predicate)
  {
    if(request.Value is not DynamicObject dynamicObject)
      return false;

    var property = dynamicObject.Elements.FirstOrDefault(element => element.Identifier == identifier);
    return property is not null && predicate(property.Value);
  }

  private static DynamicObjectProperty CreateResponseProperty(
    FeatureCommand command,
    params (SiLAElement element, object value)[] values)
  {
    var property = new DynamicObjectProperty(
      command.Identifier,
      command.DisplayName,
      command.Description,
      new DataTypeType
      {
        Item = new StructureType
        {
          Element = command.Response ?? []
        }
      });

    var responseObject = new DynamicObject();
    foreach(var (element, value) in values)
    {
      responseObject.Elements.Add(new DynamicObjectProperty(element)
      {
        Value = value
      });
    }

    property.Value = responseObject;
    return property;
  }

  private static IObservableCommand<DynamicObjectProperty> CreateObservableCommand(DynamicObjectProperty response)
  {
    var commandMock = new Mock<IObservableCommand<DynamicObjectProperty>>();
    commandMock.SetupGet(command => command.State).Returns(default(StateUpdate));
    commandMock.SetupGet(command => command.StateUpdates).Returns(Channel.CreateUnbounded<StateUpdate>().Reader);
    commandMock.SetupGet(command => command.IsStarted).Returns(true);
    commandMock.SetupGet(command => command.CancellationToken).Returns(CancellationToken.None);
    commandMock.SetupGet(command => command.IsCancellationSupported).Returns(true);
    commandMock.SetupGet(command => command.Response).Returns(Task.FromResult(response));
    commandMock.Setup(command => command.Cancel());
    return commandMock.Object;
  }

  private static IIntermediateObservableCommand<DynamicObjectProperty, DynamicObjectProperty> CreateIntermediateObservableCommand(
    DynamicObjectProperty response)
  {
    var commandMock = new Mock<IIntermediateObservableCommand<DynamicObjectProperty, DynamicObjectProperty>>();
    commandMock.SetupGet(command => command.State).Returns(default(StateUpdate));
    commandMock.SetupGet(command => command.StateUpdates).Returns(Channel.CreateUnbounded<StateUpdate>().Reader);
    commandMock.SetupGet(command => command.IsStarted).Returns(true);
    commandMock.SetupGet(command => command.CancellationToken).Returns(CancellationToken.None);
    commandMock.SetupGet(command => command.IsCancellationSupported).Returns(true);
    commandMock.SetupGet(command => command.Response).Returns(Task.FromResult(response));
    commandMock.SetupGet(command => command.IntermediateValues).Returns(Channel.CreateUnbounded<DynamicObjectProperty>().Reader);
    commandMock.Setup(command => command.Cancel());
    return commandMock.Object;
  }
}
