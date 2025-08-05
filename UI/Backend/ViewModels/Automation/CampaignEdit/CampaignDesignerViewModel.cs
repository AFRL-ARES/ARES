using Ares.Messaging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.ViewModels.Automation.CampaignEdit.Factories;
using UI.Backend.ViewModels.Factories;
using UI.Services.CampaignEdit;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class CampaignDesignerViewModel : ReactiveObject
{
  private readonly AresAutomation.AresAutomationClient _automationClient;
  private readonly CampaignEditContext _editContext;
  private readonly ExperimentDesignerFactory _experimentDesignerFactory;
  private readonly PlannableParameterDesignerFactory _plannableParameterDesignerFactory;
  private readonly PlanningDesignerFactory _planningDesignerFactory;
  private CampaignTemplate _campaignTemplate = null!;
  readonly AnalyzerInputDesignerVmFactory _analyzerInputDesignerFactory;

  public CampaignDesignerViewModel(
    AresAutomation.AresAutomationClient automationClient,
    ExperimentDesignerFactory experimentDesignerFactory,
    PlanningDesignerFactory planningDesignerFactory,
    AnalyzerInputDesignerVmFactory analyzingDesignerFactory,
    PlannableParameterDesignerFactory plannableParameterDesignerFactory,
    CampaignEditContext editContext,
    IConfiguration configuration)
  {
    _analyzerInputDesignerFactory = analyzingDesignerFactory;
    _automationClient = automationClient;
    _experimentDesignerFactory = experimentDesignerFactory;
    _planningDesignerFactory = planningDesignerFactory;
    _plannableParameterDesignerFactory = plannableParameterDesignerFactory;
    _editContext = editContext;
    IsCreatingCampaign = false;
    IsNotCreatingCampaign = true;
    Placeholder = "New Campaign 1";

    CampaignTemplate = new CampaignTemplate
    {
      Name = Placeholder,
      UniqueId = Guid.NewGuid().ToString()
    };
    CampaignTemplate.ExperimentTemplates.Add(new ExperimentTemplate() { UniqueId = Guid.NewGuid().ToString(), Name = "New Experiment" });
  }

  [Reactive] public bool IsCreatingCampaign { get; set; }

  [Reactive] public bool IsNotCreatingCampaign { get; set; }

  [Reactive] public string Placeholder { get; set; }

  public PlannableParameterDesignerViewModel? PlannableParameterDesigner { get; private set; }

  public ExperimentDesignerViewModel? ExperimentDesigner { get; private set; }

  public PlanningViewModel? PlanningDesigner { get; private set; }

  public ExperimentDesignerViewModel? CampaignCloseoutDesigner { get; private set; }

  public ExperimentDesignerViewModel? CampaignStartupDesigner { get; private set; }

  public AnalyzerDesignerViewModel? AnalyzerDesignerViewModel { get; private set; }

  public string CampaignName { get; set; } = "Unnamed Campaign";

  public CampaignTemplate CampaignTemplate
  {
    private get => _campaignTemplate;

    set
    {
      _campaignTemplate = value;
      _editContext.CurrentlyEditingCampaign = value;
      _ = Init(value);
    }
  }

  [Reactive] public bool CreationIsErrorFree { get; set; }

  [Reactive] public string? CreationErrorText { get; set; }

  private async Task Init(CampaignTemplate campaignTemplate)
  {
    CampaignName = campaignTemplate.Name;
    PlannableParameterDesigner = _plannableParameterDesignerFactory.Create(campaignTemplate.PlannableParameters);
    ExperimentDesigner = _experimentDesignerFactory.Create(campaignTemplate.ExperimentTemplates.FirstOrDefault());
    PlanningDesigner = await _planningDesignerFactory.Create(campaignTemplate);
    var commandDesigners = ExperimentDesigner?.StepDesigners?.SelectMany(sd => sd.CommandDesigners) ?? [];
    if(CampaignTemplate.ExperimentTemplates.Any())
    {
      AnalyzerDesignerViewModel = _analyzerInputDesignerFactory.Create(campaignTemplate.ExperimentTemplates.First(), commandDesigners);
    }
  }

  public CampaignTemplate Save()
  {
    CampaignTemplate.Name = CampaignName;
    CampaignTemplate.PlannableParameters.Clear();
    CampaignTemplate.PlannableParameters.AddRange(PlannableParameterDesigner?.Save() ?? Array.Empty<ParameterMetadata>());
    CampaignTemplate.ExperimentTemplates.Clear();
    if(ExperimentDesigner is not null)
      CampaignTemplate.ExperimentTemplates.Add(ExperimentDesigner.Save());

    //Try Save Instead?
    PlannableParameterDesigner?.Save();
    PlanningDesigner?.Save();
    AnalyzerDesignerViewModel?.Save();
    return CampaignTemplate;
  }

  public async Task SelectCampaignById(Guid campaignId)
  {
    var request = new CampaignRequest
    {
      UniqueId = campaignId.ToString(),
    };

    CampaignTemplate = await _automationClient.GetSingleCampaignAsync(request);
  }

  public async Task Update()
  {
    if(string.IsNullOrEmpty(CampaignName))
      CampaignName = Placeholder;

    var isUpdating = await _automationClient.CampaignExistsAsync(new CampaignRequest { UniqueId = CampaignTemplate.UniqueId });

    var nameChanged = CampaignTemplate.Name != CampaignName;
    if(nameChanged)
    {
      var campaignExists = await _automationClient.CampaignExistsAsync(new CampaignRequest { CampaignName = CampaignName });
      if(campaignExists.Value)
      {
        CreationErrorText = "Campaign Name " + CampaignName + " Already Exists!";
        CreationIsErrorFree = false;
        return;
      }
    }

    var template = Save();
    if(isUpdating.Value)
      await _automationClient.UpdateCampaignAsync(new AddOrUpdateCampaignRequest() { Template = template });

    else
      await _automationClient.AddCampaignAsync(new AddOrUpdateCampaignRequest() { Template = template });
  }
}
