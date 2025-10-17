using Ares.Device.Serial.Commands;
using LindbergFurnace.Commands.Responses;

namespace LindbergFurnace.Commands.Requests
{
  public abstract class CommandExpectingResponse<T> : SerialCommandWithResponse<T> where T : CommandResponse
  {
    protected CommandExpectingResponse(int address, FunctionCode functionCode, byte[] functionData, SerialResponseParser<T> parser) : base(parser)
    {
      Address = address;
      FunctionCode = functionCode;
      FunctionData = functionData;
    }
    
    protected override byte[] Serialize()
    {
      var messageData = new List<byte>
      {
        (byte)Address,
        (byte)FunctionCode
      };
      messageData.AddRange(FunctionData);
      var lrc = TubeFurnaceCommandHelper.Lrc(messageData.ToArray());
      messageData.Add(lrc);

      var messageStr = string.Join(string.Empty, messageData.Select(b => $"{b:X2}"));
      messageStr = $":{messageStr}\r\n";
      var serialData = messageStr.Select(chr => (byte)chr).ToArray();
      return serialData;
    }

    public int Address { get; }
    public FunctionCode FunctionCode { get; }
    public byte[] FunctionData { get; }
  }
}