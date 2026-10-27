using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Analyzer;

internal class AnalyzerTransactionEntityConfiguration : AresEntityTypeBaseConfiguration<AnalyzerTransaction>
{
  public override void Configure(EntityTypeBuilder<AnalyzerTransaction> builder)
  {
    base.Configure(builder);
    builder.Property(transaction => transaction.AnalysisRequest).HasAnalysisRequest();
#pragma warning disable CS0612 // Type or member is obsolete
    builder.Property(transaction => transaction.AnalysisResponse).HasAnalysis(); //Known depricated field, kept for backwards compatability
#pragma warning restore CS0612 // Type or member is obsolete
    builder.Property(transaction => transaction.AnalyzerResponse).HasAnalyzerResponse();
  }
}
