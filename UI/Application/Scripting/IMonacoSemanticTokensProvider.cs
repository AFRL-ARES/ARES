namespace UI.Application.Scripting;

public interface IMonacoSemanticTokensProvider
{
  SemanticToken[] GetSemanticTokens(string script);
}

public record SemanticToken(int Line, int StartColumn, int Length, string Type);

