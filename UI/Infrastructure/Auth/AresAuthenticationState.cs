namespace UI.Infrastructure.Auth;

public class AresAuthenticationState
{
  public bool Authenticated { get; set; }

  public string? Token { get; set; }

  public DateTime TokenExpiration { get; set; }

  public string? UserName { get; set; }
}
