using CompletionItemKind = BlazorMonaco.Languages.CompletionItemKind;
using MonacoCompletionItem = BlazorMonaco.Languages.CompletionItem;
using AresCompletionItem = Ares.Datamodel.Scripting.CompletionItem;
using AresCompletionItemKind = Ares.Datamodel.Scripting.CompletionItemKind;

namespace UI.JsInterops;

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

  private static CompletionItemKind MapKind(AresCompletionItemKind kind)
  {
    return kind switch
    {
      AresCompletionItemKind.Device => CompletionItemKind.Class,
      AresCompletionItemKind.Planner => CompletionItemKind.Module,
      AresCompletionItemKind.Analyzer => CompletionItemKind.Interface,
      AresCompletionItemKind.Function => CompletionItemKind.Function,
      AresCompletionItemKind.Variable => CompletionItemKind.Variable,
      AresCompletionItemKind.Struct => CompletionItemKind.Struct,
      AresCompletionItemKind.Keyword => CompletionItemKind.Keyword,
      _ => CompletionItemKind.Text
    };
  }

  private static bool IsSnippetText(string text)
  {
    return !string.IsNullOrWhiteSpace(text) && text.Contains('$', StringComparison.Ordinal);
  }
}
