using Ares.Core.Analyzing;
using Ares.Core.Device;
using Ares.Core.Grpc;
using Ares.Core.Planning;
using Ares.Device;
using Ares.Messaging;
using ARESCore;
using ARESCore.DeviceDbLoaders;
using DemoAnalyzer;
using DemoDevice;
using DemoPlanner;
using Microsoft.EntityFrameworkCore;
using SyringePumpNE1000;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ARESService;

public class ARESStarter
{
  private readonly IAnalyzerManager _analyzerManager;
  private readonly IDbContextFactory<ARESDbContext> _dbContextFactory;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IEnumerable<IDeviceDbLoader> _deviceLoaders;
  private readonly IPlannerManager _plannerManager;

  public ARESStarter(
    IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDbContextFactory<ARESDbContext> dbContextFactory,
    IPlannerManager plannerManager,
    IAnalyzerManager analyzerManager,
    IEnumerable<IDeviceDbLoader> deviceLoaders)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _dbContextFactory = dbContextFactory;
    _plannerManager = plannerManager;
    _analyzerManager = analyzerManager;
    _deviceLoaders = deviceLoaders;
  }

  public async Task Start()
  {
    await AddDemoDevice(new Uri("https://localhost:7037"));
    await AddDemoPlanner(new Uri("https://localhost:7069"));
    await AddDemoAnalyzer(new Uri("https://localhost:7086"));
    foreach (var deviceLoader in _deviceLoaders)
      await deviceLoader.Load();

    Observable.Interval(TimeSpan.FromSeconds(20))
      .Take(1)
      .Subscribe(_ => ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse { ServerStatus = ServerStatus.Error, StatusMessage = "This is a test error from server." }));
  }


  public Task AddDemoDevice(Uri address)
  {
    var testDevice = new AresDemoDevice("DemoDevice", address);
    testDevice.Activate();
    var testDeviceInterpreter = new DemoDeviceInterpreter(testDevice);
    _deviceCommandInterpreterRepo.Add(testDeviceInterpreter);
    return Task.CompletedTask;
  }

  public Task AddDemoPlanner(Uri address)
  {
    var demoPlanner = new AresDemoPlanner("Demo Planner", address);
    demoPlanner.Init();
    _plannerManager.RegisterPlanner(demoPlanner);
    return Task.CompletedTask;
  }

  public Task AddDemoAnalyzer(Uri address)
  {
    var demoAnalyzer = new AresDemoAnalyzer("Demo Analyzer", address);
    demoAnalyzer.Init();
    _analyzerManager.RegisterAnalyzer(demoAnalyzer);
    return Task.CompletedTask;
  }

  public void RemoveSyringePumpInterpreter(IDeviceCommandInterpreter<ISyringePump> syringePumpInterpreter)
  {
    // TODO:
    // Cancel running tasks
    // Stop listening
    // Close port
    // syringePumpInterpreter.Device.Disconnect();
    _deviceCommandInterpreterRepo.Remove(syringePumpInterpreter);
  }
}
