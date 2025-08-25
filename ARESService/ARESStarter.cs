using Ares.Core.Analyzing;
using Ares.Core.Device;
using Ares.Core.Grpc;
using Ares.Core.Planning;
using DemoDevice;
using AresService.DeviceDbLoaders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ares.Services;

namespace AresService;

public class AresStarter
{
  private readonly IRemoteAnalyzerManager _analyzerManager;
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IEnumerable<IDeviceDbLoader> _deviceLoaders;
  private readonly IRemotePlannerManager _plannerManager;
  private readonly IConfiguration _configuration;
  private readonly string _dataPath;
  private readonly string _resultsPath;
  private readonly string _templatesPath;
  private readonly string _devicesPath;

  public AresStarter(
    IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDbContextFactory<AresDbContext> dbContextFactory,
    IRemotePlannerManager plannerManager,
    IRemoteAnalyzerManager analyzerManager,
    IEnumerable<IDeviceDbLoader> deviceLoaders,
    IConfiguration configuration)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _dbContextFactory = dbContextFactory;
    _plannerManager = plannerManager;
    _analyzerManager = analyzerManager;
    _deviceLoaders = deviceLoaders;
    _configuration = configuration;
    _dataPath = _configuration.Get<AppSettings>().AresDataPath;
    _resultsPath = Path.Combine(_dataPath, AppSettings.ResultsFolder);
    _templatesPath = Path.Combine(_dataPath, AppSettings.TemplatesFolder);
    _devicesPath = Path.Combine(_dataPath, AppSettings.DevicesFolder);
  }

  public async Task Start()
  {
    await EnsureDataPathsExist();
    await AddDemoDevice(new Uri("https://localhost:7038"));

    foreach(var deviceLoader in _deviceLoaders)
      await deviceLoader.Load();

    await _plannerManager.LoadPlanners();
    await _analyzerManager.LoadAnalyzers();

    Observable.Interval(TimeSpan.FromSeconds(20))
      .Take(1)
      .Subscribe(_ => ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse { ServerStatus = ServerStatus.Error, StatusMessage = "This is a test error from server." }));
  }

  public Task EnsureDataPathsExist()
  {
    Directory.CreateDirectory(_devicesPath);
    Directory.CreateDirectory(_resultsPath);
    Directory.CreateDirectory(_templatesPath);

    return Task.CompletedTask;
  }

  public Task AddDemoDevice(Uri address)
  {
    var testDevice = new AresDemoDevice(address);
    testDevice.Activate();
    var testDeviceInterpreter = new DemoDeviceInterpreter(testDevice);
    _deviceCommandInterpreterRepo.Add(testDeviceInterpreter);
    return Task.CompletedTask;
  }
}
