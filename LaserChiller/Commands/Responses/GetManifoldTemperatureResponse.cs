namespace LaserChiller.Commands.Responses;

public class GetManifoldTemperatureResponse : CommandResponse
{
  public GetManifoldTemperatureResponse(double temperature) : base()
  {
    Temperature = temperature;
  }

  public double Temperature { get; set; }
}
