using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

public class AnalyzerInfoEntityConfiguration : AresEntityTypeBaseConfiguration<AnalyzerInfo>
{
  public override void Configure(EntityTypeBuilder<AnalyzerInfo> builder)
  {
    base.Configure(builder);
    builder.ToTable("Analyzers");
  }
}
