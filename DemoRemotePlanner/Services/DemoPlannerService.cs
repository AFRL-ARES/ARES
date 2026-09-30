using Ares.Datamodel;
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

    var inputs = request.PlanningParameters.ToList();
    var analysisData = request.AnalysisData.ToList();

    Console.WriteLine($"Received a total of {inputs.Count} parameters to plan for.");

    var response = new PlanningResponse();
    var newPlan = new Plan();

    var hillClimbingParameters = inputs.Where(parameter => parameter.PlannerName == "Hill Climbing Planner").ToList();

    if(hillClimbingParameters.Count > 0)
    {
      var hillClimbingPlan = await JointHillClimbingPlanner(hillClimbingParameters, analysisData);
      newPlan.PlannedParameters.AddRange(hillClimbingPlan);
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

  private async Task<List<PlannedParameter>> JointHillClimbingPlanner(IReadOnlyList<PlanningParameter> parameters, List<AnalysisData> analysisData)
  {
    const double executionResolution = 1.0;
    const double objectiveTolerance = 1e-9;

    if(parameters.Count == 0)
      return [];

    ValidateJointParameterBounds(parameters);

    var samples = ExtractJointSamples(parameters, analysisData);

    if(samples.Count == 0)
      return await CreateInitialJointPlan(parameters);

    var latest = samples[^1];
    var best = samples.MaxBy(sample => sample.Objective);
    Dictionary<string, double> proposedPoint;

    if(samples.Count == 1)
      proposedPoint = CreateInitialStep(parameters, latest.Values, executionResolution);
    
    else
    {
      var previous = samples[^2];
      var objectiveImproved = latest.Objective > previous.Objective + objectiveTolerance;

      proposedPoint = CreateHillClimbStep(
          parameters,
          previous.Values,
          latest.Values,
          objectiveImproved,
          executionResolution);
    }

    if(HasEvaluatedJointPoint(proposedPoint, samples, executionResolution / 2.0))
      proposedPoint = FindUnseenJointCandidate(parameters, best.Values, samples, executionResolution);

    return CreatePlannedParameters(parameters, proposedPoint);
  }

  private List<JointSample> ExtractJointSamples(IReadOnlyList<PlanningParameter> parameters, IReadOnlyList<AnalysisData> analysisData)
  {
    var sampleCount = analysisData.Count;

    foreach(var parameter in parameters)
      sampleCount = Math.Min(sampleCount, parameter.ParameterHistory.Count);

    var samples = new List<JointSample>();

    for(var index = 0; index < sampleCount; index++)
    {
      var objectives = analysisData[index].AnalysisObjectives;

      if(objectives is null || objectives.Count == 0)
        continue;

      var objectiveValue = objectives.First().ObjectiveValue;

      if(objectiveValue is null || 
        !AresValueHelper.IsNumericType(objectiveValue) || 
        !objectiveValue.TryGetNumericValue(out var objective) || 
        !double.IsFinite(objective))
        continue;

      var values = new Dictionary<string, double>();
      var validPoint = true;

      foreach(var parameter in parameters)
      {
        var plannedValue = parameter.ParameterHistory[index].PlannedValue;

        if(plannedValue is null ||
           !AresValueHelper.IsNumericType(plannedValue) ||
           !plannedValue.TryGetNumericValue(out var parameterValue) ||
           !double.IsFinite(parameterValue))
        {
          validPoint = false;
          break;
        }

        values[parameter.ParameterName] = parameterValue;
      }

      if(validPoint)
        samples.Add(new JointSample(values, objective));
    }

    return samples;
  }

  private async Task<List<PlannedParameter>> CreateInitialJointPlan(IReadOnlyList<PlanningParameter> parameters)
  {
    var plannedParameters = new List<PlannedParameter>();

    foreach(var parameter in parameters)
    {
      if(parameter.InitialValue is not null && parameter.InitialValue.TryGetNumericValue(out var initialValue) && double.IsFinite(initialValue))
      {
        var clampedValue = Math.Clamp(initialValue, parameter.MinimumValue, parameter.MaximumValue);

        plannedParameters.Add(new PlannedParameter
        {
          ParameterName = parameter.ParameterName,
          ParameterValue = AresValueHelper.CreateNumber((float)clampedValue)
        });

        continue;
      }

      plannedParameters.Add(await RandomPlanner(parameter));
    }

    return plannedParameters;
  }

  private static Dictionary<string, double> CreateInitialStep(
      IReadOnlyList<PlanningParameter> parameters,
      IReadOnlyDictionary<string, double> latestPoint,
      double resolution)
  {
    var proposedPoint = new Dictionary<string, double>();

    foreach(var parameter in parameters)
    {
      var range = parameter.MaximumValue - parameter.MinimumValue;
      var minimumStep = Math.Max(resolution, range / 100.0);
      var initialStep = Math.Max(range / 10.0, minimumStep);
      var latestValue = latestPoint[parameter.ParameterName];
      var direction = latestValue >= parameter.MaximumValue - resolution ? -1.0 : 1.0;
      var proposedValue = latestValue + direction * initialStep;

      proposedPoint[parameter.ParameterName] = SnapToResolution(Math.Clamp(proposedValue, parameter.MinimumValue, parameter.MaximumValue), resolution);
    }

    return proposedPoint;
  }

  private static Dictionary<string, double> CreateHillClimbStep(
      IReadOnlyList<PlanningParameter> parameters,
      IReadOnlyDictionary<string, double> previousPoint,
      IReadOnlyDictionary<string, double> latestPoint,
      bool objectiveImproved,
      double resolution)
  {
    var proposedPoint = new Dictionary<string, double>();

    foreach(var parameter in parameters)
    {
      var parameterName = parameter.ParameterName;
      var range = parameter.MaximumValue - parameter.MinimumValue;
      var minimumStep = Math.Max(resolution, range / 100.0);

      var movement = latestPoint[parameterName] - previousPoint[parameterName];

      var direction = Math.Sign(movement);
      var stepSize = Math.Abs(movement);

      if(stepSize < minimumStep)
      {
        stepSize = Math.Max(range / 10.0, minimumStep);
        direction = latestPoint[parameterName] >= parameter.MaximumValue - minimumStep ? -1 : 1;
      }

      if(!objectiveImproved)
      {
        direction = -direction;
        stepSize *= 0.5;
      }

      stepSize = Math.Max(stepSize, minimumStep);
      var proposedValue = latestPoint[parameterName] + direction * stepSize;
      proposedPoint[parameterName] = SnapToResolution(Math.Clamp(proposedValue, parameter.MinimumValue, parameter.MaximumValue), resolution);
    }

    return proposedPoint;
  }

  private static bool HasEvaluatedJointPoint(IReadOnlyDictionary<string, double> candidate, IEnumerable<JointSample> samples, double tolerance)
    => samples.Any(sample =>
        candidate.All(pair =>
            sample.Values.TryGetValue(pair.Key, out var sampleValue) && Math.Abs(sampleValue - pair.Value) <= tolerance));

  private Dictionary<string, double> FindUnseenJointCandidate(
      IReadOnlyList<PlanningParameter> parameters,
      IReadOnlyDictionary<string, double> bestPoint,
      IReadOnlyList<JointSample> samples,
      double resolution)
  {
    for(var attempt = 0; attempt < 16; attempt++)
    {
      var candidate = new Dictionary<string, double>();

      foreach(var parameter in parameters)
      {
        var range = parameter.MaximumValue - parameter.MinimumValue;
        var stepSize = Math.Max(range / 10.0, resolution);

        var direction = _random.Next(0, 2) == 0 ? -1.0 : 1.0;

        var value = bestPoint[parameter.ParameterName] + direction * stepSize;

        candidate[parameter.ParameterName] = SnapToResolution(Math.Clamp(value, parameter.MinimumValue, parameter.MaximumValue), resolution);
      }

      if(!HasEvaluatedJointPoint(candidate, samples, resolution / 2.0))
        return candidate;
    }

    for(var attempt = 0; attempt < 100; attempt++)
    {
      var candidate = new Dictionary<string, double>();

      foreach(var parameter in parameters)
      {
        var value = parameter.MinimumValue + _random.NextDouble() * (parameter.MaximumValue - parameter.MinimumValue);
        candidate[parameter.ParameterName] = SnapToResolution(value, resolution);
      }

      if(!HasEvaluatedJointPoint(candidate, samples, resolution / 2.0))
        return candidate;
    }

    // The grid is probably exhausted. Return the best known complete point.
    return new Dictionary<string, double>(bestPoint);
  }

  private static List<PlannedParameter> CreatePlannedParameters(IReadOnlyList<PlanningParameter> parameters, IReadOnlyDictionary<string, double> point)
    => parameters.Select(parameter => new PlannedParameter
    {
      ParameterName = parameter.ParameterName,
      ParameterValue = AresValueHelper.CreateNumber((float)point[parameter.ParameterName])
    }).ToList();
  
  private static void ValidateJointParameterBounds(IEnumerable<PlanningParameter> parameters)
  {
    foreach(var parameter in parameters)
    {
      if(parameter.MaximumValue < parameter.MinimumValue)
        throw new ArgumentException($"MaximumValue must be greater than or equal to MinimumValue for '{parameter.ParameterName}'.");
    }
  }

  private sealed record JointSample(Dictionary<string, double> Values, double Objective);

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

  private static double SnapToResolution(double value, double resolution)
    => Math.Round(value / resolution, MidpointRounding.AwayFromZero) * resolution;
}
