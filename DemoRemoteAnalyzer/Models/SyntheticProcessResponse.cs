namespace DemoRemoteAnalyzer.Models;

/// <summary>
/// Generates a synthetic process response space using Gaussian peaks/valleys 
/// and Perlin noise across N dimensions.
/// </summary>
public class SyntheticProcessResponse
{
  private readonly Random _rng;
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
  private PlottingGridData _plottingGrid = null;

  public IReadOnlyList<string> ParamNames => _paramNames.AsReadOnly();
  public int Dimensions => _dims;
  public double RawMin => _rawMin;
  public double RawMax => _rawMax;

  public SyntheticProcessResponse(
      IDictionary<string, (double Low, double High)> paramBounds,
      (double Min, double Max)? outputBounds = null,
      int numGaussians = 5,
      double noiseScale = 0.1,
      double noiseFrequency = 2.0,
      (double Min, double Max)? responseBounds = null,
      int? seed = null,
      int calibrationSamples = 10000)
  {
    _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    _paramBounds = new Dictionary<string, (double Low, double High)>(paramBounds);
    _outputBounds = outputBounds ?? (0.0, 1.0);
    _responseBounds = responseBounds ?? (0.0, 1.0);
    _noiseScale = noiseScale;
    _noiseFreq = noiseFrequency;

    _paramNames = _paramBounds.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
    _dims = _paramNames.Count;

    // --- Generate Random Gaussians ---
    _gaussians = new List<GaussianSpec>();

    for(int g = 0; g < numGaussians; g++)
    {
      double[] center = new double[_dims];
      double[] bandwidths = new double[_dims];

      for(int i = 0; i < _dims; i++)
      {
        string p = _paramNames[i];
        var bounds = _paramBounds[p];

        center[i] = NextUniform(_rng, bounds.Low, bounds.High);
        bandwidths[i] = (bounds.High - bounds.Low) * NextUniform(_rng, 0.1, 0.5);
      }

      double amplitude = NextUniform(_rng, -1.0, 2.0);
      _gaussians.Add(new GaussianSpec(center, bandwidths, amplitude));
    }

    // Random offset for Perlin noise across dimensions
    _noiseOffset = new double[_dims];
    for(int i = 0; i < _dims; i++)
    {
      _noiseOffset[i] = NextUniform(_rng, 0.0, 100.0);
    }

    // Calibrate output scale
    CalibrateBounds(calibrationSamples);
  }

  /// <summary>
  /// Convenience constructor supporting dictionary values as lists/arrays [low, high].
  /// </summary>
  public SyntheticProcessResponse(
      IDictionary<string, List<double>> paramBounds,
      IList<double> outputBounds = null,
      int numGaussians = 5,
      double noiseScale = 0.1,
      double noiseFrequency = 2.0,
      long? seed = null,
      int calibrationSamples = 10000)
      : this(
          paramBounds.ToDictionary(k => k.Key, v => (v.Value[0], v.Value[1])),
          outputBounds != null && outputBounds.Count >= 2 ? (outputBounds[0], outputBounds[1]) : (0.0, 1.0),
          numGaussians,
          noiseScale,
          noiseFrequency,
          (0.0, 1.0),
          seed.HasValue ? (int)(seed.Value % int.MaxValue) : null,
          calibrationSamples)
  {
  }

  /// <summary>
  /// Internal method to compute the unscaled response for an N-dimensional point array.
  /// </summary>
  private double RawEvaluate(double[] point)
  {
    // 1. Gaussian Component
    double gaussianSum = 0.0;
    foreach(var g in _gaussians)
    {
      double exponentSum = 0.0;
      for(int i = 0; i < _dims; i++)
      {
        double diff = point[i] - g.Center[i];
        double diffSq = diff * diff;
        double width = 2.0 * (g.Bandwidth[i] * g.Bandwidth[i]);
        exponentSum += diffSq / width;
      }
      gaussianSum += g.Amplitude * Math.Exp(-exponentSum);
    }

    // 2. Perlin Noise Component
    double[] normPoint = new double[_dims];
    for(int i = 0; i < _dims; i++)
    {
      string pName = _paramNames[i];
      var (low, high) = _paramBounds[pName];
      double normVal = ((point[i] - low) / (high - low)) * _noiseFreq;
      normPoint[i] = normVal + _noiseOffset[i];
    }

    double noiseVal;
    if(_dims == 1)
    {
      noiseVal = PerlinNoise.PNoise1(normPoint[0]);
    }
    else if(_dims == 2)
    {
      noiseVal = PerlinNoise.PNoise2(normPoint[0], normPoint[1]);
    }
    else if(_dims == 3)
    {
      noiseVal = PerlinNoise.PNoise3(normPoint[0], normPoint[1], normPoint[2]);
    }
    else
    {
      double tailSum = 0.0;
      for(int i = 2; i < _dims; i++)
      {
        tailSum += normPoint[i];
      }
      noiseVal = PerlinNoise.PNoise3(normPoint[0], normPoint[1], tailSum);
    }

    return gaussianSum + (noiseVal * _noiseScale);
  }

  /// <summary>
  /// Samples the parameter space to estimate global empirical min and max.
  /// </summary>
  private void CalibrateBounds(int numSamples)
  {
    double rawMin = double.MaxValue;
    double rawMax = double.MinValue;

    double[] samplePoint = new double[_dims];

    for(int s = 0; s < numSamples; s++)
    {
      for(int i = 0; i < _dims; i++)
      {
        string pName = _paramNames[i];
        var (low, high) = _paramBounds[pName];
        samplePoint[i] = NextUniform(_rng, low, high);
      }

      double val = RawEvaluate(samplePoint);
      if(val < rawMin) rawMin = val;
      if(val > rawMax) rawMax = val;
    }

    _rawMin = rawMin;
    _rawMax = rawMax;

    if(Math.Abs(_rawMax - _rawMin) < 1e-12)
    {
      _rawMax = _rawMin + 1e-9;
    }
  }

  /// <summary>
  /// Queries the synthetic space and returns a scaled, bound-clipped response.
  /// </summary>
  public double Evaluate(IDictionary<string, double> paramsDict)
  {
    double[] point = new double[_dims];
    for(int i = 0; i < _dims; i++)
    {
      string pName = _paramNames[i];
      if(!paramsDict.TryGetValue(pName, out double val))
      {
        throw new KeyNotFoundException($"Missing parameter in input: '{pName}'");
      }
      point[i] = val;
    }

    // 1. Get raw response
    double rawVal = RawEvaluate(point);

    // 2. Scale to target bounds
    double tMin = _outputBounds.Min;
    double tMax = _outputBounds.Max;
    double scaledVal = tMin + ((rawVal - _rawMin) * (tMax - tMin)) / (_rawMax - _rawMin);

    // 3. Clip the output
    return Math.Clamp(scaledVal, tMin, tMax);
  }

  /// <summary>
  /// Generates or returns cached N-dimensional grid evaluation results.
  /// </summary>
  public PlottingGridData PlottingMeshgrid(int pointsPerAxis = 100)
  {
    if(_plottingGrid != null)
      return _plottingGrid;

    var axisPoints = new List<double[]>();
    int[] shape = new int[_dims];

    for(int i = 0; i < _dims; i++)
    {
      string pName = _paramNames[i];
      var (low, high) = _paramBounds[pName];
      axisPoints.Add(LinSpace(low, high, pointsPerAxis));
      shape[i] = pointsPerAxis;
    }

    int totalPoints = (int)Math.Pow(pointsPerAxis, _dims);
    double[] evalResults = new double[totalPoints];

    // Evaluate over cartesian grid
    for(int flatIdx = 0; flatIdx < totalPoints; flatIdx++)
    {
      var currentParams = new Dictionary<string, double>();
      int temp = flatIdx;

      for(int dim = _dims - 1; dim >= 0; dim--)
      {
        int axisIdx = temp % pointsPerAxis;
        temp /= pointsPerAxis;
        currentParams[_paramNames[dim]] = axisPoints[dim][axisIdx];
      }

      evalResults[flatIdx] = Evaluate(currentParams);
    }

    _plottingGrid = new PlottingGridData
    {
      AxisPoints = axisPoints,
      Shape = shape,
      EvaluatedValues = evalResults
    };

    return _plottingGrid;
  }

  private static double NextUniform(Random rng, double min, double max)
  {
    return rng.NextDouble() * (max - min) + min;
  }

  private static double[] LinSpace(double start, double stop, int num)
  {
    double[] result = new double[num];
    double step = (stop - start) / (num - 1);
    for(int i = 0; i < num; i++)
    {
      result[i] = start + i * step;
    }
    return result;
  }
}

/// <summary>
/// Pure C# implementation of Improved Perlin Noise (matches Python `noise` module behavior).
/// </summary>
public static class PerlinNoise
{
  private static readonly int[] Permutation = {
            151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,
            8,99,37,240,21,10,23,190,6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,
            35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,74,165,71,
            134,139,48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,
            55,46,245,40,244,102,143,54,65,25,63,161,1,216,80,73,209,76,132,187,208,89,
            18,169,200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,64,52,217,226,
            250,124,123,5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,
            189,28,42,223,183,170,213,119,248,152,2,44,154,163,70,221,153,101,155,167,43,
            172,9,129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,218,246,97,
            228,251,34,242,193,238,210,144,12,191,179,162,241,81,51,145,235,249,14,239,
            107,49,192,214,31,181,199,106,157,184,84,204,176,115,121,50,45,127,4,150,254,
            138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
        };

  private static readonly int[] P = new int[512];

  static PerlinNoise()
  {
    for(int i = 0; i < 256; i++)
    {
      P[i] = Permutation[i];
      P[256 + i] = Permutation[i];
    }
  }

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
