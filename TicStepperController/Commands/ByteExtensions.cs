namespace TicStepperController.Commands;

internal static class ByteExtensions
{
  public static int ToInt32(this byte[] value)
  {
    if (value.Length != 4)
      throw new ArgumentOutOfRangeException(nameof(value), "Byte array must be of size 4 to convert to Int32");
    int intVal = 0;
    foreach (byte b in value.Reverse())
    {
      intVal <<= 8;
      intVal += b;
    }

    return intVal;
  }

  public static int ToInt16(this byte[] value)
  {
    if (value.Length != 2)
      throw new ArgumentOutOfRangeException(nameof(value), "Byte array must be of size 2 to convert to Int16");
    int intVal = 0;
    foreach (byte b in value.Reverse())
    {
      intVal <<= 8;
      intVal += b;
    }

    return intVal;
  }

  public static byte[] ToByteArray(this int target)
  {
    var msb = (byte)(((target >> 7) & 1) | ((target >> 14) & 2) | ((target >> 21) & 4) | ((target >> 28) & 8));
    var ans = new byte[]
    {
      msb,
      (byte)(target >> 0 & 0x7F),
      (byte)(target >> 8 & 0x7F),
      (byte)(target >> 16 & 0x7F),
      (byte)(target >> 24 & 0x7F)
    };
    return ans;
  }

  public static int FromPololuByteArray(this byte[] value)
  {
    if (value.Length != 5)
      throw new ArgumentOutOfRangeException(nameof(value), "Pololu byte array must be of size 5 to convert to Int32");

    var msb = value[0];
    int target = 0;

    target |= (value[1] | ((msb & 1) << 7)) << 0;
    target |= (value[2] | (((msb >> 1) & 1) << 7)) << 8;
    target |= (value[3] | (((msb >> 2) & 1) << 7)) << 16;
    target |= (value[4] | (((msb >> 3) & 1) << 7)) << 24;

    return target;
  }
}