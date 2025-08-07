using Ares.Datamodel.Planning;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DemoRemotePlanner.Services;
public class DemoPlannerService : AresPlannerGrpc.AresPlannerGrpcBase
{
  private readonly Random _random;

  public DemoPlannerService()
  {
    _random = new Random();
  }

  public override async Task<PlanResponse> Plan(PlanRequest request, ServerCallContext context)
  {
    Console.WriteLine("Planning Requested!");
    var inputs = request.PlanningParameters;
    Console.WriteLine($"Received a total of {inputs.Count} parameters to plan for.");
    var response = new PlanResponse();

    foreach(var parameter in inputs)
    {
      switch (parameter.PlannerName)
      {
        case "Random Planner":
        {
          var plannedParam = await RandomPlanner(parameter);
          response.ParameterNames.Add(plannedParam.ParameterName);
          response.ParameterValues.Add(plannedParam.ParameterValue);
          break;
        }
        case "Gradual Planner":
        {
          var gradualPlannedParam = await GradualPlanner(parameter);
          response.ParameterNames.Add(gradualPlannedParam.ParameterName);
          response.ParameterValues.Add(gradualPlannedParam.ParameterValue);
          break;
        }
        default:
        {
          Console.WriteLine("Unrecognized Planned Requested! Defaulting to random planner...");
          var plannedParam = await RandomPlanner(parameter);
          response.ParameterNames.Add(plannedParam.ParameterName);
          response.ParameterValues.Add(plannedParam.ParameterValue);
          break;
        }
      }
    }

    return response;
  }

  public override Task<Capabilities> RequestCapabilities(Empty request, ServerCallContext context)
  {
    var capabilitesResponse = new Capabilities();

    var randomPlanner = new Planner()
    {
      PlannerName = "Random Planner",
      Description = "A planner that returns random temperatures for the demo device.",
      UniqueId = Guid.NewGuid().ToString(),
      Version = "1.0.0"
    };

    var gradualIncreasePlanner = new Planner()
    {
      PlannerName = "Gradual Planner",
      Description = "A planner that returns a temperature value that gradually increases from the previously provided value by 5 degrees.",
      UniqueId = Guid.NewGuid().ToString(),
      Version = "1.0.0"
    };

    var randomSetting = new PlannerSetting() 
    { 
      SettingName = "Dual Randomization", 
      SettingValue = new SettingValue 
      { 
        BoolValue = false 
      },  
      Optional = true 
    };

    capabilitesResponse.AvailablePlanners.Add(randomPlanner);
    capabilitesResponse.AvailablePlanners.Add(gradualIncreasePlanner);
    capabilitesResponse.AdapterSettings.Add(randomSetting);
    capabilitesResponse.ServiceName = "Demo Planner Service";
    capabilitesResponse.TimeoutSeconds = 30;

    return Task.FromResult(capabilitesResponse);
  }

  public Task<PlannedParameter> RandomPlanner(PlanningParameter aresParameter) 
  {
    var randomDouble = _random.NextDouble();
    var plannedParam = new PlannedParameter();
    plannedParam.ParameterName = aresParameter.ParameterName;
    plannedParam.ParameterValue = (float)(aresParameter.MinimumValue + (randomDouble * (aresParameter.MaximumValue - aresParameter.MinimumValue)));
    return Task.FromResult(plannedParam);
  }

  public Task<PlannedParameter> GradualPlanner(PlanningParameter aresParameter)
  {
    var response = new PlannedParameter();
    response.ParameterName = aresParameter.ParameterName;

    if(aresParameter.ParameterHistory.Count == 0)
      response.ParameterValue = (float)aresParameter.MinimumValue;

    else
    {
      var previousValue = aresParameter.ParameterHistory.Last();
      var incrementedValue = previousValue + 5;

      if(incrementedValue > aresParameter.MaximumValue)
        response.ParameterValue = (float)aresParameter.MaximumValue;

      else
        response.ParameterValue = (float)incrementedValue;
    }

    return Task.FromResult(response);
  }
}
