using Ares.Core.Tests.Data.Device;

namespace Ares.Core.Tests;

internal class DeviceLibraryTests
{
  [Test]
  public void DeviceCommandInterpreter_Should_Return_Properly_Ordered_Commands()
  {
    var device = new TestDevice();
    var interpreter = new TestDeviceInterpreter(device);
    var commands = interpreter.CommandsToIndexedMetadatas().ToArray();

    Assert.That(commands.All(metadata => metadata.ParameterMetadatas.Select((parameterMetadata, i) => parameterMetadata.Index == i).All(b => b)), "One or more parameters were not ordered properly.");
  }
}
