using Ares.Datamodel.Templates;

namespace UI.Services.CampaignEdit;

/// <summary>
/// Kind of a hacky way to give certain designers a way to reference any necessary info from the campaign itself.
/// Ex.: Campaign template has plannable parameters, but the command parameter designer does not have a way to get to those
/// unless they are passed through all the way from the campaign template to those designers or if we use something
/// like this context in scoped context (one scope can edit one campaign at a time anyways).
/// </summary>
public class CampaignEditContext
{
  public CampaignTemplate? CurrentlyEditingCampaign { get; set; }
}
