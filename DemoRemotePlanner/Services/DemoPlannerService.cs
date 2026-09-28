using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Planning.Remote;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DemoRemotePlanner.Services;
public class DemoPlannerService : AresRemotePlannerService.AresRemotePlannerServiceBase
{
  private readonly Random _random;
  private readonly Planner _hillClimbingPlanner;

  public DemoPlannerService()
  {
    _random = new Random();

    _hillClimbingPlanner = new Planner()
    {
      PlannerName = "Hill Climbing Planner",
      Description = "A planner that uses a simple hill-climbing step based on previous parameter history.",
      Version = "1.0.0"
    };
  }

  public override async Task<PlanningResponse> Plan(PlanningRequest request, ServerCallContext context)
  {
    Console.WriteLine("Planning Requested!");
    var inputs = request.PlanningParameters;
    Console.WriteLine($"Received a total of {inputs.Count} parameters to plan for.");
    var response = new PlanningResponse();
    var newPlan = new Plan();
    var objectives = request.AnalysisData.ToList();


    foreach(var parameter in inputs)
    {
      switch(parameter.PlannerName)
      {
        case "Hill Climbing Planner":
        {
          var hillClimbingParam = await HillClimbingPlanner(parameter, objectives);
          newPlan.PlannedParameters.Add(hillClimbingParam);
          break;
        }
        default:
        {
          Console.WriteLine("Unrecognized Planned Requested! Defaulting to random planner...");
          var plannedParam = await RandomPlanner(parameter);
          newPlan.PlannedParameters.Add(plannedParam);
          break;
        }
      }
    }

    newPlan.PlanningOutcome = Outcome.Success;
    response.Plans.Add(newPlan);
    return response;
  }

  public override Task<ConnectionStatus> GetConnectionStatus(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new ConnectionStatus
    {
      Status = AresStatus.Connected,
      Info = "Demo Planner Service is connected!"
    });
  }

  public override Task<InfoResponse> GetInfo(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new InfoResponse
    {
      Name = "Demo Planner Service",
      Description = "A demo of the ARES remote planenr service capabilities",
      Version = "1.0.0"
    });
  }

  public override Task<StateResponse> GetState(Empty request, ServerCallContext context)
    => Task.FromResult(new StateResponse { State = State.Active, StateMessage = "The Demo Planner Service is active!" });

  public override Task<PlannerServiceCapabilities> GetPlannerServiceCapabilities(Empty request, ServerCallContext context)
  {
    var capabilitesResponse = new PlannerServiceCapabilities();

    capabilitesResponse.AvailablePlanners.Add(_hillClimbingPlanner);
    capabilitesResponse.ServiceName = "Demo Planner Service";
    capabilitesResponse.TimeoutSeconds = 30;
    capabilitesResponse.AcceptedTypes.Add(AresDataType.Number);
    capabilitesResponse.AcceptedTypes.Add(AresDataType.Float);
    capabilitesResponse.AcceptedTypes.Add(AresDataType.Int);
    capabilitesResponse.MultiObjectiveCapable = true;

    capabilitesResponse.SettingsSchema = new AresStructSchema
    {
      Fields =
      {
        ["Dual Randomization"] = AresSchemaBuilder.Entry(AresDataType.Boolean).AsOptional().Build()
      }
    };

    return Task.FromResult(capabilitesResponse);
  }

  public Task<PlannedParameter> RandomPlanner(PlanningParameter aresParameter)
  {
    var randomDouble = _random.NextDouble();
    var plannedParam = new PlannedParameter();
    plannedParam.ParameterName = aresParameter.ParameterName;
    var randomizedValue = (float)(aresParameter.MinimumValue + (randomDouble * (aresParameter.MaximumValue - aresParameter.MinimumValue)));
    var roundedValue = Math.Round(randomizedValue, 2);
    plannedParam.ParameterValue = AresValueHelper.CreateFloat(roundedValue);
    return Task.FromResult(plannedParam);
  }

  public Task<PlannedParameter> GradualPlanner(PlanningParameter aresParameter)
  {
    var response = new PlannedParameter();
    response.ParameterName = aresParameter.ParameterName;

    if(aresParameter.ParameterHistory.Count == 0)
      response.ParameterValue = AresValueHelper.CreateNumber((float)aresParameter.MinimumValue);

    else
    {
      var previousValue = aresParameter.ParameterHistory.Last().PlannedValue;
      double incrementedValue;

      if(previousValue.HasNumberValue)
        incrementedValue = previousValue.NumberValue + 5;

      else
      {
        Console.WriteLine("The previous assigned value didn't exist, setting newest value to minimum allowed value.");
        incrementedValue = aresParameter.MinimumValue;
      }

      if(incrementedValue > aresParameter.MaximumValue)
        response.ParameterValue = AresValueHelper.CreateNumber((float)aresParameter.MaximumValue);

      else
        response.ParameterValue = AresValueHelper.CreateNumber((float)incrementedValue);
    }

    return Task.FromResult(response);
  }

  public async Task<PlannedParameter> HillClimbingPlanner(PlanningParameter aresParameter, List<AnalysisData> analysisData)
  {
    var response = new PlannedParameter
    {
      ParameterName = aresParameter.ParameterName
    };

    var minimum = aresParameter.MinimumValue;
    var maximum = aresParameter.MaximumValue;

    if(maximum < minimum)
      throw new ArgumentException(
        "MaximumValue must be greater than or equal to MinimumValue.");

    var range = maximum - minimum;

    if(Math.Abs(range) < 1e-12)
    {
      response.ParameterValue =
        AresValueHelper.CreateNumber((float)minimum);

      return response;
    }

    /*
     * Pair parameter history and analysis data before filtering.
     *
     * This assumes ParameterHistory[i] corresponds to analysisData[i].
     * Matching them by a shared run/iteration ID would be safer if one exists.
     */
    var sampleCount = Math.Min(
      aresParameter.ParameterHistory.Count,
      analysisData.Count);

    var samples = new List<(double Parameter, double Objective)>();

    for(var i = 0; i < sampleCount; i++)
    {
      var parameterHistory = aresParameter.ParameterHistory[i];
      var analysis = analysisData[i];

      if(parameterHistory.PlannedValue is null ||
          !AresValueHelper.IsNumericType(parameterHistory.PlannedValue))
      {
        continue;
      }

      if(analysis.AnalysisObjectives is null ||
          analysis.AnalysisObjectives.Count == 0)
      {
        continue;
      }

      var objectiveValue = analysis.AnalysisObjectives.First().ObjectiveValue;

      if(objectiveValue is null ||
          !AresValueHelper.IsNumericType(objectiveValue))
      {
        continue;
      }

      if(!parameterHistory.PlannedValue.TryGetNumericValue(
            out var parameter) ||
          !objectiveValue.TryGetNumericValue(out var objective))
      {
        continue;
      }

      if(double.IsNaN(parameter) ||
          double.IsInfinity(parameter) ||
          double.IsNaN(objective) ||
          double.IsInfinity(objective))
      {
        continue;
      }

      samples.Add((parameter, objective));
    }

    /*
     * No completed numeric samples yet: use the configured initial value,
     * or fall back to the random planner.
     */
    if(samples.Count == 0)
    {
      if(aresParameter.InitialValue is not null &&
          aresParameter.InitialValue.HasNumberValue)
      {
        var initialValue = Math.Clamp(
          aresParameter.InitialValue.NumberValue,
          minimum,
          maximum);

        response.ParameterValue =
          AresValueHelper.CreateNumber((float)initialValue);
      }
      else
      {
        var randomParameter = await RandomPlanner(aresParameter);
        response.ParameterValue = randomParameter.ParameterValue;
      }

      return response;
    }

    /*
     * One sample is not enough to establish whether a direction improves the
     * objective. Take an initial exploratory step.
     */
    double direction = 0;

    if(samples.Count == 1)
    {
      var currentValue = samples[0].Parameter;
      var initialStep = Math.Abs(range) / 10.0;

      // Move toward the center if the first value is already at a boundary.
      direction = currentValue >= maximum - 1e-6 ? -1.0 : 1.0;
      var proposedValue = currentValue + direction * initialStep;

      proposedValue = Math.Clamp(proposedValue, minimum, maximum);

      response.ParameterValue =
        AresValueHelper.CreateNumber((float)proposedValue);

      return response;
    }

    var previous = samples[^2];
    var latest = samples[^1];

    var lastMovement = latest.Parameter - previous.Parameter;
    var stepSize = Math.Abs(lastMovement);
    direction = Math.Sign(lastMovement);

    const double parameterTolerance = 1e-6;
    const double objectiveTolerance = 1e-9;

    /*
     * A zero movement can occur after clamping or from duplicate history
     * entries. Select a fresh step and ensure it points inward at a boundary.
     */
    if(stepSize < parameterTolerance)
    {
      stepSize = Math.Abs(range) / 10.0;

      if(latest.Parameter >= maximum - parameterTolerance)
        direction = -1;
      else if(latest.Parameter <= minimum + parameterTolerance)
        direction = 1;
      else
        direction = 1;
    }

    /*
     * This assumes a larger objective is better.
     *
     * If the objective improved, continue in the direction of the last move.
     * Otherwise reverse direction and reduce the step size.
     */
    var objectiveImproved =
      latest.Objective > previous.Objective + objectiveTolerance;

    if(!objectiveImproved)
    {
      direction = -direction;
      stepSize *= 0.5;
    }

    var proposed = latest.Parameter + direction * stepSize;

    /*
     * Do not simply clamp an out-of-range proposal. Clamping can repeatedly
     * produce the same boundary value. Instead, reflect inward and reduce the
     * step.
     */
    if(proposed > maximum || proposed < minimum)
    {
      direction = -direction;
      stepSize *= 0.5;
      proposed = latest.Parameter + direction * stepSize;
    }

    proposed = Math.Clamp(proposed, minimum, maximum);

    response.ParameterValue =
      AresValueHelper.CreateNumber((float)proposed);

    return response;
  }
}
