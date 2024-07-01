using Ares.Device;
using Ares.Messaging;
using Ares.Test;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Tests.Data.Device;

public class TestDeviceInterpreter : DeviceCommandInterpreter<TestDevice, TestDeviceCommand>
{
  public TestDeviceInterpreter(TestDevice device) : base(device)
  {
  }

  protected override Task<DeviceCommandResult> ParseAndPerformDeviceAction(TestDeviceCommand deviceCommandEnum, Parameter[] parameters, CancellationToken cancellationToken)
  {
    switch (deviceCommandEnum)
    {
      case TestDeviceCommand.Record:
      case TestDeviceCommand.Record2:
      case TestDeviceCommand.Record3:
        var result = new DeviceCommandResult();
        var reply = new TestReply();
        var param = parameters.First(parameter => parameter.Metadata.Name == TestDeviceCommandParameter.ReplyParameter.ToString());
        reply.Message = $"Device received {param.Value.Value}";
        reply.Number = param.Value.Value;
        result.Result = Any.Pack(reply);
        result.Success = true;
        result.UniqueId = Guid.NewGuid().ToString();
        return Task.FromResult(result);
      default:
        throw new ArgumentOutOfRangeException(nameof(deviceCommandEnum), deviceCommandEnum, null);
    }
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    // return in opposite order to test the ordering.
    return new[] { GetTestMetadata(2), GetTestMetadata(1), GetTestMetadata(0) };
  }

  private CommandMetadata GetTestMetadata(int idx)
  {
    var testCommandMetadata = new CommandMetadata
    {
      Description = "Test command that takes in an int and returns an int",
      DeviceName = Device.Name,
      Name = ((TestDeviceCommand)idx).ToString(),
      UniqueId = Guid.NewGuid().ToString(),
      OutputMetadata = new OutputMetadata
      {
        UniqueId = Guid.NewGuid().ToString(),
        FullName = typeof(TestReply).FullName,
        Description = "A test response for the test command",
        Index = idx
      }
    };

    testCommandMetadata.ParameterMetadatas.Add(new ParameterMetadata
    {
      Name = TestDeviceCommandParameter.ReplyParameter3.ToString(),
      UniqueId = Guid.NewGuid().ToString(),
      Unit = "TestUnit",
      Index = 2
    });

    testCommandMetadata.ParameterMetadatas.Add(new ParameterMetadata
    {
      Name = TestDeviceCommandParameter.ReplyParameter2.ToString(),
      UniqueId = Guid.NewGuid().ToString(),
      Unit = "TestUnit",
      Index = 1
    });

    testCommandMetadata.ParameterMetadatas.Add(new ParameterMetadata
    {
      Name = TestDeviceCommandParameter.ReplyParameter.ToString(),
      UniqueId = Guid.NewGuid().ToString(),
      Unit = "TestUnit",
      Index = 0
    });

    return testCommandMetadata;
  }
}
