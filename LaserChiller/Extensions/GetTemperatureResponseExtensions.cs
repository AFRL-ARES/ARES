using Chiller.Services;
using LaserChiller.Commands.Responses;

namespace LaserChiller.Extensions;

public static class GetTemperatureResponseExtensions
{
  public static ManifoldTemperatureResponse ToProto(this GetManifoldTemperatureResponse response)
  {
    var data = new ManifoldTemperatureResponse
    {
      ManifoldTemperature = response.Temperature
    };

    return data;
  }
}
