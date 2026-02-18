using Antlr4.Runtime;
using Ares.Datamodel.Scripting;
using AresScript.Generated;
using AresScript.Interpreters;

namespace AresScript.ScriptAnalysis;

public static partial class AresScriptAnalysis
{
  public static async Task<(AresFunctionInvocation[] Invocations, Diagnostic[] Diagnostics)> ValidateAndCollectInvocationsAsync(
    string? script,
    AresScriptEnvironment environment,
    AresValidationInterpreter.ValidationMode mode = AresValidationInterpreter.ValidationMode.Strict,
    bool traverseFunctionDeclarationBodies = true)
  {
    var diagnostics = new List<Diagnostic>();
    var invocations = Array.Empty<AresFunctionInvocation>();

    var stream = new AntlrInputStream(script ?? string.Empty);
    var lexer = new AresIndentationLexer(stream);
    var lexerListener = new CollectingLexerErrorListener(diagnostics);
    lexer.RemoveErrorListeners();
    lexer.AddErrorListener(lexerListener);

    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    var parserListener = new CollectingParserErrorListener(diagnostics);
    parser.RemoveErrorListeners();
    parser.AddErrorListener(parserListener);

    AresLangParser.ProgramContext? programCtx = null;
    try
    {
      programCtx = parser.program();
    }
    catch(Exception ex)
    {
      AppendDiagnosticFromException(diagnostics, ex);
    }

    if(programCtx is not null)
    {
      var validator = new AresValidationInterpreter(
        environment,
        mode,
        line: null,
        traverseFunctionDeclarationBodies: traverseFunctionDeclarationBodies);
      try
      {
        await validator.Visit(programCtx);
        invocations = validator.FunctionInvocations.ToArray();
      }
      catch(Exception ex)
      {
        AppendDiagnosticFromException(diagnostics, ex);
        invocations = validator.FunctionInvocations.ToArray();
      }
    }

    return (invocations, diagnostics.ToArray());
  }
}
