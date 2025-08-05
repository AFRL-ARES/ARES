using Ares.SyringePump.Ne1000.Messaging;
using SyringePumpNE1000.Commands.Responses;
using System.Diagnostics;
using System.Text;

namespace SyringePumpNE1000.Simulation;

public class SimSyringePump
{
  private readonly Action<byte[]> _byteSender;
  private readonly byte[] _versionResponseBytes = Encoding.UTF8.GetBytes("NE1000V3.929-SIM");
  private readonly byte[] _getDiameterResponseBytes = Encoding.UTF8.GetBytes("29.20");
  private readonly byte[] _phaseResponseBytes = Encoding.UTF8.GetBytes("01");
  private readonly byte[] _volumeDispensedResponseBytes = Encoding.UTF8.GetBytes("I0.974W0.959ML");
  private readonly byte[] _phaseFunctionRateRespopnseBytes = Encoding.UTF8.GetBytes("10.00MM");
  private readonly byte[] _phaseFunctionResponseBytes = Encoding.UTF8.GetBytes("RAT");
  private readonly byte[] _phaseFunctionDirectionResponseBytes = Encoding.UTF8.GetBytes("INF");
  private readonly byte[] _phaseFunctionVolumeResponseBytes = Encoding.UTF8.GetBytes("5.000ML");

  public SimSyringePump(Action<byte[]> byteSender, string deviceName)
  {
    _byteSender = byteSender;
    Name = deviceName;
  }

  public void SendCommand(byte[] command)
  {
    var cmd = Encoding.ASCII.GetString(command);
    var response = new Response(5000, StatusPrompt.PromptS);

    if(cmd.Contains("DIA"))
      _byteSender(_getDiameterResponseBytes);

    else if(cmd.Contains("VER"))
      _byteSender(_versionResponseBytes);


    else if(cmd.Contains("STP"))
    {
      Debug.WriteLine($"Received Stop Command for {Name}!");
    }
  }

  public string Name { get; set; }

  public int Address { get; set; }
}
