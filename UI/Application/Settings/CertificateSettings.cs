namespace UI.Application.Settings;

internal record CertificateSettings
{
  public string? Path { get; set; }
  public string? Password { get; set; }
}
