using Antlr4.Runtime;
using Ares.Datamodel.Scripting;
using AresScript.Environment;
using AresScript.Generated;
using AresScript.Interpreters;

namespace AresScript.ScriptAnalysis;

public static partial class AresScriptAnalysis
{
  public static async Task<Diagnostic[]> ValidateScriptAsync(
    string? script,
    AresScriptEnvironment environment,
    AresValidationInterpreter.ValidationMode mode = AresValidationInterpreter.ValidationMode.Strict)
  {
    var diagnostics = new List<Diagnostic>();

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
      var validator = new AresValidationInterpreter(environment, mode);
      try
      {
        await validator.Visit(programCtx);
      }
      catch(Exception ex)
      {
        AppendDiagnosticFromException(diagnostics, ex);
      }
    }

    return diagnostics.ToArray();
  }

  private static void AppendDiagnosticFromException(ICollection<Diagnostic> diagnostics, Exception ex)
  {
    var message = ex.Message ?? "Validation error";
    var line = 1;
    var column = 1;

    if(ex is AresInterpreterException interpreterException)
    {
      line = interpreterException.Line > 0 ? interpreterException.Line : 1;
      column = interpreterException.Column > 0 ? interpreterException.Column : 1;
      message = interpreterException.DetailMessage;
    }

    diagnostics.Add(new Diagnostic
    {
      StartLine = line,
      StartColumn = column,
      EndLine = line,
      EndColumn = column,
      Message = message,
      Severity = DiagnosticSeverity.Error,
      Code = string.Empty
    });
  }

  private sealed class CollectingLexerErrorListener : IAntlrErrorListener<int>
  {
    private readonly ICollection<Diagnostic> _diagnostics;

    public CollectingLexerErrorListener(ICollection<Diagnostic> diagnostics)
    {
      _diagnostics = diagnostics;
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,
      string msg, RecognitionException e)
    {
      _diagnostics.Add(CreateDiagnostic(line, charPositionInLine, msg));
    }
  }

  private sealed class CollectingParserErrorListener : BaseErrorListener
  {
    private readonly ICollection<Diagnostic> _diagnostics;

    public CollectingParserErrorListener(ICollection<Diagnostic> diagnostics)
    {
      _diagnostics = diagnostics;
    }

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
      string msg, RecognitionException e)
    {
      _diagnostics.Add(CreateDiagnostic(line, charPositionInLine, msg, offendingSymbol));
    }
  }

  private static Diagnostic CreateDiagnostic(int line, int charPositionInLine, string message, IToken? offendingSymbol = null)
  {
    var startColumn = Math.Max(1, charPositionInLine + 1);
    var tokenLength = offendingSymbol?.Text?.Length ?? 1;
    var endColumn = Math.Max(startColumn, startColumn + tokenLength - 1);

    return new Diagnostic
    {
      StartLine = Math.Max(1, line),
      StartColumn = startColumn,
      EndLine = Math.Max(1, line),
      EndColumn = endColumn,
      Message = message,
      Severity = DiagnosticSeverity.Error,
      Code = string.Empty
    };
  }
}
