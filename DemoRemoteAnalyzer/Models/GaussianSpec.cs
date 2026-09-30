namespace DemoRemoteAnalyzer.Models;

/// <summary>
/// Represents a Gaussian bell curve peak/valley in N-dimensional space.
/// </summary>
public class GaussianSpec
{
  public double[] Center { get; set; }
  public double[] Bandwidth { get; set; }
  public double Amplitude { get; set; }

  public GaussianSpec(double[] center, double[] bandwidth, double amplitude)
  {
    Center = center;
    Bandwidth = bandwidth;
    Amplitude = amplitude;
  }
}