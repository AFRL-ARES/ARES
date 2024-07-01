using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.Helpers;
using UI.Backend.ViewModels.Factories;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class ExperimentDesignerViewModel : ReactiveObject
{
  private readonly StepDesignerFactory _stepDesignerFactory;
  private readonly AresValidation.AresValidationClient _validationClient;
  private string _analyzerId = string.Empty;
  private IEnumerable<AnalyzerInfo>? _availableAnalyzers;
  private ExperimentTemplate _experimentTemplate = null!;
  readonly AresAutomation.AresAutomationClient _automationClient;

  public ExperimentDesignerViewModel(StepDesignerFactory stepDesignerFactory, AresAutomation.AresAutomationClient automationClient, AresValidation.AresValidationClient validationClient)
  {
    _automationClient = automationClient;
    _stepDesignerFactory = stepDesignerFactory;
    _validationClient = validationClient;
    ExperimentTemplate = new ExperimentTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Experiment Template "
    };

    if (_availableAnalyzers is null)
      _ = UpdateAvailableAnalyzers();
  }

  public ExperimentDesignerViewModel(ExperimentTemplate existingTemplate,
    StepDesignerFactory stepDesignerFactory,
    AresAutomation.AresAutomationClient automationClient,
    AresValidation.AresValidationClient validationClient) : this(stepDesignerFactory, automationClient, validationClient)
  {
    ExperimentTemplate = existingTemplate;
  }

  public async Task UpdateAvailableAnalyzers()
  {
    var analyzers = await _automationClient.GetAllAnalyzersAsync(new Empty());
    AvailableAnalyzers = analyzers.Analyzers.ToList();
  }

  public ExperimentTemplate ExperimentTemplate
  {
    private get => _experimentTemplate;

    set
    {
      _experimentTemplate = value;
      Init(value);
    }
  }

  [Reactive]
  public string Name { get; set; } = "Unnamed Template";

  public IList<StepDesignerViewModel> StepDesigners { get; private set; } = Array.Empty<StepDesignerViewModel>();

  public CommandTemplate? ExperimentOutputProviderCommand { get; set; }

  public string AnalyzerId
  {
    get => _analyzerId;

    set
    {
      _analyzerId = value;
      AdjustAnalyzerId();
      _ = CheckAnalyzer();
    }
  }

  public AnalyzerInfo? Analyzer { get; private set; }

  public IEnumerable<AnalyzerInfo>? AvailableAnalyzers
  {
    get => _availableAnalyzers;

    set
    {
      AdjustAnalyzerId();
      _availableAnalyzers = value;
    }
  }

  private void AdjustAnalyzerId()
  {
    // the available analyzers will always get a new unique id when requested, so we have to find the one analyzer in the list
    // that is currently selected and update its unique id accordingly
    var existingAnalyzer = _availableAnalyzers?.FirstOrDefault(info => info.Name == Analyzer?.Name && info.Type == Analyzer.Type && info.Version == Analyzer.Version && info.UniqueId != Analyzer.UniqueId);
    if (existingAnalyzer is not null && Analyzer is not null)
      existingAnalyzer.UniqueId = Analyzer.UniqueId;
  }

  public async Task CheckAnalyzer()
  {
    if (string.IsNullOrEmpty(AnalyzerId))
      return;

    if (_availableAnalyzers is null)
      await UpdateAvailableAnalyzers();

    var analyzer = _availableAnalyzers!.First(info => info.UniqueId == AnalyzerId);
    var outputCommand = ExperimentTemplate.GetOutputCommand();
    if (outputCommand is null)
      return;

    var result = await _validationClient.ValidateAnalyzerSelectionAsync(new AnalyzerValidationRequest { Analyzer = analyzer, OutputCommandMetadata = outputCommand.Metadata });
    if (result.Success)
      Analyzer = analyzer;
  }

  private void Init(ExperimentTemplate existingTemplate)
  {
    Name = existingTemplate.Name;
    StepDesigners = existingTemplate.StepTemplates.Select(template => _stepDesignerFactory.Create(template)).OrderBy(model => model.Index).ToList();
    if (!string.IsNullOrEmpty(existingTemplate.OutputCommandId))
    {
      var commandDesigner = StepDesigners.SelectMany(model => model.CommandDesigners).First(model => model.CommandTemplate.UniqueId == existingTemplate.OutputCommandId);
      commandDesigner.ExperimentOutputProvider = true;
      ExperimentOutputProviderCommand = commandDesigner.CommandTemplate;
    }

    Analyzer = existingTemplate.Analyzer;
    AnalyzerId = Analyzer?.UniqueId ?? string.Empty;
  }

  public ExperimentTemplate Save()
  {
    ExperimentTemplate.Name = Name;
    ExperimentTemplate.StepTemplates.Clear();
    ExperimentTemplate.StepTemplates.AddRange(StepDesigners.Select(designer => designer.Save()));
    ExperimentTemplate.OutputCommandId = StepDesigners.SelectMany(model => model.CommandDesigners).FirstOrDefault(model => model.ExperimentOutputProvider)?.CommandTemplate.UniqueId ?? string.Empty;
    ExperimentTemplate.Analyzer = Analyzer;
    return ExperimentTemplate;
  }

  public StepDesignerViewModel AddStep()
  {
    var stepDesigner = _stepDesignerFactory.Create();
    stepDesigner.Index = StepDesigners.Count;
    StepDesigners.Add(stepDesigner);
    return stepDesigner;
  }

  public void RemoveStep(StepDesignerViewModel vm)
  {
    StepDesigners.Remove(vm);
    ReindexSteps();
  }

  public void MoveStepDesignerUp(StepDesignerViewModel vm)
  {
    if (vm.Index == 0)
      return;

    StepDesigners.RemoveAt(vm.Index);
    StepDesigners.Insert(vm.Index - 1, vm);
    ReindexSteps();
  }

  public void MoveStepDesignerDown(StepDesignerViewModel vm)
  {
    if (vm.Index == StepDesigners.Count - 1)
      return;

    StepDesigners.RemoveAt(vm.Index);
    StepDesigners.Insert(vm.Index + 1, vm);
    ReindexSteps();
  }

  private void ReindexSteps()
  {
    var idx = 0;
    foreach (var stepDesigner in StepDesigners)
      stepDesigner.Index = idx++;
  }
}
