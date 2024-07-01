namespace HerkulexDRS.Commands;
internal class ChecksumCalculater
{
  public byte CalculateChecksumOne(byte[] commandBytes)
  {
    //Command that contains no additional data
    if (commandBytes.Length == 5)
    {
      return (byte)(7 ^ commandBytes[3] ^ commandBytes[4] & 0xFE);
    }

    var checksumOne = commandBytes.Length ^ commandBytes[3] ^ commandBytes[4];

    //Calculation using additional command data
    foreach (var data in commandBytes[5..])
    {
      checksumOne = checksumOne ^ data;
    }

    return (byte)(checksumOne & 0xFE);
  }

  public byte CalculateChecksumTwo(byte checksumOne)
  {
    return (byte)(checksumOne & 0xFE);
  }
}
