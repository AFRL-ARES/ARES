import { language } from './areslang.monarch.js';
import { conf } from './areslang.language-configuration.js';
//import { editor } from 'monaco-editor';

/**
 * Registers the ARES language with Monaco Editor.
 * This should be called once before the editor is initialized with the 'ares' language.
 */
export async function registerAresLanguage(): Promise<void> {
  const monacoReady = await waitForMonaco();
  if (!monacoReady) {
    console.error('Monaco Editor is not loaded. Ensure BlazorMonaco is properly initialized.');
    return;
  }

  // Register the language ID if it hasn't been registered yet
  const languages = monaco.languages.getLanguages();
  if (!languages.some((lang: { id: string }) => lang.id === 'ares')) {
    monaco.languages.register({ id: 'ares' });
  }

  // Set the Monarch tokens provider
  monaco.languages.setMonarchTokensProvider('ares', language);

  // Set the language configuration
  monaco.languages.setLanguageConfiguration('ares', conf);

  monaco.editor.defineTheme('ares-dark', {
    base: 'vs-dark',
    inherit: true,
    rules: [
      { token: 'identifier.function', foreground: '#DCDCAA' },
      { token: 'function', foreground: '#DCDCAA' },
      { token: 'keyword.flow', foreground: '#C586C0' },
      { token: 'keyword.special', foreground: '#CE9178' },
      { token: 'variable', foreground: '#9CDCFE' },
      { token: 'namespace', foreground: '#4EC9B0' }
    ],
    colors: {}
  });
  
  monaco.editor.setTheme('ares-dark');
}

async function waitForMonaco(maxAttempts = 20, delayMs = 50): Promise<boolean> {
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    if (typeof monaco !== 'undefined') {
      return true;
    }

    await delay(delayMs);
  }

  return typeof monaco !== 'undefined';
}

function delay(ms: number): Promise<void> {
  return new Promise(resolve => window.setTimeout(resolve, ms));
}
