namespace DemoDevice;

public class SimulatedPillar
{
  // private int[] _currentSpectrum;

  private bool _started;
  private double _accumulatedEnergy;
  private double _energyExchangeRate = 0.2;
  private double _baseTemp = 21.85;
  private double _laserPower;

  public SimulatedPillar() { Start(); }


  private void Start()
  {
    if (_started)
      return;
    _started = true;
    Task.Run(EnergyExchange);
  }

  private void EnergyExchange()
  {
    while (true)
    {
      var powerInMw = LaserPower * 500;
      _accumulatedEnergy += powerInMw;
      _accumulatedEnergy -= _accumulatedEnergy * 0.1 * _energyExchangeRate;
      Thread.Sleep(10);
    }
  }

  public double LaserPower { get => _laserPower; set => _laserPower = value < 0 ? 0 : value; }

  public double CurrentTemperature => _baseTemp + _accumulatedEnergy / 10;
}