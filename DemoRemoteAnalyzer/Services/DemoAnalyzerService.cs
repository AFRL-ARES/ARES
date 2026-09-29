using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using DemoRemoteAnalyzer.Models;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DemoRemoteAnalyzer.Services;
public class DemoAnalyzerService : AresRemoteAnalyzerService.AresRemoteAnalyzerServiceBase
{
  private readonly DemoResponseSurfaceAnalyzer _demoResponseSurfaceAnalyzer;
  private readonly Config _demoAnalyzerConfig;

  public DemoAnalyzerService()
  {
    _demoAnalyzerConfig = new Config() { Objectives = new List<ObjectiveSchema>()
    {
      new ObjectiveSchema() { ObjectiveName = "Yield" }
    }};

    _demoResponseSurfaceAnalyzer = new DemoResponseSurfaceAnalyzer() { Cfg = _demoAnalyzerConfig };
  }

  public override Task<AnalysisObjectivesResponse> GetAnalysisObjectives(Empty request, ServerCallContext context)
  {
    var objectivesResponse = new AnalysisObjectivesResponse();
    var valueSchema = new AresValueSchema() 
    { 
      Type = AresDataType.Float,
      Description = "The calculated yield of the experiment."
    };

    objectivesResponse.ParameterSchema.Fields.Add("Yield", valueSchema);

    return Task.FromResult(objectivesResponse);
  }

  public override Task<StateResponse> GetState(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new StateResponse { State = State.Active, StateMessage = "The Demo Analyzer is Active!" });
  }

  public override Task<AnalysisResponse> Analyze(AnalysisRequest request, ServerCallContext context)
  {
    Console.WriteLine("[Demo Analyzer] - Analysis Requested");

    var temperatureInput = request.Inputs.Fields[DemoDataTypes.Temperature.Key];
    var temperatureFound = temperatureInput.TryGetNumericValue(out var numericTemperatureValue);
    if(temperatureFound)
      Console.WriteLine($"[Demo Analyzer] - Temperature Input: {numericTemperatureValue}");

    var flowRateInput = request.Inputs.Fields[DemoDataTypes.FlowRate.Key];
    var flowRateFound = flowRateInput.TryGetNumericValue(out var flowRateValue);
    if(flowRateFound)
      Console.WriteLine($"[Demo Analyzer] - Flow Rate Input: {flowRateValue}");

    var analysisResponse = _demoResponseSurfaceAnalyzer.DemoResponse(request);

    if(analysisResponse != null)
      return Task.FromResult(analysisResponse);

    else
      return Task.FromResult(new AnalysisResponse 
      { 
        AnalysisOutcome = Outcome.Failure, 
        ErrorString = "Demo Response Analyzer failed to return a proper analysis response" 
      });
  }

  public override Task<AnalysisParametersResponse> GetAnalysisParameters(Empty request, ServerCallContext context)
  {
    var response = new AnalysisParametersResponse
    {
      ParameterSchema = new AresStructSchema
      {
        Fields =
        {
          [DemoDataTypes.Temperature.Key] = DemoDataTypes.Temperature.Value,
          [DemoDataTypes.FlowRate.Key] = DemoDataTypes.FlowRate.Value
        }
      }
    };

    return Task.FromResult(response);
  }

  public override Task<AnalyzerCapabilities> GetAnalyzerCapabilities(Empty request, ServerCallContext context)
  {

    var objectiveSchema = AresSchemaBuilder.Empty().AddEntry("Yield", new AresValueSchema { Type = AresDataType.Float, Optional = false, Description = "The yield of the reaction" }).Build();

    var capabilities = new AnalyzerCapabilities
    {
      SettingsSchema = new AresStructSchema
      {
        Fields =
        {
          [DemoSettings.TemperatureMax.Key] = DemoSettings.TemperatureMax.Value,
          [DemoSettings.TemperatureMin.Key] = DemoSettings.TemperatureMin.Value,
          [DemoSettings.FlowRateMax.Key] = DemoSettings.FlowRateMax.Value,
          [DemoSettings.FlowRateMin.Key] = DemoSettings.FlowRateMin.Value
        }
      },
      ObjectiveOutputSchema = objectiveSchema
    };


    return Task.FromResult(capabilities);
  }

  public override Task<ConnectionStatus> GetConnectionStatus(Empty request, ServerCallContext context)
  {
    var response = new ConnectionStatus { Status = AresStatus.Connected };

    return Task.FromResult(response);
  }

  public override Task<InfoResponse> GetInfo(Empty request, ServerCallContext context)
  {
    var infoResponse = new InfoResponse
    {
      Description = "Generates a synthetic process space for sampling with ARES OS",
      Name = "Demo Response Surface Analyzer",
      Version = "0.8.0"
    };

    return Task.FromResult(infoResponse);
  }

  public override Task<ParameterValidationResult> ValidateInputs(ParameterValidationRequest request, ServerCallContext context)
  {
    Console.WriteLine("[Demo Analyzer] - Validating inputs");
    if(request.InputSchema.Fields.ContainsKey(DemoDataTypes.Temperature.Key))
      Console.WriteLine($"[Demo Analyzer] - Validation found data key {DemoDataTypes.Temperature.Key}");
    
    else
    {
      Console.WriteLine($"[Demo Analyzer] - Could Not Find Data with a Key of: {DemoDataTypes.Temperature.Key}.");
      Console.WriteLine("[Demo Analyzer] - Found following items:");
      foreach(var schemaItem in request.InputSchema.Fields)
        Console.WriteLine($"{schemaItem.Key}:{schemaItem.Value}");
    }

    if(request.InputSchema.Fields.ContainsKey(DemoDataTypes.FlowRate.Key))
      Console.WriteLine($"[Demo Analyzer] - Validation Found Data Key: {DemoDataTypes.FlowRate.Key}");

    else
    {
      Console.WriteLine($"[Demo Analyzer]: Could Not Find Data with a Key of: {DemoDataTypes.FlowRate.Key}.");
      Console.WriteLine("[Demo Analyzer] - Found following items:");
      foreach(var schemaItem in request.InputSchema.Fields)
        Console.WriteLine($"{schemaItem.Key}:{schemaItem.Value}");
    }

    return base.ValidateInputs(request, context);
  }
}
