using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Scripting;
using Ares.Services;
using Microsoft.JSInterop;
using System.Text;
using UI.Application.Scripting;
using ScriptingService = Ares.Core.Grpc.Services.AresScriptingService;

namespace UI.Infrastructure.Monaco.Interops;

public sealed class MonacoHoverProvider(ScriptingService scriptingService) : IMonacoHoverProvider
{
  private readonly ScriptingService _scriptingService = scriptingService;

  [JSInvokable]
  public async Task<string?> GetHoverText(string script, int line, int column, string identifier)
  {
    var response = await _scriptingService.GetSymbolMetadata(new SymbolMetadataRequest
    {
      Script = script,
      CursorLine = line,
      CursorColumn = column,
      Identifier = identifier ?? string.Empty
    }, null!);

    if(response.Metadata is null || !response.Found)
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

    switch(response.ShapeCase)
    {
      case ScriptSymbolMetadata.ShapeOneofCase.FunctionShape:
        AppendFunctionShape(sb, response.FunctionShape);
        break;
      case ScriptSymbolMetadata.ShapeOneofCase.ValueShape:
        AppendValueShape(sb, response.ValueShape);
        break;
      default:
        break;
    }

    return sb.Length == 0 ? null : sb.ToString();
  }

  private static void AppendFunctionShape(StringBuilder sb, ScriptSymbolMetadata.Types.FunctionShape functionShape)
  {
    if(functionShape.InputSchema is not null && functionShape.InputSchema.Fields.Count > 0)
    {
      AppendDataSchemaSection(sb, "Inputs", functionShape.InputSchema);
    }

    if(functionShape.OutputSchema is not null && functionShape.OutputSchema.Type is not AresDataType.Unit and not AresDataType.UnspecifiedType)
    {
      AppendAresValueSchemaSection(sb, "Outputs", functionShape.OutputSchema);
    }
  }

  private static void AppendValueShape(StringBuilder sb, ScriptSymbolMetadata.Types.ValueShape valueShape)
  {
    if(valueShape.Schema is not null && valueShape.Schema.Type is not AresDataType.UnspecifiedType)
    {
      AppendAresValueSchemaSection(sb, "Schema", valueShape.Schema);
    }

    // TODO: revisit the value. It seems that we have a value regardless of whether or not there's
    // a real value if that makes sense. Need to figure out if there's a way to distinguish a constant
    // provided by the system instead of any ol' value symbol. -AB
    //if(valueShape.Value is not null)
    //{
    //  sb.AppendLine();
    //  sb.Append("Value: ");
    //  sb.Append("```text");
    //  sb.Append(valueShape.Value.Stringify());
    //  sb.Append("```");
    //}
  }

  private static void AppendDescription(StringBuilder sb, string? detail, string? documentation)
  {
    var trimmedDetail = detail?.Trim();
    var trimmedDoc = documentation?.Trim();
    var detailNull = string.IsNullOrWhiteSpace(trimmedDetail);
    var docNull = string.IsNullOrWhiteSpace(trimmedDoc);

    if(detailNull && docNull)
    {
      return;
    }

    sb.AppendLine().AppendLine();

    if(!detailNull)
    {
      sb.AppendLine(trimmedDetail);
    }

    if(!docNull && !string.Equals(trimmedDoc, trimmedDetail, StringComparison.Ordinal))
    {
      if(!detailNull)
      {
        sb.AppendLine();
      }

      sb.AppendLine(trimmedDoc);
    }
  }

  private static void AppendDataSchemaSection(StringBuilder sb, string title, AresStructSchema? schema)
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
      sb.Append(field.Key).Append(": ").AppendLine(field.Value.Stringify());
    }

    sb.AppendLine("```");
  }

  private static void AppendAresValueSchemaSection(StringBuilder sb, string title, AresValueSchema? entry)
  {
    if(entry is null)
    {
      return;
    }

    sb.AppendLine().AppendLine();
    sb.Append("**").Append(title).AppendLine("**");
    sb.AppendLine("```text");
    sb.AppendLine(entry.Stringify());
    sb.AppendLine("```");
    return;
    if(entry.Type == AresDataType.Struct && entry.StructSchema is not null && entry.StructSchema.Fields.Count > 0)
    {
      foreach(var field in entry.StructSchema.Fields)
      {
        sb.Append(field.Key).Append(": ").AppendLine(FormatAresValueSchema(field.Value));
      }
    }
    else if(entry.Type == AresDataType.List && entry.ListElementSchema is not null)
    {
      sb.Append("List<").Append(FormatAresValueSchema(entry.ListElementSchema)).AppendLine(">");
    }
    else
    {
      sb.AppendLine(FormatAresValueSchema(entry));
    }

    sb.AppendLine("```");
  }

  private static string FormatAresValueSchema(AresValueSchema entry)
  {
    var typeName = entry.Type.ToString();
    return entry.Optional ? $"{typeName} (optional)" : typeName;
  }
}
