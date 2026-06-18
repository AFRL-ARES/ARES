using Ares.Core.Device.Providers;
using Ares.Core.Device.State.Logging;
using Ares.Core.Tests.Data.Device;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ares.Core.Tests.Device.State.Logging;

internal class StateLoggerManagerTests
{
  private DbContextOptions<CoreDatabaseContext> _dbOptions;
  private Mock<IDbContextFactory<CoreDatabaseContext>> _dbContextFactory;
  private DeviceStateLoggerRepository _repository;
  private Mock<IDeviceStateLoggerFactory> _loggerFactory;
  private StateLoggerManager _manager;

  [SetUp]
  public void SetUp()
  {
    _dbOptions = new DbContextOptionsBuilder<CoreDatabaseContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    _dbContextFactory = new Mock<IDbContextFactory<CoreDatabaseContext>>();
    _dbContextFactory
      .Setup(factory => factory.CreateDbContext())
      .Returns(() => new CoreDatabaseContext(_dbOptions));
    _repository = new DeviceStateLoggerRepository();
    _loggerFactory = new Mock<IDeviceStateLoggerFactory>();
    _manager = new StateLoggerManager(
      _repository,
      _loggerFactory.Object,
      Mock.Of<ILogger<StateLoggerManager>>(),
      _dbContextFactory.Object,
      Mock.Of<IAresDeviceProvider>());
  }

  [Test]
  public async Task EnableOverrideAsync_WithLoggingEnabled_AppliesEnabledClone()
  {
    var logger = AddLogger("device-1");
    var settings = CreateSettings(loggingEnabled: false);
    DeviceLoggingSettings appliedSettings = null;
    logger
      .Setup(stateLogger => stateLogger.UpdateSettings(It.IsAny<DeviceLoggingSettings>()))
      .Callback<DeviceLoggingSettings>(value => appliedSettings = value)
      .Returns(Task.CompletedTask);

    await _manager.EnableOverrideAsync(settings, true);

    Assert.Multiple(() =>
    {
      Assert.That(appliedSettings, Is.Not.Null);
      Assert.That(appliedSettings!.LoggingEnabled, Is.True);
      Assert.That(appliedSettings.LoggingType, Is.EqualTo(settings.LoggingType));
      Assert.That(settings.LoggingEnabled, Is.False);
      Assert.That(appliedSettings, Is.Not.SameAs(settings));
    });
  }

  [Test]
  public async Task EnableOverrideAsync_WithLoggingDisabled_AppliesDisabledSettings()
  {
    var logger = AddLogger("device-1");
    DeviceLoggingSettings appliedSettings = null;
    logger
      .Setup(stateLogger => stateLogger.UpdateSettings(It.IsAny<DeviceLoggingSettings>()))
      .Callback<DeviceLoggingSettings>(value => appliedSettings = value)
      .Returns(Task.CompletedTask);

    await _manager.EnableOverrideAsync(CreateSettings(loggingEnabled: true), false);

    Assert.That(appliedSettings, Is.Not.Null);
    Assert.That(appliedSettings!.LoggingEnabled, Is.False);
  }

  [Test]
  public async Task DisableOverrideAsync_RestoresPersistedSettings()
  {
    var logger = AddLogger("device-1");
    var persistedSettings = CreateSettings("device-1", loggingEnabled: false);
    await SaveSettings(persistedSettings);

    await _manager.EnableOverrideAsync(CreateSettings(loggingEnabled: true), true);
    await _manager.DisableOverrideAsync();

    logger.Verify(
      stateLogger => stateLogger.UpdateSettings(It.Is<DeviceLoggingSettings>(
        settings => settings.DeviceId == "device-1" && !settings.LoggingEnabled)),
      Times.Once);
  }

  [Test]
  public async Task DisableOverrideAsync_RestoresSettingsUpdatedDuringOverride()
  {
    var logger = AddLogger("device-1");
    await SaveSettings(CreateSettings("device-1", loggingEnabled: false));
    await _manager.EnableOverrideAsync(CreateSettings(loggingEnabled: true), true);
    var updatedSettings = new DeviceLoggingSettings
    {
      DeviceId = "device-1",
      LoggingEnabled = true,
      LoggingType = DeviceLoggingSettings.Types.LoggingType.Interval,
      IntervalMs = 250
    };

    await _manager.UpdateLogger("device-1", updatedSettings);
    await _manager.DisableOverrideAsync();

    logger.Verify(
      stateLogger => stateLogger.UpdateSettings(It.Is<DeviceLoggingSettings>(
        settings => settings.DeviceId == "device-1"
          && settings.LoggingEnabled
          && settings.LoggingType == DeviceLoggingSettings.Types.LoggingType.Interval
          && settings.IntervalMs == 250)),
      Times.Once);
  }

  [Test]
  public async Task SetupLogger_DuringOverride_StartsWithOverrideSettings()
  {
    var device = new TestDevice("Device", "device-1");
    var logger = CreateFactoryLogger(device.UniqueId);
    var suppliedSettings = CreateSettings(loggingEnabled: false);
    DeviceLoggingSettings startedSettings = null;
    logger
      .Setup(stateLogger => stateLogger.Start(It.IsAny<DeviceLoggingSettings>()))
      .Callback<DeviceLoggingSettings>(value => startedSettings = value)
      .Returns(Task.CompletedTask);

    await _manager.EnableOverrideAsync(suppliedSettings, true);
    await _manager.SetupLogger(device);

    Assert.Multiple(() =>
    {
      Assert.That(startedSettings, Is.Not.Null);
      Assert.That(startedSettings!.LoggingEnabled, Is.True);
      Assert.That(startedSettings, Is.Not.SameAs(suppliedSettings));
    });
  }

  [Test]
  public async Task AfterDisable_NewDevicesUseDatabaseSettingsAndUpdatesApplyNormally()
  {
    var existingLogger = AddLogger("existing-device");
    existingLogger
      .Setup(logger => logger.UpdateSettings(It.IsAny<DeviceLoggingSettings>()))
      .Returns(Task.CompletedTask);
    await _manager.EnableOverrideAsync(CreateSettings(loggingEnabled: true), true);
    await _manager.DisableOverrideAsync();

    var device = new TestDevice("Device", "device-1");
    var persistedSettings = CreateSettings(device.UniqueId, loggingEnabled: false);
    await SaveSettings(persistedSettings);
    var newLogger = CreateFactoryLogger(device.UniqueId);
    DeviceLoggingSettings startedSettings = null;
    newLogger
      .Setup(logger => logger.Start(It.IsAny<DeviceLoggingSettings>()))
      .Callback<DeviceLoggingSettings>(value => startedSettings = value)
      .Returns(Task.CompletedTask);

    await _manager.SetupLogger(device);
    var updatedSettings = CreateSettings(device.UniqueId, loggingEnabled: true);
    await _manager.UpdateLogger(device.UniqueId, updatedSettings);

    Assert.Multiple(() =>
    {
      Assert.That(startedSettings, Is.Not.Null);
      Assert.That(startedSettings.DeviceId, Is.EqualTo(persistedSettings.DeviceId));
      Assert.That(startedSettings.LoggingEnabled, Is.EqualTo(persistedSettings.LoggingEnabled));
      Assert.That(startedSettings.LoggingType, Is.EqualTo(persistedSettings.LoggingType));
      Assert.That(startedSettings.IntervalMs, Is.EqualTo(persistedSettings.IntervalMs));
    });
    newLogger.Verify(logger => logger.UpdateSettings(updatedSettings), Times.Once);
  }

  private Mock<IDeviceStateLogger> AddLogger(string deviceId)
  {
    var logger = new Mock<IDeviceStateLogger>();
    logger.SetupGet(stateLogger => stateLogger.DeviceId).Returns(deviceId);
    logger
      .Setup(stateLogger => stateLogger.UpdateSettings(It.IsAny<DeviceLoggingSettings>()))
      .Returns(Task.CompletedTask);
    _repository[deviceId] = logger.Object;
    return logger;
  }

  private Mock<IDeviceStateLogger> CreateFactoryLogger(string deviceId)
  {
    var logger = new Mock<IDeviceStateLogger>();
    logger.SetupGet(stateLogger => stateLogger.DeviceId).Returns(deviceId);
    logger.Setup(stateLogger => stateLogger.Start(It.IsAny<DeviceLoggingSettings>())).Returns(Task.CompletedTask);
    _loggerFactory.Setup(factory => factory.Create(It.IsAny<Ares.Device.IAresDevice>())).Returns(logger.Object);
    return logger;
  }

  private async Task SaveSettings(DeviceLoggingSettings settings)
  {
    await using var context = new CoreDatabaseContext(_dbOptions);
    context.DeviceLoggingSettings.Add(settings);
    await context.SaveChangesAsync();
  }

  private static DeviceLoggingSettings CreateSettings(string deviceId = "", bool loggingEnabled = true)
  {
    return new DeviceLoggingSettings
    {
      DeviceId = deviceId,
      LoggingEnabled = loggingEnabled,
      LoggingType = DeviceLoggingSettings.Types.LoggingType.OnChange,
      IntervalMs = 100
    };
  }
}
