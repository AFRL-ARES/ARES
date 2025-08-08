using System.Globalization;
using System.Text;

namespace LindbergFurnace.Commands.Responses.Parsers
{
  internal class WriteMultipleRegistersResponseParser : ResponseParser<WriteMultipleRegistersResponse>
  {
    public WriteMultipleRegistersResponseParser(int address)
    {
      Address = (byte)address;
    }

    protected override bool TryParseResponse(byte[] data, out WriteMultipleRegistersResponse? response)
    {
      var str = Encoding.UTF8.GetString(data);
      if (str.StartsWith(':'))
        data = data[1..];

      var dataStream = new MemoryStream(data);
      var dataReader = new BinaryReader(dataStream);

      if (data.Length < 2)
      {
        response = null;
        return false;
      }
      var addressAsciiHex = dataReader.ReadChars(2);
      if (!int.TryParse(addressAsciiHex, NumberStyles.HexNumber, null, out var address))
      {
        response = null;
        return false;
      }
      if (address != Address)
      {
        response = null;
        return false;
      }

      var functionCodeAsciiHex = dataReader.ReadChars(2);
      var functionCode = (FunctionCode)int.Parse(functionCodeAsciiHex, NumberStyles.HexNumber);
      if(functionCode != FunctionCode.WriteMultiple)
      {
        response = null;
        return false;
      }

      var registerStartUpperAsciiHex = dataReader.ReadChars(2);
      var registerStartUpper = int.Parse(registerStartUpperAsciiHex, NumberStyles.HexNumber);
      var registerStartLowerAsciiHex = dataReader.ReadChars(2);
      var registerStartLower= int.Parse(registerStartLowerAsciiHex, NumberStyles.HexNumber);
      var registerStartNumber = (registerStartUpper << 8) | (registerStartLower);
      var registerStart = (Register)registerStartNumber;

      var numRegistersAsciiHex = dataReader.ReadChars(2);
      var numRegisters = int.Parse(numRegistersAsciiHex, NumberStyles.HexNumber);
      var writtenRegisters =
        Enumerable.Range(1, numRegisters + 1)
        .Select(index => (Register)(registerStart + (index - 1)))
        .ToArray();

      response = new WriteMultipleRegistersResponse(address, functionCode, writtenRegisters );
      return true;
    }

    public byte Address { get; }
  }
}
