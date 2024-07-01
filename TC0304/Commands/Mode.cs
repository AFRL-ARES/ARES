namespace TC0304.Commands;

public enum Mode
{
  Normal,
  Maximum,
  Minimum,
  MaxMin
}
internal static class ModeExtensions
{
  public static Mode FromInt(int i)
  {
    return i switch
    {
      0 => Mode.Normal,
      1 => Mode.Maximum,
      2 => Mode.Minimum,
      3 => Mode.MaxMin,
      _ => throw new ArgumentOutOfRangeException(nameof(Mode))
    };
  }
}
