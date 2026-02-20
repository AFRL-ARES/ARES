using Ares.Datamodel;
using Ares.Datamodel.Scripting;
using Ares.Services;
using Microsoft.JSInterop;
using System.Text;
using UI.Application.Scripting;

namespace UI.Infrastructure.Monaco.Interops;

public sealed class MonacoHoverProvider(AresScriptingService.AresScriptingServiceClient aresScriptingServiceClient) : IMonacoHoverProvider
{
  private readonly AresScriptingService.AresScriptingServiceClient _aresScriptingServiceClient = aresScriptingServiceClient;

  [JSInvokable]
  public async Task<string?> GetHoverText(string script, int line, int column, string identifier)
  {
    var response = await _aresScriptingServiceClient.GetSymbolMetadataAsync(new SymbolMetadataRequest
    {
      Script = script,
      CursorLine = line,
      CursorColumn = column,
      Identifier = identifier ?? string.Empty
    });

    if(response.Metadata is null || !response.Metadata.Found)
    {
      return null;
    }

    return BuildHoverMarkdown(response.Metadata);
  }

  private static string? BuildHoverMarkdown(ScriptSymbolMetadata response)
  {
    var sb = new StringBuilder();
    var displayName = string.IsNullOrWhiteSpace(response.ParentIdentifier)
      ? response.Identifier
      : $"{response.ParentIdentifier}.{response.Identifier}";

    if(!string.IsNullOrWhiteSpace(displayName))
    {
      sb.Append("**").Append(displayName).Append("**");
    }

    AppendDescription(sb, response.Detail, response.Documentation);

    if(response.InputSchema is not null && response.InputSchema.Fields.Count > 0)
    {
      AppendDataSchemaSection(sb, "Inputs", response.InputSchema);
    }

    if(response.OutputSchema is not null
      && response.OutputSchema.Type is not AresDataType.Unit and not AresDataType.UnspecifiedType)
    {
      AppendSchemaEntrySection(sb, "Output", response.OutputSchema);
    }

    if(response.Schema is not null && response.Schema.Type != AresDataType.UnspecifiedType)
    {
      AppendSchemaEntrySection(sb, "Schema", response.Schema);
    }

    return sb.Length == 0 ? null : sb.ToString();
  }

  private static void AppendDescription(StringBuilder sb, string? detail, string? documentation)
  {
    var trimmedDetail = detail?.Trim();
    var trimmedDoc = documentation?.Trim();

    if(string.IsNullOrWhiteSpace(trimmedDetail) && string.IsNullOrWhiteSpace(trimmedDoc))
    {
      return;
    }

    sb.AppendLine().AppendLine();

    if(!string.IsNullOrWhiteSpace(trimmedDetail))
    {
      sb.AppendLine(trimmedDetail);
    }

    if(!string.IsNullOrWhiteSpace(trimmedDoc) && !string.Equals(trimmedDoc, trimmedDetail, StringComparison.Ordinal))
    {
      if(!string.IsNullOrWhiteSpace(trimmedDetail))
      {
        sb.AppendLine();
      }

      sb.AppendLine(trimmedDoc);
    }
  }

  private static void AppendDataSchemaSection(StringBuilder sb, string title, AresDataSchema? schema)
  {
    if(schema is null || schema.Fields.Count == 0)
    {
      return;
    }

    sb.AppendLine().AppendLine();
    sb.Append("**").Append(title).AppendLine("**");
    sb.AppendLine("```text");

    foreach(var field in schema.Fields)
    {
      sb.Append(field.Key).Append(": ").AppendLine(FormatSchemaEntry(field.Value));
    }

    sb.AppendLine("```");
  }

  private static void AppendSchemaEntrySection(StringBuilder sb, string title, SchemaEntry? entry)
  {
    if(entry is null)
    {
      return;
    }

    sb.AppendLine().AppendLine();
    sb.Append("**").Append(title).AppendLine("**");
    sb.AppendLine("```text");
    if(entry.Type == AresDataType.Struct && entry.StructSchema is not null && entry.StructSchema.Fields.Count > 0)
    {
      foreach(var field in entry.StructSchema.Fields)
      {
        sb.Append(field.Key).Append(": ").AppendLine(FormatSchemaEntry(field.Value));
      }
    }
    else if(entry.Type == AresDataType.List && entry.ListElementSchema is not null)
    {
      sb.Append("List<").Append(FormatSchemaEntry(entry.ListElementSchema)).AppendLine(">");
    }
    else
    {
      sb.AppendLine(FormatSchemaEntry(entry));
    }

    sb.AppendLine("```");
  }

  private static string FormatSchemaEntry(SchemaEntry entry)
  {
    var typeName = entry.Type.ToString();
    return entry.Optional ? $"{typeName} (optional)" : typeName;
  }
}
