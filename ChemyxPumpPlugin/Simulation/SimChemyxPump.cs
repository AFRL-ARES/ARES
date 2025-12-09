using System;
using System.Collections.Generic;
using System.Text;

namespace ChemyxPumpPlugin.Simulation;

public class SimChemyxPump
{
  private readonly Action<byte[]> _byteSender;

  public SimChemyxPump(Action<byte[]> byteSender)
  {
    _byteSender = byteSender;
    StartInternalStateUpdater();
  }

  private void StartInternalStateUpdater()
  {
    var random = new Random();
    Task.Factory.StartNew(() => {
      while(true)
      {
        Task.Delay(500).Wait();
      }
    },TaskCreationOptions.LongRunning);
  }

  public void SendCommand(byte[] command)
  {
    var random = new Random();
    Task.Delay(random.Next(50, 100)).ContinueWith(_ => {
      var cmd = Encoding.ASCII.GetString(command);
      ProcessCommand(cmd);
    });
  }

  private void ProcessCommand(string command)
  {
    if(command.StartsWith('A'))
      SendData();
  }

  private void SendData()
  {
    var data = new byte[45];
    _byteSender(data);
  }
}
