using System.Reflection;
using Ares.Core;
using Ares.Messages.DeviceStates.Chiller;
using Ares.Messages.DeviceStates.Mfc;
using Ares.Messages.DeviceStates.RestDevice;
using Ares.Messages.DeviceStates.RestSerialDevice;
using Ares.Messages.DeviceStates.SyringePump;
using Ares.Messages.DeviceStates.Tc0304;
using Ares.Messages.DeviceStates.TicStepperController;
using Ares.Messages.DeviceStates.TubeFurnace;
using Microsoft.EntityFrameworkCore;

namespace AresService.Data;

public class AresDbContext : CoreDatabaseContext
{
  public AresDbContext(DbContextOptions<AresDbContext> options) : base(options)
  {
  }

  public DbSet<MfcState> MfcStates => Set<MfcState>();
  public DbSet<Tc0304State> Tc0304States => Set<Tc0304State>();
  public DbSet<SyringePumpState> SyringePumpStates => Set<SyringePumpState>();
  public DbSet<TicStepperControllerState> TicStepperControllerStates => Set<TicStepperControllerState>();
  public DbSet<TubeFurnaceStateEntity> TubeFurnaceStates => Set<TubeFurnaceStateEntity>();
  public DbSet<ChillerState> ChillerStates => Set<ChillerState>();
  public DbSet<RestDeviceStateEntity> RestDeviceStates => Set<RestDeviceStateEntity>();
  public DbSet<RestSerialDeviceStateEntity> RestSerialDeviceStates => Set<RestSerialDeviceStateEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    DatabaseRuntimeEnvironment.DatabaseProvider = Database.ProviderName;
    var assembly = Assembly.GetAssembly(typeof(AresDbContext));
    if(assembly is null)
      return;

    modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    base.OnModelCreating(modelBuilder);
  }
}
