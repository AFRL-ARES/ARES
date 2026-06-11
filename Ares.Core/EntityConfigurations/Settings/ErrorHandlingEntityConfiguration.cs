using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Settings;

internal class ErrorHandlingEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceErrorHandlingConfig>
{
  public override void Configure(EntityTypeBuilder<DeviceErrorHandlingConfig> builder)
  {
    base.Configure(builder);
    builder.ToTable("ErrorHandlingConfigs");
  }
}
