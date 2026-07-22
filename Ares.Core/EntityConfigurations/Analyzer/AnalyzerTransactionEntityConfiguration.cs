using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Analyzer;

internal class AnalyzerTransactionEntityConfiguration : AresEntityTypeBaseConfiguration<AnalyzerTransaction>
{
  public override void Configure(EntityTypeBuilder<AnalyzerTransaction> builder)
  {
    base.Configure(builder);
    builder.Property(transaction => transaction.AnalysisRequest).HasAnalysisRequest();
    builder.Property(transaction => transaction.AnalyzerResponse).HasAnalysis();
  }
}
