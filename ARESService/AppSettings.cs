namespace ARESService;

public class AppSettings
{
  public TokensConfig? TokensConfig { get; set; }
}
public class TokensConfig
{
  public string? Issuer { get; set; }
  public string? Audience { get; set; }
  public string? Key { get; set; }
}
