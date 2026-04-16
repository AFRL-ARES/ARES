using Ares.Core.Device.Sila;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Device;
using Moq;
using Tecan.Sila2;
using Tecan.Sila2.Client;

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

  private static SilaDevice CreateDevice(ServerData serverData)
  {
    return new SilaDevice(
      serverData,
      new DeviceConnectionInfo
      {
        DeviceId = "sila-device",
        DeviceName = "SiLA Device"
      },
      new SilaClient());
  }

  private static ServerData CreateServerData(IEnumerable<Feature> features)
  {
    return new ServerData(
      new ServerConfig("Test Server", Guid.NewGuid()),
      new ServerInformation("Test Type", "Test Description", "https://example.com", "1.0.0"),
      features,
      new Mock<IClientChannel>().Object);
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
    SiLAElement[] responses = null)
  {
    return new FeatureCommand
    {
      Identifier = identifier,
      Parameter = parameters ?? [],
      Response = responses ?? []
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
}
