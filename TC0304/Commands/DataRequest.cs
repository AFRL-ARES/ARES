using System.Text;
using Ares.Device.Serial.Commands;

namespace TC0304.Commands;

internal class DataRequest : SerialCommandWithResponse<DataResponse>
{
  public DataRequest() : base(new DataResponseParser())
  {
  }

  protected override byte[] Serialize()
    => Encoding.ASCII.GetBytes("A");
}
