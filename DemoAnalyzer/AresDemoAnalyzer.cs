using Ares.Core.Analyzing;
using Ares.Messaging;
using DemoDevice;

namespace DemoAnalyzer;
public class AresDemoAnalyzer : AnalyzerBase<GrowthResponse>
{
  readonly Uri _address;
  public AresDemoAnalyzer(string name, Uri address) : base(name, new Version(1, 0))
  {
    _address = address;
  }

  protected override async Task<Analysis> AnalyzeMessage(GrowthResponse input, CancellationToken cancellationToken)
  {
    var client = ClientStore.DemoPlanningClient;
    var result = await client.AnalyzeAsync(new AnalysisRequest { Growth = input.Growth });
    return new Analysis
    {
      Analyzer = new() { Name = Name, Type = GetType().Name, Version = Version.ToString() },
      Result = Convert.ToSingle(result.Value)
    };
  }

  public void Init()
  {
    ClientStore.CreateClient(_address);
  }
}
