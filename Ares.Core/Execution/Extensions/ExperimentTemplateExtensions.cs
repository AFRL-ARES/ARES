using Ares.Messaging;

namespace Ares.Core.Execution.Extensions;

internal static class ExperimentTemplateExtensions
{
  /// <summary>
  /// Gets all the <see cref="Parameter" />s from an <see cref="ExperimentTemplate" />
  /// </summary>
  /// <param name="template"></param>
  /// <returns></returns>
  public static IEnumerable<Parameter> GetAllParameters(this ExperimentTemplate template)
    => template.StepTemplates
      .SelectMany(stepTemplate => stepTemplate.CommandTemplates)
      .SelectMany(commandTemplate => commandTemplate.Parameters);

  /// <summary>
  /// Gets all the parameters that need to be planned from a given <see cref="ExperimentTemplate" />
  /// </summary>
  /// <param name="template">The template to check for parameters</param>
  /// <returns>Collection of planned parameters</returns>
  public static IEnumerable<Parameter> GetAllPlannedParameters(this ExperimentTemplate template)
    => template.GetAllParameters().Where(parameter => parameter.Planned);

  /// <summary>
  /// Checks whether or not every <see cref="Parameter" /> within an experiment has a value. If so then
  /// that means the template is resolved and can be sent to execution.
  /// </summary>
  /// <param name="template">The template to check if resolved</param>
  /// <returns>True if resolved, false otherwise</returns>
  public static bool IsResolved(this ExperimentTemplate template)
    => template.GetAllParameters().All(parameter => parameter.Value is not null);

  /// <summary>
  /// Given an experiment template, creates a new experiment template with a new unique id
  /// as well as a new id for any nested templates.
  /// </summary>
  /// <param name="template">The <see cref="ExperimentTemplate" /> to clone</param>
  /// <returns>A new instance of experiment template</returns>
  public static ExperimentTemplate CloneWithNewIds(this ExperimentTemplate template)
  {
    var newTemplate = template.Clone();
    newTemplate.UniqueId = Guid.NewGuid().ToString();
    foreach (var stepTemplate in newTemplate.StepTemplates)
    {
      stepTemplate.UniqueId = Guid.NewGuid().ToString();
      foreach (var commandTemplate in stepTemplate.CommandTemplates)
      {
        var cmdTemplateId = Guid.NewGuid().ToString();
        if (commandTemplate.UniqueId == template.OutputCommandId)
          newTemplate.OutputCommandId = cmdTemplateId;

        commandTemplate.Metadata.UniqueId = Guid.NewGuid().ToString();
        commandTemplate.UniqueId = cmdTemplateId;
        foreach (var metadataParameterMetadata in commandTemplate.Metadata.ParameterMetadatas)
        {
          metadataParameterMetadata.UniqueId = Guid.NewGuid().ToString();
          foreach (var constraint in metadataParameterMetadata.Constraints)
            constraint.UniqueId = Guid.NewGuid().ToString();
        }

        foreach (var argument in commandTemplate.Parameters)
        {
          argument.UniqueId = Guid.NewGuid().ToString();
          argument.Metadata.UniqueId = Guid.NewGuid().ToString();
          if (argument.Value is not null)
            argument.Value.UniqueId = Guid.NewGuid().ToString();

          foreach (var constraint in argument.Metadata.Constraints)
            constraint.UniqueId = Guid.NewGuid().ToString();
        }
      }
    }

    return newTemplate;
  }
}
