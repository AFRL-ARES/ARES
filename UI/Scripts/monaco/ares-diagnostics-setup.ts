import type { DotNet } from '@microsoft/dotnet-js-interop';

type MonacoDiagnostic = {
  startLineNumber: number;
  startColumn: number;
  endLineNumber: number;
  endColumn: number;
  message: string;
  severity: number;
  code?: string | null;
};

export function setupDiagnostics(diagnosticsService: DotNet.DotNetObject, debounceMs = 250) {
  if (typeof monaco === 'undefined') {
    console.error('Monaco Editor is not loaded. Ensure BlazorMonaco is properly initialized.');
    return;
  }

  const model = monaco.editor.getModels().find(m => m.getLanguageId() === 'ares');
  if (!model) {
    console.warn('No ARES Monaco model found for diagnostics.');
    return;
  }

  const updateMarkers = () => {
    diagnosticsService
      .invokeMethodAsync('GetDiagnostics', model.getValue())
      .then((diagnostics) => {
        const markers = (diagnostics as MonacoDiagnostic[]).map(d => ({
          startLineNumber: d.startLineNumber,
          startColumn: d.startColumn,
          endLineNumber: d.endLineNumber,
          endColumn: d.endColumn,
          message: d.message,
          severity: mapSeverity(d.severity),
          source: d.code || 'Ares'
        }));

        monaco.editor.setModelMarkers(model, 'ares', markers);
      })
      .catch((err) => console.error('Failed to fetch diagnostics', err));
  };

  const debouncedUpdate = debounce(updateMarkers, debounceMs);
  const disposable = model.onDidChangeContent(() => debouncedUpdate());

  updateMarkers();

  model.onWillDispose(() => {
    disposable.dispose();
    monaco.editor.setModelMarkers(model, 'ares', []);
  });
}

function mapSeverity(severity: number): number {
  switch (severity) {
    case 0: // Error
      return monaco.MarkerSeverity.Error;
    case 1: // Warning
      return monaco.MarkerSeverity.Warning;
    case 2: // Info
      return monaco.MarkerSeverity.Info;
    case 3: // Hint
      return monaco.MarkerSeverity.Hint;
    default:
      return monaco.MarkerSeverity.Error;
  }
}

function debounce(fn: () => void, waitMs: number) {
  let timeout: number | undefined;
  return () => {
    if (timeout !== undefined) {
      clearTimeout(timeout);
    }
    timeout = window.setTimeout(() => fn(), waitMs);
  };
}
