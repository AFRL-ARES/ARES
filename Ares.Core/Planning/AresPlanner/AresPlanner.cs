using Ares.Messaging;
using Ares.Messaging.Analyzing;
using Ares.Messaging.Planning;
using Ares.Tools;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using System.Reactive.Linq;

namespace Ares.Core.Planning.AresPlanner;

public class AresPlanner : IPlanner
{
  readonly Uri _address;

  public AresPlanner(string name, Uri address)
  {
    _address = address;
    Name = name;
    Address = address.OriginalString;
    Status = new PlannerStatus { PlannerState = PlannerState.Inactive, Message = $"{name} has not been activated" };
    UniqueId = Guid.NewGuid().ToString();
  }

  public async Task<IEnumerable<PlanResult>> Plan(IEnumerable<ParameterMetadata> plannableParameters, IEnumerable<CompletedExperiment> completedExperiments, IEnumerable<Analysis> _experimentAnalyses, CancellationToken cancellationToken)
  {
    var client = ClientStore.AresPlanningClient ?? throw new InvalidOperationException($"Failed to plan as the remote client has not been established yet.");
    var planRequest = new PlanRequest();
    planRequest.PlanningParameters.AddRange(plannableParameters.Select(parameter => ConvertToPlanningParameter(parameter, completedExperiments)));
    var result = await client.PlanAsync(planRequest, deadline: DateTime.UtcNow.AddSeconds(30));
    return ToPlanResults(result, plannableParameters);
  }

  public IEnumerable<PlanResult> ToPlanResults(PlanResponse result, IEnumerable<ParameterMetadata> plannableMetadata)
  {
    var planResults = new List<PlanResult>();

    if(result.ParameterValues.Count() != result.ParameterNames.Count())
      return planResults;

    for(int i = 0; i < result.ParameterNames.Count; i++)
    {
      var matchingMetadata = plannableMetadata.FirstOrDefault(data => data.Name == result.ParameterNames[i]);

      //What do we do if we don't find the old metadata?
      if(matchingMetadata is null)
      {
        matchingMetadata = new ParameterMetadata();
        matchingMetadata.Name = result.ParameterNames[i];
      }

      var valueResult = AresValueHelper.CreateNumber(result.ParameterValues[i]);
      var aresPlanResult = new PlanResult(matchingMetadata, valueResult);
      planResults.Add(aresPlanResult);
    }

    return planResults;
  }

  public PlanningParameter ConvertToPlanningParameter(ParameterMetadata metadata, IEnumerable<CompletedExperiment> experimentHistory)
  {
    var relevantInfo = experimentHistory.SelectMany(experiment => experiment.Parameters.Where(param => param.PlanningMetadata.Name == metadata.Name));
    var parameter = new PlanningParameter
    {
      ParameterName = metadata.Name,
      IsPlanned = true,
      DataType = metadata.GetType().ToString()
    };
    parameter.ParameterHistory.AddRange(relevantInfo.Select(param => double.Parse(param.Value.Value.StringValue)));

    if(metadata.Constraints.Any())
    {
      var constraint = metadata.Constraints.First();
      parameter.MinimumValue = constraint.Minimum;
      parameter.MaximumValue = constraint.Maximum;
    }

    parameter.PlannerName = metadata.PlannerName;
    return parameter;
  }

  public async Task Init()
  {
    ClientStore.CreateClient(_address);
    var client = ClientStore.AresPlanningClient;
    Capabilities? response = null;

    try
    {
      response = await client.RequestCapabilitiesAsync(new Empty());
    }

    catch(Exception)
    {
      Status.PlannerState = PlannerState.Error;
      Status.Message = "Failed to establish a connection with the planner!";
      return;
    }

    if(response is null)
    {
      Status.PlannerState = PlannerState.Error;
      Status.Message = "Planner returned a null capability response!";
      return;
    }

    AvailablePlanners.Clear();
    AdapterSettings.Clear();

    AvailablePlanners.AddRange(response.AvailablePlanners);
    AdapterSettings.AddRange(response.AdapterSettings);

    Timeout = TimeSpan.FromSeconds(response.TimeoutSeconds);
    await Task.Delay(TimeSpan.FromSeconds(0.5));
    Status.PlannerState = PlannerState.Active;
    Status.Message = $"Successfully activated {Name}!";
  }

  public string Name { get; set; }
  public Version Version { get; set; } = new Version(1, 0);
  public PlannerStatus Status { get; protected set; }
  public IList<Planner> AvailablePlanners { get; } = new List<Planner>();
  public IList<PlannerSetting> AdapterSettings { get; } = new List<PlannerSetting>();
  public string Address { get; set; }
  public string UniqueId { get; set; }
  public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
