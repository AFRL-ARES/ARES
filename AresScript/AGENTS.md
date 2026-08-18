# AGENTS.md

Project-local guidance for coding agents working in `AresScript`.

## Scope

- Keep changes focused and minimal.
- Do not revert unrelated local edits.
- Prefer consistency with existing patterns over introducing new abstractions.

## AresScript Change Rules

- If you change grammar in `ARES/AresScript/AresLang.g4`, update all affected layers in the same change:
  - Runtime behavior in `ARES/AresScript/Interpreters/AresBaseInterpreter.cs`
  - Validation behavior in `ARES/AresScript/Interpreters/AresValidationInterpreter.cs`
  - Type inference behavior in `ARES/AresScript/Interpreters/AresTypeInferenceInterpreter.cs`
  - Tests in `ARES/AresScript.Tests/Program.cs`
  - Script builder support under `ARES/AresScript/ScriptBuilding/AresScriptBuilder.cs`
- If the repo workflow requires parser/lexer generated artifacts, regenerate them when grammar changes.

## Functions and Extensions

- Keep standard/system function contracts consistent across:
  - Registration in `ARES/AresScript/StandardLibrary.cs`
  - Environment exposure in `ARES/AresScript/AresScriptEnvironment.cs`
  - Invocation/dispatch logic in interpreters
  - Validation arity/type checks
  - Completion metadata/snippets
- If adding extension functions, ensure member access validation does not incorrectly report "member not found" for valid extension-backed members.

## UI/Editor Sync

- If language tokens/operators/literals change, update Monaco tokenization in `ARES/UI/Scripts/monaco/areslang.monarch.ts`.
- Keep completion/snippet behavior aligned with scripting capabilities.

## Testing Checklist

- Run script tests after language/runtime/validation changes:
  - `dotnet test ARES/AresScript.Tests/AresScript.Tests.csproj`
- If UI-side language tooling changed, run the relevant UI build/test command when available.
- Prefer adding or updating tests with every language feature/fix.

## Error Quality

- Validation errors should include clear messages and source position (line/column) where possible.
- Maintain parity between validation-time failures and runtime failures for arity and obvious contract violations.
