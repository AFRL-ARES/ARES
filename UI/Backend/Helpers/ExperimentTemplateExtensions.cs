using Ares.Messaging;

namespace UI.Backend.Helpers;

internal static class ExperimentTemplateExtensions
{
  public static IEnumerable<Parameter> GetAllParameters(this ExperimentTemplate template)
    => template.StepTemplates
      .SelectMany(stepTemplate => stepTemplate.CommandTemplates)
      .SelectMany(commandTemplate => commandTemplate.Parameters);

  public static IEnumerable<Parameter> GetAllPlannedParameters(this ExperimentTemplate template)
    => template.GetAllParameters().Where(parameter => parameter.Planned);

  public static bool IsResolved(this ExperimentTemplate template)
    => template.GetAllParameters().All(parameter => parameter.Value is not null);

  public static CommandTemplate? GetOutputCommand(this ExperimentTemplate template)
    => template.StepTemplates
      .SelectMany(stepTemplate => stepTemplate.CommandTemplates)
      .FirstOrDefault(commandTemplate => commandTemplate.UniqueId == template.OutputCommandId);
}
