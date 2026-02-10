using MonacoCompletionItem = BlazorMonaco.Languages.CompletionItem;

namespace UI.Domain.Scripting;

public interface IMonacoCompletionProvider
{
  Task<MonacoCompletionItem[]> GetCompletionItems(string script, int line, int column);
}
