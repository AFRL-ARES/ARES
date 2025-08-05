using Ares.Messaging.Device;
using FlirCM3.Config;
using FlirCM3.Services;
using ReactiveUI;
using System.ComponentModel.DataAnnotations;

namespace UI.Backend.ViewModels.Settings.Device.CM3Camera
{
  public class FlirCM3ConfigEditViewModel : ReactiveObject
  {
    private readonly FlirCM3CameraRpc.FlirCM3CameraRpcClient _client;
    private readonly FlirCM3Config _config;
    private readonly AresDevices.AresDevicesClient _devicesClient;
    private string? _name;
    private double _exposureCompensation = 0;
    private double _gain = 0;
    private double _redBalance = 2;
    private double _blueBalance = 2;
    private double _blackLevel = 0;
    private int _exposureTime = 30291;
    private int _width = 1280;
    private int _height = 1024;
    private int _offsetX = 0;
    private int _offsetY = 0;


    public FlirCM3ConfigEditViewModel(FlirCM3CameraRpc.FlirCM3CameraRpcClient client,
      AresDevices.AresDevicesClient devicesClient)
    {
      _client = client;
      _devicesClient = devicesClient;
      _config = new FlirCM3Config();
      NewConfig = true;
    }

    public FlirCM3ConfigEditViewModel(FlirCM3CameraRpc.FlirCM3CameraRpcClient client,
      AresDevices.AresDevicesClient devicesClient,
      FlirCM3Config config)
    {
      _client = client;
      _config = config;
      _devicesClient = devicesClient;
      _name = config.Name;
      Simulated = config.Simulated;
      NewConfig = false;
    }

    [Required]
    public string? Name
    {
      get => _name;

      set
      {
        if(!NewConfig)
          return;

        _name = value;
      }
    }

    public bool NewConfig { get; set; }
    public bool Simulated { get; set; }
    public bool Modified { get; set; }

    //In EV units, -7.5 to 2.5
    public double ExposureCompensation
    {
      get => _exposureCompensation;

      set
      {
        if(_exposureCompensation != value)
        {
          _exposureCompensation = value;
          Modified = true;
        }
      }
    }

    // 10 microseconds to 30291 microseconds
    public int ExposureTime
    {
      get => _exposureTime;

      set
      {
        if(_exposureTime != value)
        {
          _exposureTime = value;
          Modified = true;
        }
      }
    }

    // 0 - 18 decibels
    public double Gain
    {
      get => _gain;

      set
      {
        if(_gain != value)
        {
          _gain = value;
          Modified = true;
        }
      }
    }

    //0 - 25
    public double BlackLevel
    {
      get => _blackLevel;

      set
      {
        if(value != _blackLevel)
        {
          _blackLevel = value;
          Modified = true;
        }
      }
    }

    //0.25 - 4
    public double RedBalanceRatio
    {
      get => _redBalance;

      set
      {
        if(value != _redBalance)
        {
          _redBalance = value;
          Modified = true;
        }
      }
    }

    //0.25 - 4
    public double BlueBalanceRatio
    {
      get => _blueBalance;

      set
      {
        if(value != _blueBalance)
        {
          _blueBalance = value;
          Modified = true;
        }
      }
    }

    //Min 16, Max 1280
    public int Width
    {
      get => _width;

      set
      {
        if(value != _width)
        {
          _width = value;
          Modified = true;
        }
      }
    }

    //Min 2, Max 1024
    public int Height
    {
      get => _height;

      set
      {
        if(value != _height)
        {
          _height = value;
          Modified = true;
        }
      }
    }

    //Variable minimums and maximums
    public int OffsetX
    {
      get => _offsetX;

      set
      {
        if(value != _offsetX)
        {
          _offsetX = value;
          Modified = true;
        }
      }
    }

    //Variable minimums and maximums
    public int OffsetY
    {
      get => _offsetY;

      set
      {
        if(value != _offsetY)
        {
          _offsetY = value;
          Modified = true;
        }
      }

    }

    public FlirCM3Config Save()
    {
      var config = Modified ? new FlirCM3Config { Name = Name, Simulated = Simulated } : _config;

      config.ExposureCompensation = ExposureCompensation;
      config.ExposureTime = ExposureTime;
      config.Gain = Gain;
      config.BlackLevel = BlackLevel;
      config.RedBalance = RedBalanceRatio;
      config.BlueBalance = BlueBalanceRatio;
      config.CaptureWidth = Width;
      config.CaptureHeight = Height;
      config.OffsetX = OffsetX;
      config.OffsetY = OffsetY;
      config.Simulated = Simulated;
      config.Name = Name;

      return config;
    }
  }
}
