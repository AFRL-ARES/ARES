namespace TicStepperController.Commands;
internal static class ByteExtensions
{
  public static int ToInt32(this byte[] value)
  {
    if (value.Length != 4)
      throw new ArgumentOutOfRangeException("Byte array must be of size 4 to convert to Int32");
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
      throw new ArgumentOutOfRangeException("Byte array must be of size 2 to convert to Int16");
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
}
