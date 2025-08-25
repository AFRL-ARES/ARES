using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;
internal class AnalyzerInfoEntityConfiguration : AresEntityTypeBaseConfiguration<AnalyzerInfo>
{
  public override void Configure(EntityTypeBuilder<AnalyzerInfo> builder)
  {
    base.Configure(builder);

    builder
      .HasOne(p => p.Capabilities)
      .WithOne()
      .HasForeignKey<AnalyzerCapabilities>("AnalyzerInfoId")
      .IsRequired()
      .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);

    builder.Navigation(p => p.Capabilities).AutoInclude();
  }
}
