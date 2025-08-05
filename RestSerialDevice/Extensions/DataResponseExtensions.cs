using GenericSerialDevice.Commands.Responses;
using RestSerialDevice.DataModel;
using RestSerialDevice.Services;

namespace RestSerialDevice.Extensions;

public static class DataResponseExtensions
{
  public static Data ToProto(this ReadDataResponse response)
  {
    var data = new Data();
    data.Values.Add(response.Values);
    return data;
  }

  public static DataResponse ToInternal(this Data data)
  {
    var response = new DataResponse();
    response.Data = data;
    return response;
  }
}
