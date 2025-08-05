using Ares.Messaging.Device;
using FlirCM3.Config;
using FlirCM3.Services;
using Google.Protobuf.WellKnownTypes;

namespace FlirCM3.Simulation
{
  public class SimCM3Camera : IFlirCM3Camera
  {
    private readonly string _simResultImage = "test_result.tiff";

    public SimCM3Camera(string deviceName)
    {
      Name = deviceName;
      Status = new DeviceStatus();
    }

    public Task<bool> Activate()
    {
      Status.DeviceState = DeviceState.Active;
      Status.Message = "Successfully activated simulated camera!";

      return Task.FromResult(true);
    }

    public async Task<CaptureImageResponse> CaptureImage(string basePath)
    {
      var simImagePath = GenerateResultImagePath();
      var savePath = Path.Combine(basePath, _simResultImage);

      try
      {
        File.Copy(simImagePath, savePath, true);
      }

      catch(Exception ex)
      {
        Console.WriteLine(ex.Message);
      }

      await Task.Delay(TimeSpan.FromSeconds(3));
      var data = await File.ReadAllBytesAsync(simImagePath);

      return new CaptureImageResponse();
    }

    private string GenerateResultImagePath()
    {
      var directory = Directory.GetCurrentDirectory();

      if(directory is null)
        return string.Empty;

      directory = Directory.GetParent(directory).FullName;

      return Path.Combine(directory, "FlirCM3", "Simulation", _simResultImage);
    }

    public async Task SetExposureTime(double desiredExposureTime)
    {
      ExposureTime = desiredExposureTime;
      await Task.Delay(TimeSpan.FromSeconds(3));
    }

    public ValueTask DisposeAsync()
    {
      return ValueTask.CompletedTask;
    }

    public void PopulateSettings(FlirCM3Config config)
    {
      Width = config.CaptureWidth;
      Height = config.CaptureHeight;
      OffsetX = config.OffsetX;
      OffsetY = config.OffsetY;
      ExposureTime = config.ExposureTime;
      Gain = config.Gain;
      BlackLevel = config.BlackLevel;
      BlueBalance = config.BlueBalance;
      RedBalance = config.RedBalance;
    }

    public string Name { get; set; }
    public DeviceStatus Status { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double ExposureTime { get; set; }
    public double Gain { get; set; }
    public double BlackLevel { get; set; }
    public double BlueBalance { get; set; }
    public double RedBalance { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public byte[] DisplayImageData { get; set; } = Array.Empty<byte>();
    public string LatestImagePath { get; set; } = string.Empty;
  }
}
