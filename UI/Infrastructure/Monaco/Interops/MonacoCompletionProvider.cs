using Ares.Services;
using Ares.Core.Grpc.Services;
using Microsoft.JSInterop;
using UI.Application.Scripting;
using MonacoCompletionItem = BlazorMonaco.Languages.CompletionItem;

namespace UI.Infrastructure.Monaco.Interops;

public class MonacoCompletionProvider(Ares.Core.Grpc.Services.AresScriptingService aresScriptingServiceClient) : IMonacoCompletionProvider
{
  private readonly Ares.Core.Grpc.Services.AresScriptingService _aresScriptingServiceClient = aresScriptingServiceClient;

  [JSInvokable]
  public async Task<MonacoCompletionItem[]> GetCompletionItems(string script, int line, int column)
  {
    var request = new CompletionRequest
    {
      CursorColumn = column,
      CursorLine = line,
      Script = script
    };

    var completions = await _aresScriptingServiceClient.GetCompletions(request, null);
    return completions.Items.Select(item => item.ToMonacoCompletionItem()).ToArray();
  }
}


