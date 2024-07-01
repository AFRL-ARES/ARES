namespace DemoDeviceSim;

public class PillarRobot
{
  public double CurrentPillarSize { get; private set; }

  public double CurrentPillarTemp { get; private set; } = 21;

  public int CurrentPillarIndex { get; private set; } = 1;

  public void NextPillar()
  {
    CurrentPillarIndex++;
  }

  public async Task SetTemperature(double temperature)
  {
    CurrentPillarTemp = temperature;
    await Task.Delay(TimeSpan.FromMilliseconds(200));
    Console.WriteLine($"Temperature on pillar {CurrentPillarIndex} has been set to {CurrentPillarTemp} C°.");
    await Task.Delay(TimeSpan.FromSeconds(1));
    CurrentPillarSize = temperature * 0.75;
    Console.WriteLine($"Pillar {CurrentPillarIndex} grew to {CurrentPillarSize} mm");
  }
}
