using Ares.Core.Execution.Executors;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace UI.Features.CampaignEdit.ViewModels;

public record CommandOutputVariableReference(string Path, AresDataType Type, bool Compatible = true)
{
  public bool IsDisabled => !Compatible;

  public string DisplayText => Compatible ? $"{Path} ({Type})" : $"{Path} ({Type}, incompatible)";
}

public static class CommandOutputVariableReferenceBuilder
{
  public static CommandOutputVariableReference[] Build(CommandDesignerViewModel commandDesigner)
  {
    if(!commandDesigner.OutputProvider || string.IsNullOrWhiteSpace(commandDesigner.OutputVariableName))
      return [];

    var outputSchema = commandDesigner.OutputSchema;
    if(outputSchema is null)
      return [];

    return Build(commandDesigner.OutputVariableName, outputSchema).ToArray();
  }

  public static CommandOutputVariableReference[] Build(CommandTemplate commandTemplate)
  {
    if(!commandTemplate.HasOutputVarName)
      return [];

    var outputSchema = commandTemplate.CommandTypeCase switch
    {
      CommandTemplate.CommandTypeOneofCase.DeviceCommand => commandTemplate.DeviceCommand.Metadata?.OutputMetadata?.DataSchema,
      CommandTemplate.CommandTypeOneofCase.SystemCommand => SystemOperationCatalog.Find(commandTemplate.SystemCommand.Operation)?.OutputSchema,
      _ => null
    };
    if(outputSchema is null || outputSchema.Type is AresDataType.Unit or AresDataType.UnspecifiedType)
      return [];

    return Build(commandTemplate.OutputVarName, outputSchema).ToArray();
  }

  public static CommandOutputVariableReference[] MarkCompatibility(
    IEnumerable<CommandOutputVariableReference> references,
    AresValueSchema parameterSchema)
    => references
      .Select(reference => reference with { Compatible = IsCompatible(parameterSchema.Type, reference.Type) })
      .ToArray();

  private static IEnumerable<CommandOutputVariableReference> Build(string path, AresValueSchema schema)
  {
    yield return new CommandOutputVariableReference(path, schema.Type);

    if(schema.Type != AresDataType.Struct || schema.StructSchema is null)
      yield break;

    foreach(var field in schema.StructSchema.Fields)
    {
      foreach(var nestedReference in Build($"{path}.{field.Key}", field.Value))
        yield return nestedReference;
    }
  }

  private static bool IsCompatible(AresDataType parameterType, AresDataType variableType)
    => parameterType == AresDataType.Any || variableType == AresDataType.Any || parameterType == variableType;
}
