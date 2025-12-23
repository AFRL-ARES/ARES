using FlirCM3.Services;
using ReactiveUI.Fody.Helpers;
using UI.Pages.Shared.Devices.CM3Camera;

namespace UI.Backend.ViewModels.Devices.CM3Camera;

public class CM3CameraUnitControlViewModel : DeviceUnitControlViewModel
{
  private readonly FlirCM3CameraRpc.FlirCM3CameraRpcClient _client;

  public CM3CameraUnitControlViewModel(string deviceId, string deviceName, FlirCM3CameraRpc.FlirCM3CameraRpcClient client) : base(deviceId, deviceName)
  {
    _client = client;
    ViewType = typeof(CM3CameraControlWidgetView);
    DefaultWidth = 30;
  }

  public async Task SetExposureTime()
  {
    await _client.SetExposureTimeAsync(new SetExposureTimeRequest() { CameraId = DeviceId, ExposureTime = ExposureTime });
  }

  public async Task CaptureImage()
  {
    var capture_response = await _client.CaptureImageAsync(new CaptureImageRequest() { CameraId = DeviceId });
    ImageData = capture_response.ImageData.ToByteArray();

    var response = await _client.GetDisplayImageAsync(new GetDisplayImageRequest() { CameraId = DeviceId });
    DisplayData = response.DisplayImageData.ToByteArray();
  }

  [Reactive]
  public double ExposureTime { get; set; }

  [Reactive]
  public byte[]? ImageData { get; set; }

  [Reactive]
  public byte[]? DisplayData { get; set; }
}
