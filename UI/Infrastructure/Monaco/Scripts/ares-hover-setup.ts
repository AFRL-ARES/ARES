import type { CancellationToken, editor, IDisposable, languages, Position } from 'monaco-editor';
import type { DotNet } from '@microsoft/dotnet-js-interop';

let hoverDisposable: IDisposable | null = null;

export function setupHover(hoverService: DotNet.DotNetObject) {
  if (typeof monaco === 'undefined') {
    console.error('Monaco Editor is not loaded. Ensure BlazorMonaco is properly initialized.');
    return;
  }

  hoverDisposable?.dispose();
  const provider = new AresLangHoverProvider(hoverService);
  hoverDisposable = monaco.languages.registerHoverProvider('ares', provider);
}

export function disposeHover() {
  hoverDisposable?.dispose();
  hoverDisposable = null;
}

class AresLangHoverProvider implements languages.HoverProvider {
  private readonly hoverService: DotNet.DotNetObject;

  constructor(hoverService: DotNet.DotNetObject) {
    this.hoverService = hoverService;
  }

  provideHover(
    model: editor.ITextModel,
    position: Position,
    token: CancellationToken
  ): languages.ProviderResult<languages.Hover> {
    const word = model.getWordAtPosition(position);
    if (!word || !word.word) {
      return null;
    }

    return this.hoverService
      .invokeMethodAsync('GetHoverText', model.getValue(), position.lineNumber, position.column, word.word)
      .then((hoverText) => {
        if (!hoverText) {
          return null;
        }

        return {
          range: new monaco.Range(position.lineNumber, word.startColumn, position.lineNumber, word.endColumn),
          contents: [{ value: hoverText as string }]
        };
      });
  }
}
