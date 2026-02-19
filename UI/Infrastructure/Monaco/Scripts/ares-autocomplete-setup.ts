import type { CancellationToken, editor, IDisposable, languages, Position } from 'monaco-editor';
import type { DotNet } from '@microsoft/dotnet-js-interop';

let autoCompleteDisposable: IDisposable | null = null;

export function setupAutoComplete(autoCompleteService: DotNet.DotNetObject) {
  if (typeof monaco === 'undefined') {
    console.error('Monaco Editor is not loaded. Ensure BlazorMonaco is properly initialized.');
    return;
  }

  autoCompleteDisposable?.dispose();
  const autoCompleteProvider = new AresLangAutocompleteProvider(autoCompleteService);
  autoCompleteDisposable = monaco.languages.registerCompletionItemProvider('ares', autoCompleteProvider);
}

export function disposeAutoComplete() {
  autoCompleteDisposable?.dispose();
  autoCompleteDisposable = null;
}

export class AresLangAutocompleteProvider implements languages.CompletionItemProvider {
  autoCompleteService: DotNet.DotNetObject

  constructor(autoCompleteService: DotNet.DotNetObject) {
    this.autoCompleteService = autoCompleteService;
  }

  triggerCharacters?: string[] | undefined = ['.', ':'];

  provideCompletionItems(model: editor.ITextModel, position: Position, context: languages.CompletionContext, token: CancellationToken): languages.ProviderResult<languages.CompletionList> {
    return this.autoCompleteService
      .invokeMethodAsync('GetCompletionItems', model.getValue(), position.lineNumber, position.column)
      .then((suggestions) => ({ suggestions: suggestions as languages.CompletionItem[] }));
  }

  resolveCompletionItem?(item: languages.CompletionItem, token: CancellationToken): languages.ProviderResult<languages.CompletionItem> {
    return item;
  }
}
