using Ares.Datamodel.Templates;
using Ares.Services;
using Radzen;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.ViewModels.Factories;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class ExperimentDesignerViewModel : ReactiveObject
{
  private readonly StepDesignerFactory _stepDesignerFactory;
  private readonly AresValidation.AresValidationClient _validationClient;
  private ExperimentTemplate _experimentTemplate = null!;
  readonly AresAutomation.AresAutomationClient _automationClient;
  private readonly NotificationService _notificationService;

  public ExperimentDesignerViewModel(StepDesignerFactory stepDesignerFactory,
    AresAutomation.AresAutomationClient automationClient,
    AresValidation.AresValidationClient validationClient,
    NotificationService notificationService)
  {
    _automationClient = automationClient;
    _stepDesignerFactory = stepDesignerFactory;
    _validationClient = validationClient;
    _notificationService = notificationService;
    ExperimentTemplate = new ExperimentTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Experiment Template "
    };


  }

  public ExperimentDesignerViewModel(ExperimentTemplate existingTemplate,
    StepDesignerFactory stepDesignerFactory,
    AresAutomation.AresAutomationClient automationClient,
    AresValidation.AresValidationClient validationClient,
    NotificationService notificationService) : this(stepDesignerFactory, automationClient, validationClient, notificationService)
  {
    ExperimentTemplate = existingTemplate;
  }

  private void Init(ExperimentTemplate existingTemplate)
  {
    Name = existingTemplate.Name;
    StepDesigners = existingTemplate.StepTemplates.Select(template => _stepDesignerFactory.Create(template)).OrderBy(model => model.Index).ToList();
    StartupStepDesigners = existingTemplate.StartupStepTemplates.Select(template => _stepDesignerFactory.Create(template)).OrderBy(model => model.Index).ToList();
    CloseoutStepDesigners = existingTemplate.CloseoutStepTemplates.Select(template => _stepDesignerFactory.Create(template)).OrderBy(model => model.Index).ToList();
    if(existingTemplate.StepTemplates.Select(step => step.CommandTemplates.Select(cmd => cmd.UserOutputKeyMap)).Any())
    {
      var commandDesigners = StepDesigners.SelectMany(model => model.CommandDesigners).Where(model => model.CommandTemplate.UserOutputKeyMap.Any());
      foreach(var designer in commandDesigners)
      {
        designer.ExperimentOutputProvider = true;
      }

      ExperimentOutputProviderCommand = commandDesigners.Select(designer => designer.CommandTemplate);
    }
  }

  public ExperimentTemplate Save()
  {
    if(StartupStepDesigners is null || CloseoutStepDesigners is null || StepDesigners is null)
    {
      _notificationService.Notify(NotificationSeverity.Error, "A Step Designer was null! No data saved.");
      return ExperimentTemplate;
    }

    ExperimentTemplate.Name = Name;
    ExperimentTemplate.StepTemplates.Clear();
    ExperimentTemplate.StepTemplates.AddRange(StepDesigners.Select(designer => designer.Save()));
    ExperimentTemplate.StartupStepTemplates.Clear();
    ExperimentTemplate.StartupStepTemplates.AddRange(StartupStepDesigners.Select(designer => designer.Save()));
    ExperimentTemplate.CloseoutStepTemplates.Clear();
    ExperimentTemplate.CloseoutStepTemplates.AddRange(CloseoutStepDesigners.Select(designer => designer.Save()));
    return ExperimentTemplate;
  }

  public StepDesignerViewModel AddStep()
  {
    var stepDesigner = _stepDesignerFactory.Create();
    stepDesigner.Index = StepDesigners.Count;
    StepDesigners.Add(stepDesigner);
    return stepDesigner;
  }

  public StepDesignerViewModel AddStartupStep()
  {
    var stepDesigner = _stepDesignerFactory.Create();
    stepDesigner.Index = StartupStepDesigners.Count;
    StartupStepDesigners.Add(stepDesigner);
    return stepDesigner;
  }

  public StepDesignerViewModel AddCloseoutStep()
  {
    var stepDesigner = _stepDesignerFactory.Create();
    stepDesigner.Index = CloseoutStepDesigners.Count;
    CloseoutStepDesigners.Add(stepDesigner);
    return stepDesigner;
  }

  public void RemoveStep(StepDesignerViewModel vm)
  {
    if(StepDesigners is not null)
    {
      StepDesigners.Remove(vm);
      ReindexSteps();
    }
  }

  public void RemoveStartupStep(StepDesignerViewModel vm)
  {
    if(StartupStepDesigners is not null)
    {
      StartupStepDesigners.Remove(vm);
      ReindexSteps();
    }
  }

  public void RemoveCloseoutStep(StepDesignerViewModel vm)
  {
    if(CloseoutStepDesigners is not null)
    {
      CloseoutStepDesigners.Remove(vm);
      ReindexSteps();
    }
  }

  public void MoveStepDesignerUp(StepDesignerViewModel vm)
  {
    if(StepDesigners is null || vm.Index == 0)
      return;

    StepDesigners.RemoveAt(vm.Index);
    StepDesigners.Insert(vm.Index - 1, vm);
    ReindexSteps();
  }

  public void MoveStepDesignerDown(StepDesignerViewModel vm)
  {
    if(StepDesigners is null || vm.Index == StepDesigners.Count - 1)
      return;

    StepDesigners.RemoveAt(vm.Index);
    StepDesigners.Insert(vm.Index + 1, vm);
    ReindexSteps();
  }

  public void MoveStartupStepUp(StepDesignerViewModel vm)
  {
    if(StartupStepDesigners is null || vm.Index == 0)
      return;

    StartupStepDesigners.RemoveAt(vm.Index);
    StartupStepDesigners.Insert(vm.Index - 1, vm);
    ReindexStartupSteps();
  }

  public void MoveStartupStepDown(StepDesignerViewModel vm)
  {
    if(StartupStepDesigners is null || vm.Index == StartupStepDesigners.Count - 1)
      return;

    StartupStepDesigners.RemoveAt(vm.Index);
    StartupStepDesigners.Insert(vm.Index + 1, vm);
    ReindexStartupSteps();
  }

  public void MoveCloseoutStepUp(StepDesignerViewModel vm)
  {
    if(CloseoutStepDesigners is null || vm.Index == 0)
      return;

    CloseoutStepDesigners.RemoveAt(vm.Index);
    CloseoutStepDesigners.Insert(vm.Index - 1, vm);
    ReindexCloseoutSteps();
  }

  public void MoveCloseoutStepDown(StepDesignerViewModel vm)
  {
    if(CloseoutStepDesigners is null || vm.Index == CloseoutStepDesigners.Count - 1)
      return;

    CloseoutStepDesigners.RemoveAt(vm.Index);
    CloseoutStepDesigners.Insert(vm.Index + 1, vm);
    ReindexCloseoutSteps();
  }

  private void ReindexSteps()
  {
    if(StepDesigners is not null)
    {
      var idx = 0;
      foreach(var stepDesigner in StepDesigners)
        stepDesigner.Index = idx++;
    }
  }

  private void ReindexStartupSteps()
  {
    if(StartupStepDesigners is not null)
    {
      var idx = 0;
      foreach(var startupStep in StartupStepDesigners)
        startupStep.Index = idx++;
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

  public IList<StepDesignerViewModel>? StepDesigners { get; private set; }

  public IList<StepDesignerViewModel>? StartupStepDesigners { get; private set; }

  public IList<StepDesignerViewModel>? CloseoutStepDesigners { get; private set; }

  public IEnumerable<CommandTemplate>? ExperimentOutputProviderCommand { get; set; }
}
