using Ares.Services;
using Microsoft.JSInterop;
using UI.Domain.Scripting;
using MonacoCompletionItem = BlazorMonaco.Languages.CompletionItem;

namespace UI.Infrastructure.Monaco.Interops;

public class MonacoCompletionProvider(AresScriptingService.AresScriptingServiceClient aresScriptingServiceClient) : IMonacoCompletionProvider
{
  private readonly AresScriptingService.AresScriptingServiceClient _aresScriptingServiceClient = aresScriptingServiceClient;

  [JSInvokable]
  public async Task<MonacoCompletionItem[]> GetCompletionItems(string script, int line, int column)
  {
    var request = new CompletionRequest
    {
      CursorColumn = column,
      CursorLine = line,
      Script = script
    };

    var completions = await _aresScriptingServiceClient.GetCompletionsAsync(request);
    return completions.Items.Select(item => item.ToMonacoCompletionItem()).ToArray();
  }
}

