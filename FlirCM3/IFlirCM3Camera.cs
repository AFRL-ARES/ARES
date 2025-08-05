using Ares.Device.USB;
using FlirCM3.Config;
using FlirCM3.Services;

namespace FlirCM3
{
  public interface IFlirCM3Camera : IAresUSBDevice, IAsyncDisposable
  {
    void PopulateSettings(FlirCM3Config config);
    Task<CaptureImageResponse> CaptureImage(string basePath);
    Task SetExposureTime(double desiredExposureTime);

    byte[] ImageData { get; }

    byte[] DisplayImageData { get; }

    string LatestImagePath { get; }
  }
}
