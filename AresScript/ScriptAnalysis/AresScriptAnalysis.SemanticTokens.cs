using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using AresScript.Generated;

namespace AresScript.ScriptAnalysis;

public static partial class AresScriptAnalysis
{
  public static IReadOnlyList<ScriptSemanticToken> BuildSemanticTokens(string? script)
  {
    if(string.IsNullOrWhiteSpace(script))
    {
      return [];
    }

    try
    {
      var stream = new AntlrInputStream(script);
      var lexer = new AresIndentationLexer(stream);
      var tokenStream = new CommonTokenStream(lexer);
      var parser = new AresLangParser(tokenStream);
      var program = parser.program();

      var collector = new SemanticTokenCollector();
      collector.Visit(program);
      return collector.Tokens;
    }
    catch
    {
      return [];
    }
  }

  private sealed class SemanticTokenCollector : AresLangBaseVisitor<object?>
  {
    public List<ScriptSemanticToken> Tokens { get; } = [];

    public override object? VisitFunctionCall(AresLangParser.FunctionCallContext context)
    {
      var expr = context.expression();
      if(expr is AresLangParser.AtomExprContext atomExpr)
      {
        if(atomExpr.atom() is AresLangParser.IdContext id)
        {
          AddToken(id.ID().Symbol, ScriptSemanticTokenType.Function);
        }
      }
      else if(expr is AresLangParser.MemberAccessContext memberAccess)
      {
        AddToken(memberAccess.ID().Symbol, ScriptSemanticTokenType.Function);
      }

      return base.VisitFunctionCall(context);
    }

    public override object? VisitLValueId(AresLangParser.LValueIdContext context)
    {
      AddToken(context.ID().Symbol, ScriptSemanticTokenType.Variable);
      return base.VisitLValueId(context);
    }

    public override object? VisitId([NotNull] AresLangParser.IdContext context)
    {
      AddToken(context.ID().Symbol, ScriptSemanticTokenType.Variable);
      return base.VisitId(context);
    }

    private void AddToken(IToken token, ScriptSemanticTokenType type)
    {
      if(string.IsNullOrEmpty(token.Text))
      {
        return;
      }

      Tokens.Add(new ScriptSemanticToken(
        token.Line - 1,
        token.Column + 1,
        token.Text.Length,
        type));
    }
  }
}

public readonly record struct ScriptSemanticToken(
  int Line,
  int StartColumn,
  int Length,
  ScriptSemanticTokenType Type);

public enum ScriptSemanticTokenType
{
  Variable,
  Function
}
