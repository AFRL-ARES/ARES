using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Templates;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using NuGet.Packaging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Features.CampaignEdit.ViewModels;

public partial class AnalyzerDesignerViewModel : ReactiveObject
{
  private readonly AnalysisService _analysisService;
  private readonly ExperimentTemplate _experimentTemplate;
  readonly IEnumerable<CommandDesignerViewModel> _commandDesignerViewModels;
  readonly IEnumerable<CommandDesignerViewModel> _startupCommandDesignerViewModels;
  readonly AnalyzerService _analyzerManagementClient;
  private string? _analyzerId = null;

  public AnalyzerDesignerViewModel(AnalysisService analysisService,
    AnalyzerService analyzerManagementClient, 
    ExperimentTemplate experimentTemplate, 
    IEnumerable<CommandDesignerViewModel> commandDesignerViewModels,
    IEnumerable<CommandDesignerViewModel> startupCommandDesignerViewModels)
  {
    _analyzerManagementClient = analyzerManagementClient;
    _commandDesignerViewModels = commandDesignerViewModels;
    _startupCommandDesignerViewModels = startupCommandDesignerViewModels;
    _analysisService = analysisService;
    _experimentTemplate = experimentTemplate;

    AnalyzerId = string.IsNullOrEmpty(_experimentTemplate.AnalyzerId) ? "NONE-ANALYZER" : _experimentTemplate.AnalyzerId;
    OutputInputMappings = [];
  }

  [Reactive]
  public partial ExperimentOutputAnalyzerInputMapping[] OutputInputMappings { get; private set; }

  public async Task UpdateMappings()
  {
    if(AnalyzerId == default)
    {
      OutputInputMappings = [];
      return;
    }

    var parameters = await _analysisService.GetAnalyzerParameters(
      new AnalyzerParametersRequest { AnalyzerId = AnalyzerId }, null);

    var inputMappings = parameters.AnalysisSchema.Fields.Select(field => new ExperimentOutputAnalyzerInputMapping(field.Key, field.Value.Type, !field.Value.Optional)).ToArray();

    CalculateAppropriateOutputs(inputMappings);

    OutputInputMappings = inputMappings;

    foreach(var analyzerMapping in _experimentTemplate.AnalyzerMaps)
    {
      var inputMapping = OutputInputMappings.FirstOrDefault(mapping => mapping.AnalyzerInputKey == analyzerMapping.Key);
      if(inputMapping is null)
      {
        continue;
      }

      inputMapping.ExperimentOutput = analyzerMapping.Value;
    }
  }

  public async Task UpdateAvailableAnalyzers()
  {
    var analyzers = await _analyzerManagementClient.GetAllAnalyzers(new Empty(), null);
    AvailableAnalyzers = analyzers.Analyzers.ToList();
  }

  public async Task CheckAnalyzer()
  {
    if(string.IsNullOrEmpty(AnalyzerId))
      return;

    if(AvailableAnalyzers is null)
      await UpdateAvailableAnalyzers();
    
    var request = new AnalyzerInfoRequest() { AnalyzerId = AnalyzerId };
    Analyzer = (await _analyzerManagementClient.GetInfo(request, null)).Info;
  }

  private void CalculateAppropriateOutputs(IEnumerable<ExperimentOutputAnalyzerInputMapping> outputInputMappings)
  {
    foreach(var outputInputMap in outputInputMappings)
    {
      var outputs = _commandDesignerViewModels.SelectMany(cdv => cdv.OutputKeyMap)
        .Where(okm => okm.DeviceOutputType == outputInputMap.InputType)
        .Select(okm => okm.CustomName)
        .ToArray();

      var startupOutputs = _startupCommandDesignerViewModels.SelectMany(cdv => cdv.OutputKeyMap)
        .Where(okm => okm.DeviceOutputType == outputInputMap.InputType)
        .Select(okm => okm.CustomName)
        .ToArray();

      var combinedOutputs = outputs.Concat(startupOutputs);

      outputInputMap.MatchingOutputs = combinedOutputs.ToArray();
    }
  }

  public void Save()
  {
    var analyzerMappings = OutputInputMappings
      .Where(mapping => mapping.ExperimentOutput is not null)
      .Select(mapping => new KeyValuePair<string, string>(mapping.AnalyzerInputKey, mapping.ExperimentOutput!));

    _experimentTemplate.AnalyzerMaps.Clear();
    _experimentTemplate.AnalyzerMaps.AddRange(analyzerMappings);
    _experimentTemplate.AnalyzerId = AnalyzerId;
  }

  public string? AnalyzerId
  {
    get => _analyzerId;

    set
    {
      this.RaiseAndSetIfChanged(ref _analyzerId, value);
      _ = CheckAnalyzer();
      _ = UpdateMappings();
    }
  }

  public AnalyzerInfo? Analyzer { get; private set; }

  public IEnumerable<AnalyzerInfo>? AvailableAnalyzers { get; set; }
}

public record ExperimentOutputAnalyzerInputMapping
{
  public ExperimentOutputAnalyzerInputMapping(string analyzerInputKey, AresDataType inputDataType, bool required)
  {
    AnalyzerInputKey = analyzerInputKey;
    InputType = inputDataType;
    Required = required;
  }

  public string AnalyzerInputKey { get; }
  public AresDataType InputType { get; }
  public bool Required { get; }
  public string[] MatchingOutputs { get; set; } = [];
  public string? ExperimentOutput { get; set; }
}
