using Ares.Datamodel;
using AresScript.ScriptBuilding;
using AresScript.Symbols;
using Microsoft.JSInterop;
using UI.Application.Scripting;
using MonacoCompletionItem = BlazorMonaco.Languages.CompletionItem;

namespace UI.Features.CustomCommand;

internal sealed class CustomCommandMonacoProviderAdapter(
  IMonacoCompletionProvider completionProvider,
  IMonacoDiagnosticsProvider diagnosticsProvider,
  IMonacoSemanticTokensProvider semanticTokensProvider,
  IMonacoHoverProvider hoverProvider,
  Func<CustomCommandScriptContext> getContext)
  : IMonacoCompletionProvider, IMonacoDiagnosticsProvider, IMonacoSemanticTokensProvider, IMonacoHoverProvider
{
  private const int WrappedBodyLineOffset = 1;
  private const int WrappedBodyColumnOffset = 2;

  [JSInvokable]
  public Task<MonacoCompletionItem[]> GetCompletionItems(string script, int line, int column)
  {
    var wrappedScript = BuildWrappedScript(script);
    return completionProvider.GetCompletionItems(
      wrappedScript,
      ToWrappedBodyLine(line),
      ToWrappedBodyColumn(column));
  }

  [JSInvokable]
  public async Task<MonacoDiagnostic[]> GetDiagnostics(string script)
  {
    var wrappedScript = BuildWrappedScript(script);
    var diagnostics = await diagnosticsProvider.GetDiagnostics(wrappedScript);
    return diagnostics.Select(MapDiagnostic).ToArray();
  }

  [JSInvokable]
  public SemanticToken[] GetSemanticTokens(string script)
  {
    var wrappedScript = BuildWrappedScript(script);
    return semanticTokensProvider.GetSemanticTokens(wrappedScript)
      .Where(token => token.Line > WrappedBodyLineOffset)
      .Select(token => token with
      {
        Line = ToBodyLine(token.Line),
        StartColumn = ToBodyColumn(token.StartColumn)
      })
      .ToArray();
  }

  [JSInvokable]
  public Task<string?> GetHoverText(string script, int line, int column, string identifier)
  {
    var wrappedScript = BuildWrappedScript(script);
    return hoverProvider.GetHoverText(
      wrappedScript,
      ToWrappedBodyLine(line),
      ToWrappedBodyColumn(column),
      identifier);
  }

  internal static int ToWrappedBodyLine(int bodyLine) => Math.Max(1, bodyLine) + WrappedBodyLineOffset;

  internal static int ToWrappedBodyColumn(int bodyColumn) => Math.Max(1, bodyColumn) + WrappedBodyColumnOffset;

  internal static int ToBodyLine(int wrappedLine) => Math.Max(1, wrappedLine - WrappedBodyLineOffset);

  internal static int ToBodyColumn(int wrappedColumn) => Math.Max(1, wrappedColumn - WrappedBodyColumnOffset);

  internal static MonacoDiagnostic MapDiagnostic(MonacoDiagnostic diagnostic)
  {
    if(diagnostic.StartLineNumber <= WrappedBodyLineOffset)
    {
      return diagnostic with
      {
        StartLineNumber = 1,
        StartColumn = 1,
        EndLineNumber = 1,
        EndColumn = Math.Max(1, diagnostic.EndColumn),
        Message = $"Generated signature: {diagnostic.Message}"
      };
    }

    return diagnostic with
    {
      StartLineNumber = ToBodyLine(diagnostic.StartLineNumber),
      StartColumn = ToBodyColumn(diagnostic.StartColumn),
      EndLineNumber = ToBodyLine(diagnostic.EndLineNumber),
      EndColumn = ToBodyColumn(diagnostic.EndColumn)
    };
  }

  private string BuildWrappedScript(string body)
  {
    var context = getContext();
    return CustomCommandScriptBuilder.BuildWrappedScript(
      context.CommandName,
      context.Parameters,
      context.OutputSchema,
      body);
  }
}

internal sealed record CustomCommandScriptContext(
  string CommandName,
  IReadOnlyList<AresScriptParameter> Parameters,
  AresValueSchema OutputSchema);
