using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace UI.Domain.Experiments;

internal static class ExperimentTemplateExtensions
{
  public static IEnumerable<Parameter> GetAllParameters(this ExperimentTemplate template)
    => template.StepTemplates
      .SelectMany(stepTemplate => stepTemplate.CommandTemplates)
      .SelectMany(commandTemplate => commandTemplate.Parameters);

  public static IEnumerable<Parameter> GetAllPlannedParameters(this ExperimentTemplate template)
    => template.GetAllParameters().Where(parameter => parameter.IsPlanned());

  public static bool IsResolved(this ExperimentTemplate template)
    => template.GetAllParameters().All(parameter => parameter.GetValue() is not null);

  public static CommandTemplate[] GetAllOutputCommands(this ExperimentTemplate template)
    => template.StepTemplates
    .SelectMany(step => step.CommandTemplates)
    .Where(command => command.HasOutputVarName).ToArray();
}
