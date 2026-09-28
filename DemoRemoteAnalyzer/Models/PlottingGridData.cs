namespace DemoRemoteAnalyzer.Models;

/// <summary>
/// Holds the calculated meshgrid points and evaluated values for plotting.
/// </summary>
public class PlottingGridData
{
  public List<double[]> AxisPoints { get; set; } = new List<double[]>();
  public int[] Shape { get; set; } = [];
  public double[] EvaluatedValues { get; set; } = [];
}
