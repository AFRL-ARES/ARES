namespace UI.Application.Scripting;

public interface IMonacoHoverProvider
{
  Task<string?> GetHoverText(string script, int line, int column, string identifier);
}

