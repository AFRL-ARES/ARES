using System.Globalization;
using System.Text;

namespace LindbergFurnace.Commands.Responses.Parsers
{
  internal class ReadMultipleReigstersResponseParser : ResponseParser<ReadMultipleRegistersResponse>
  {
    public ReadMultipleReigstersResponseParser(int address)
    {
      Address = address;
    }
    protected override bool TryParseResponse(byte[] data, out ReadMultipleRegistersResponse? response)
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
      if(address != Address)
      {
        response = null;
        return false;
      }


      var functionCodeAsciiHex = dataReader.ReadChars(2);
      var functionCode = (FunctionCode)int.Parse(functionCodeAsciiHex, NumberStyles.HexNumber);

      if(functionCode != FunctionCode.ReadMultiple)
      {
        response = null;
        return false;
      }

      var byteCountAsciiHex = dataReader.ReadChars(2);
      var byteCount = int.Parse(byteCountAsciiHex, NumberStyles.HexNumber);
      var charCount = byteCount * 2; // TODO: Verify we need to multiply by 2. It seems like the documentation mixes numbers as characters vs bytes. "08" is 2 bytes (not 0008) yet "0000 0001 0001 0000" is 08 bytes?
      var registerContents = new byte[charCount / 4][];
      for (int i = 0; i < registerContents.Length; i++)
      {
        var registerData = dataReader.ReadBytes(4);
        registerContents[i] = registerData;
      }

      response = new ReadMultipleRegistersResponse(address, functionCode, byteCount, registerContents);
      return true;
    }

    public int Address { get; }
  }
}