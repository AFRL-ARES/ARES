using Ares.Datamodel.Templates;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Radzen;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Features.CampaignEdit.Factories;

namespace UI.Features.CampaignEdit.ViewModels;

public partial class CloseoutDesignerViewModel : ReactiveObject
{
  private readonly StepDesignerFactory _stepDesignerFactory;
  private readonly ValidationService _validationClient;
  private ExperimentTemplate _closeoutTemplate = null!;
  readonly AutomationService _automationClient;
  private readonly NotificationService _notificationService;

  public CloseoutDesignerViewModel(StepDesignerFactory stepDesignerFactory,
    AutomationService automationClient,
    ValidationService validationClient,
    NotificationService notificationService)
  {
    _automationClient = automationClient;
    _stepDesignerFactory = stepDesignerFactory;
    _validationClient = validationClient;
    _notificationService = notificationService;
    Name = "Unnamed Startup Template";
    CloseoutTemplate = new ExperimentTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Closeout Template"
    };
  }

  public CloseoutDesignerViewModel(ExperimentTemplate existingTemplate,
    StepDesignerFactory stepDesignerFactory,
    AutomationService automationClient,
    ValidationService validationClient,
    NotificationService notificationService) : this(stepDesignerFactory, automationClient, validationClient, notificationService)
  {
    CloseoutTemplate = existingTemplate;
  }

  private void Init(ExperimentTemplate existingTemplate)
  {
    Name = existingTemplate.Name;
    CloseoutStepDesigners = existingTemplate.StepTemplates.Select(template => _stepDesignerFactory.Create(template)).OrderBy(model => model.Index).ToList();
    if(existingTemplate.StepTemplates.SelectMany(step => step.CommandTemplates).Any(cmd => cmd.HasOutputVarName))
    {
      var commandDesigners = CloseoutStepDesigners.SelectMany(model => model.CommandDesigners).Where(model => model.CommandTemplate.HasOutputVarName);
      foreach(var designer in commandDesigners)
      {
        designer.OutputProvider = true;
      }

      ExperimentOutputProviderCommand = commandDesigners.Select(designer => designer.CommandTemplate);
    }

    RefreshVariableReferences();
  }

  public ExperimentTemplate Save()
  {
    if(CloseoutStepDesigners is null)
    {
      _notificationService.Notify(NotificationSeverity.Error, "A Step Designer was null! No data saved.");
      return CloseoutTemplate;
    }

    RefreshVariableReferences();
    CloseoutTemplate.Name = Name;
    CloseoutTemplate.StepTemplates.Clear();
    CloseoutTemplate.StepTemplates.AddRange(CloseoutStepDesigners.Select(designer => designer.Save()));
    return CloseoutTemplate;
  }

  public void MoveCloseoutStepUp(StepDesignerViewModel vm)
  {
    if(CloseoutStepDesigners is null || vm.Index == 0)
      return;

    CloseoutStepDesigners.RemoveAt(vm.Index);
    CloseoutStepDesigners.Insert(vm.Index - 1, vm);
    ReindexCloseoutSteps();
    RefreshVariableReferences();
  }

  public void MoveCloseoutStepDown(StepDesignerViewModel vm)
  {
    if(CloseoutStepDesigners is null || vm.Index == CloseoutStepDesigners.Count - 1)
      return;

    CloseoutStepDesigners.RemoveAt(vm.Index);
    CloseoutStepDesigners.Insert(vm.Index + 1, vm);
    ReindexCloseoutSteps();
    RefreshVariableReferences();
  }

  public StepDesignerViewModel AddCloseoutStep()
  {
    var stepDesigner = _stepDesignerFactory.Create();
    stepDesigner.Index = CloseoutStepDesigners.Count;
    CloseoutStepDesigners.Add(stepDesigner);
    RefreshVariableReferences();
    return stepDesigner;
  }

  public void RemoveCloseoutStep(StepDesignerViewModel vm)
  {
    if(CloseoutStepDesigners is not null)
    {
      CloseoutStepDesigners.Remove(vm);
      ReindexCloseoutSteps();
      RefreshVariableReferences();
    }
  }

  private void ReindexCloseoutSteps()
  {
    if(CloseoutStepDesigners is not null)
    {
      var idx = 0;
      foreach(var closeoutStep in CloseoutStepDesigners)
        closeoutStep.Index = idx++;
    }
  }

  public ExperimentTemplate CloseoutTemplate
  {
    private get => _closeoutTemplate;

    set
    {
      _closeoutTemplate = value;
      Init(value);
    }
  }

  [Reactive]
  public partial string Name { get; set; }
  public IList<StepDesignerViewModel> CloseoutStepDesigners { get; private set; } = [];
  public IEnumerable<CommandTemplate>? ExperimentOutputProviderCommand { get; set; }

  public CommandOutputVariableReference[] GetPriorVariableReferences(StepDesignerViewModel stepDesigner)
  {
    var priorReferences = new List<CommandOutputVariableReference>();
    foreach(var currentStep in CloseoutStepDesigners.OrderBy(step => step.Index))
    {
      if(currentStep == stepDesigner)
        return priorReferences.ToArray();

      priorReferences.AddRange(currentStep.GetOutputVariableReferences());
    }

    return priorReferences.ToArray();
  }

  public void RefreshVariableReferences()
  {
    foreach(var stepDesigner in CloseoutStepDesigners.OrderBy(step => step.Index))
      stepDesigner.SetPriorVariableReferences(GetPriorVariableReferences(stepDesigner));
  }
}
