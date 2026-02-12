using MonacoCompletionItem = BlazorMonaco.Languages.CompletionItem;

namespace UI.Application.Scripting;

public interface IMonacoCompletionProvider
{
  Task<MonacoCompletionItem[]> GetCompletionItems(string script, int line, int column);
}

