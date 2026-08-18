using Ares.Services;
using Microsoft.JSInterop;
using UI.Application.Scripting;
using MonacoCompletionItem = BlazorMonaco.Languages.CompletionItem;
using ScriptingService = Ares.Core.Grpc.Services.AresScriptingService;

namespace UI.Infrastructure.Monaco.Interops;

public class MonacoCompletionProvider(ScriptingService scriptingService) : IMonacoCompletionProvider
{
  private readonly ScriptingService _scriptingService = scriptingService;

  [JSInvokable]
  public async Task<MonacoCompletionItem[]> GetCompletionItems(string script, int line, int column)
  {
    var request = new CompletionRequest
    {
      CursorColumn = column,
      CursorLine = line,
      Script = script
    };

    var completions = await _scriptingService.GetCompletions(request, null!);
    return completions.Items.Select(item => item.ToMonacoCompletionItem()).ToArray();
  }
}


