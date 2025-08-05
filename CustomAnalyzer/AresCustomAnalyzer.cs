using Ares.Core.Analyzing;
using Ares.Core.AresEnvironment;
using Ares.Messaging;
using Ares.Messaging.Analyzing;

namespace CustomAnalyzer;
public class AresCustomAnalyzer : AnalyzerBase
{
  readonly Uri _uri;

  public AresCustomAnalyzer(Uri uri) : base("Custom Analyzer", "CustomAnalyzer", "1.0.0")
  {
    _uri = uri;
  }

  public override Task<Analysis> Analyze(AresStruct inputs, AresStruct _settings, CancellationToken cancellationToken)
  {
    return Analyze(inputs, cancellationToken);
  }

  public override async Task<Analysis> Analyze(AresStruct inputs, CancellationToken cancellationToken)
  {
    var client = CustomAnalyzerClientStore.CustomAnalyzerPlanningClient;
    var defaultAnalysis = new Analysis
    {
      Result = -1
    };
    var previousExperiment = AresEnvironment.GetEnvironmentVariable(VariableType.PreviousExperimentPath);
    var startup = AresEnvironment.GetEnvironmentVariable(VariableType.CampaignStartupFolder);

    if(previousExperiment is null || startup is null)
    {
      defaultAnalysis.ErrorString = "Ares Environement Variables weren't correctly set!";
      return defaultAnalysis;
    }

    var image_path = Path.Combine(previousExperiment ?? string.Empty, "sample_image.tif");
    var baseline_path = Path.Combine(startup ?? string.Empty, "sample_image.tif");

    if(client is null)
    {
      defaultAnalysis.ErrorString = "Analyzer client was null!";
      return defaultAnalysis;
    }

    var analysisRequest = new AnalysisRequest { Wavelength = inputs.Fields["Wavelength"].NumberValue };
    analysisRequest.RamanValues.AddRange(inputs.Fields["DeviceData"].NumberArrayValue.Numbers.Select(val => (int)val));
    analysisRequest.RamanShift.AddRange(inputs.Fields["RamanShift"].NumberArrayValue.Numbers.SkipLast(1));
    analysisRequest.ResultsPath = inputs.Fields["ResultOutputPath"].StringValue;
    analysisRequest.ImagePath = image_path;
    analysisRequest.RefImagePath = baseline_path;
    analysisRequest.ExperimentUuid = inputs.Fields["ExperimentId"].StringValue;

    AnalysisResponse? response;
    try
    {
      response = await client.AnalyzeAsync(analysisRequest, deadline: DateTime.UtcNow.AddSeconds(5));

      return new Analysis
      {
        Result = Convert.ToSingle(response.Value)
      };
    }

    catch(Exception ex)
    {
      defaultAnalysis.ErrorString = ex.Message;
      return defaultAnalysis;
    }
  }

  public override Task<AnalyzerCapabilities> GetCapabilities(CancellationToken cancellationToken)
  {
    return Task.FromResult(new AnalyzerCapabilities());
  }

  public override Task<AresDataSchema> GetParameters(CancellationToken cancellationToken)
  {
    var schema = new AresDataSchema();

    var deviceDataSchema = new SchemaEntry { Optional = false, Type = AresDataType.NumberArray };
    var ramanShiftSchema = new SchemaEntry { Optional = false, Type = AresDataType.NumberArray };
    var wavelengthSchema = new SchemaEntry { Optional = false, Type = AresDataType.Number };
    var resultOutputPathSchema = new SchemaEntry { Optional = false, Type = AresDataType.String };
    var experimentIdSchema = new SchemaEntry { Optional = false, Type = AresDataType.String };

    schema.Fields["DeviceData"] = deviceDataSchema;
    schema.Fields["RamanShift"] = ramanShiftSchema;
    schema.Fields["Wavelength"] = wavelengthSchema;
    schema.Fields["ResultOutputPath"] = resultOutputPathSchema;
    schema.Fields["ExperimentId"] = experimentIdSchema;

    return Task.FromResult(schema);
  }

  public override Task Init()
  {
    CustomAnalyzerClientStore.CreateClient(_uri);

    return base.Init();
  }
}
