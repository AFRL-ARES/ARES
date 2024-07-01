using Ares.Core;
using Ares.Messages.DeviceStates.Mfc;
using Ares.Messages.DeviceStates.SyringePump;
using Ares.Messages.DeviceStates.Tc0304;
using Ares.Messages.DeviceStates.TicStepperController;
using Ares.Messages.DeviceStates.TubeFurnace;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ARESCore;

public class ARESDbContext : CoreDatabaseContext
{
  public ARESDbContext(DbContextOptions<ARESDbContext> options) : base(options)
  {
  }

  public DbSet<MfcState> MfcStates => Set<MfcState>();
  public DbSet<Tc0304State> Tc0304States => Set<Tc0304State>();
  public DbSet<SyringePumpState> SyringePumpStates => Set<SyringePumpState>();
  public DbSet<TicStepperControllerState> TicStepperControllerStates => Set<TicStepperControllerState>();
  public DbSet<TubeFurnaceStateEntity> TubeFurnaceStates => Set<TubeFurnaceStateEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    var assembly = Assembly.GetAssembly(typeof(ARESDbContext));
    if (assembly is null)
      return;

    modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    base.OnModelCreating(modelBuilder);
  }
}
