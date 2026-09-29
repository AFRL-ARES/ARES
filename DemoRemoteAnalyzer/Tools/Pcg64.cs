using System.Numerics;

namespace DemoRemoteAnalyzer.Tools;

public class Pcg64
{
  // Canonical 128-bit multiplier used by the reference PCG64 algorithm
  private static readonly BigInteger Multiplier = BigInteger.Parse("47026247687942121848144207491837523525");
  private static readonly BigInteger Mask128 = (BigInteger.One << 128) - 1;

  private BigInteger _state;
  private BigInteger _inc;

  public Pcg64(ulong seed, ulong streamId = 1442695040888963407UL)
  {
    // Initialize state and increment (must be odd)
    _inc = (new BigInteger(streamId) << 1) | 1;
    _state = 0;
    NextUInt64();
    _state = (_state + seed) & Mask128;
    NextUInt64();
  }

  private static ulong RejectionThreshold(ulong range)
  {
    return unchecked((0UL - range) % range);
  }

  public ulong NextUInt64()
  {
    // Save current state to generate output
    BigInteger oldState = _state;

    // Advance internal 128-bit Linear Congruential Generator (LCG)
    _state = (oldState * Multiplier + _inc) & Mask128;

    // XSL-RR 128/64 permutation: Xorshift-Low then Random-Rotate
    ulong xorFolded = (ulong)(((oldState >> 64) ^ oldState) & 0xFFFFFFFFFFFFFFFF);
    int rot = (int)(oldState >> 122);

    return RotateRight64(xorFolded, rot);
  }

  /// <summary>
  /// Returns a random 64-bit unsigned integer within the specified range.
  /// </summary>
  /// <param name="min">The inclusive lower bound.</param>
  /// <param name="max">The exclusive upper bound.</param>
  public ulong NextUInt64(ulong min, ulong max)
  {
    if(min >= max)
    {
      throw new ArgumentOutOfRangeException(
          nameof(min),
          "The minimum bound must be strictly less than the maximum bound.");
    }

    ulong range = max - min;
    ulong threshold = RejectionThreshold(range);

    ulong result;
    do
    {
      result = NextUInt64();
    }
    while(result < threshold);

    return min + (result % range);
  }

  /// <summary>
  /// Returns a random 32-bit signed integer within the specified range.
  /// </summary>
  /// <param name="min">The inclusive lower bound.</param>
  /// <param name="max">The exclusive upper bound.</param>
  public int Next(int min, int max)
  {
    if(min >= max)
    {
      throw new ArgumentOutOfRangeException(
          nameof(min),
          "The minimum bound must be strictly less than the maximum bound.");
    }

    ulong range = (ulong)((long)max - min);
    ulong threshold = RejectionThreshold(range);

    ulong result;
    do
    {
      result = NextUInt64();
    }
    while(result < threshold);

    return checked((int)((long)min + (long)(result % range)));
  }

  /// <summary>
  /// Returns a random floating-point number that is greater than or equal to 0.0, 
  /// and less than 1.0.
  /// </summary>
  public double NextDouble()
  {
    ulong eligibleBits = NextUInt64() >> 11;
    return eligibleBits / 9007199254740992.0;
  }

  /// <summary>
  /// Returns a random floating-point number within the specified range.
  /// </summary>
  /// <param name="min">The inclusive lower bound.</param>
  /// <param name="max">The exclusive upper bound.</param>
  public double NextDouble(double min, double max)
  {
    if(min >= max)
    {
      throw new ArgumentOutOfRangeException(nameof(min), "The minimum bound must be strictly less than the maximum bound.");
    }

    return min + (NextDouble() * (max - min));
  }

  private static ulong RotateRight64(ulong value, int count)
  {
    count &= 63;

    if(count == 0)
    {
      return value;
    }

    return (value >> count) | (value << (64 - count));
  }
}
