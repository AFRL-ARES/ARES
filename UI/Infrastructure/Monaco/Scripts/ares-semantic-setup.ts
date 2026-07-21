import type { editor, IDisposable, languages } from 'monaco-editor';
import type { DotNet } from '@microsoft/dotnet-js-interop';

type SemanticToken = {
  line: number;
  startColumn: number;
  length: number;
  type: string;
  modifiers?: string[] | null;
};

const legend: languages.SemanticTokensLegend = {
  tokenTypes: ['function', 'variable', 'namespace'],
  tokenModifiers: []
};

let semanticTokensDisposable: IDisposable | null = null;
const semanticTokenListeners = new Set<() => void>();

export function setupSemanticTokens(provider: DotNet.DotNetObject) {
  if (typeof monaco === 'undefined') {
    console.error('Monaco Editor is not loaded. Ensure BlazorMonaco is properly initialized.');
    return;
  }

  semanticTokensDisposable?.dispose();
  semanticTokensDisposable = monaco.languages.registerDocumentSemanticTokensProvider('ares', {
    getLegend() {
      return legend;
    },
    onDidChange(listener) {
      semanticTokenListeners.add(listener);
      return {
        dispose() {
          semanticTokenListeners.delete(listener);
        }
      };
    },
    provideDocumentSemanticTokens(model: editor.ITextModel) {
      return provider.invokeMethodAsync('GetSemanticTokens', model.getValue())
        .then((tokens) => {
          const data = encodeSemanticTokens(model, tokens as SemanticToken[]);
          return { data };
        });
    },
    releaseDocumentSemanticTokens() {
      // no-op
    }
  });
}

export function refreshSemanticTokens() {
  semanticTokenListeners.forEach(listener => listener());
}

export function disposeSemanticTokens() {
  semanticTokensDisposable?.dispose();
  semanticTokensDisposable = null;
  semanticTokenListeners.clear();
}

function encodeSemanticTokens(model: editor.ITextModel, tokens: SemanticToken[]): Uint32Array {
  const sorted = tokens
    .filter(t => t.length > 0 && t.line >= 0 && t.line < model.getLineCount() && t.startColumn >= 0)
    .sort((a, b) => a.line === b.line ? a.startColumn - b.startColumn : a.line - b.line);

  const data: number[] = [];
  let lastLine = 0;
  let lastChar = 0;

  for (const token of sorted) {
    const line = token.line;
    const start = token.startColumn;
    const lineLength = model.getLineLength(line + 1);
    if (start >= lineLength) {
      continue;
    }

    const length = Math.min(token.length, lineLength - start);
    const tokenType = legend.tokenTypes.indexOf(token.type);
    if (tokenType < 0) {
      continue;
    }

    const lineDelta = line - lastLine;
    const charDelta = lineDelta === 0 ? start - lastChar : start;
    data.push(lineDelta, charDelta, length, tokenType, 0);

    lastLine = line;
    lastChar = start;
  }

  return new Uint32Array(data);
}
