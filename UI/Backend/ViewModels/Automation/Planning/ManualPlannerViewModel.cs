using Ares.Messaging;
using Ares.Messaging.Planning;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components.Forms;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Automation.Planning;

public class ManualPlannerViewModel : ReactiveObject
{
  private readonly AresPlanning.AresPlanningClient _client;

  public ManualPlannerViewModel(AresPlanning.AresPlanningClient client)
  {
    _client = client;
    _ = Init();
  }

  [Reactive]
  public IEnumerable<ManualPlannerSet> ManualPlannerSetCollection { get; private set; } = Array.Empty<ManualPlannerSet>();

  private async Task Init()
  {
    var collection = await _client.GetManualPlannerSeedAsync(new Empty());
    ManualPlannerSetCollection = collection.PlannedValues;
  }

  public async Task<bool> FileUploaded(IBrowserFile file)
  {
    var collection = new ManualPlannerSetCollection();
    var stream = file.OpenReadStream();
    var reader = new StreamReader(stream);
    var result = await reader.ReadLineAsync();
    var header = result?.Split(',', StringSplitOptions.TrimEntries);
    if (header is null)
      return false;

    result = await reader.ReadLineAsync();
    while (result is not null)
    {
      try
      {
        var splitResult = result.Split(',');
        var plannerSet = new ManualPlannerSet();
        plannerSet.ParameterValues.AddRange(splitResult.Select((s, i) => new ParameterNameValuePair { Name = header[i], Value = double.Parse(s) }));
        collection.PlannedValues.Add(plannerSet);
      }
      catch (Exception)
      {
        return false;
      }

      result = await reader.ReadLineAsync();
    }

    if (!collection.PlannedValues.Any())
      return true;

    await _client.SeedManualPlannerAsync(new ManualPlannerSeed { PlannerValues = collection });
    await Init();

    return true;
  }
}
