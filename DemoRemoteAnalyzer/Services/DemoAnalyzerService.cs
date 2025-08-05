using Ares.Messaging.Analyzing;
using Ares.Messaging.Analyzing.Remote;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DemoRemoteAnalyzer.Services;
public class DemoAnalyzerService : AresRemoteAnalyzerService.AresRemoteAnalyzerServiceBase
{
  public DemoAnalyzerService()
  {
  }

  public override Task<AnalyzerStateResponse> GetState(Empty request, ServerCallContext context)
  {
    Console.WriteLine("State requested");
    return Task.FromResult(new AnalyzerStateResponse { State = AnalyzerState.Active });
  }

  public override Task<Analysis> Analyze(AnalysisRequest request, ServerCallContext context)
  {
    Console.WriteLine("Analysis requested");
    var numInput = request.Inputs.Fields[DemoDataTypes.InputNumber.Key];
    var input = numInput.NumberValue;
    Console.WriteLine($"Analysis input: {input}");
    var analysis = new Analysis
    {
      Result = (float)input,
      Success = true
    };

    var numOperand = request.Inputs.Fields.GetValueOrDefault(DemoDataTypes.Operand.Key);
    if(numOperand is null)
    {
      Console.WriteLine("No operand specified, returning base number");
      return Task.FromResult(analysis);
    }
    var operand = numOperand.NumberValue;

    var operation = request.Settings.Fields[DemoDataTypes.Operation.Key];
    var operationValue = operation.StringValue;

    analysis.Result = operationValue switch
    {
      "Multiply" => (float)(analysis.Result * operand),
      "Divide" => (float)(analysis.Result / operand),
      _ => 0,// you broke it :(
    };

    return Task.FromResult(analysis);
  }

  public override Task<AnalysisParametersResponse> GetAnalysisParameters(Empty request, ServerCallContext context)
  {
    Console.WriteLine("Analysis parameters requested");
    var response = new AnalysisParametersResponse
    {
      ParameterSchema = new Ares.Messaging.AresDataSchema()
    };

    response.ParameterSchema.Fields[DemoDataTypes.InputNumber.Key] = DemoDataTypes.InputNumber.Value;
    response.ParameterSchema.Fields[DemoDataTypes.Operand.Key] = DemoDataTypes.Operand.Value;

    return Task.FromResult(response);
  }

  public override Task<AnalyzerCapabilities> GetAnalyzerCapabilities(Empty request, ServerCallContext context)
  {
    Console.WriteLine("Capabilities requested");
    var capabilities = new AnalyzerCapabilities
    {
      SettingsSchema = new Ares.Messaging.AresDataSchema()
    };
    capabilities.SettingsSchema.Fields[DemoDataTypes.Operation.Key] = DemoDataTypes.Operation.Value;
    capabilities.SettingsSchema.Fields[DemoDataTypes.RandomTags.Key] = DemoDataTypes.RandomTags.Value;
    capabilities.SettingsSchema.Fields[DemoDataTypes.PreselectedTags.Key] = DemoDataTypes.PreselectedTags.Value;


    return Task.FromResult(capabilities);
  }

  public override Task<ConnectionStatusResponse> GetConnectionStatus(Empty request, ServerCallContext context)
  {
    var response = new ConnectionStatusResponse { Status = ConnectionStatus.Connected };

    return Task.FromResult(response);
  }

  public override Task<InfoResponse> GetInfo(Empty request, ServerCallContext context)
  {
    Console.WriteLine("Info requested");
    var infoResponse = new InfoResponse
    {
      Description = "Give me a number and I'll give it back or multiply it by the multiplier parameter :)",
      Name = "DemoAnalyzer",
      Version = "1.0.1"
    };

    return Task.FromResult(infoResponse);
  }

  public override Task<ParameterValidationResult> ValidateInputs(ParameterValidationRequest request, ServerCallContext context)
  {
    Console.WriteLine("Validating inputs");
    if(request.InputSchema.Fields.ContainsKey(DemoDataTypes.InputNumber.Key))
    {
      Console.WriteLine($"Validation found data key {DemoDataTypes.InputNumber.Key}");
    }
    else
    {
      Console.WriteLine($"Did not found data with a key of {DemoDataTypes.InputNumber.Key}.");
      Console.WriteLine("Found following items:");
      foreach(var schemaItem in request.InputSchema.Fields)
      {
        Console.WriteLine($"{schemaItem.Key}:{schemaItem.Value}");
      }
    }

    if(request.InputSchema.Fields.ContainsKey(DemoDataTypes.Operand.Key))
    {
      Console.WriteLine($"Validation found data key {DemoDataTypes.Operand.Key}");
    }
    else
    {
      Console.WriteLine($"Did not found data with a key of {DemoDataTypes.Operand.Key}. But it was optional, so doesn't matter :)");
    }

    // the base input validator will take care of checking for required params so no need to do that manually unless
    // you need some specific validation logic
    return base.ValidateInputs(request, context);
  }
}
