using Ares.Services;
using Microsoft.JSInterop;
using MonacoCompletionItem = BlazorMonaco.Languages.CompletionItem;

namespace UI.JsInterops;

public class MonacoCompletionProvider(AresScriptingService.AresScriptingServiceClient aresScriptingServiceClient)
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

