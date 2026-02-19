using Antlr4.Runtime;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Scripting;
using Ares.Services;
using AresScript;
using AresScript.Generated;
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
    if(string.IsNullOrWhiteSpace(identifier))
    {
      return null;
    }

    var request = new CompletionRequest
    {
      CursorColumn = column,
      CursorLine = line,
      Script = script
    };

    var completions = await _aresScriptingServiceClient.GetCompletionsAsync(request);
    var item = completions.Items.FirstOrDefault(i => string.Equals(i.Label, identifier, StringComparison.Ordinal));

    var hoverMarkdown = BuildHoverMarkdown(item);
    var markdown = hoverMarkdown?.markdown;
    var hasSchema = hoverMarkdown?.hasSchema ?? false;

    return string.IsNullOrWhiteSpace(markdown) ? null : markdown;
  }

  private static (string markdown, bool hasSchema)? BuildHoverMarkdown(CompletionItem? item)
  {
    if(item is null)
    {
      return null;
    }
    var sb = new StringBuilder();
    var schemaFound = false;
    var label = item.Label ?? string.Empty;
    var name = string.IsNullOrWhiteSpace(item.ParentIdentifier) ? label : $"{item.ParentIdentifier}.{label}";

    if(!string.IsNullOrWhiteSpace(name))
    {
      sb.Append("**").Append(name).Append("**");
    }

    AppendDescription(sb, item.Detail, item.Documentation);
    if(item.InputSchema is not null)
    {
      schemaFound = true;
      AppendDataSchemaSection(sb, "Inputs", item.InputSchema);
    }
    if(item.OutputSchema?.Type != AresDataType.Unit)
    {
      schemaFound = true;
      AppendSchemaEntrySection(sb, "Output", item.OutputSchema);
    }

    if(item.Schema is not null)
    {
      schemaFound = true;
      AppendSchemaEntrySection(sb, "Schema", item.Schema);
    }

    return (sb.ToString(), schemaFound);
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

  private static AresLangParser.ProgramContext? TryParseProgram(string script)
  {
    try
    {
      var stream = new AntlrInputStream(script);
      var lexer = new AresIndentationLexer(stream);
      var tokenStream = new CommonTokenStream(lexer);
      var parser = new AresLangParser(tokenStream);
      return parser.program();
    }
    catch
    {
      return null;
    }
  }

  private static AresSystemFunction CreateSystemFunction(FunctionSymbol function, string namespaceName)
  {
    var id = string.IsNullOrWhiteSpace(function.Id) ? function.Name : function.Id;
    var name = string.IsNullOrWhiteSpace(function.Name) ? id : function.Name;
    var inputSchema = function.InputSchema ?? new AresDataSchema();
    var outputSchema = function.OutputSchema ?? new SchemaEntry();
    var description = function.Description ?? string.Empty;
    return new AresSystemFunction(id, name, DummyFunction, inputSchema, outputSchema, namespaceName, description);
  }

  private static Task<AresValue> DummyFunction(List<AresValue> _, ScriptExecutionControlToken __)
  {
    return Task.FromResult(AresValueHelper.CreateNull());
  }

  private static string AppendInferredSchema(string? markdown, string identifier, SchemaEntry schema)
  {
    var sb = new StringBuilder();
    if(!string.IsNullOrWhiteSpace(markdown))
    {
      sb.Append(markdown);
    }
    else
    {
      sb.Append("**").Append(identifier).Append("**");
    }

    AppendSchemaEntrySection(sb, "Inferred Type", schema);
    return sb.ToString();
  }
}

