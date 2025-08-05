namespace DemoDeviceSim;

public class PillarRobot
{
  public double CurrentPillarSize { get; private set; }

  public double CurrentPillarTemp { get; private set; } = 21;

  public int CurrentPillarIndex { get; private set; } = 1;

  public async Task NextPillar()
  {
    CurrentPillarIndex++;
    await Task.Delay(TimeSpan.FromSeconds(3));
  }

  public async Task SetTemperature(double temperature)
  {
    CurrentPillarTemp = temperature;
    Console.WriteLine($"Temperature on pillar {CurrentPillarIndex} has been set to {CurrentPillarTemp} C°.");
    //CurrentPillarSize = temperature * 0.75;
    CurrentPillarSize = 500 / (1 + (0.01 * Math.Pow((temperature - 1035), 2)));
    Console.WriteLine($"Pillar {CurrentPillarIndex} grew to {CurrentPillarSize} mm");
    await Task.Delay(TimeSpan.FromMilliseconds(3000));
  }
}
