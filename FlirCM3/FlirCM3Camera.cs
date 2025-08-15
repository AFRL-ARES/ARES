using Ares.Datamodel.Device;
using Ares.Device.USB;
using FlirCM3.Config;
using FlirCM3.Services;
using Google.Protobuf;
using SpinnakerNET;

namespace FlirCM3;

public class FlirCM3Camera : AresUSBDevice, IFlirCM3Camera
{
  private readonly ManagedSystem _managedSystem;
  private readonly IManagedImageProcessor _imageProcessor;
  private readonly IManagedCamera _camera;

  public FlirCM3Camera(string deviceName) : base(deviceName)
  {
    _managedSystem = new ManagedSystem();
    _imageProcessor = new ManagedImageProcessor();
    _camera = _managedSystem.GetCameras().First();
  }

  public override Task<bool> Activate()
  {
    Status = new DeviceOperationalStatus();

    //Initialize camera, and turn off a few settings we need defaulted to off.
    _camera.Init();
    _camera.ExposureAuto.Value = ExposureAutoEnums.Off.ToString();
    _camera.ExposureTime.Value = _camera.ExposureTime.Max;
    _camera.GainAuto.Value = GainAutoEnums.Off.ToString();
    _camera.AcquisitionMode.Value = AcquisitionModeEnums.SingleFrame.ToString();
    _camera.PixelFormat.Value = PixelFormatEnums.RGB8.ToString();
    _camera.BalanceWhiteAuto.Value = BalanceWhiteAutoEnums.Off.ToString();

    Status.OperationalState = OperationalState.Active;
    Status.Message = "Activated Flir CM3 Camera";

    return Task.FromResult(true);
  }

  public override Task EnterSafeMode()
  {
    //It's a camera...
    return Task.CompletedTask;
  }

  public ValueTask DisposeAsync()
  {
    _managedSystem.Dispose();
    _imageProcessor.Dispose();
    _camera?.Dispose();

    return ValueTask.CompletedTask;
  }

  public Task SetExposureTime(double desiredExposureTime)
  {
    //Exposure time is in microseconds. Minimum of 10 microseconds, maximum of 1 second
    if(_camera.ExposureTime == null || !_camera.ExposureTime.IsWritable || !_camera.ExposureTime.IsReadable)
      return Task.CompletedTask;

    _camera.ExposureTime.Value = (desiredExposureTime > _camera.ExposureTime.Max ? _camera.ExposureTime.Max : desiredExposureTime);
    return Task.CompletedTask;
  }

  public async Task<CaptureImageResponse> CaptureImage(string basePath)
  {
    if(_camera.IsStreaming())
      _camera.EndAcquisition();

    double timeout = _camera.ExposureTime.Value / 1000 + 1000;
    _camera.BeginAcquisition();
    var rawImage = _camera.GetNextImage((ulong)timeout);
    //TODO: Utilize this more properly once we fix our analyzer
    //var imageFileName = $"sample_image_{rawImage.TimeStamp}.tiff";

    if(rawImage.IsIncomplete)
      Console.WriteLine($"Image incomplete with image status {rawImage.ImageStatus}");

    //Convert the image
    var convertedImage = _imageProcessor.Convert(rawImage, PixelFormatEnums.RGB8);

    //Save to file location
    var savePath = Path.Combine(basePath, "sample_image.tif");
    var pngPath = Path.Combine(basePath, "sample_image.png");

    convertedImage.Save(savePath);
    convertedImage.Save(pngPath);

    ImageData = await File.ReadAllBytesAsync(savePath);
    DisplayImageData = await File.ReadAllBytesAsync(pngPath);
    LatestImagePath = savePath;

    if(File.Exists(pngPath))
      File.Delete(pngPath);

    _camera.EndAcquisition();
    return new CaptureImageResponse() { ImageData = ByteString.CopyFrom(ImageData) };
  }

  public void PopulateSettings(FlirCM3Config config)
  {
    _camera.Width.Value = config.CaptureWidth;
    _camera.Height.Value = config.CaptureHeight;
    _camera.OffsetX.Value = config.OffsetX;
    _camera.OffsetY.Value = config.OffsetY;

    //Exposure time, gain, black level, and pixel format
    _camera.ExposureTime.Value = config.ExposureTime;
    _camera.Gain.Value = config.Gain;
    _camera.BlackLevel.Value = config.BlackLevel;
    _camera.PixelFormat.Value = PixelFormatEnums.RGB8.ToString();

    //Balance Ratio Red and Blue
    _camera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Blue.ToString();
    _camera.BalanceRatio.Value = config.BlueBalance;

    _camera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Red.ToString();
    _camera.BalanceRatio.Value = config.RedBalance;
  }

  public byte[] ImageData { get; set; } = Array.Empty<byte>();

  public byte[] DisplayImageData { get; set; } = Array.Empty<byte>();

  public string LatestImagePath { get; set; } = string.Empty;
}
