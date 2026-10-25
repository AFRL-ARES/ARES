using System.Reflection;
using System.Reactive.Subjects;
using Ares.Core.Device.Providers;
using Ares.Core.Device.Sila;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tecan.Sila2;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;
using UI.Infrastructure.Devices.Sila;

namespace UI.Tests.Infrastructure.Devices.Sila;

[TestFixture]
public class SilaDeviceAdapterTests
{
  [Test]
  public async Task StartStateStream_ForwardsSilaDeviceStateUpdates()
  {
    const string deviceId = "sila-device";
    var channel = new Mock<IClientChannel>();
    var serverData = new ServerData(
      new ServerConfig("Test Server", Guid.NewGuid()),
      new ServerInformation("Test Type", "Test Description", "127.0.0.1:50051", "1.0.0"),
      [],
      channel.Object);

    await using var device = new SilaDevice(
      serverData,
      new DeviceConnectionInfo
      {
        DeviceId = deviceId,
        DeviceName = "SiLA Device"
      },
      new SilaClient(),
      NullLogger<SilaDevice>.Instance);

    var deviceProvider = new Mock<IAresDeviceProvider>();
    deviceProvider.Setup(provider => provider.GetDevice<SilaDevice>(deviceId)).Returns(device);

    await using var adapter = new SilaDeviceAdapter(
      deviceProvider.Object,
      deviceId,
      NullLogger<SilaDeviceAdapter>.Instance);

    AresStruct? receivedState = null;
    using var subscription = adapter.StateStream.Subscribe(state => receivedState = state);
    await adapter.StartStateStream();

    var expectedState = new AresStruct
    {
      Fields =
      {
        ["PumpControl.FlowRate"] = AresValueHelper.CreateNumber(12.5)
      }
    };

    GetStateSubject(device).OnNext(expectedState);

    Assert.That(receivedState, Is.SameAs(expectedState));
  }

  private static BehaviorSubject<AresStruct> GetStateSubject(SilaDevice device)
  {
    var field = typeof(SilaDevice).GetField("_stateSubject", BindingFlags.Instance | BindingFlags.NonPublic);
    return (BehaviorSubject<AresStruct>)field!.GetValue(device)!;
  }
}
