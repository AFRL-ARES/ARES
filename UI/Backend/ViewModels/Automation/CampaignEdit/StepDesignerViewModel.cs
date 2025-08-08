using Ares.Datamodel.Templates;
using ReactiveUI;
using UI.Backend.ViewModels.Factories;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class StepDesignerViewModel : ReactiveObject
{
  private readonly CommandDesignerFactory _commandDesignerFactory;
  private StepTemplate _stepTemplate = null!;

  public StepDesignerViewModel(CommandDesignerFactory commandDesignerFactory)
  {
    _commandDesignerFactory = commandDesignerFactory;
    StepTemplate = new StepTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "New Step"
    };
  }

  public StepDesignerViewModel(StepTemplate existingStepTemplate, CommandDesignerFactory commandDesignerFactory)
  {
    _commandDesignerFactory = commandDesignerFactory;
    StepTemplate = existingStepTemplate;
  }

  public string Name
  {
    get => StepTemplate.Name;

    set
    {
      StepTemplate.Name = value;
      this.RaisePropertyChanged();
    }
  }

  public bool Parallel
  {
    get => StepTemplate.IsParallel;

    set
    {
      StepTemplate.IsParallel = value;
      this.RaisePropertyChanged();
    }
  }

  public int Index { get; set; }

  public StepTemplate StepTemplate
  {
    get => _stepTemplate;

    set
    {
      _stepTemplate = value;
      Init(value);
    }
  }

  public IList<CommandDesignerViewModel> CommandDesigners { get; private set; } = Array.Empty<CommandDesignerViewModel>();

  private void Init(StepTemplate existingTemplate)
  {
    Name = existingTemplate.Name;
    Parallel = existingTemplate.IsParallel;
    Index = Convert.ToInt32(existingTemplate.Index);
    CommandDesigners = existingTemplate.CommandTemplates.Select(template => _commandDesignerFactory.Create(template)).OrderBy(model => model.Index).ToList();
  }

  public StepTemplate Save()
  {
    StepTemplate.Name = Name;
    StepTemplate.IsParallel = Parallel;
    StepTemplate.Index = Index;
    StepTemplate.CommandTemplates.Clear();
    StepTemplate.CommandTemplates.AddRange(CommandDesigners.Select(designer => designer.Save()));
    return StepTemplate;
  }

  public CommandDesignerViewModel AddCommandDesigner()
  {
    var newDesigner = _commandDesignerFactory.Create();
    newDesigner.Index = CommandDesigners.Count;
    CommandDesigners.Add(newDesigner);
    return newDesigner;
  }

  public void RemoveCommandDesigner(CommandDesignerViewModel vm)
  {
    CommandDesigners.Remove(vm);
    ReindexCommands();
  }

  public void MoveCommandDesignerUp(CommandDesignerViewModel vm)
  {
    if(vm.Index == 0)
      return;

    CommandDesigners.RemoveAt(vm.Index);
    CommandDesigners.Insert(vm.Index - 1, vm);
    ReindexCommands();
  }

  public void MoveCommandDesignerDown(CommandDesignerViewModel vm)
  {
    if(vm.Index == CommandDesigners.Count - 1)
      return;

    CommandDesigners.RemoveAt(vm.Index);
    CommandDesigners.Insert(vm.Index + 1, vm);
    ReindexCommands();
  }

  private void ReindexCommands()
  {
    var idx = 0;
    foreach(var commandDesigner in CommandDesigners)
      commandDesigner.Index = idx++;
  }
}
