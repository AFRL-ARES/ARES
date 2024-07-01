using HerkulexDRS.DataModel;
using HerkulexDRS.Responses;

namespace HerkulexDRS.Extensions;
public static class GetPositionResponseExtensions
{
  public static Data ToProto(this GetPositionResponse response)
  {
    var data = new Data
    {
      Position = response.Position,
    };

    return data;
  }
}
