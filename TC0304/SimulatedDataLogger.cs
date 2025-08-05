using System.Text;

namespace TC0304;

internal class SimulatedDataLogger
{
  private readonly Action<byte[]> _byteSender;
  private bool _celsius = true;
  private bool _hold;
  private int _probe1Temperature = 300;
  private int _probe2Temperature = 300;

  public SimulatedDataLogger(Action<byte[]> byteSender)
  {
    _byteSender = byteSender;
    StartTempUpdater();
  }

  private void StartTempUpdater()
  {
    var random = new Random();
    Task.Factory.StartNew(() => {
      Thread.CurrentThread.Name = "Sim Datalogger Temperture Randomizer Thread";
        while (true)
        {
          _probe1Temperature = random.Next(100, 900);
          _probe2Temperature = random.Next(100, 900);
          Task.Delay(500).Wait();
        }
      },
      TaskCreationOptions.LongRunning);
  }

  public void SendCommand(byte[] command)
  {
    var random = new Random();
    Task.Delay(random.Next(100, 300)).ContinueWith(_ => {
      var cmd = Encoding.ASCII.GetString(command);
      ProcessCommand(cmd);
    });
  }

  private void ProcessCommand(string command)
  {
    if (command.StartsWith('A'))
      SendData();
    else if (command.StartsWith('H'))
      _hold = !_hold;
    else if (command.StartsWith('C'))
      _celsius = !_celsius;
  }

  private void SendData()
  {
    var data = new byte[45];
    Array.Fill<byte>(data, 0b_0);
    data[0] = 2;
    data[44] = 3;
    if (_hold)
      data[1] += 0b_0010_0000;

    if (_celsius)
      data[1] += 0b_1000_0000;

    data[7] = (byte)(_probe1Temperature >> 8);
    data[8] = (byte)_probe1Temperature;
    data[9] = (byte)(_probe2Temperature >> 8);
    data[10] = (byte)_probe2Temperature;
    data[11] = 0x_7F;
    data[12] = 0x_FF;
    data[13] = 0x_7F;
    data[14] = 0x_FF;

    _byteSender(data);
  }
}
