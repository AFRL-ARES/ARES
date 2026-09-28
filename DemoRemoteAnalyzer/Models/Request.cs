namespace DemoRemoteAnalyzer.Models;

public class Request
{
  public Dictionary<string, double> Inputs { get; set; } = new Dictionary<string, double>();
  public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
}
