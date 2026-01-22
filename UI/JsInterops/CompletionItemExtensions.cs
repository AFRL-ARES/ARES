namespace UI.JsInterops;

using System;
using Ares.Services;
using CompletionItemKind = BlazorMonaco.Languages.CompletionItemKind;
using MonacoCompletionItem = BlazorMonaco.Languages.CompletionItem;

public static class CompletionItemExtensions
{
  public static MonacoCompletionItem ToMonacoCompletionItem(this CompletionItem completionItem)
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

  private static CompletionItemKind MapKind(Ares.Services.CompletionItemKind kind)
  {
    return kind switch
    {
      Ares.Services.CompletionItemKind.Device => CompletionItemKind.Class,
      Ares.Services.CompletionItemKind.Planner => CompletionItemKind.Module,
      Ares.Services.CompletionItemKind.Analyzer => CompletionItemKind.Interface,
      Ares.Services.CompletionItemKind.Function => CompletionItemKind.Function,
      Ares.Services.CompletionItemKind.Variable => CompletionItemKind.Variable,
      Ares.Services.CompletionItemKind.Struct => CompletionItemKind.Struct,
      Ares.Services.CompletionItemKind.Keyword => CompletionItemKind.Keyword,
      _ => CompletionItemKind.Text
    };
  }

  private static bool IsSnippetText(string text)
  {
    return !string.IsNullOrWhiteSpace(text) && text.Contains('$', StringComparison.Ordinal);
  }
}
