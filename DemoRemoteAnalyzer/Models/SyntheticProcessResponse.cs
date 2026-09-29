using DemoRemoteAnalyzer.Tools;

namespace DemoRemoteAnalyzer.Models;

/// <summary>
/// Generates a synthetic process response space using Gaussian peaks/valleys
/// and Perlin noise across N dimensions.
/// </summary>
public class SyntheticProcessResponse
{
  private readonly Pcg64 _rng;
  private readonly Dictionary<string, (double Low, double High)> _paramBounds;
  private readonly (double Min, double Max) _outputBounds;
  private readonly (double Min, double Max) _responseBounds;
  private readonly List<string> _paramNames;
  private readonly int _dims;
  private readonly double _noiseScale;
  private readonly double _noiseFreq;
  private readonly List<GaussianSpec> _gaussians;
  private readonly double[] _noiseOffset;
  private double _rawMin = 0.0;
  private double _rawMax = 1.0;

  public SyntheticProcessResponse(IDictionary<string, (double Low, double High)> paramBounds,
    (double Min, double Max)? outputBounds = null,
    int numGaussians = 5,
    double noiseScale = 0.1,
    double noiseFrequency = 2.0,
    (double Min, double Max)? responseBounds = null,
    ulong? seed = null,
    int calibrationSamples = 10000)
  {
    ValidateConstructorArguments(
        paramBounds,
        numGaussians,
        noiseScale,
        noiseFrequency,
        calibrationSamples);

    _rng = seed.HasValue ? new Pcg64(seed.Value) : new Pcg64((ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    _paramBounds = new Dictionary<string, (double Low, double High)>(paramBounds);
    _outputBounds = outputBounds ?? (0.0, 1.0);
    _responseBounds = responseBounds ?? (0.0, 1.0);
    _noiseScale = noiseScale;
    _noiseFreq = noiseFrequency;

    ValidateRange("Output", _outputBounds.Min, _outputBounds.Max);
    ValidateRange("Response", _responseBounds.Min, _responseBounds.Max);

    foreach(var entry in _paramBounds)
      ValidateRange(entry.Key, entry.Value.Low, entry.Value.High);

    _paramNames = _paramBounds.Keys.OrderBy(key => key, StringComparer.Ordinal).ToList();

    _dims = _paramNames.Count;

    _gaussians = new List<GaussianSpec>();

    // Generate Gaussian peaks and valleys.
    for(int gaussianIndex = 0; gaussianIndex < numGaussians; gaussianIndex++)
    {
      var center = new double[_dims];
      var bandwidths = new double[_dims];

      for(int dimension = 0; dimension < _dims; dimension++)
      {
        var parameterName = _paramNames[dimension];
        var bounds = _paramBounds[parameterName];

        center[dimension] = NextUniform(_rng, bounds.Low, bounds.High);

        bandwidths[dimension] = (bounds.High - bounds.Low) * NextUniform(_rng, 0.05, 0.25);
      }

      var amplitude = NextUniform(_rng, -1.0, 2.0);

      _gaussians.Add(new GaussianSpec(center, bandwidths, amplitude));
    }

    // Generate random Perlin-noise offsets.
    _noiseOffset = new double[_dims];

    for(int dimension = 0; dimension < _dims; dimension++)
    {
      _noiseOffset[dimension] = NextUniform(_rng, 0.0, 100.0);
    }

    CalibrateBounds(calibrationSamples);
  }

  /// <summary>
  /// Convenience constructor supporting dictionary values as [low, high] lists.
  /// </summary>
  public SyntheticProcessResponse(IDictionary<string, List<double>> paramBounds,
    IList<double>? outputBounds = null,
    int numGaussians = 5,
    double noiseScale = 0.1,
    double noiseFrequency = 2.0,
    ulong? seed = null,
    int calibrationSamples = 10000) : this(ConvertParameterBounds(paramBounds), 
      ConvertOutputBounds(outputBounds),
      numGaussians,
      noiseScale,
      noiseFrequency,
      (0.0, 1.0),
      seed,
      calibrationSamples)
  {
  }

  /// <summary>
  /// Computes the unscaled response for a point.
  /// </summary>
  private double RawEvaluate(double[] point)
  {
    double gaussianSum = 0.0;

    foreach(var gaussian in _gaussians)
    {
      double exponentSum = 0.0;

      for(int dimension = 0; dimension < _dims; dimension++)
      {
        double difference = point[dimension] - gaussian.Center[dimension];
        double width = 2.0 * gaussian.Bandwidth[dimension] * gaussian.Bandwidth[dimension];
        exponentSum += (difference * difference) / width;
      }

      gaussianSum += gaussian.Amplitude * Math.Exp(-exponentSum);
    }

    var normalizedPoint = new double[_dims];

    for(int dimension = 0; dimension < _dims; dimension++)
    {
      var parameterName = _paramNames[dimension];
      var bounds = _paramBounds[parameterName];

      normalizedPoint[dimension] = ((point[dimension] - bounds.Low) / (bounds.High - bounds.Low)) * _noiseFreq + _noiseOffset[dimension];
    }

    double noiseValue;

    if(_dims == 1)
      noiseValue = PerlinNoise.PNoise1(normalizedPoint[0]);
    
    else if(_dims == 2)
      noiseValue = PerlinNoise.PNoise2(normalizedPoint[0], normalizedPoint[1]);

    else if(_dims == 3)
      noiseValue = PerlinNoise.PNoise3(normalizedPoint[0], normalizedPoint[1], normalizedPoint[2]);
    
    else
    {
      double tailSum = 0.0;

      for(int dimension = 2; dimension < _dims; dimension++)
        tailSum += normalizedPoint[dimension];

      noiseValue = PerlinNoise.PNoise3(normalizedPoint[0], normalizedPoint[1], tailSum);
    }

    return gaussianSum + noiseValue * _noiseScale;
  }

  /// <summary>
  /// Samples the parameter space to estimate the global response bounds.
  /// </summary>
  private void CalibrateBounds(int numSamples)
  {
    double rawMin = double.MaxValue;
    double rawMax = double.MinValue;
    var samplePoint = new double[_dims];

    // This follows the same broad sampling strategy as the Python implementation.
    for(int sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
    {
      for(int dimension = 0; dimension < _dims; dimension++)
      {
        var parameterName = _paramNames[dimension];
        var bounds = _paramBounds[parameterName];

        samplePoint[dimension] = NextUniform(_rng, bounds.Low, bounds.High);
      }

      var value = RawEvaluate(samplePoint);

      if(!double.IsFinite(value))
        throw new InvalidOperationException("Calibration produced a nonfinite response.");

      if(value < rawMin)
        rawMin = value;

      if(value > rawMax)
        rawMax = value;
    }

    _rawMin = rawMin;
    _rawMax = rawMax;

    // Prevent division by zero for a flat response surface.
    if(Math.Abs(_rawMax - _rawMin) <= 1e-12 * Math.Max(1.0, Math.Max(Math.Abs(_rawMin), Math.Abs(_rawMax))))
      _rawMax = _rawMin + 1e-9;
  }

  /// <summary>
  /// Evaluates and scales a point into the configured output bounds.
  /// </summary>
  public double Evaluate(IDictionary<string, double> paramsDict)
  {
    if(paramsDict is null)
      throw new ArgumentNullException(nameof(paramsDict));

    var point = new double[_dims];

    for(int dimension = 0; dimension < _dims; dimension++)
    {
      var parameterName = _paramNames[dimension];

      if(!paramsDict.TryGetValue(parameterName, out var value))
        throw new KeyNotFoundException($"Missing parameter in input: '{parameterName}'");

      if(!double.IsFinite(value))
        throw new ArgumentException($"Parameter '{parameterName}' must be finite.");

      point[dimension] = value;
    }

    var rawValue = RawEvaluate(point);

    if(!double.IsFinite(rawValue))
      throw new InvalidOperationException("Response evaluation produced a nonfinite value.");
    

    var targetMinimum = _outputBounds.Min;
    var targetMaximum = _outputBounds.Max;

    var scaledValue = targetMinimum + ((rawValue - _rawMin) * (targetMaximum - targetMinimum)) / (_rawMax - _rawMin);

    return Math.Clamp(scaledValue, targetMinimum, targetMaximum);
  }

  private static Dictionary<string, (double Low, double High)> ConvertParameterBounds(IDictionary<string, List<double>> paramBounds)
  {
    if(paramBounds is null)
      throw new ArgumentNullException(nameof(paramBounds));

    var convertedBounds = new Dictionary<string, (double Low, double High)>();

    foreach(var entry in paramBounds)
    {
      if(entry.Value is null || entry.Value.Count < 2)
        throw new ArgumentException($"Bounds for '{entry.Key}' must contain [low, high].", nameof(paramBounds));

      convertedBounds[entry.Key] = (entry.Value[0], entry.Value[1]);
    }

    return convertedBounds;
  }

  private static (double Min, double Max) ConvertOutputBounds(IList<double>? outputBounds)
  {
    if(outputBounds is null)
      return (0.0, 1.0);

    if(outputBounds.Count < 2)
      throw new ArgumentException("Output bounds must contain [low, high].", nameof(outputBounds));
    
    return (outputBounds[0], outputBounds[1]);
  }

  private static void ValidateConstructorArguments(
      IDictionary<string, (double Low, double High)> paramBounds,
      int numGaussians,
      double noiseScale,
      double noiseFrequency,
      int calibrationSamples)
  {
    if(paramBounds is null || paramBounds.Count == 0)
      throw new ArgumentException("At least one parameter bound is required.", nameof(paramBounds));
    

    if(numGaussians < 1)
      throw new ArgumentOutOfRangeException(nameof(numGaussians));

    if(calibrationSamples < 1)
      throw new ArgumentOutOfRangeException(nameof(calibrationSamples));

    if(!double.IsFinite(noiseScale) || noiseScale < 0.0)
      throw new ArgumentOutOfRangeException(nameof(noiseScale));

    if(!double.IsFinite(noiseFrequency) || noiseFrequency <= 0.0)
      throw new ArgumentOutOfRangeException(nameof(noiseFrequency));
  }

  private static void ValidateRange(string name, double minimum, double maximum)
  {
    if(!double.IsFinite(minimum) || !double.IsFinite(maximum) || minimum >= maximum)
      throw new ArgumentException($"Bounds for '{name}' must contain two finite values in ascending order.");
  }

  private static double NextUniform(Pcg64 rng, double minimum, double maximum)
    => rng.NextDouble() * (maximum - minimum) + minimum;
}

/// <summary>
/// Pure C# implementation of Improved Perlin Noise (matches Python `noise` module behavior).
/// </summary>
public static class PerlinNoise
{
  private static readonly int[] P = new int[512];

  private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
  private static double Lerp(double t, double a, double b) => a + t * (b - a);

  private static double Grad(int hash, double x)
  {
    return (hash & 1) == 0 ? x : -x;
  }

  private static double Grad(int hash, double x, double y)
  {
    int h = hash & 7;
    double u = h < 4 ? x : y;
    double v = h < 4 ? y : x;
    return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
  }

  private static double Grad(int hash, double x, double y, double z)
  {
    int h = hash & 15;
    double u = h < 8 ? x : y;
    double v = h < 4 ? y : h == 12 || h == 14 ? x : z;
    return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
  }

  public static double PNoise1(double x)
  {
    int X = (int)Math.Floor(x) & 255;
    x -= Math.Floor(x);
    double u = Fade(x);
    return Lerp(u, Grad(P[X], x), Grad(P[X + 1], x - 1));
  }

  public static double PNoise2(double x, double y)
  {
    int X = (int)Math.Floor(x) & 255;
    int Y = (int)Math.Floor(y) & 255;

    x -= Math.Floor(x);
    y -= Math.Floor(y);

    double u = Fade(x);
    double v = Fade(y);

    int A = P[X] + Y;
    int B = P[X + 1] + Y;

    return Lerp(v,
        Lerp(u, Grad(P[A], x, y), Grad(P[B], x - 1, y)),
        Lerp(u, Grad(P[A + 1], x, y - 1), Grad(P[B + 1], x - 1, y - 1)));
  }

  public static double PNoise3(double x, double y, double z)
  {
    int X = (int)Math.Floor(x) & 255;
    int Y = (int)Math.Floor(y) & 255;
    int Z = (int)Math.Floor(z) & 255;

    x -= Math.Floor(x);
    y -= Math.Floor(y);
    z -= Math.Floor(z);

    double u = Fade(x);
    double v = Fade(y);
    double w = Fade(z);

    int A = P[X] + Y;
    int AA = P[A] + Z;
    int AB = P[A + 1] + Z;
    int B = P[X + 1] + Y;
    int BA = P[B] + Z;
    int BB = P[B + 1] + Z;

    return Lerp(w,
        Lerp(v,
            Lerp(u, Grad(P[AA], x, y, z), Grad(P[BA], x - 1, y, z)),
            Lerp(u, Grad(P[AB], x, y - 1, z), Grad(P[BB], x - 1, y - 1, z))),
        Lerp(v,
            Lerp(u, Grad(P[AA + 1], x, y, z - 1), Grad(P[BA + 1], x - 1, y, z - 1)),
            Lerp(u, Grad(P[AB + 1], x, y - 1, z - 1), Grad(P[BB + 1], x - 1, y - 1, z - 1))));
  }
}
