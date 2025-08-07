namespace VerdiV6Laser.Commands.Responses
{
  internal class LaserPowerResponse : CommandResponse
  {
    public LaserPowerResponse(double power) : base()
    {
      Power = power;
    }

    public double Power { get; set; }
  }
}
