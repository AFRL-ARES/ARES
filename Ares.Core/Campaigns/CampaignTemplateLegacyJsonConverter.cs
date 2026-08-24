using Ares.Core.Execution.Executors;
using Ares.Datamodel.Templates;
using System.Text.Json.Nodes;

namespace Ares.Core.Campaigns;

internal static class CampaignTemplateLegacyJsonConverter
{
  private const string LegacyAresDeviceId = "ARES-CORE-DEVICE";

  // Before custom commands, command templates only contained device-command metadata directly.
  // Legacy imports need that metadata wrapped in the current typed device-command shape.
  public static void Convert(JsonNode node)
  {
    if(node is JsonObject jsonObject)
    {
      if(jsonObject["commandTemplates"] is JsonArray commands)
        foreach(var command in commands.OfType<JsonObject>())
        {
          var hasCurrentType = command.ContainsKey("deviceCommand")
            || command.ContainsKey("systemCommand")
            || command.ContainsKey("customCommandInvocation");
          if(!hasCurrentType && command.Remove("metadata", out var metadata))
          {
            if(TryGetSystemOperation(metadata, out var operation))
              command["systemCommand"] = new JsonObject { ["operation"] = (int)operation };
            else
              command["deviceCommand"] = new JsonObject { ["metadata"] = metadata };
          }

          if(!command.ContainsKey("argumentBindings") && command.Remove("parameters", out var parameters))
            command["argumentBindings"] = parameters;
        }

      foreach(var child in jsonObject.Select(property => property.Value).ToArray())
        if(child is not null)
          Convert(child);
    }
    else if(node is JsonArray jsonArray)
      foreach(var child in jsonArray)
        if(child is not null)
          Convert(child);
  }

  private static bool TryGetSystemOperation(JsonNode? metadataNode, out SystemOperation operation)
  {
    operation = SystemOperation.Undefined;
    if(metadataNode is not JsonObject metadata
      || metadata["deviceId"]?.GetValue<string>() != LegacyAresDeviceId)
      return false;

    var commandName = metadata["name"]?.GetValue<string>();
    return Enum.TryParse(commandName, ignoreCase: false, out operation)
      && operation != SystemOperation.Undefined
      && SystemOperationCatalog.Find(operation) is not null;
  }
}
