using FlirCM3.Services;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Devices.CM3Camera
{
  public class CM3CameraUnitControlViewModel : UsbDeviceUnitViewModel
  {
    private readonly FlirCM3CameraRpc.FlirCM3CameraRpcClient _client;

    public CM3CameraUnitControlViewModel(string deviceName, FlirCM3CameraRpc.FlirCM3CameraRpcClient client) : base(deviceName)
    {
      _client = client;
    }

    public async Task SetExposureTime()
    {
      await _client.SetExposureTimeAsync(new SetExposureTimeRequest() { CameraName = DeviceName, ExposureTime = ExposureTime });
    }

    public async Task CaptureImage()
    {
      var capture_response = await _client.CaptureImageAsync(new CaptureImageRequest() { CameraName = DeviceName });
      ImageData = capture_response.ImageData.ToByteArray();

      var response = await _client.GetDisplayImageAsync(new GetDisplayImageRequest() { CameraName = DeviceName });
      DisplayData = response.DisplayImageData.ToByteArray();
    }

    [Reactive]
    public double ExposureTime { get; set; }

    [Reactive]
    public byte[]? ImageData { get; set; }

    [Reactive]
    public byte[]? DisplayData { get; set; }
  }
}
