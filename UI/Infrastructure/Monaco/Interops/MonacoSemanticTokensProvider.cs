using Antlr4.Runtime;
using Microsoft.JSInterop;
using AresScript;
using AresScript.Generated;
using Antlr4.Runtime.Misc;
using UI.Domain.Scripting;

namespace UI.Infrastructure.Monaco.Interops;

public sealed class MonacoSemanticTokensProvider : IMonacoSemanticTokensProvider
{
  [JSInvokable]
  public SemanticToken[] GetSemanticTokens(string script)
  {
    if(string.IsNullOrEmpty(script))
    {
      return Array.Empty<SemanticToken>();
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
      return collector.Tokens.ToArray();
    }
    catch
    {
      return [];
    }
  }

  private sealed class SemanticTokenCollector : AresLangBaseVisitor<object?>
  {
    public List<SemanticToken> Tokens { get; } = [];

    //public override object? VisitFunctionDecl(AresLangParser.FunctionDeclContext context)
    //{
    //  var ids = context.functionDeclaration().ID();
    //  if(ids.Length > 0)
    //  {
    //    AddToken(ids[0].Symbol, "function");
    //  }

    //  return base.VisitFunctionDecl(context);
    //}

    public override object? VisitFunctionCall(AresLangParser.FunctionCallContext context)
    {
      var expr = context.expression();
      if(expr is AresLangParser.AtomExprContext atomExpr)
      {
        if(atomExpr.atom() is AresLangParser.IdContext id)
        {
          AddToken(id.ID().Symbol, "function");
        }
      }
      else if(expr is AresLangParser.MemberAccessContext memberAccess)
      {
        AddToken(memberAccess.ID().Symbol, "function");
      }

      return base.VisitFunctionCall(context);
    }

    public override object? VisitLValueId(AresLangParser.LValueIdContext context)
    {
      AddToken(context.ID().Symbol, "variable");
      return base.VisitLValueId(context);
    }

    public override object? VisitId([NotNull] AresLangParser.IdContext context)
    {
      AddToken(context.ID().Symbol, "variable");
      return base.VisitId(context);
    }

    private void AddToken(IToken token, string type)
    {
      if(token is null || string.IsNullOrEmpty(token.Text))
      {
        return;
      }

      var line = token.Line - 1;
      var startColumn = token.Column + 1;
      var length = token.Text.Length;
      Tokens.Add(new SemanticToken(line, startColumn, length, type));
    }
  }
}
