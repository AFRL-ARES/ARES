using Ares.Datamodel.Scripting;
using AresCompletionItem = Ares.Datamodel.Scripting.CompletionItem;
using CompletionItemKind = BlazorMonaco.Languages.CompletionItemKind;
using MonacoCompletionItem = BlazorMonaco.Languages.CompletionItem;

namespace UI.Infrastructure.Monaco.Interops;

public static class CompletionItemExtensions
{
  public static MonacoCompletionItem ToMonacoCompletionItem(this AresCompletionItem completionItem)
  {
    ArgumentNullException.ThrowIfNull(completionItem);

    var label = completionItem.Label ?? string.Empty;
    var insertText = string.IsNullOrWhiteSpace(completionItem.InsertText) ? label : completionItem.InsertText;

    var item = new MonacoCompletionItem
    {
      LabelAsString = label,
      InsertText = insertText,
      Detail = completionItem.Detail ?? string.Empty,
      DocumentationAsString = completionItem.Documentation ?? string.Empty,
      Kind = MapKind(completionItem.Kind),
      FilterText = label,
      SortText = label,
    };

    if(IsSnippetText(insertText))
    {
      item.InsertTextRules = BlazorMonaco.Languages.CompletionItemInsertTextRule.InsertAsSnippet;
    }

    return item;
  }

  private static CompletionItemKind MapKind(SymbolKind kind)
  {
    return kind switch
    {
      SymbolKind.Device => CompletionItemKind.Class,
      SymbolKind.Planner => CompletionItemKind.Module,
      SymbolKind.Analyzer => CompletionItemKind.Interface,
      SymbolKind.Function => CompletionItemKind.Function,
      SymbolKind.Variable => CompletionItemKind.Variable,
      SymbolKind.Struct => CompletionItemKind.Struct,
      SymbolKind.Keyword => CompletionItemKind.Keyword,
      SymbolKind.Type => CompletionItemKind.TypeParameter,
      _ => CompletionItemKind.Text
    };
  }

  private static bool IsSnippetText(string text)
  {
    return !string.IsNullOrWhiteSpace(text) && text.Contains('$', StringComparison.Ordinal);
  }
}
